using Application.Mappers;
using Application.Models.Dtos;
using Application.Models.Enums;
using System.Text.RegularExpressions;

namespace Application.Services.Normalization;

public interface IResourceNormalizationService
{
    Task<CategorizedResourcesDto> GetResourcesAsync(CancellationToken cancellationToken = default);
}

public class ResourceNormalizationService(ICloudPricingRepositoryProvider cloudPricingRepository) : IResourceNormalizationService
{
    // Internal enum for mapping - matches the old ResourceSubCategory values
    private enum ResourceSubCategory
    {
        None = 0,
        Uncategorized = 1,
        
        // Compute (100-199)
        VirtualMachines = 100,
        CloudFunctions = 101,
        Kubernetes = 102,
        ContainerInstances = 103,

        // Database (200-299)
        Relational = 200,
        NoSQL = 202,
        Caching = 204,

        // Storage (300-399)
        ObjectStorage = 300,
        BlobStorage = 301,
        BlockStorage = 302,
        FileStorage = 303,
        Backup = 304,

        // Networking (400-499)
        VpnGateway = 400,
        LoadBalancer = 401,
        ApiGateway = 402,
        Dns = 403,
        CDN = 404,

        // Analytics / AI (500-599)
        DataWarehouse = 500,
        Streaming = 501,
        MachineLearning = 502,

        // Management (600-699)
        Queueing = 600,
        Messaging = 601,
        Secrets = 602,
        Compliance = 603,
        Monitoring = 604,

        // Security (700-799)
        WebApplicationFirewall = 700,
        IdentityManagement = 701,

        // ML (880-899)
        AIServices = 801,
        MLPlatforms = 802,
        IntelligentSearch = 803,
    }
    
