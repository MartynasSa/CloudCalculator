using System;
using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services.Normalization;

namespace Application.Facade;

public interface ICloudPricingFileFacade
{
    Task<CategorizedResourcesDto> GetCategorizedResourcesAsync(UsageSize usage, CancellationToken cancellationToken);
}

public class CloudPricingFileFacade(IResourceNormalizationService resourceNormalizationService) : ICloudPricingFileFacade
{
    public async Task<CategorizedResourcesDto> GetCategorizedResourcesAsync(UsageSize usage, CancellationToken cancellationToken)
    {
        var neededResources = Enum.GetValues<ResourceCategory>();

        return await resourceNormalizationService.GetResourcesAsync(neededResources, usage, cancellationToken);
    }
}