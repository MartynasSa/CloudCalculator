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
        return CalculateCostComparisonsAsync(new CalculationRequest()
        {
            Resources = template.Resources,
            Usage = templateDto.Usage
        }, ct);
    }

    public async Task<TemplateCostComparisonResultDto> CalculateCostComparisonsAsync(CalculationRequest template, CancellationToken ct = default)
    {
        var resources = await resourceNormalizationService.GetResourcesAsync(ct);
        var requestedSubCategories = EnumerateRequestedSubCategories(template.Resources).ToList();

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
            foreach (var requestedSubCategory in requestedSubCategories)
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

    private static IEnumerable<ResourceSubCategory> EnumerateRequestedSubCategories(ResourcesDto resources)
    {
        if (resources is null)
        {
            yield break;
        }

        foreach (var compute in resources.Computes.Where(c => c != ComputeType.None))
        {
            yield return (ResourceSubCategory)compute;
        }

        foreach (var database in resources.Databases.Where(d => d != DatabaseType.None))
        {
            yield return (ResourceSubCategory)database;
        }

        foreach (var storage in resources.Storages.Where(s => s != StorageType.None))
        {
            yield return (ResourceSubCategory)storage;
        }

        foreach (var network in resources.Networks.Where(n => n != NetworkingType.None))
        {
            yield return (ResourceSubCategory)network;
        }

        foreach (var analytic in resources.Analytics.Where(a => a != AnalyticsType.None))
        {
            yield return (ResourceSubCategory)analytic;
        }

        foreach (var management in resources.Management.Where(m => m != ManagementType.None))
        {
            yield return (ResourceSubCategory)management;
        }

        foreach (var security in resources.Security.Where(s => s != SecurityType.None))
        {
            yield return (ResourceSubCategory)security;
        }

        foreach (var ai in resources.AI.Where(a => a != AIType.None))
        {
            yield return (ResourceSubCategory)ai;
        }
    }

    private (decimal, object) CalculateResourceCost(FilteredResourcesDto filteredResources, UsageSize usageSize, CloudProvider cloudProvider, ResourceSubCategory subCategory)
    {
        var key = (usageSize, cloudProvider);

        return subCategory switch
        {
            // Compute
            ResourceSubCategory.VirtualMachines when filteredResources.ComputeInstances.TryGetValue(key, out var vm)
                => (CalculateHourlyCost(vm.PricePerHour, HoursPerMonth), vm),

            ResourceSubCategory.CloudFunctions when filteredResources.CloudFunctions.TryGetValue(key, out var cf)
                => (CalculateCloudFunctionCost(cf, usageSize), cf),

            ResourceSubCategory.Kubernetes when filteredResources.Kubernetes.TryGetValue(key, out var k8s)
                => (CalculateHourlyCost(k8s.PricePerHour, HoursPerMonth), k8s),

            ResourceSubCategory.ContainerInstances when filteredResources.Networking.TryGetValue(key, out var ci)
                => (CalculateHourlyCost((ci as dynamic)?.PricePerHour, HoursPerMonth), ci),

            // Database
            ResourceSubCategory.Relational when filteredResources.Databases.TryGetValue(key, out var db)
                => (CalculateHourlyCost(db.PricePerHour, HoursPerMonth), db),

            ResourceSubCategory.NoSQL when filteredResources.Databases.TryGetValue(key, out var nosql)
                => (CalculateHourlyCost(nosql.PricePerHour, HoursPerMonth), nosql),

            ResourceSubCategory.Caching when filteredResources.Caching.TryGetValue(key, out var cache)
                => (CalculateHourlyCost(cache.PricePerHour, HoursPerMonth), cache),

            // Storage
            ResourceSubCategory.BlobStorage when filteredResources.BlobStorage.TryGetValue(key, out var blob)
                => (CalculateBlobStorageCost(blob, usageSize), blob),

            ResourceSubCategory.ObjectStorage when filteredResources.BlobStorage.TryGetValue(key, out var obj)
                => (CalculateBlobStorageCost(obj, usageSize), obj),

            // Networking
            ResourceSubCategory.LoadBalancer when filteredResources.LoadBalancers.TryGetValue(key, out var lb)
                => (CalculateMonthlyCost(lb.PricePerMonth), lb),

            ResourceSubCategory.ApiGateway when filteredResources.ApiGateways.TryGetValue(key, out var api)
                => (CalculateApiGatewayCost(api, usageSize), api),

            ResourceSubCategory.CDN when filteredResources.CDN.TryGetValue(key, out var cdn)
                => (CalculateCdnCost(cdn, usageSize), cdn),

            // Analytics
            ResourceSubCategory.DataWarehouse when filteredResources.DataWarehouses.TryGetValue(key, out var dw)
                => (CalculateHourlyCost(dw.PricePerHour, HoursPerMonth), dw),

            // Management
            ResourceSubCategory.Monitoring when filteredResources.Monitoring.TryGetValue(key, out var monitoring)
                => (CalculateMonthlyCost(monitoring.PricePerMonth), monitoring),

            ResourceSubCategory.Messaging when filteredResources.Messaging.TryGetValue(key, out var msg)
                => (CalculateMonthlyCost(msg.PricePerMonth), msg),

            ResourceSubCategory.Queueing when filteredResources.Queueing.TryGetValue(key, out var queue)
                => (CalculateMonthlyCost(queue.PricePerMonth), queue),

            // Security
            ResourceSubCategory.IdentityManagement when filteredResources.IdentityManagement.TryGetValue(key, out var idm)
                => (CalculateIdentityManagementCost(idm, usageSize), idm),

            ResourceSubCategory.WebApplicationFirewall when filteredResources.WebApplicationFirewall.TryGetValue(key, out var waf)
                => (CalculateWebApplicationFirewallCost(waf, usageSize), waf),

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

    private decimal CalculateHourlyCost(decimal? pricePerHour, int hours)
    {
        if (pricePerHour is null || pricePerHour <= 0)
        {
            return 0m;
        }

        return pricePerHour.Value * hours;
    }

    private decimal CalculateMonthlyCost(decimal? pricePerMonth)
    {
        if (pricePerMonth is null || pricePerMonth <= 0)
        {
            return 0m;
        }

        return pricePerMonth.Value;
    }

    private decimal CalculateCloudFunctionCost(NormalizedCloudFunctionDto? cloudFunction, UsageSize usageSize)
    {
        if (cloudFunction is null)
        {
            return 0m;
        }

        // Estimate based on usage size
        var estimatedRequests = usageSize switch
        {
            UsageSize.Small => 1_000_000m,      // 1M requests/month
            UsageSize.Medium => 10_000_000m,    // 10M requests/month
            UsageSize.Large => 50_000_000m,     // 50M requests/month
            UsageSize.ExtraLarge => 200_000_000m, // 200M requests/month
            _ => 1_000_000m
        };

        if (cloudFunction.PricePerRequest is not null && cloudFunction.PricePerRequest > 0)
        {
            return cloudFunction.PricePerRequest.Value * estimatedRequests;
        }

        if (cloudFunction.PricePerGbSecond is not null && cloudFunction.PricePerGbSecond > 0)
        {
            // Assume 128MB memory, 200ms average execution time
            var gbSeconds = estimatedRequests * 0.128m * 0.2m;
            return cloudFunction.PricePerGbSecond.Value * gbSeconds;
        }

        return 0m;
    }

    private decimal CalculateBlobStorageCost(NormalizedBlobStorageDto? storage, UsageSize usageSize)
    {
        if (storage is null)
        {
            return 0m;
        }

        // Estimate storage based on usage size
        var estimatedGb = usageSize switch
        {
            UsageSize.Small => 100m,      // 100 GB
            UsageSize.Medium => 500m,     // 500 GB
            UsageSize.Large => 2000m,     // 2 TB
            UsageSize.ExtraLarge => 10000m, // 10 TB
            _ => 100m
        };

        if (storage.PricePerGbMonth is not null && storage.PricePerGbMonth > 0)
        {
            return storage.PricePerGbMonth.Value * estimatedGb;
        }

        return 0m;
    }

    private decimal CalculateApiGatewayCost(NormalizedApiGatewayDto? apiGateway, UsageSize usageSize)
    {
        if (apiGateway is null)
        {
            return 0m;
        }

        if (apiGateway.PricePerMonth is not null && apiGateway.PricePerMonth > 0)
        {
            return apiGateway.PricePerMonth.Value;
        }

        if (apiGateway.PricePerRequest is not null && apiGateway.PricePerRequest > 0)
        {
            // Estimate based on usage size
            var estimatedRequests = usageSize switch
            {
                UsageSize.Small => 1_000_000m,
                UsageSize.Medium => 10_000_000m,
                UsageSize.Large => 50_000_000m,
                UsageSize.ExtraLarge => 200_000_000m,
                _ => 1_000_000m
            };

            return apiGateway.PricePerRequest.Value * estimatedRequests;
        }

        return 0m;
    }

    private decimal CalculateCdnCost(NormalizedCdnDto? cdn, UsageSize usageSize)
    {
        if (cdn is null)
        {
            return 0m;
        }

        // Estimate data transfer based on usage size
        var estimatedGbOut = usageSize switch
        {
            UsageSize.Small => 100m,      // 100 GB/month
            UsageSize.Medium => 500m,     // 500 GB/month
            UsageSize.Large => 2000m,     // 2 TB/month
            UsageSize.ExtraLarge => 10000m, // 10 TB/month
            _ => 100m
        };

        if (cdn.PricePerGbOut is not null && cdn.PricePerGbOut > 0)
        {
            return cdn.PricePerGbOut.Value * estimatedGbOut;
        }

        return 0m;
    }

    private decimal CalculateIdentityManagementCost(NormalizedIdentityManagementDto? idm, UsageSize usageSize)
    {
        if (idm is null)
        {
            return 0m;
        }

        // Estimate users based on usage size
        var estimatedUsers = usageSize switch
        {
            UsageSize.Small => 1000m,      // 1K users
            UsageSize.Medium => 10000m,    // 10K users
            UsageSize.Large => 50000m,     // 50K users
            UsageSize.ExtraLarge => 200000m, // 200K users
            _ => 1000m
        };

        if (idm.PricePerUser is not null && idm.PricePerUser > 0)
        {
            return idm.PricePerUser.Value * estimatedUsers;
        }

        if (idm.PricePerRequest is not null && idm.PricePerRequest > 0)
        {
            var estimatedRequests = estimatedUsers * 100m; // Assume 100 requests per user per month
            return idm.PricePerRequest.Value * estimatedRequests;
        }

        if (idm.PricePerAuthentication is not null && idm.PricePerAuthentication > 0)
        {
            var estimatedAuths = estimatedUsers * 50m; // Assume 50 authentications per user per month
            return idm.PricePerAuthentication.Value * estimatedAuths;
        }

        return 0m;
    }

    private decimal CalculateWebApplicationFirewallCost(NormalizedWebApplicationFirewallDto? waf, UsageSize usageSize)
    {
        if (waf is null)
        {
            return 0m;
        }

        if (waf.PricePerHour is not null && waf.PricePerHour > 0)
        {
            return waf.PricePerHour.Value * HoursPerMonth;
        }

        // Estimate data transfer for WAF
        var estimatedGb = usageSize switch
        {
            UsageSize.Small => 100m,
            UsageSize.Medium => 500m,
            UsageSize.Large => 2000m,
            UsageSize.ExtraLarge => 10000m,
            _ => 100m
        };

        if (waf.PricePerGb is not null && waf.PricePerGb > 0)
        {
            return waf.PricePerGb.Value * estimatedGb;
        }

        if (waf.PricePerRule is not null && waf.PricePerRule > 0)
        {
            var estimatedRules = 10m; // Assume 10 rules
            return waf.PricePerRule.Value * estimatedRules;
        }

        return 0m;
    }
}