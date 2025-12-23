using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services;

namespace Application.Facade;

public interface ICloudPricingFileFacade
{
    Task<PagedResult<CloudPricingProductDto>?> GetOrCreatePagedAsync(PricingRequest? pagination, CancellationToken cancellationToken);

    Task<DistinctFiltersDto> GetDistinctFiltersAsync(CancellationToken cancellationToken);

    Task<ProductFamilyMappingsDto> GetProductFamilyMappingsAsync(CancellationToken cancellationToken);
    
    Task<CategorizedResourcesDto> GetCategorizedResourcesAsync(UsageSize usage, CancellationToken cancellationToken);
}

public class CloudPricingFileFacade(ICloudPricingRepositoryFacade cloudPricingProvider, IResourceNormalizationService resourceNormalizationService) : ICloudPricingFileFacade
{
    public async Task<PagedResult<CloudPricingProductDto>?> GetOrCreatePagedAsync(PricingRequest? pagination, CancellationToken cancellationToken)
    {
        var request = pagination ?? new PricingRequest();
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, request.PageSize);

        var full = await cloudPricingProvider.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var products = full.Data?.Products ?? new List<CloudPricingProductDto>();

        var filtered = products.AsEnumerable();

        // Product-level filters
        if (request.VendorName != null)
            filtered = filtered.Where(p => p.VendorName == request.VendorName);

        if (!string.IsNullOrWhiteSpace(request.Service))
            filtered = filtered.Where(p => !string.IsNullOrWhiteSpace(p.Service) && p.Service.Contains(request.Service, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Region))
            filtered = filtered.Where(p => !string.IsNullOrWhiteSpace(p.Region) && p.Region.Contains(request.Region, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.ProductFamily))
            filtered = filtered.Where(p => !string.IsNullOrWhiteSpace(p.ProductFamily) && p.ProductFamily.Contains(request.ProductFamily, StringComparison.OrdinalIgnoreCase));

        // Price-level filters: keep products that have at least one matching price
        if (!string.IsNullOrWhiteSpace(request.StartUsageAmount)
            || !string.IsNullOrWhiteSpace(request.EndUsageAmount)
            || !string.IsNullOrWhiteSpace(request.PurchaseOption)
            || !string.IsNullOrWhiteSpace(request.TermPurchaseOption)
            || !string.IsNullOrWhiteSpace(request.TermLength)
            || !string.IsNullOrWhiteSpace(request.TermOfferingClass))
        {
            filtered = filtered.Where(p => p.Prices?.Any(price =>
                MatchesPrice(price, request)) ?? false);
        }

        var total = filtered.Count();
        var skip = (page - 1) * pageSize;
        var items = filtered.Skip(skip).Take(pageSize).ToList();

