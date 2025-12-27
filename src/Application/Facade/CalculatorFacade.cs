using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services.Calculator;
using Application.Services.Normalization;

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
                    var cost = CalculateResourceCost(filteredResources, usageSize, cloudProvider, requestedSubCategory);
                    
                    if (cost > 0m)
                    {
                        totalCost += cost;
                        costDetails.Add(CreateCostDetail(filteredResources, usageSize, cloudProvider, requestedSubCategory, cost));
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

    private decimal CalculateResourceCost(FilteredResourcesDto filteredResources, UsageSize usageSize, CloudProvider cloudProvider, ResourceSubCategory subCategory)
    {
        var key = (usageSize, cloudProvider);

        return subCategory switch
        {
            ResourceSubCategory.VirtualMachines when filteredResources.ComputeInstances.TryGetValue(key, out var vm)
                => calculatorService.CalculateVmCost(vm, CalculatorService.HoursPerMonth),
            
            ResourceSubCategory.Relational when filteredResources.Databases.TryGetValue(key, out var db)
                => calculatorService.CalculateDatabaseCost(db, CalculatorService.HoursPerMonth),
            
            ResourceSubCategory.LoadBalancer when filteredResources.LoadBalancers.TryGetValue(key, out var lb)
                => calculatorService.CalculateLoadBalancerCost(lb),
            
            ResourceSubCategory.Monitoring when filteredResources.Monitoring.TryGetValue(key, out var monitoring)
                => calculatorService.CalculateMonitoringCost(monitoring),
            
            _ => 0m
        };
    }

    private static TemplateCostResourceSubCategoryDetailsDto CreateCostDetail(
        FilteredResourcesDto filteredResources,
        UsageSize usageSize,
        CloudProvider cloudProvider,
        ResourceSubCategory subCategory, 
        decimal cost)
    {
        var key = (usageSize, cloudProvider);
        var costDetail = new TemplateCostResourceSubCategoryDetailsDto
        {
            Cost = cost,
            ResourceSubCategory = subCategory,
            ResourceDetails = new Dictionary<string, object>()
        };

        switch (subCategory)
        {
            case ResourceSubCategory.VirtualMachines when filteredResources.ComputeInstances.TryGetValue(key, out var vm):
                costDetail.ResourceDetails = new Dictionary<string, object>
                {
                    ["instanceName"] = vm.InstanceName ?? "",
                    ["vCpu"] = vm.VCpu ?? 0,
                    ["memory"] = vm.Memory ?? "",
                    ["pricePerHour"] = vm.PricePerHour ?? 0m
                };
                break;

            case ResourceSubCategory.Relational when filteredResources.Databases.TryGetValue(key, out var db):
                costDetail.ResourceDetails = new Dictionary<string, object>
                {
                    ["instanceName"] = db.InstanceName ?? "",
                    ["vCpu"] = db.VCpu ?? 0,
                    ["memory"] = db.Memory ?? "",
                    ["databaseEngine"] = db.DatabaseEngine ?? "",
                    ["pricePerHour"] = db.PricePerHour ?? 0m
                };
                break;

            case ResourceSubCategory.LoadBalancer when filteredResources.LoadBalancers.TryGetValue(key, out var lb):
                costDetail.ResourceDetails = new Dictionary<string, object>
                {
                    ["name"] = lb.Name ?? "",
                    ["pricePerMonth"] = lb.PricePerMonth ?? 0m
                };
                break;

            case ResourceSubCategory.Monitoring when filteredResources.Monitoring.TryGetValue(key, out var monitoring):
                costDetail.ResourceDetails = new Dictionary<string, object>
                {
                    ["name"] = monitoring.Name ?? "",
                    ["pricePerMonth"] = monitoring.PricePerMonth ?? 0m
                };
                break;
        }

        return costDetail;
    }
}