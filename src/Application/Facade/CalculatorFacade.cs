using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services;
using Application.Services.Calculator;
using Application.Services.Normalization;

namespace Application.Facade;

public interface ICalculatorFacade
{
    Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(TemplateDto templateDto, CancellationToken ct = default);
}

public class CalculatorFacade(
    IResourceNormalizationService resourceNormalizationService,
    IPriceProvider priceProvider,
    ICalculatorService calculatorService,
    ITemplateService templateService) : ICalculatorFacade
{
    public async Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(TemplateDto request, CancellationToken ct = default)
    {
        // If resources aren't provided in the request, get them from the template service
        var template = request.Resources.Count > 0 
            ? request 
            : templateService.GetTemplate(request.Template);

        var resources = await resourceNormalizationService.GetResourcesAsync(ct);
        var result = new TemplateCostComparisonResultDto
        {
            Resources = template.Resources
        };

        var cloudProviders = new[] { CloudProvider.AWS, CloudProvider.Azure, CloudProvider.GCP };
        var usageSizes = new[] { UsageSize.Small, UsageSize.Medium, UsageSize.Large, UsageSize.ExtraLarge };

        foreach (var cloudProvider in cloudProviders)
        {
            foreach (var usageSize in usageSizes)
            {
                var costDetails = new List<TemplateCostResourceSubCategoryDetailsDto>();
                decimal totalCost = 0m;

                // Calculate VM costs if required
                if (template.Resources.Contains(ResourceSubCategory.VirtualMachines))
                {
                    var vmPrices = priceProvider.GetVm(resources.ComputeInstances, cloudProvider);
                    var vm = vmPrices[usageSize];
                    var vmCost = calculatorService.CalculateVmCost(vm, CalculatorService.HoursPerMonth);
                    totalCost += vmCost;

                    if (vmCost > 0)
                    {
                        costDetails.Add(new TemplateCostResourceSubCategoryDetailsDto
                        {
                            Cost = vmCost,
                            ResourceSubCategory = ResourceSubCategory.VirtualMachines,
                            ResourceDetails = new Dictionary<string, object>
                            {
                                ["instanceName"] = vm?.InstanceName ?? "",
                                ["vCpu"] = vm?.VCpu ?? 0,
                                ["memory"] = vm?.Memory ?? "",
                                ["pricePerHour"] = vm?.PricePerHour ?? 0m
                            }
                        });
                    }
                }

                // Calculate Database costs if required
                if (template.Resources.Contains(ResourceSubCategory.Relational))
                {
                    var dbPrices = priceProvider.GetDatabase(resources.Databases, cloudProvider, 2, 4);
                    var db = dbPrices[usageSize];
                    var dbCost = calculatorService.CalculateDatabaseCost(db, CalculatorService.HoursPerMonth);
                    totalCost += dbCost;

                    if (dbCost > 0)
                    {
                        costDetails.Add(new TemplateCostResourceSubCategoryDetailsDto
                        {
                            Cost = dbCost,
                            ResourceSubCategory = ResourceSubCategory.Relational,
                            ResourceDetails = new Dictionary<string, object>
                            {
                                ["instanceName"] = db?.InstanceName ?? "",
                                ["vCpu"] = db?.VCpu ?? 0,
                                ["memory"] = db?.Memory ?? "",
                                ["databaseEngine"] = db?.DatabaseEngine ?? "",
                                ["pricePerHour"] = db?.PricePerHour ?? 0m
                            }
                        });
                    }
                }

                // Calculate LoadBalancer costs if required
                if (template.Resources.Contains(ResourceSubCategory.LoadBalancer))
                {
                    var lbPrices = priceProvider.GetLoadBalancer(resources.LoadBalancers, cloudProvider);
                    var lb = lbPrices[usageSize];
                    var lbCost = calculatorService.CalculateLoadBalancerCost(lb);
                    totalCost += lbCost;

                    if (lbCost > 0)
                    {
                        costDetails.Add(new TemplateCostResourceSubCategoryDetailsDto
                        {
                            Cost = lbCost,
                            ResourceSubCategory = ResourceSubCategory.LoadBalancer,
                            ResourceDetails = new Dictionary<string, object>
                            {
                                ["name"] = lb?.Name ?? "",
                                ["pricePerMonth"] = lb?.PricePerMonth ?? 0m
                            }
                        });
                    }
                }

                // Calculate Monitoring costs if required
                if (template.Resources.Contains(ResourceSubCategory.Monitoring))
                {
                    var monitoringPrices = priceProvider.GetMonitoring(resources.Monitoring, cloudProvider);
                    var monitoring = monitoringPrices[usageSize];
                    var monitoringCost = calculatorService.CalculateMonitoringCost(monitoring);
                    totalCost += monitoringCost;

                    if (monitoringCost > 0)
                    {
                        costDetails.Add(new TemplateCostResourceSubCategoryDetailsDto
                        {
                            Cost = monitoringCost,
                            ResourceSubCategory = ResourceSubCategory.Monitoring,
                            ResourceDetails = new Dictionary<string, object>
                            {
                                ["name"] = monitoring?.Name ?? "",
                                ["pricePerMonth"] = monitoring?.PricePerMonth ?? 0m
                            }
                        });
                    }
                }

                result.CloudCosts.Add(new TemplateCostComparisonResultCloudProviderDto
                {
                    CloudProvider = cloudProvider,
                    UsageSize = usageSize,
                    TotalMonthlyPrice = totalCost,
                    CostDetails = costDetails
                });
            }
        }

        return result;
    }
}