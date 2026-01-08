using Application.Models.Dtos;
using Application.Services;
using Application.Services.Normalization;

namespace Application.Facade;

public interface ICloudPricingFileFacade
{
    Task<CategorizedResourcesDto> GetCategorizedResourcesAsync(CancellationToken cancellationToken);
    Task<CloudPricingDto> GetAllAsync(CancellationToken cancellationToken);
}

public class CloudPricingFileFacade(IResourceNormalizationService resourceNormalizationService, ICloudPricingRepositoryProvider cloudPricingRepositoryProvider) : ICloudPricingFileFacade
{
    public async Task<CategorizedResourcesDto> GetCategorizedResourcesAsync(CancellationToken cancellationToken)
    {
        return await resourceNormalizationService.GetResourcesAsync(cancellationToken);
    }

    public async Task<CloudPricingDto> GetAllAsync(CancellationToken cancellationToken)
    {
        return await cloudPricingRepositoryProvider.GetAllAsync(cancellationToken);
    }
}