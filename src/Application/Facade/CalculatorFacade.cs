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

        // Get all prices once, grouped by usage size
        var filteredResources = priceProvider.GetPrices(resources);

        foreach (var usageSize in usageSizes)
        {
            var resourcesForSize = filteredResources.CategorizedResources[usageSize];
            var cloudCostsList = new List<TemplateCostComparisonResultCloudProviderDto>();

            foreach (var cloudProvider in cloudProviders)
            {
                var costDetails = new List<TemplateCostResourceSubCategoryDetailsDto>();
                decimal totalCost = 0m;

                // Calculate VM costs if required
                if (template.Resources.Contains(ResourceSubCategory.VirtualMachines))
                {
                    var vm = resourcesForSize.ComputeInstances.FirstOrDefault(v => v.Cloud == cloudProvider);
                    var vmCost = calculatorService.CalculateVmCost(vm, CalculatorService.HoursPerMonth);
                    totalCost += vmCost;

                    if (vmCost > 0 && vm != null)
                    {
                        costDetails.Add(new TemplateCostResourceSubCategoryDetailsDto
                        {
                            Cost = vmCost,
                            ResourceSubCategory = ResourceSubCategory.VirtualMachines,
                            ResourceDetails = new Dictionary<string, object>
                            {
                                ["instanceName"] = vm.InstanceName ?? "",
                                ["vCpu"] = vm.VCpu ?? 0,
                                ["memory"] = vm.Memory ?? "",
                                ["pricePerHour"] = vm.PricePerHour ?? 0m
                            }
                        });
                    }
                }

                // Calculate Database costs if required
                if (template.Resources.Contains(ResourceSubCategory.Relational))
                {
                    var db = resourcesForSize.Databases.FirstOrDefault(d => d.Cloud == cloudProvider);
                    var dbCost = calculatorService.CalculateDatabaseCost(db, CalculatorService.HoursPerMonth);
                    totalCost += dbCost;

                    if (dbCost > 0 && db != null)
                    {
                        costDetails.Add(new TemplateCostResourceSubCategoryDetailsDto
                        {
                            Cost = dbCost,
                            ResourceSubCategory = ResourceSubCategory.Relational,
                            ResourceDetails = new Dictionary<string, object>
                            {
                                ["instanceName"] = db.InstanceName ?? "",
                                ["vCpu"] = db.VCpu ?? 0,
                                ["memory"] = db.Memory ?? "",
                                ["databaseEngine"] = db.DatabaseEngine ?? "",
                                ["pricePerHour"] = db.PricePerHour ?? 0m
                            }
                        });
                    }
                }

                // Calculate LoadBalancer costs if required
                if (template.Resources.Contains(ResourceSubCategory.LoadBalancer))
                {
                    var lb = resourcesForSize.LoadBalancers.FirstOrDefault(l => l.Cloud == cloudProvider);
                    var lbCost = calculatorService.CalculateLoadBalancerCost(lb);
                    totalCost += lbCost;

                    if (lbCost > 0 && lb != null)
                    {
                        costDetails.Add(new TemplateCostResourceSubCategoryDetailsDto
                        {
                            Cost = lbCost,
                            ResourceSubCategory = ResourceSubCategory.LoadBalancer,
                            ResourceDetails = new Dictionary<string, object>
                            {
                                ["name"] = lb.Name ?? "",
                                ["pricePerMonth"] = lb.PricePerMonth ?? 0m
                            }
                        });
                    }
                }

                // Calculate Monitoring costs if required
                if (template.Resources.Contains(ResourceSubCategory.Monitoring))
                {
                    var monitoring = resourcesForSize.Monitoring.FirstOrDefault(m => m.Cloud == cloudProvider);
                    var monitoringCost = calculatorService.CalculateMonitoringCost(monitoring);
                    totalCost += monitoringCost;

                    if (monitoringCost > 0 && monitoring != null)
                    {
                        costDetails.Add(new TemplateCostResourceSubCategoryDetailsDto
                        {
                            Cost = monitoringCost,
                            ResourceSubCategory = ResourceSubCategory.Monitoring,
                            ResourceDetails = new Dictionary<string, object>
                            {
                                ["name"] = monitoring.Name ?? "",
                                ["pricePerMonth"] = monitoring.PricePerMonth ?? 0m
                            }
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
}