    // Regex for extracting GCP machine family from description
    private static readonly Regex GcpMachineFamilyRegex = new(@"^([a-zA-Z][0-9a-zA-Z]*)\s+Instance\s+(Core|Ram)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    // Reference CPU and memory specs for cost comparison (Medium size: 4 vCPU, 8 GB RAM)
    private const int ReferenceCpuCount = 4;
    private const double ReferenceMemoryGb = 8.0;
    
    // VM configuration specs matching PriceProvider.GetVirtualMachineSpecs
    private static readonly (int VCpu, double Memory, string Name)[] VmSizeConfigurations = new[]
    {
        (VCpu: 2, Memory: 4.0, Name: "Small"),
        (VCpu: 4, Memory: 8.0, Name: "Medium"),
        (VCpu: 8, Memory: 16.0, Name: "Large"),
        (VCpu: 16, Memory: 32.0, Name: "ExtraLarge")
    };
    
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
            { ("", "AmazonElastiCache"), (ResourceCategory.Database, ResourceSubCategory.Caching) },
            
            // Network
            { ("Load Balancer", "AmazonEC2"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Load Balancer", "AWSELB"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Amazon API Gateway Cache", "AmazonApiGateway"), (ResourceCategory.Networking, ResourceSubCategory.ApiGateway) },
            { ("", "AmazonCloudFront"), (ResourceCategory.Networking, ResourceSubCategory.CDN) },
            
            // Storage
            { ("", "AmazonS3"), (ResourceCategory.Storage, ResourceSubCategory.BlobStorage) },
            { ("Storage", "AmazonEC2"), (ResourceCategory.Storage, ResourceSubCategory.BlockStorage) },
            
            // Analytics
            { ("", "AmazonRedshift"), (ResourceCategory.Analytics, ResourceSubCategory.DataWarehouse) },
            
            // Messaging
            { ("", "AmazonSNS"), (ResourceCategory.Management, ResourceSubCategory.Messaging) },
            { ("", "AmazonSQS"), (ResourceCategory.Management, ResourceSubCategory.Messaging) },
            { ("API Request", "AWSQueueService"), (ResourceCategory.Management, ResourceSubCategory.Queueing) },
            { ("", "AmazonCloudWatch"), (ResourceCategory.Management, ResourceSubCategory.Monitoring) },
            
            // Security
            { ("Amazon Bedrock", "AmazonCognito"), (ResourceCategory.Security, ResourceSubCategory.IdentityManagement) },
            { ("AWS Firewall", "AWSNetworkFirewall"), (ResourceCategory.Security, ResourceSubCategory.WebApplicationFirewall) },
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
            { ("ApplicationServices", "Cloud Memorystore for Redis"), (ResourceCategory.Database, ResourceSubCategory.Caching) },
            
            // Network
            { ("Networking", "Load Balancer"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Networking", "Azure API Management"), (ResourceCategory.Networking, ResourceSubCategory.ApiGateway) },
            { ("Networking", "Azure CDN"), (ResourceCategory.Networking, ResourceSubCategory.CDN) },
            { ("Networking", "Azure Firewall"), (ResourceCategory.Security, ResourceSubCategory.WebApplicationFirewall) },
            
            // Storage
            { ("Storage", "Azure Blob Storage"), (ResourceCategory.Storage, ResourceSubCategory.BlobStorage) },
            { ("Storage", "Storage"), (ResourceCategory.Storage, ResourceSubCategory.BlockStorage) },
            
            // Analytics
            { ("Analytics", "Azure Synapse Analytics"), (ResourceCategory.Analytics, ResourceSubCategory.DataWarehouse) },

            // Messaging
            { ("Integration", "Service Bus"), (ResourceCategory.Management, ResourceSubCategory.Messaging) },
            { ("Integration", "Event Hubs"), (ResourceCategory.Management, ResourceSubCategory.Messaging) },
            { ("Internet of Things", "Event Grid"), (ResourceCategory.Management, ResourceSubCategory.Queueing) },
            { ("Management and Governance", "Azure Monitor"), (ResourceCategory.Management, ResourceSubCategory.Monitoring) },
            
            // Security
            { ("Security", "Azure Active Directory for External Identities"), (ResourceCategory.Security, ResourceSubCategory.IdentityManagement) },
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
            { ("Databases", "Redis Cache"), (ResourceCategory.Database, ResourceSubCategory.Caching) },
            
            // Network
            { ("Network", "Compute Engine"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Network", "API Gateway"), (ResourceCategory.Networking, ResourceSubCategory.ApiGateway) },
            { ("Network", "Cloud CDN"), (ResourceCategory.Networking, ResourceSubCategory.CDN) },
            { ("Network", "Networking"), (ResourceCategory.Security, ResourceSubCategory.WebApplicationFirewall) },
            
            // Storage
            { ("Storage", "Cloud Storage"), (ResourceCategory.Storage, ResourceSubCategory.BlobStorage) },
            { ("Storage", "Compute Engine"), (ResourceCategory.Storage, ResourceSubCategory.BlockStorage) },
            
            // Analytics
            { ("ApplicationServices", "BigQuery"), (ResourceCategory.Analytics, ResourceSubCategory.DataWarehouse) },
            { ("ApplicationServices", "Cloud Pub/Sub"), (ResourceCategory.Management, ResourceSubCategory.Messaging) },
            { ("ApplicationServices", "Cloud Tasks"), (ResourceCategory.Management, ResourceSubCategory.Queueing) },
            { ("ApplicationServices", "Cloud Logging"), (ResourceCategory.Management, ResourceSubCategory.Monitoring) },
            
            // Security
            { ("ApplicationServices", "Identity Platform"), (ResourceCategory.Security, ResourceSubCategory.IdentityManagement) },
        };

    public async Task<CategorizedResourcesDto> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        var data = await cloudPricingRepository.GetAllAsync(cancellationToken);

        var result = new CategorizedResourcesDto();

        foreach (var product in data.Data.Products)
        {
            var (category, subCategory) = product.VendorName switch
            {
                CloudProvider.AWS => MapAwsProductFamilyToCategoryAndSubCategory(product.ProductFamily, product.Service),
                CloudProvider.Azure => MapAzureProductFamilyToCategoryAndSubCategory(product.ProductFamily, product.Service),
                CloudProvider.GCP => MapGcpProductFamilyToCategoryAndSubCategory(product.ProductFamily, product.Service),
                _ => (ResourceCategory.Other, ResourceSubCategory.Uncategorized)
            };

            switch ((category, subCategory))
            {
                case (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines):
                    // Skip GCP VMs - they will be created synthetically later
                    if (product.VendorName != CloudProvider.GCP)
                    {
                        result.ComputeInstances.Add(NormalizationMapper.MapToComputeInstance(product, category, (int)subCategory));
                    }
                    break;
                case (ResourceCategory.Compute, ResourceSubCategory.CloudFunctions):
                    result.CloudFunctions.Add(NormalizationMapper.MapToCloudFunction(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Compute, ResourceSubCategory.Kubernetes):
                    result.Kubernetes.Add(NormalizationMapper.MapToKubernetes(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Compute, ResourceSubCategory.ContainerInstances):
                    result.ContainerInstances.Add(NormalizationMapper.MapToContainerInstance(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Database, ResourceSubCategory.Relational):
                    result.Databases.Add(NormalizationMapper.MapToDatabase(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Database, ResourceSubCategory.NoSQL):
                    result.Databases.Add(NormalizationMapper.MapToDatabase(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Database, ResourceSubCategory.Caching):
                    result.Caching.Add(NormalizationMapper.MapToCaching(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer):
                    result.LoadBalancers.Add(NormalizationMapper.MapToLoadBalancer(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Networking, ResourceSubCategory.ApiGateway):
                    result.ApiGateways.Add(NormalizationMapper.MapToApiGateway(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Networking, ResourceSubCategory.CDN):
                    result.CDN.Add(NormalizationMapper.MapToCdn(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Storage, ResourceSubCategory.BlobStorage):
                    result.BlobStorage.Add(NormalizationMapper.MapToBlobStorage(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Storage, ResourceSubCategory.BlockStorage):
                    result.BlockStorage.Add(NormalizationMapper.MapToBlockStorage(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Analytics, ResourceSubCategory.DataWarehouse):
                    result.DataWarehouses.Add(NormalizationMapper.MapToDataWarehouse(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Management, ResourceSubCategory.Messaging):
                    result.Messaging.Add(NormalizationMapper.MapToMessaging(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Management, ResourceSubCategory.Queueing):
                    result.Queueing.Add(NormalizationMapper.MapToQueueing(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Management, ResourceSubCategory.Monitoring):
                    result.Monitoring.Add(NormalizationMapper.MapToMonitoring(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Security, ResourceSubCategory.IdentityManagement):
                    result.IdentityManagement.Add(NormalizationMapper.MapToIdentityManagement(product, category, (int)subCategory));
                    break;
                case (ResourceCategory.Security, ResourceSubCategory.WebApplicationFirewall):
                    result.WebApplicationFirewall.Add(NormalizationMapper.MapToWebApplicationFirewall(product, category, (int)subCategory));
                    break;
                default:
                    break;
            }
        }

        // Create synthetic GCP VMs from CPU and RAM pricing
        AddSyntheticGcpVirtualMachines(data.Data.Products, result);

        // Sort ComputeInstances by Cloud provider, then InstanceName, then Region for stable ordering
        result.ComputeInstances = result.ComputeInstances
            .OrderBy(x => x.Cloud)
            .ThenBy(x => x.InstanceName)
            .ThenBy(x => x.Region)
            .ToList();

        return result;
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

    private static void AddSyntheticGcpVirtualMachines(List<CloudPricingProductDto> products, CategorizedResourcesDto result)
    {
        // GCP prices VMs by CPU and RAM separately, so we need to create synthetic VM instances
        // by combining CPU and RAM pricing for standard machine families
        
        var gcpVmProducts = products
            .Where(p => p.VendorName == CloudProvider.GCP 
                && p.ProductFamily == "Compute" 
                && p.Service == "Compute Engine")
            .ToList();

        // Group by region to create VMs for each region
        var productsByRegion = gcpVmProducts.GroupBy(p => p.Region);

        foreach (var regionGroup in productsByRegion)
        {
            var region = regionGroup.Key;
            var regionProducts = regionGroup.ToList();

            // Find CPU and RAM pricing for standard machine families
            var cpuProducts = regionProducts
                .Where(p => p.Attributes.Any(a => a.Key == "resourceGroup" && a.Value == "CPU"))
                .Where(p => p.Prices.Any(pr => pr.Unit == "hour"))
                .ToList();

            var ramProducts = regionProducts
                .Where(p => p.Attributes.Any(a => a.Key == "resourceGroup" && a.Value == "RAM"))
                .Where(p => p.Prices.Any(pr => pr.Unit == "gibibyte hour"))
                .ToList();

            // Extract machine families from CPU products (e.g., "N2", "E2", "C2")
            var machineFamilies = new List<(string Family, decimal CpuPricePerHour, decimal RamPricePerGbHour)>();

            foreach (var cpuProduct in cpuProducts)
            {
                var description = cpuProduct.Attributes.FirstOrDefault(a => a.Key == "description")?.Value ?? "";
                var cpuPrice = cpuProduct.Prices.FirstOrDefault()?.Usd;
                
                if (cpuPrice == null || cpuPrice <= 0) continue;

                // Extract machine family from description (e.g., "N2 Instance Core" -> "N2")
                var familyMatch = GcpMachineFamilyRegex.Match(description);
                if (!familyMatch.Success) continue;

                var family = familyMatch.Groups[1].Value;

                // Find matching RAM pricing
                var ramDescPrefix = $"{family} Instance Ram";
                var ramProduct = ramProducts.FirstOrDefault(p =>
                {
                    var ramDesc = p.Attributes.FirstOrDefault(a => a.Key == "description")?.Value ?? "";
                    return ramDesc.StartsWith(ramDescPrefix, StringComparison.OrdinalIgnoreCase);
                });

                var ramPrice = ramProduct?.Prices.FirstOrDefault()?.Usd;
                if (ramPrice == null || ramPrice <= 0) continue;

                machineFamilies.Add((family, cpuPrice.Value, ramPrice.Value));
            }

            // Create synthetic VMs for each machine family using the cheapest option
            if (machineFamilies.Any())
            {
                // Use the cheapest machine family for this region (based on reference Medium size)
                var cheapestFamily = machineFamilies
                    .OrderBy(f => (f.CpuPricePerHour * ReferenceCpuCount) + (f.RamPricePerGbHour * (decimal)ReferenceMemoryGb))
                    .FirstOrDefault();

                if (cheapestFamily != default)
                {
                    // Create VMs for different usage sizes using predefined configurations
                    foreach (var config in VmSizeConfigurations)
                    {
                        var pricePerHour = CalculateGcpVmPricePerHour(config.VCpu, config.Memory, cheapestFamily.CpuPricePerHour, cheapestFamily.RamPricePerGbHour);
                        
                        result.ComputeInstances.Add(new NormalizedComputeInstanceDto
                        {
                            Category = ResourceCategory.Compute,
                            SubCategoryValue = (int)ComputeType.VirtualMachines,
                            Cloud = CloudProvider.GCP,
                            InstanceName = $"{cheapestFamily.Family}-{config.VCpu}vCPU-{config.Memory}GB",
                            Region = region,
                            VCpu = config.VCpu,
                            Memory = $"{config.Memory} GiB",
                            PricePerHour = pricePerHour
                        });
                    }
                }
            }
        }
    }

    private static decimal CalculateGcpVmPricePerHour(int vCpu, double memoryGb, decimal cpuPricePerHour, decimal ramPricePerGbHour)
    {
        return (vCpu * cpuPricePerHour) + ((decimal)memoryGb * ramPricePerGbHour);
    }
}