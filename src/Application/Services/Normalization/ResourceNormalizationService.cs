using Application.Models.Dtos;
using Application.Models.Enums;

namespace Application.Services.Normalization;

public interface IResourceNormalizationService
{
    Task<CategorizedResourcesDto> GetResourcesAsync(CancellationToken cancellationToken = default);
}

public class ResourceNormalizationService(ICloudPricingRepositoryProvider cloudPricingRepository) : IResourceNormalizationService
{
    
    private static readonly Dictionary<(string ProductFamily, string Service), (ResourceCategory Category, ResourceSubCategory SubCategory)> AwsProductFamilyServiceMap =
        new()
        {
            // Compute
            { ("Compute Instance", "AmazonEC2"), (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines) },
            { ("Serverless", "AWSLambda"), (ResourceCategory.Compute, ResourceSubCategory.CloudFunctions) },
            { ("Compute", "AmazonEKS"), (ResourceCategory.Compute, ResourceSubCategory.Kubernetes) },
            
            // Databases
            { ("Database Instance", "AmazonRDS"), (ResourceCategory.Database, ResourceSubCategory.Relational) },
            { ("", "AmazonDynamoDB"), (ResourceCategory.Database, ResourceSubCategory.NoSQL) },
            
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
            { ("Databases", "SQL Database"), (ResourceCategory.Database, ResourceSubCategory.Relational) },
            { ("Databases", "Azure Cosmos DB"), (ResourceCategory.Database, ResourceSubCategory.NoSQL) },
            
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
            { ("ApplicationServices", "Cloud SQL"), (ResourceCategory.Database, ResourceSubCategory.Relational) },
            { ("Storage", "Firebase Realtime Database"), (ResourceCategory.Database, ResourceSubCategory.NoSQL) },
            
            // Network
            { ("Networking", "Cloud Load Balancing"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Networking", "API Gateway"), (ResourceCategory.Networking, ResourceSubCategory.ApiGateway) },
            
            // Storage
            { ("Storage", "Cloud Storage"), (ResourceCategory.Storage, ResourceSubCategory.BlobStorage) },
        };

    public async Task<CategorizedResourcesDto> GetResourcesAsync(CancellationToken cancellationToken = default)
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

            // Ensure category exists in dictionary
            if (!categories.ContainsKey(category))
            {
                categories[category] = new CategoryResourcesDto { Category = category };
            }

            var categoryDto = categories[category];

            // Process different resource types
            switch ((category, subCategory))
            {
                case (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines):
                    categoryDto.ComputeInstances.Add(MapToComputeInstance(product));
                    break;
                case (ResourceCategory.Database, _):
                    categoryDto.Databases.Add(MapToDatabase(product));
                    break;
                case (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer):
                    // Load balancers are handled separately with GetNormalizedLoadBalancers
                    break;
                case (ResourceCategory.Management, ResourceSubCategory.Monitoring):
                    // Monitoring is handled separately with GetNormalizedMonitoring
                    break;
                default:
                    // Generic resource
                    categoryDto.Networking.Add(MapToNormalizedResource(product, category, subCategory));
                    break;
            }
        }

        categories[ResourceCategory.Networking].LoadBalancers.AddRange(GetNormalizedLoadBalancers());
        categories[ResourceCategory.Management].Monitoring.AddRange(GetNormalizedMonitoring());


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
        return product.Prices.FirstOrDefault()?.Usd;
    }

    private static List<NormalizedLoadBalancerDto> GetNormalizedLoadBalancers()
    {
        return new List<NormalizedLoadBalancerDto>
        {
            new() { Cloud = CloudProvider.AWS,   Name = "Application Load Balancer", PricePerMonth = 16.51m },
            new() { Cloud = CloudProvider.AWS,   Name = "Application Load Balancer", PricePerMonth = 49.53m },
            new() { Cloud = CloudProvider.AWS,   Name = "Application Load Balancer", PricePerMonth = 165.1m },
            new() { Cloud = CloudProvider.Azure, Name = "Azure Load Balancer",       PricePerMonth = 0 },
            new() { Cloud = CloudProvider.Azure, Name = "Azure Load Balancer",       PricePerMonth = 0 },
            new() { Cloud = CloudProvider.Azure, Name = "Azure Load Balancer",       PricePerMonth = 0 },
            new() { Cloud = CloudProvider.GCP,   Name = "Cloud Load Balancing",      PricePerMonth = 18.41m },
            new() { Cloud = CloudProvider.GCP,   Name = "Cloud Load Balancing",      PricePerMonth = 55.23m },
            new() { Cloud = CloudProvider.GCP,   Name = "Cloud Load Balancing",      PricePerMonth = 184.1m }
        };
    }

    private static List<NormalizedMonitoringDto> GetNormalizedMonitoring()
    {
        return new List<NormalizedMonitoringDto>
        {
            new() { Cloud = CloudProvider.GCP,  Name = "Cloud Ops",     PricePerMonth = 4m },
            new() { Cloud = CloudProvider.GCP,  Name = "Cloud Ops",     PricePerMonth = 12m },
            new() { Cloud = CloudProvider.GCP,  Name = "Cloud Ops",     PricePerMonth = 40m },
            new() { Cloud = CloudProvider.Azure, Name = "Azure Monitor", PricePerMonth = 6m },
            new() { Cloud = CloudProvider.Azure, Name = "Azure Monitor", PricePerMonth = 18m },
            new() { Cloud = CloudProvider.Azure, Name = "Azure Monitor", PricePerMonth = 60m },
            new() { Cloud = CloudProvider.AWS,  Name = "CloudWatch",    PricePerMonth = 5m },
            new() { Cloud = CloudProvider.AWS,  Name = "CloudWatch",    PricePerMonth = 15m },
            new() { Cloud = CloudProvider.AWS,  Name = "CloudWatch",    PricePerMonth = 50m }
        };
    }
}
