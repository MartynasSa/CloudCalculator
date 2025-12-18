using Application.Models.Dtos;
using Application.Ports;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure;

public interface ICloudPricingFileFacade
{
    Task<PagedResult<CloudPricingProductDto>> GetOrCreatePagedAsync(PaginationParameters? pagination, CancellationToken cancellationToken);
}

public class CloudPricingFileFacade(ICloudPricingRepository cloudPricingRepository, IMemoryCache cache) : ICloudPricingFileFacade
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

    /// <summary>
    /// Returns a cached value for the given key or uses the provided factory to produce and cache the value.
    /// Facade is deliberately storage-only: it does not know how the value is created (parsing logic kept in repository).
    /// </summary>
    public Task<CloudPricingDto> GetOrCreateAsync(string key, Func<CancellationToken, Task<CloudPricingDto>> factory, CancellationToken cancellationToken)
    {
        return cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultTtl;
            // Call repository-provided factory to create the value
            return await factory(cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Returns a cached paged result. On cache miss, loads full data from repository, applies pagination here,
    /// and caches the paged result. Pagination responsibilities moved from repository to this facade.
    /// </summary>
    public Task<PagedResult<CloudPricingProductDto>> GetOrCreatePagedAsync(PaginationParameters? pagination, CancellationToken cancellationToken)
    {
        pagination ??= new PaginationParameters();

        var page = Math.Max(1, pagination.Page);
        var pageSize = Math.Max(1, pagination.PageSize);

        var cacheKey = $"cloud-pricing:page={page}:pageSize={pageSize}";

        return cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultTtl;

            // On cache miss: fetch combined dataset from repository (repository keeps file-loading/parsing logic)
            var full = await cloudPricingRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var products = full.Data?.Products ?? new List<CloudPricingProductDto>();

            var total = products.Count;
            var skip = (page - 1) * pageSize;
            var items = products.Skip(skip).Take(pageSize).ToList();

            return new PagedResult<CloudPricingProductDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        });
    }
}