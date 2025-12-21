using Application.Models.Dtos;
using Application.Ports;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Services;

public interface ICloudPricingRepositoryFacade
{
    Task<CloudPricingDto> GetAllAsync(CancellationToken cancellationToken);
}

public class CloudPricingRepositoryFacade(ICloudPricingRepository cloudPricingRepository, IMemoryCache cache) : ICloudPricingRepositoryFacade
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);
    private const string CacheKey = "cloud-pricing:all-data";

    public Task<CloudPricingDto> GetAllAsync(CancellationToken cancellationToken)
    {
        return cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultTtl;
            return await cloudPricingRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        });
    }
}
