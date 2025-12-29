using Application.Mappers;
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
            { ("", "AmazonElastiCache"), (ResourceCategory.Database, ResourceSubCategory.Caching) },
            
            // Network
            { ("Load Balancer", "AmazonEC2"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Load Balancer", "AWSELB"), (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer) },
            { ("Amazon API Gateway Cache", "AmazonApiGateway"), (ResourceCategory.Networking, ResourceSubCategory.ApiGateway) },
            { ("", "AmazonCloudFront"), (ResourceCategory.Networking, ResourceSubCategory.CDN) },
            
            // Storage
            { ("", "AmazonS3"), (ResourceCategory.Storage, ResourceSubCategory.BlobStorage) },
            // Analytics
            { ("", "AmazonRedshift"), (ResourceCategory.Analytics, ResourceSubCategory.DataWarehouse) },
            // Messaging
            { ("", "AmazonSNS"), (ResourceCategory.Management, ResourceSubCategory.Messaging) },
            { ("", "AmazonSQS"), (ResourceCategory.Management, ResourceSubCategory.Messaging) },

            { ("API Request", "AWSQueueService"), (ResourceCategory.Management, ResourceSubCategory.Queueing) },
            { ("", "AmazonCloudWatch"), (ResourceCategory.Management, ResourceSubCategory.Monitoring) },
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
            
            // Storage
            { ("Storage", "Azure Blob Storage"), (ResourceCategory.Storage, ResourceSubCategory.BlobStorage) },
            // Analytics
            { ("Analytics", "Azure Synapse Analytics"), (ResourceCategory.Analytics, ResourceSubCategory.DataWarehouse) },

            // Messaging
            { ("Integration", "Service Bus"), (ResourceCategory.Management, ResourceSubCategory.Messaging) },
            { ("Integration", "Event Hubs"), (ResourceCategory.Management, ResourceSubCategory.Messaging) },

            { ("Internet of Things", "Event Grid"), (ResourceCategory.Management, ResourceSubCategory.Queueing) },
            { ("Management and Governance", "Azure Monitor"), (ResourceCategory.Management, ResourceSubCategory.Monitoring) },
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
            
            // Storage
            { ("Storage", "Cloud Storage"), (ResourceCategory.Storage, ResourceSubCategory.BlobStorage) },
            // Analytics
            { ("ApplicationServices", "BigQuery"), (ResourceCategory.Analytics, ResourceSubCategory.DataWarehouse) },
            { ("ApplicationServices", "Cloud Pub/Sub"), (ResourceCategory.Management, ResourceSubCategory.Messaging) },
            { ("ApplicationServices", "Cloud Tasks"), (ResourceCategory.Management, ResourceSubCategory.Queueing) },

            { ("ApplicationServices", "Cloud Logging"), (ResourceCategory.Management, ResourceSubCategory.Monitoring) },
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
                    result.ComputeInstances.Add(NormalizationMapper.MapToComputeInstance(product, category, subCategory));
                    break;
                case (ResourceCategory.Compute, ResourceSubCategory.CloudFunctions):
                    result.CloudFunctions.Add(NormalizationMapper.MapToCloudFunction(product, category, subCategory));
                    break;
                case (ResourceCategory.Compute, ResourceSubCategory.Kubernetes):
                    result.Kubernetes.Add(NormalizationMapper.MapToKubernetes(product, category, subCategory));
                    break;
                case (ResourceCategory.Compute, ResourceSubCategory.ContainerInstances):
                    result.ContainerInstances.Add(NormalizationMapper.MapToContainerInstance(product, category, subCategory));
                    break;
                case (ResourceCategory.Database, ResourceSubCategory.Relational):
                    result.Databases.Add(NormalizationMapper.MapToDatabase(product, category, subCategory));
                    break;
                case (ResourceCategory.Database, ResourceSubCategory.NoSQL):
                    result.Databases.Add(NormalizationMapper.MapToDatabase(product, category, subCategory));
                    break;
                case (ResourceCategory.Database, ResourceSubCategory.Caching):
                    result.Caching.Add(NormalizationMapper.MapToCaching(product, category, subCategory));
                    break;
                case (ResourceCategory.Networking, ResourceSubCategory.LoadBalancer):
                    result.LoadBalancers.Add(NormalizationMapper.MapToLoadBalancer(product, category, subCategory));
                    break;
                case (ResourceCategory.Networking, ResourceSubCategory.ApiGateway):
                    result.ApiGateways.Add(NormalizationMapper.MapToApiGateway(product, category, subCategory));
                    break;
                case (ResourceCategory.Networking, ResourceSubCategory.CDN):
                    result.CDN.Add(NormalizationMapper.MapToCdn(product, category, subCategory));
                    break;
                case (ResourceCategory.Storage, ResourceSubCategory.BlobStorage):
                    result.BlobStorage.Add(NormalizationMapper.MapToBlobStorage(product, category, subCategory));
                    break;
                case (ResourceCategory.Analytics, ResourceSubCategory.DataWarehouse):
                    result.DataWarehouses.Add(NormalizationMapper.MapToDataWarehouse(product, category, subCategory));
                    break;
                case (ResourceCategory.Management, ResourceSubCategory.Messaging):
                    result.Messaging.Add(NormalizationMapper.MapToMessaging(product, category, subCategory));
                    break;
                case (ResourceCategory.Management, ResourceSubCategory.Queueing):
                    result.Queueing.Add(NormalizationMapper.MapToQueueing(product, category, subCategory));
                    break;
                case (ResourceCategory.Management, ResourceSubCategory.Monitoring):
                    result.Monitoring.Add(NormalizationMapper.MapToMonitoring(product, category, subCategory));
                    break;
                default:
                    break;
            }
        }

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
}