        return new PagedResult<CloudPricingProductDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DistinctFiltersDto> GetDistinctFiltersAsync(CancellationToken cancellationToken)
    {
        var full = await cloudPricingProvider.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var products = full.Data?.Products ?? new List<CloudPricingProductDto>();

        var comparer = StringComparer.OrdinalIgnoreCase;

        var vendorNames = products
            .Select(p => p.VendorName)
            .Distinct()
            .ToList();

        var services = products
            .Where(p => !string.IsNullOrWhiteSpace(p.Service))
            .Select(p => p.Service!.Trim())
            .Distinct(comparer)
            .OrderBy(s => s, comparer)
            .ToList();

        var regions = products
            .Where(p => !string.IsNullOrWhiteSpace(p.Region))
            .Select(p => p.Region!.Trim())
            .Distinct(comparer)
            .OrderBy(s => s, comparer)
            .ToList();

        var families = products
            .Where(p => !string.IsNullOrWhiteSpace(p.ProductFamily))
            .Select(p => p.ProductFamily!.Trim())
            .Distinct(comparer)
            .OrderBy(s => s, comparer)
            .ToList();

        var attributeGroups = products
            .SelectMany(p => p.Attributes ?? Enumerable.Empty<CloudPricingAttributeDto>())
            .Where(a => !string.IsNullOrWhiteSpace(a.Key))
            .GroupBy(a => a.Key!.Trim(), comparer)
            .Select(g => new CloudPricingAttributeSummary
            {
                Key = g.Key,
                Values = g
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                    .Select(x => x.Value!.Trim())
                    .Distinct(comparer)
                    .OrderBy(v => v, comparer)
                    .ToList()
            })
            .OrderBy(s => s.Key, comparer)
            .ToList();

        var allPrices = products.SelectMany(p => p.Prices ?? Enumerable.Empty<CloudPricingPriceDto>()).ToList();

        var startUsageAmounts = allPrices
            .Select(p => p.StartUsageAmount?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s, comparer)
            .ToList()!;

        var endUsageAmounts = allPrices
            .Select(p => p.EndUsageAmount?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s, comparer)
            .ToList()!;

        var purchaseOptions = allPrices
            .Where(p => !string.IsNullOrWhiteSpace(p.PurchaseOption))
            .Select(p => p.PurchaseOption!.Trim())
            .Distinct(comparer)
            .OrderBy(s => s, comparer)
            .ToList();

        var termPurchaseOptions = allPrices
            .Where(p => !string.IsNullOrWhiteSpace(p.TermPurchaseOption))
            .Select(p => p.TermPurchaseOption!.Trim())
            .Distinct(comparer)
            .OrderBy(s => s, comparer)
            .ToList();

        var termLengths = allPrices
            .Where(p => !string.IsNullOrWhiteSpace(p.TermLength))
            .Select(p => p.TermLength!.Trim())
            .Distinct(comparer)
            .OrderBy(s => s, comparer)
            .ToList();

        var termOfferingClasses = allPrices
            .Where(p => !string.IsNullOrWhiteSpace(p.TermOfferingClass))
            .Select(p => p.TermOfferingClass!.Trim())
            .Distinct(comparer)
            .OrderBy(s => s, comparer)
            .ToList();

        return new DistinctFiltersDto
        {
            VendorNames = vendorNames,
            Services = services,
            Regions = regions,
            ProductFamilies = families,
            AttributeSummaries = attributeGroups,
            StartUsageAmounts = startUsageAmounts,
            EndUsageAmounts = endUsageAmounts,
            PurchaseOptions = purchaseOptions,
            TermPurchaseOptions = termPurchaseOptions,
            TermLengths = termLengths,
            TermOfferingClasses = termOfferingClasses
        };
    }

    private static bool MatchesPrice(CloudPricingPriceDto price, PricingRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.StartUsageAmount) &&
            (price.StartUsageAmount == null || !price.StartUsageAmount.Contains(request.StartUsageAmount, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (!string.IsNullOrWhiteSpace(request.EndUsageAmount) &&
            (price.EndUsageAmount == null || !price.EndUsageAmount.Contains(request.EndUsageAmount, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (!string.IsNullOrWhiteSpace(request.PurchaseOption) &&
            (price.PurchaseOption == null || !price.PurchaseOption.Contains(request.PurchaseOption, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (!string.IsNullOrWhiteSpace(request.TermPurchaseOption) &&
            (price.TermPurchaseOption == null || !price.TermPurchaseOption.Contains(request.TermPurchaseOption, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (!string.IsNullOrWhiteSpace(request.TermLength) &&
            (price.TermLength == null || !price.TermLength.Contains(request.TermLength, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (!string.IsNullOrWhiteSpace(request.TermOfferingClass) &&
            (price.TermOfferingClass == null || !price.TermOfferingClass.Contains(request.TermOfferingClass, StringComparison.OrdinalIgnoreCase)))
            return false;

        return true;
    }

    public async Task<ProductFamilyMappingsDto> GetProductFamilyMappingsAsync(CancellationToken cancellationToken)
    {
        return await resourceNormalizationService.GetProductFamilyMappingsAsync(cancellationToken);
    }

    public async Task<CategorizedResourcesDto> GetCategorizedResourcesAsync(UsageSize usage, CancellationToken cancellationToken)
    {
        return await resourceNormalizationService.GetCategorizedResourcesAsync(usage, cancellationToken);
    }
}