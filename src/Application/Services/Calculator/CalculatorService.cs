using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services.Normalization;
using System.Text.Json;

namespace Application.Services.Calculator;

public interface ICalculatorService
{
    Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(CalculationRequest templateDto, CancellationToken ct = default);

    Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(CalculateTemplateRequest templateDto, CancellationToken ct = default);
}

public class CalculatorService(IResourceNormalizationService resourceNormalizationService,
    IPriceProvider priceProvider, ITemplateService templateService) : ICalculatorService
{
    public const int HoursPerMonth = 730;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumMemberConverter() }
    };
    public Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(CalculateTemplateRequest templateDto, CancellationToken ct = default)
    {
        var template = templateService.GetTemplate(templateDto.Template, templateDto.Usage);
        return CalculateCostComparisonsAsync(new CalculationRequest() { 
            Resources = template.Resources, 
            Usage = templateDto.Usage 
        }, ct);
    }

    public async Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(CalculationRequest template, CancellationToken ct = default)
    {
        var resources = await resourceNormalizationService.GetResourcesAsync(ct);
        var result = new TemplateCostComparisonResultDto
        {
            Resources = template.Resources,
            CloudCosts = new List<TemplateCostComparisonResultCloudProviderDto>(),
            Usage = template.Usage
        };

        var cloudProviders = new[] { CloudProvider.AWS, CloudProvider.Azure, CloudProvider.GCP };
        // Get all prices once, grouped by (UsageSize, CloudProvider)
        var filteredResources = priceProvider.GetPrices(resources);

        foreach (var cloudProvider in cloudProviders)
        {
            var costDetails = new List<TemplateCostResourceSubCategoryDetailsDto>();
            decimal totalCost = 0m;

            // Calculate costs based on requested resource subcategories
            foreach (var requestedSubCategory in template.Resources)
            {
                var (cost, resourceDetails) = CalculateResourceCost(filteredResources, template.Usage, cloudProvider, requestedSubCategory);

                totalCost += cost;
                costDetails.Add(new TemplateCostResourceSubCategoryDetailsDto()
                {
                    ResourceSubCategory = requestedSubCategory,
                    Cost = cost,
                    ResourceDetails = resourceDetails != null ? JsonSerializer.Serialize(resourceDetails, JsonOptions) : null
                });
            }

            result.CloudCosts.Add(new TemplateCostComparisonResultCloudProviderDto
            {
                CloudProvider = cloudProvider,
                TotalMonthlyPrice = totalCost,
                CostDetails = costDetails,
            });
        }

        return result;
    }

    private (decimal, object) CalculateResourceCost(FilteredResourcesDto filteredResources, UsageSize usageSize, CloudProvider cloudProvider, ResourceSubCategory subCategory)
    {
        var key = (usageSize, cloudProvider);

        return subCategory switch
        {
            ResourceSubCategory.VirtualMachines when filteredResources.ComputeInstances.TryGetValue(key, out var vm)
                => (CalculateVmCost(vm, CalculatorService.HoursPerMonth), vm),

            ResourceSubCategory.Kubernetes when filteredResources.Kubernetes.TryGetValue(key, out var k8s)
                => (CalculateKubernetesCost(k8s, CalculatorService.HoursPerMonth), k8s),

            ResourceSubCategory.Relational when filteredResources.Databases.TryGetValue(key, out var db)
                => (CalculateDatabaseCost(db, CalculatorService.HoursPerMonth), db),

            ResourceSubCategory.LoadBalancer when filteredResources.LoadBalancers.TryGetValue(key, out var lb)
                => (CalculateLoadBalancerCost(lb), lb),

            ResourceSubCategory.Monitoring when filteredResources.Monitoring.TryGetValue(key, out var monitoring)
                => (CalculateMonitoringCost(monitoring), monitoring),

            _ => (0m, null)
        };
    }

    private decimal CalculateVmCost(NormalizedComputeInstanceDto? vm, int usageHoursPerMonth)
    {
        if (vm?.PricePerHour is null || vm.PricePerHour <= 0)
        {
            return 0m;
        }

        return vm.PricePerHour.Value * usageHoursPerMonth;
    }

    private decimal CalculateKubernetesCost(NormalizedKubernetesDto? kubernetes, int usageHoursPerMonth)
    {
        if (kubernetes?.PricePerHour is null || kubernetes.PricePerHour <= 0)
        {
            return 0m;
        }

        return kubernetes.PricePerHour.Value * usageHoursPerMonth;
    }

    private decimal CalculateDatabaseCost(NormalizedDatabaseDto? database, int usageHoursPerMonth)
    {
        if (database?.PricePerHour is null || database.PricePerHour <= 0)
        {
            return 0m;
        }

        return database.PricePerHour.Value * usageHoursPerMonth;
    }

    private decimal CalculateLoadBalancerCost(NormalizedLoadBalancerDto? loadBalancer)
    {
        if (loadBalancer?.PricePerMonth is null || loadBalancer.PricePerMonth <= 0)
        {
            return 0m;
        }

        return loadBalancer.PricePerMonth.Value;
    }

    private decimal CalculateMonitoringCost(NormalizedMonitoringDto? monitoring)
    {
        if (monitoring?.PricePerMonth is null || monitoring.PricePerMonth <= 0)
        {
            return 0m;
        }

        return monitoring.PricePerMonth.Value;
    }
}