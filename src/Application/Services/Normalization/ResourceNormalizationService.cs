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
                case (ResourceCategory.Database, _):
                    result.Databases.Add(NormalizationMapper.MapToDatabase(product, category, subCategory));
                    break;
                default:
                    result.Networking.Add(NormalizationMapper.MapToNormalizedResource(product, category, subCategory));
                    break;
            }
        }

        result.Monitoring.AddRange(GetNormalizedMonitoring());


        return new CategorizedResourcesDto { 
        };
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

    private static List<NormalizedMonitoringDto> GetNormalizedMonitoring()
    {
        return new List<NormalizedMonitoringDto>
        {
            new() { Cloud = CloudProvider.GCP, Category = ResourceCategory.Management, SubCategory = ResourceSubCategory.Monitoring, Name = "Cloud Ops", PricePerMonth = 4m },
            new() { Cloud = CloudProvider.GCP, Category = ResourceCategory.Management, SubCategory = ResourceSubCategory.Monitoring, Name = "Cloud Ops", PricePerMonth = 12m },
            new() { Cloud = CloudProvider.GCP, Category = ResourceCategory.Management, SubCategory = ResourceSubCategory.Monitoring, Name = "Cloud Ops", PricePerMonth = 40m },
            new() { Cloud = CloudProvider.Azure, Category = ResourceCategory.Management, SubCategory = ResourceSubCategory.Monitoring, Name = "Azure Monitor", PricePerMonth = 6m },
            new() { Cloud = CloudProvider.Azure, Category = ResourceCategory.Management, SubCategory = ResourceSubCategory.Monitoring, Name = "Azure Monitor", PricePerMonth = 18m },
            new() { Cloud = CloudProvider.Azure, Category = ResourceCategory.Management, SubCategory = ResourceSubCategory.Monitoring, Name = "Azure Monitor", PricePerMonth = 60m },
            new() { Cloud = CloudProvider.AWS, Category = ResourceCategory.Management, SubCategory = ResourceSubCategory.Monitoring, Name = "CloudWatch", PricePerMonth = 5m },
            new() { Cloud = CloudProvider.AWS, Category = ResourceCategory.Management, SubCategory = ResourceSubCategory.Monitoring, Name = "CloudWatch", PricePerMonth = 15m },
            new() {Cloud = CloudProvider.AWS, Category = ResourceCategory.Management, SubCategory = ResourceSubCategory.Monitoring, Name = "CloudWatch", PricePerMonth = 50m}
        };
    }
}
