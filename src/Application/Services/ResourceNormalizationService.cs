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
    private static readonly Dictionary<(string ProductFamily, string Service), (ResourceCategory Category, ResourceSubCategory SubCategory)> AwsProductFamilyServiceMap =
        new()
        {

        };

    // Azure-specific mapping based on productFamily and service combination
    private static readonly Dictionary<(string ProductFamily, string Service), (ResourceCategory Category, ResourceSubCategory SubCategory)> AzureProductFamilyServiceMap =
        new()
        {

        };

    // GCP-specific mapping based on productFamily and service combination
    private static readonly Dictionary<(string ProductFamily, string Service), (ResourceCategory Category, ResourceSubCategory SubCategory)> GcpProductFamilyServiceMap =
        new()
        {

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
            if (string.IsNullOrWhiteSpace(product.ProductFamily))
                continue;

            var (category, subCategory) = product.VendorName switch
            {
                CloudProvider.AWS => MapAwsProductFamilyToCategoryAndSubCategory(product.ProductFamily, product.Service),
                CloudProvider.Azure => MapAzureProductFamilyToCategoryAndSubCategory(product.ProductFamily, product.Service),
                CloudProvider.GCP => MapGcpProductFamilyToCategoryAndSubCategory(product.ProductFamily, product.Service),
                _ => (ResourceCategory.Other, ResourceSubCategory.Uncategorized)
            };

        return new CategorizedResourcesDto { Categories = categories };
    }


    private static decimal? GetPricePerHour(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault(p => IsOnDemand(p.PurchaseOption))?.Usd
                    ?? product.Prices.FirstOrDefault()?.Usd;
        return price;
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