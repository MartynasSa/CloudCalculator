using System;
using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services;

namespace Application.Facade;

public interface ICloudPricingFileFacade
{
    Task<CategorizedResourcesDto> GetCategorizedResourcesAsync(UsageSize usage, CancellationToken cancellationToken);
    Task<ProductFamilyMappingsDto> GetProductFamilyMappingsAsync(CancellationToken cancellationToken);
}

public class CloudPricingFileFacade(IResourceNormalizationService resourceNormalizationService) : ICloudPricingFileFacade
{
    public async Task<CategorizedResourcesDto> GetCategorizedResourcesAsync(UsageSize usage, CancellationToken cancellationToken)
    {
        var neededResources = Enum.GetValues<ResourceCategory>();

        return await resourceNormalizationService.GetResourcesAsync(neededResources, usage, cancellationToken);
    }

    public async Task<ProductFamilyMappingsDto> GetProductFamilyMappingsAsync(CancellationToken cancellationToken)
    {
        return await resourceNormalizationService.GetProductFamilyMappingsAsync(cancellationToken);
    }
}