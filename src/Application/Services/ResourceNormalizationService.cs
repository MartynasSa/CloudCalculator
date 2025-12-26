using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Ports;

namespace Application.Services;

public interface IResourceNormalizationService
{
    Task<CategorizedResourcesDto> GetResourcesAsync(IReadOnlyCollection<ResourceCategory> neededResources, UsageSize usage, CancellationToken cancellationToken = default);
}

public class ResourceNormalizationService(ICloudPricingRepository cloudPricingRepository) : IResourceNormalizationService
{
    // Map product families and services to categories based on file structure
    // The file structure is: Data/{Category}/{SubCategory}/{provider}.json
    // For example: Data/Compute/VM/aws.json -> Category: Compute, SubCategory: VirtualMachines
    
    private static readonly Dictionary<(string ProductFamily, string Service), (ResourceCategory Category, ResourceSubCategory SubCategory)> AwsProductFamilyServiceMap =
        new()
        {
            // Compute
            { ("Compute Instance", "AmazonEC2"), (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines) },
            { ("Serverless", "AWSLambda"), (ResourceCategory.Compute, ResourceSubCategory.CloudFunctions) },
            { ("Compute", "AmazonEKS"), (ResourceCategory.Compute, ResourceSubCategory.Kubernetes) },
            
            // Databases
            { ("Database Instance", "AmazonRDS"), (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases) },
            { ("", "AmazonDynamoDB"), (ResourceCategory.Databases, ResourceSubCategory.NoSQL) },
            
            // Network
            { ("Load Balancer", "AmazonEC2"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Load Balancer", "AWSELB"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Amazon API Gateway Cache", "AmazonApiGateway"), (ResourceCategory.Networking, ResourceSubCategory.ApiGateway) },
            
            // Storage
            { ("", "AmazonS3"), (ResourceCategory.Storage, ResourceSubCategory.BlobStorage) },
        };

