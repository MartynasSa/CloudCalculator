using Application.Models.Dtos;
using Application.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Facade;

public interface ICloudPricingFileFacade
{
    Task<PagedResult<CloudPricingProductDto>?> GetOrCreatePagedAsync(PricingRequest? pagination, CancellationToken cancellationToken);

    Task<DistinctFiltersDto> GetDistinctFiltersAsync(CancellationToken cancellationToken);
}

public class CloudPricingFileFacade(ICloudPricingProvider cloudPricingProvider, IMemoryCache cache) : ICloudPricingFileFacade
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    public Task<PagedResult<CloudPricingProductDto>?> GetOrCreatePagedAsync(PricingRequest? pagination, CancellationToken cancellationToken)
    {
        var request = pagination ?? new PricingRequest();
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, request.PageSize);

        var vendorKey = string.IsNullOrWhiteSpace(request.VendorName) ? "any" : request.VendorName.Trim().ToLowerInvariant();
        var serviceKey = string.IsNullOrWhiteSpace(request.Service) ? "any" : request.Service.Trim().ToLowerInvariant();
        var regionKey = string.IsNullOrWhiteSpace(request.Region) ? "any" : request.Region.Trim().ToLowerInvariant();
        var familyKey = string.IsNullOrWhiteSpace(request.ProductFamily) ? "any" : request.ProductFamily.Trim().ToLowerInvariant();

        var startKey = string.IsNullOrWhiteSpace(request.StartUsageAmount) ? "any" : request.StartUsageAmount.Trim().ToLowerInvariant();
        var endKey = string.IsNullOrWhiteSpace(request.EndUsageAmount) ? "any" : request.EndUsageAmount.Trim().ToLowerInvariant();
        var purchaseKey = string.IsNullOrWhiteSpace(request.PurchaseOption) ? "any" : request.PurchaseOption.Trim().ToLowerInvariant();
        var termPurchaseKey = string.IsNullOrWhiteSpace(request.TermPurchaseOption) ? "any" : request.TermPurchaseOption.Trim().ToLowerInvariant();
        var termLengthKey = string.IsNullOrWhiteSpace(request.TermLength) ? "any" : request.TermLength.Trim().ToLowerInvariant();
        var offeringKey = string.IsNullOrWhiteSpace(request.TermOfferingClass) ? "any" : request.TermOfferingClass.Trim().ToLowerInvariant();

        var cacheKey = $"cloud-pricing:page={page}:pageSize={pageSize}:vendor={vendorKey}:service={serviceKey}:region={regionKey}:family={familyKey}:start={startKey}:end={endKey}:purchase={purchaseKey}:termPurchase={termPurchaseKey}:termLength={termLengthKey}:offeringClass={offeringKey}";

        return cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultTtl;

            // Get queryable data source
            var queryable = await cloudPricingProvider.GetProductsQueryableAsync(cancellationToken).ConfigureAwait(false);

            // Apply filters using LINQ (which can be translated to OData queries)
            if (!string.IsNullOrWhiteSpace(request.VendorName))
            {
                queryable = queryable.Where(p => p.VendorName != null && p.VendorName.Contains(request.VendorName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.Service))
            {
                queryable = queryable.Where(p => p.Service != null && p.Service.Contains(request.Service, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.Region))
            {
                queryable = queryable.Where(p => p.Region != null && p.Region.Contains(request.Region, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(request.ProductFamily))
            {
                queryable = queryable.Where(p => p.ProductFamily != null && p.ProductFamily.Contains(request.ProductFamily, StringComparison.OrdinalIgnoreCase));
            }

            // Price-level filtering: if any of the price filters are present, keep products that have at least one price that matches
            if (!string.IsNullOrWhiteSpace(request.StartUsageAmount)
                || !string.IsNullOrWhiteSpace(request.EndUsageAmount)
                || !string.IsNullOrWhiteSpace(request.PurchaseOption)
                || !string.IsNullOrWhiteSpace(request.TermPurchaseOption)
                || !string.IsNullOrWhiteSpace(request.TermLength)
                || !string.IsNullOrWhiteSpace(request.TermOfferingClass))
            {
                queryable = queryable.Where(p => p.Prices != null && p.Prices.Any(price =>
                    (string.IsNullOrWhiteSpace(request.StartUsageAmount) || (price.StartUsageAmount != null && price.StartUsageAmount.Contains(request.StartUsageAmount, StringComparison.OrdinalIgnoreCase))) &&
                    (string.IsNullOrWhiteSpace(request.EndUsageAmount) || (price.EndUsageAmount != null && price.EndUsageAmount.Contains(request.EndUsageAmount, StringComparison.OrdinalIgnoreCase))) &&
                    (string.IsNullOrWhiteSpace(request.PurchaseOption) || (price.PurchaseOption != null && price.PurchaseOption.Contains(request.PurchaseOption, StringComparison.OrdinalIgnoreCase))) &&
                    (string.IsNullOrWhiteSpace(request.TermPurchaseOption) || (price.TermPurchaseOption != null && price.TermPurchaseOption.Contains(request.TermPurchaseOption, StringComparison.OrdinalIgnoreCase))) &&
                    (string.IsNullOrWhiteSpace(request.TermLength) || (price.TermLength != null && price.TermLength.Contains(request.TermLength, StringComparison.OrdinalIgnoreCase))) &&
                    (string.IsNullOrWhiteSpace(request.TermOfferingClass) || (price.TermOfferingClass != null && price.TermOfferingClass.Contains(request.TermOfferingClass, StringComparison.OrdinalIgnoreCase)))
                ));
            }

            // Get total count before pagination
            var total = queryable.Count();
            
            // Apply pagination
            var skip = (page - 1) * pageSize;
            var items = queryable.Skip(skip).Take(pageSize).ToList();

            return new PagedResult<CloudPricingProductDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        });
    }

    public Task<DistinctFiltersDto> GetDistinctFiltersAsync(CancellationToken cancellationToken)
    {
        const string cacheKey = "cloud-pricing:distinct";

        return cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultTtl;

            var full = await cloudPricingProvider.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var products = full.Data?.Products ?? new List<CloudPricingProductDto>();

            var comparer = StringComparer.OrdinalIgnoreCase;

            var vendorNames = products
                .Where(p => !string.IsNullOrWhiteSpace(p.VendorName))
                .Select(p => p.VendorName!.Trim())
                .Distinct(comparer)
                .OrderBy(s => s, comparer)
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

            // New distinct lists derived from prices
            var allPrices = products.SelectMany(p => p.Prices ?? Enumerable.Empty<CloudPricingPriceDto>()).ToList();

            var startUsageAmounts = allPrices
                .Select(p => p.StartUsageAmount?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s, comparer)
                .ToList();

            var endUsageAmounts = allPrices
                .Select(p => p.EndUsageAmount?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .OrderBy(s => s, comparer)
                .ToList();

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
        });
    }
}