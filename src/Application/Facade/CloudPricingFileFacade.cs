using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services;

namespace Application.Facade;

public interface ICloudPricingFileFacade
{
    Task<CategorizedResourcesDto> GetCategorizedResourcesAsync(UsageSize usage, CancellationToken cancellationToken);
}

public class CloudPricingFileFacade(IResourceNormalizationService resourceNormalizationService) : ICloudPricingFileFacade
{
    public async Task<CategorizedResourcesDto> GetCategorizedResourcesAsync(UsageSize usage, CancellationToken cancellationToken)
    {
        return await resourceNormalizationService.GetCategorizedResourcesAsync(usage, cancellationToken);
    }
}