    // Azure-specific mapping based on productFamily and service combination
    private static readonly Dictionary<(string ProductFamily, string Service), (ResourceCategory Category, ResourceSubCategory SubCategory)> AzureProductFamilyServiceMap =
        new()
        {
            // Compute
            { ("Compute", "Virtual Machines"), (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines) },
            { ("Serverless", "Azure Functions"), (ResourceCategory.Compute, ResourceSubCategory.CloudFunctions) },
            { ("Compute", "Azure Kubernetes Service"), (ResourceCategory.Compute, ResourceSubCategory.Kubernetes) },
            
            // Databases
            { ("Databases", "SQL Database"), (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases) },
            { ("Databases", "Azure Cosmos DB"), (ResourceCategory.Databases, ResourceSubCategory.NoSQL) },
            
            // Network
            { ("Networking", "Load Balancer"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Networking", "Azure API Management"), (ResourceCategory.Networking, ResourceSubCategory.ApiGateway) },
            
            // Storage
            { ("Storage", "Azure Blob Storage"), (ResourceCategory.Storage, ResourceSubCategory.BlobStorage) },
        };

    // GCP-specific mapping based on productFamily and service combination
    private static readonly Dictionary<(string ProductFamily, string Service), (ResourceCategory Category, ResourceSubCategory SubCategory)> GcpProductFamilyServiceMap =
        new()
        {
            // Compute
            { ("Compute", "Compute Engine"), (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines) },
            { ("Serverless", "Cloud Functions"), (ResourceCategory.Compute, ResourceSubCategory.CloudFunctions) },
            { ("Compute", "Google Kubernetes Engine"), (ResourceCategory.Compute, ResourceSubCategory.Kubernetes) },
            
            // Databases
            { ("ApplicationServices", "Cloud SQL"), (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases) },
            { ("Storage", "Firebase Realtime Database"), (ResourceCategory.Databases, ResourceSubCategory.NoSQL) },
            
            // Network
            { ("Networking", "Cloud Load Balancing"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Networking", "API Gateway"), (ResourceCategory.Networking, ResourceSubCategory.ApiGateway) },
            
            // Storage
            { ("Storage", "Cloud Storage"), (ResourceCategory.Storage, ResourceSubCategory.BlobStorage) },
        };

    public async Task<CategorizedResourcesDto> GetResourcesAsync(
        IReadOnlyCollection<ResourceCategory> neededResources,
        UsageSize usage,
        CancellationToken cancellationToken = default)
    {
        var data = await cloudPricingRepository.GetAllAsync(cancellationToken);

        var categories = new Dictionary<ResourceCategory, CategoryResourcesDto>();

        foreach (var product in data.Data.Products)
        {
            var (category, subCategory) = product.VendorName switch
            {
                CloudProvider.AWS => MapAwsProductFamilyToCategoryAndSubCategory(product.ProductFamily, product.Service),
                CloudProvider.Azure => MapAzureProductFamilyToCategoryAndSubCategory(product.ProductFamily, product.Service),
                CloudProvider.GCP => MapGcpProductFamilyToCategoryAndSubCategory(product.ProductFamily, product.Service),
                _ => (ResourceCategory.Other, ResourceSubCategory.Uncategorized)
            };

            // Skip if not in needed resources
            if (!neededResources.Contains(category))
                continue;

            // Ensure category exists in dictionary
            if (!categories.ContainsKey(category))
            {
                categories[category] = new CategoryResourcesDto { Category = category };
            }

            var categoryDto = categories[category];

            // Process different resource types
            if (category == ResourceCategory.Compute && subCategory == ResourceSubCategory.VirtualMachines)
            {
                categoryDto.ComputeInstances.Add(MapToComputeInstance(product));
            }
            else if (category == ResourceCategory.Databases)
            {
                categoryDto.Databases.Add(MapToDatabase(product));
            }
            else if (category == ResourceCategory.Networking && subCategory == ResourceSubCategory.LoadBalancer)
            {
                // Load balancers are handled separately with fixed pricing
            }
            else if (category == ResourceCategory.Management && subCategory == ResourceSubCategory.Monitoring)
            {
                // Monitoring is handled separately with GetNormalizedMonitoring
            }
            else
            {
                // Generic resource
                categoryDto.Networking.Add(MapToNormalizedResource(product, category, subCategory));
            }
        }

        // Add load balancers for networking category
        if (neededResources.Contains(ResourceCategory.Networking))
        {
            if (!categories.ContainsKey(ResourceCategory.Networking))
            {
                categories[ResourceCategory.Networking] = new CategoryResourcesDto { Category = ResourceCategory.Networking };
            }
            
            categories[ResourceCategory.Networking].LoadBalancers.AddRange(GetNormalizedLoadBalancers(usage));
        }

        // Add monitoring for management category
        if (neededResources.Contains(ResourceCategory.Management))
        {
            if (!categories.ContainsKey(ResourceCategory.Management))
            {
                categories[ResourceCategory.Management] = new CategoryResourcesDto { Category = ResourceCategory.Management };
            }
            
            categories[ResourceCategory.Management].Monitoring.AddRange(GetNormalizedMonitoring(usage));
        }

        return new CategorizedResourcesDto { Categories = categories };
    }

    private static (ResourceCategory Category, ResourceSubCategory SubCategory) MapAwsProductFamilyToCategoryAndSubCategory(string productFamily, string service)
    {
        var key = (productFamily ?? string.Empty, service ?? string.Empty);
        if (AwsProductFamilyServiceMap.TryGetValue(key, out var mapping))
        {
            return mapping;
        }
        return (ResourceCategory.Other, ResourceSubCategory.Uncategorized);
    }

    private static (ResourceCategory Category, ResourceSubCategory SubCategory) MapAzureProductFamilyToCategoryAndSubCategory(string productFamily, string service)
    {
        var key = (productFamily ?? string.Empty, service ?? string.Empty);
        if (AzureProductFamilyServiceMap.TryGetValue(key, out var mapping))
        {
            return mapping;
        }
        return (ResourceCategory.Other, ResourceSubCategory.Uncategorized);
    }

    private static (ResourceCategory Category, ResourceSubCategory SubCategory) MapGcpProductFamilyToCategoryAndSubCategory(string productFamily, string service)
    {
        var key = (productFamily ?? string.Empty, service ?? string.Empty);
        if (GcpProductFamilyServiceMap.TryGetValue(key, out var mapping))
        {
            return mapping;
        }
        return (ResourceCategory.Other, ResourceSubCategory.Uncategorized);
    }

    private static NormalizedComputeInstanceDto MapToComputeInstance(CloudPricingProductDto product)
    {
        var instanceName = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "vmSize")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "armSkuName")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "machineType")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value;
        
        // Fallback to "Unknown" if still empty
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            instanceName = "Unknown";
        }
        
        var vcpuStr = product.Attributes.FirstOrDefault(a => a.Key == "vcpu")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "numberOfCores")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "vCpusAvailable")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "vCPUs")?.Value;
        
        var memory = product.Attributes.FirstOrDefault(a => a.Key == "memory")?.Value
                    ?? product.Attributes.FirstOrDefault(a => a.Key == "memoryInGB")?.Value
                    ?? product.Attributes.FirstOrDefault(a => a.Key == "memoryGb")?.Value;

        return new NormalizedComputeInstanceDto
        {
            Cloud = product.VendorName,
            InstanceName = instanceName,
            Region = product.Region,
            VCpu = int.TryParse(vcpuStr, out var cpu) ? cpu : null,
            Memory = !string.IsNullOrWhiteSpace(memory) ? memory : null,
            PricePerHour = GetPricePerHour(product)
        };
    }

    private static NormalizedDatabaseDto MapToDatabase(CloudPricingProductDto product)
    {
        var instanceName = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "databaseEngine")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "armSkuName")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "machineType")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value;
        
        // Fallback to "Unknown" if still empty
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            instanceName = "Unknown";
        }
        
        var vcpuStr = product.Attributes.FirstOrDefault(a => a.Key == "vcpu")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "numberOfCores")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "vCpusAvailable")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "vCPUs")?.Value;
        
        var memory = product.Attributes.FirstOrDefault(a => a.Key == "memory")?.Value
                    ?? product.Attributes.FirstOrDefault(a => a.Key == "memoryInGB")?.Value
                    ?? product.Attributes.FirstOrDefault(a => a.Key == "memoryGb")?.Value;
        
        var databaseEngine = product.Attributes.FirstOrDefault(a => a.Key == "databaseEngine")?.Value
                            ?? product.Attributes.FirstOrDefault(a => a.Key == "engine")?.Value
                            ?? product.Attributes.FirstOrDefault(a => a.Key == "databaseFamily")?.Value;

        return new NormalizedDatabaseDto
        {
            Cloud = product.VendorName,
            InstanceName = instanceName,
            Region = product.Region,
            DatabaseEngine = databaseEngine,
            VCpu = int.TryParse(vcpuStr, out var cpu) ? cpu : null,
            Memory = !string.IsNullOrWhiteSpace(memory) ? memory : null,
            PricePerHour = GetPricePerHour(product)
        };
    }

    private static NormalizedResourceDto MapToNormalizedResource(CloudPricingProductDto product, ResourceCategory category, ResourceSubCategory subCategory)
    {
        var attributes = product.Attributes.ToDictionary(a => a.Key, a => a.Value);
        
        return new NormalizedResourceDto
        {
            Cloud = product.VendorName,
            Service = product.Service,
            Region = product.Region,
            Category = category,
            SubCategory = subCategory,
            ProductFamily = product.ProductFamily,
            ResourceName = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value,
            PricePerHour = GetPricePerHour(product),
            Attributes = attributes
        };
    }

    private static decimal? GetPricePerHour(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault(p => IsOnDemand(p.PurchaseOption))?.Usd
                    ?? product.Prices.FirstOrDefault()?.Usd;
        return price;
    }

    private static bool IsOnDemand(string? purchaseOption)
    {
        if (string.IsNullOrWhiteSpace(purchaseOption))
            return true;
        
        return purchaseOption.Equals("OnDemand", StringComparison.OrdinalIgnoreCase);
    }

    private static List<NormalizedLoadBalancerDto> GetNormalizedLoadBalancers(UsageSize usage)
    {
        var gcpPricing = usage switch
        {
            UsageSize.Small => 18.41m,
            UsageSize.Medium => 55.23m,
            UsageSize.Large => 184.1m,
            _ => 0m
        };

        var azurePricing = usage switch
        {
            UsageSize.Small => 0m,
            UsageSize.Medium => 0m,
            UsageSize.Large => 0m,
            _ => 0m
        };

        var awsPricing = usage switch
        {
            UsageSize.Small => 16.51m,
            UsageSize.Medium => 49.53m,
            UsageSize.Large => 165.1m,
            _ => 0m
        };

        return new List<NormalizedLoadBalancerDto>
        {
            new() { Cloud = CloudProvider.AWS, Name = "Application Load Balancer", PricePerMonth = awsPricing },
            new() { Cloud = CloudProvider.Azure, Name = "Azure Load Balancer", PricePerMonth = azurePricing },
            new() { Cloud = CloudProvider.GCP, Name = "Cloud Load Balancing", PricePerMonth = gcpPricing }
        };
    }

    private static List<NormalizedMonitoringDto> GetNormalizedMonitoring(UsageSize usage)
    {
        var gcpPricing = usage switch
        {
            UsageSize.Small => 4m,
            UsageSize.Medium => 12m,
            UsageSize.Large => 40m,
            _ => 0m
        };

        var azurePricing = usage switch
        {
            UsageSize.Small => 6m,
            UsageSize.Medium => 18m,
            UsageSize.Large => 60m,
            _ => 0m
        };

        var awsPricing = usage switch
        {
            UsageSize.Small => 5m,
            UsageSize.Medium => 15m,
            UsageSize.Large => 50m,
            _ => 0m
        };

        return new List<NormalizedMonitoringDto>
        {
            new() { Cloud = CloudProvider.GCP,  Name = "Cloud Ops",     PricePerMonth = gcpPricing },
            new() { Cloud = CloudProvider.Azure, Name = "Azure Monitor", PricePerMonth = azurePricing },
            new() { Cloud = CloudProvider.AWS,  Name = "CloudWatch",    PricePerMonth = awsPricing }
        };
    }
}
