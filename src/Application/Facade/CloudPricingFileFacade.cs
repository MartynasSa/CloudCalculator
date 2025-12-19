using Application.Models.Dtos;
using Application.Ports;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Facade;

public interface ICloudPricingFileFacade
{
    Task<PagedResult<CloudPricingProductDto>> GetOrCreatePagedAsync(PricingRequest? pagination, CancellationToken cancellationToken);

    Task<DistinctFiltersDto> GetDistinctFiltersAsync(CancellationToken cancellationToken);
}

public class CloudPricingFileFacade(ICloudPricingRepository cloudPricingRepository, IMemoryCache cache) : ICloudPricingFileFacade
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

        var cacheKey = $"cloud-pricing:page={page}:pageSize={pageSize}:vendor={vendorKey}:service={serviceKey}:region={regionKey}:family={familyKey}";

        return cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultTtl;

            var full = await cloudPricingRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var products = full.Data?.Products ?? new List<CloudPricingProductDto>();

            IEnumerable<CloudPricingProductDto> query = products;

            if (!string.IsNullOrWhiteSpace(request.VendorName))
            {
                query = query.Where(p => p.VendorName?.Contains(request.VendorName, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (!string.IsNullOrWhiteSpace(request.Service))
            {
                query = query.Where(p => p.Service?.Contains(request.Service, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (!string.IsNullOrWhiteSpace(request.Region))
            {
                query = query.Where(p => p.Region?.Contains(request.Region, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (!string.IsNullOrWhiteSpace(request.ProductFamily))
            {
                query = query.Where(p => p.ProductFamily?.Contains(request.ProductFamily, StringComparison.OrdinalIgnoreCase) == true);
            }

            var filteredList = query.ToList();
            var total = filteredList.Count;
            var skip = (page - 1) * pageSize;
            var items = filteredList.Skip(skip).Take(pageSize).ToList();

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

            var full = await cloudPricingRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
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

            return new DistinctFiltersDto
            {
                VendorNames = vendorNames,
                Services = services,
                Regions = regions,
                ProductFamilies = families,
                AttributeSummaries = attributeGroups
            };
        });
    }
}