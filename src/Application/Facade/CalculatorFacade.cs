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

                // Get resource for this specific combination
                if (filteredResources.Resources.TryGetValue((usageSize, cloudProvider), out var resource))
                {
                    // Calculate costs based on requested resource subcategories
                    foreach (var requestedSubCategory in template.Resources)
                    {
                        var cost = CalculateResourceCost(resource, requestedSubCategory);
                        
                        if (cost > 0m)
                        {
                            totalCost += cost;
                            costDetails.Add(CreateCostDetail(resource, requestedSubCategory, cost));
                        }
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

    private decimal CalculateResourceCost(NormalizedResource resource, ResourceSubCategory subCategory)
    {
        return subCategory switch
        {
            ResourceSubCategory.VirtualMachines when resource is NormalizedComputeInstanceDto vm 
                => calculatorService.CalculateVmCost(vm, CalculatorService.HoursPerMonth),
            
            ResourceSubCategory.Relational when resource is NormalizedDatabaseDto db 
                => calculatorService.CalculateDatabaseCost(db, CalculatorService.HoursPerMonth),
            
            ResourceSubCategory.LoadBalancer when resource is NormalizedLoadBalancerDto lb 
                => calculatorService.CalculateLoadBalancerCost(lb),
            
            ResourceSubCategory.Monitoring when resource is NormalizedMonitoringDto monitoring 
                => calculatorService.CalculateMonitoringCost(monitoring),
            
            _ => 0m
        };
    }

    private static TemplateCostResourceSubCategoryDetailsDto CreateCostDetail(
        NormalizedResource resource, 
        ResourceSubCategory subCategory, 
        decimal cost)
    {
        var costDetail = new TemplateCostResourceSubCategoryDetailsDto
        {
            Cost = cost,
            ResourceSubCategory = subCategory,
            ResourceDetails = new Dictionary<string, object>()
        };

        switch (resource)
        {
            case NormalizedComputeInstanceDto vm:
                costDetail.ResourceDetails = new Dictionary<string, object>
                {
                    ["instanceName"] = vm.InstanceName ?? "",
                    ["vCpu"] = vm.VCpu ?? 0,
                    ["memory"] = vm.Memory ?? "",
                    ["pricePerHour"] = vm.PricePerHour ?? 0m
                };
                break;

            case NormalizedDatabaseDto db:
                costDetail.ResourceDetails = new Dictionary<string, object>
                {
                    ["instanceName"] = db.InstanceName ?? "",
                    ["vCpu"] = db.VCpu ?? 0,
                    ["memory"] = db.Memory ?? "",
                    ["databaseEngine"] = db.DatabaseEngine ?? "",
                    ["pricePerHour"] = db.PricePerHour ?? 0m
                };
                break;

            case NormalizedLoadBalancerDto lb:
                costDetail.ResourceDetails = new Dictionary<string, object>
                {
                    ["name"] = lb.Name ?? "",
                    ["pricePerMonth"] = lb.PricePerMonth ?? 0m
                };
                break;

            case NormalizedMonitoringDto monitoring:
                costDetail.ResourceDetails = new Dictionary<string, object>
                {
                    ["name"] = monitoring.Name ?? "",
                    ["pricePerMonth"] = monitoring.PricePerMonth ?? 0m
                };
                break;

            case NormalizedResourceDto resourceDto:
                costDetail.ResourceDetails = new Dictionary<string, object>
                {
                    ["service"] = resourceDto.Service,
                    ["pricePerHour"] = resourceDto.PricePerHour ?? 0m
                };
                break;
        }

        return costDetail;
    }
}