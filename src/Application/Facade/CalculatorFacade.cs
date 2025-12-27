using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services.Calculator;
using Application.Services.Normalization;
using System.Text.Json;

namespace Application.Facade;

public interface ICalculatorFacade
{
    Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(CalculationRequest templateDto, CancellationToken ct = default);
}

public class CalculatorFacade(
    IResourceNormalizationService resourceNormalizationService,
    IPriceProvider priceProvider,
    ICalculatorService calculatorService) : ICalculatorFacade
{
    public async Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(CalculationRequest template, CancellationToken ct = default)
    {
        var resources = await resourceNormalizationService.GetResourcesAsync(ct);
        var result = new TemplateCostComparisonResultDto
        {
            Resources = template.Resources
        };

        var cloudProviders = new[] { CloudProvider.AWS, CloudProvider.Azure, CloudProvider.GCP };
        var usageSizes = new[] { UsageSize.Small, UsageSize.Medium, UsageSize.Large, UsageSize.ExtraLarge };

        // Get all prices once, grouped by (UsageSize, CloudProvider)
        var filteredResources = priceProvider.GetPrices(resources);

        foreach (var usageSize in usageSizes)
        {
            var cloudCostsList = new List<TemplateCostComparisonResultCloudProviderDto>();

            foreach (var cloudProvider in cloudProviders)
            {
                var costDetails = new List<TemplateCostResourceSubCategoryDetailsDto>();
                decimal totalCost = 0m;

                // Calculate costs based on requested resource subcategories
                foreach (var requestedSubCategory in template.Resources)
                {
                    var (cost, resourceDetails) = CalculateResourceCost(filteredResources, usageSize, cloudProvider, requestedSubCategory);
                    
                    if (cost > 0m)
                    {
                        totalCost += cost;
                        costDetails.Add(new TemplateCostResourceSubCategoryDetailsDto()
                        {
                            ResourceSubCategory = requestedSubCategory,
                            Cost = cost,
                            ResourceDetails = JsonSerializer.Serialize(resourceDetails)
                        });
                    }
                }

                cloudCostsList.Add(new TemplateCostComparisonResultCloudProviderDto
                {
                    CloudProvider = cloudProvider,
                    TotalMonthlyPrice = totalCost,
                    CostDetails = costDetails
                });
            }

            result.CloudCosts[usageSize] = cloudCostsList;
        }

        return result;
    }

    private (decimal, object) CalculateResourceCost(FilteredResourcesDto filteredResources, UsageSize usageSize, CloudProvider cloudProvider, ResourceSubCategory subCategory)
    {
        var key = (usageSize, cloudProvider);

        return subCategory switch
        {
            ResourceSubCategory.VirtualMachines when filteredResources.ComputeInstances.TryGetValue(key, out var vm)
                => (calculatorService.CalculateVmCost(vm, CalculatorService.HoursPerMonth), vm),
            
            ResourceSubCategory.Relational when filteredResources.Databases.TryGetValue(key, out var db)
                => (calculatorService.CalculateDatabaseCost(db, CalculatorService.HoursPerMonth), db),
            
            ResourceSubCategory.LoadBalancer when filteredResources.LoadBalancers.TryGetValue(key, out var lb)
                => (calculatorService.CalculateLoadBalancerCost(lb), lb),
            
            ResourceSubCategory.Monitoring when filteredResources.Monitoring.TryGetValue(key, out var monitoring)
                => (calculatorService.CalculateMonitoringCost(monitoring), monitoring),
            
            _ => (0m, null)
        };
    }
}