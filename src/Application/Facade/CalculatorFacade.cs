using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services.Normalization;

namespace Application.Facade;

public interface ICalculatorFacade
{
    Task<TemplateCostComparisonDto> CalculateCostComparisonsAsync(TemplateDto templateDto, CancellationToken ct = default);
}

public class CalculatorFacade(IResourceNormalizationService resourceNormalizationService) : ICalculatorFacade
{
    public async Task<TemplateCostComparisonDto> CalculateCostComparisonsAsync(TemplateDto request, CancellationToken ct = default)
    {
        var resources = await resourceNormalizationService.GetResourcesAsync(ct);

        var result = new TemplateCostComparisonDto
        {
            Template = request.Template
        };

        var subcategories = GetSubCategoriesForTemplate(request.Template);
        var usageSizes = new[] { UsageSize.Small, UsageSize.Medium, UsageSize.Large };

        foreach (var usage in usageSizes)
        {
            var usageBreakdown = new UsageCostBreakdownDto
            {
                Usage = usage
            };

            foreach (var cloud in new[] { CloudProvider.AWS, CloudProvider.Azure, CloudProvider.GCP })
            {
                var cloudCost = new CloudProviderCostDto();

                foreach (var subCategory in subcategories)
                {
                    var cost = await CalculateSubCategoryCostAsync(subCategory, cloud, usage, resources, ct);

                    if (cost > 0)
                    {
                        cloudCost.Details[subCategory] = cost;
                    }

                    SetBreakdownCost(cloudCost.Breakdown, subCategory, cost);
                }

                cloudCost.TotalMonthlyPrice = cloudCost.Details.Values.Sum();

                usageBreakdown.CloudProviderCosts[cloud] = cloudCost;
            }

            result.UsageBreakdowns.Add(usageBreakdown);
        }

        return result;
    }

    private static Task<decimal> CalculateSubCategoryCostAsync(
        ResourceSubCategory subCategory,
        CloudProvider cloud,
        UsageSize usage,
        CategorizedResourcesDto resources,
        CancellationToken ct)
    {
        var category = MapSubCategoryToCategory(subCategory);

        if (category == ResourceCategory.Unknown)
        {
            return Task.FromResult(0m);
        }

        try
        {
            var cost = subCategory switch
            {
                ResourceSubCategory.VirtualMachines => CalculateVirtualMachineCost(resources, cloud, usage),
                ResourceSubCategory.CloudFunctions => CalculateCloudFunctionsCost(resources, cloud, usage),
                ResourceSubCategory.Kubernetes => CalculateKubernetesCost(resources, cloud, usage),
                ResourceSubCategory.ContainerInstances => CalculateContainerInstancesCost(resources, cloud, usage),
                ResourceSubCategory.Relational => CalculateDatabaseCost(resources, cloud, usage),
                ResourceSubCategory.NoSQL => CalculateDatabaseCost(resources, cloud, usage),
                ResourceSubCategory.DatabaseStorage => CalculateDatabaseStorageCost(resources, cloud, usage),
                ResourceSubCategory.Caching => CalculateCachingCost(resources, cloud, usage),
                ResourceSubCategory.ObjectStorage => CalculateStorageCost(resources, cloud, usage),
                ResourceSubCategory.BlobStorage => CalculateStorageCost(resources, cloud, usage),
                ResourceSubCategory.FileStorage => CalculateStorageCost(resources, cloud, usage),
                ResourceSubCategory.Backup => CalculateBackupCost(resources, cloud, usage),
                ResourceSubCategory.VpnGateway => CalculateVpnGatewayCost(resources, cloud, usage),
                ResourceSubCategory.LoadBalancer => CalculateLoadBalancerCost(resources, cloud, usage),
                ResourceSubCategory.ApiGateway => CalculateApiGatewayCost(resources, cloud, usage),
                ResourceSubCategory.Dns => CalculateDnsCost(resources, cloud, usage),
                ResourceSubCategory.CDN => CalculateCdnCost(resources, cloud, usage),
                ResourceSubCategory.DataWarehouse => CalculateDataWarehouseCost(resources, cloud, usage),
                ResourceSubCategory.Streaming => CalculateStreamingCost(resources, cloud, usage),
                ResourceSubCategory.MachineLearning => CalculateMachineLearningCost(resources, cloud, usage),
                ResourceSubCategory.Queueing => CalculateQueueingCost(resources, cloud, usage),
                ResourceSubCategory.Messaging => CalculateMessagingCost(resources, cloud, usage),
                ResourceSubCategory.Secrets => CalculateSecretsCost(resources, cloud, usage),
                ResourceSubCategory.Compliance => CalculateComplianceCost(resources, cloud, usage),
                ResourceSubCategory.Monitoring => CalculateMonitoringCost(resources, cloud, usage),
                _ => 0m
            };

            return Task.FromResult(cost);
        }
        catch
        {
            return Task.FromResult(0m);
        }
    }

    private async Task<Dictionary<CloudProvider, TemplateVirtualMachineDto>> GetVirtualMachinesAsync(UsageSize usage)
    {
        var categorized = await resourceNormalizationService.GetResourcesAsync();

        var instances = categorized.ComputeInstances ?? [];

        var result = new Dictionary<CloudProvider, TemplateVirtualMachineDto>();
        var specs = GetVirtualMachineSpecs(usage);

        foreach (var cloud in new[] { CloudProvider.AWS, CloudProvider.Azure, CloudProvider.GCP })
        {
            var cloudInstances = instances
                .Where(i => i.Cloud == cloud)
                .Where(i => i.VCpu.HasValue && i.Memory != null)
                .ToList();

            var matchedInstance = FindCheapestInstance(
                cloudInstances,
                specs.MinCpu,
                specs.MinMemory,
                cloud);

            if (matchedInstance != null)
            {
                result[cloud] = new TemplateVirtualMachineDto
                {
                    InstanceName = matchedInstance.InstanceName,
                    CpuCores = matchedInstance.VCpu ?? specs.MinCpu,
                    Memory = ParseMemory(matchedInstance.Memory) ?? specs.MinMemory,
                    PricePerMonth = CalculateMonthlyPrice(matchedInstance.PricePerHour),
                };
            }
        }

        return result;
    }

    private async Task<Dictionary<CloudProvider, TemplateDatabaseDto>> GetDatabasesAsync(UsageSize usage)
    {
        var categorized = await resourceNormalizationService.GetResourcesAsync();

        var databases = categorized.Databases ?? [];

        var result = new Dictionary<CloudProvider, TemplateDatabaseDto>();
        var specs = GetDatabaseSpecs(usage);

        foreach (var cloud in new[] { CloudProvider.AWS, CloudProvider.Azure, CloudProvider.GCP })
        {
            var cloudDatabases = databases
                .Where(d => d.Cloud == cloud)
                .Where(d => d.DatabaseEngine != null)
                .Where(d => d.VCpu.HasValue && d.Memory != null)
                .ToList();

            var matchedDatabase = FindCheapestInstance(
                cloudDatabases,
                specs.MinCpu,
                specs.MinMemory,
                cloud);

            if (matchedDatabase != null)
            {
                result[cloud] = new TemplateDatabaseDto
                {
                    InstanceName = matchedDatabase.InstanceName,
                    CpuCores = matchedDatabase.VCpu ?? specs.MinCpu,
                    Memory = ParseMemory(matchedDatabase.Memory) ?? specs.MinMemory,
                    DatabaseEngine = matchedDatabase.DatabaseEngine,
                    PricePerMonth = CalculateMonthlyPrice(matchedDatabase.PricePerHour),
                };
            }
        }

        return result;
    }

    private static (int MinCpu, double MinMemory) GetVirtualMachineSpecs(UsageSize usage)
    {
        return usage switch
        {
            UsageSize.Small => (MinCpu: 2, MinMemory: 4),
            UsageSize.Medium => (MinCpu: 4, MinMemory: 8),
            UsageSize.Large => (MinCpu: 8, MinMemory: 16),
            _ => throw new ArgumentOutOfRangeException(nameof(usage)),
        };
    }

    private static (int MinCpu, double MinMemory) GetDatabaseSpecs(UsageSize usage)
    {
        return usage switch
        {
            UsageSize.Small => (MinCpu: 1, MinMemory: 2),
            UsageSize.Medium => (MinCpu: 2, MinMemory: 4),
            UsageSize.Large => (MinCpu: 4, MinMemory: 8),
            _ => throw new ArgumentOutOfRangeException(nameof(usage)),
        };
    }

    private static NormalizedComputeInstanceDto? FindCheapestInstance(
        List<NormalizedComputeInstanceDto> instances,
        int minCpu,
        double minMemory,
        CloudProvider cloud)
    {
        return instances
            .Where(i => (i.VCpu ?? 0) >= minCpu)
            .Where(i => (ParseMemory(i.Memory) ?? 0) >= minMemory)
            .Where(i => (i.PricePerHour ?? 0m) > 0m)
            .OrderBy(i => i.PricePerHour ?? decimal.MaxValue)
            .FirstOrDefault();
    }

    private static NormalizedDatabaseDto? FindCheapestInstance(
        List<NormalizedDatabaseDto> instances,
        int minCpu,
        double minMemory,
        CloudProvider cloud)
    {
        return instances
            .Where(i => (i.VCpu ?? 0) >= minCpu)
            .Where(i => (ParseMemory(i.Memory) ?? 0) >= minMemory)
            .Where(i => (i.PricePerHour ?? 0m) > 0m)
            .OrderBy(i => i.PricePerHour ?? decimal.MaxValue)
            .FirstOrDefault();
    }

    private static double? ParseMemory(string? memory)
    {
        if (string.IsNullOrWhiteSpace(memory))
        {
            return null;
        }

        var parts = memory.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && double.TryParse(parts[0], out var value))
        {
            return value;
        }

        return null;
    }

    private static decimal CalculateMonthlyPrice(decimal? pricePerHour)
    {
        if (!pricePerHour.HasValue)
        {
            return 0m;
        }

        return pricePerHour.Value * 730m;
    }

    private Dictionary<CloudProvider, TemplateLoadBalancerDto> GetLoadBalancers(UsageSize usage)
    {
        var categorizedTask = resourceNormalizationService.GetResourcesAsync();

        var categorized = categorizedTask.GetAwaiter().GetResult();

        var loadBalancers = categorized.LoadBalancers ?? [];

        var result = new Dictionary<CloudProvider, TemplateLoadBalancerDto>();

        foreach (var lb in loadBalancers)
        {
            result[lb.Cloud] = new TemplateLoadBalancerDto
            {
                Name = lb.Name,
                PricePerMonth = lb.PricePerMonth ?? 0m
            };
        }

        return result;
    }

    private Dictionary<CloudProvider, TemplateMonitoringDto> GetMonitoring(UsageSize usage)
    {
        var categorizedTask = resourceNormalizationService.GetResourcesAsync();

        var categorized = categorizedTask.GetAwaiter().GetResult();

        var monitoring = categorized.Monitoring ?? [];

        var result = new Dictionary<CloudProvider, TemplateMonitoringDto>();

        foreach (var mon in monitoring)
        {
            result[mon.Cloud] = new TemplateMonitoringDto
            {
                Name = mon.Name,
                PricePerMonth = mon.PricePerMonth ?? 0m
            };
        }

        return result;
    }

    private static List<ResourceSubCategory> GetSubCategoriesForTemplate(TemplateType template)
    {
        return template switch
        {
            TemplateType.Saas => new List<ResourceSubCategory>
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            },
            TemplateType.WordPress => new List<ResourceSubCategory>
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer
            },
            TemplateType.RestApi => new List<ResourceSubCategory>
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            },
            TemplateType.StaticSite => new List<ResourceSubCategory>
            {
                ResourceSubCategory.LoadBalancer
            },
            TemplateType.Ecommerce => new List<ResourceSubCategory>
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            },
            TemplateType.MobileAppBackend => new List<ResourceSubCategory>
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            },
            TemplateType.HeadlessFrontendApi => new List<ResourceSubCategory>
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            },
            TemplateType.DataAnalytics => new List<ResourceSubCategory>
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            },
            TemplateType.MachineLearning => new List<ResourceSubCategory>
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            },
            TemplateType.ServerlessEventDriven => new List<ResourceSubCategory>
            {
                ResourceSubCategory.LoadBalancer
            },
            TemplateType.Blank => new List<ResourceSubCategory>(),
            _ => new List<ResourceSubCategory>()
        };
    }

    private static ResourceCategory MapSubCategoryToCategory(ResourceSubCategory subCategory)
    {
        return subCategory switch
        {
            ResourceSubCategory.VirtualMachines => ResourceCategory.Compute,
            ResourceSubCategory.CloudFunctions => ResourceCategory.Compute,
            ResourceSubCategory.Kubernetes => ResourceCategory.Compute,
            ResourceSubCategory.ContainerInstances => ResourceCategory.Compute,
            ResourceSubCategory.Relational => ResourceCategory.Database,
            ResourceSubCategory.NoSQL => ResourceCategory.Database,
            ResourceSubCategory.DatabaseStorage => ResourceCategory.Database,
            ResourceSubCategory.Caching => ResourceCategory.Database,
            ResourceSubCategory.ObjectStorage => ResourceCategory.Storage,
            ResourceSubCategory.BlobStorage => ResourceCategory.Storage,
            ResourceSubCategory.FileStorage => ResourceCategory.Storage,
            ResourceSubCategory.Backup => ResourceCategory.Storage,
            ResourceSubCategory.VpnGateway => ResourceCategory.Networking,
            ResourceSubCategory.LoadBalancer => ResourceCategory.Networking,
            ResourceSubCategory.ApiGateway => ResourceCategory.Networking,
            ResourceSubCategory.Dns => ResourceCategory.Networking,
            ResourceSubCategory.CDN => ResourceCategory.Networking,
            ResourceSubCategory.DataWarehouse => ResourceCategory.Analytics,
            ResourceSubCategory.Streaming => ResourceCategory.Analytics,
            ResourceSubCategory.MachineLearning => ResourceCategory.AI_ML,
            ResourceSubCategory.Queueing => ResourceCategory.Management,
            ResourceSubCategory.Messaging => ResourceCategory.Management,
            ResourceSubCategory.Secrets => ResourceCategory.Security,
            ResourceSubCategory.Compliance => ResourceCategory.Security,
            ResourceSubCategory.Monitoring => ResourceCategory.Management,
            _ => ResourceCategory.Unknown
        };
    }

    private static void SetBreakdownCost(CostBreakdownDto breakdown, ResourceSubCategory subCategory, decimal cost)
    {
        switch (subCategory)
        {
            case ResourceSubCategory.VirtualMachines:
                breakdown.VirtualMachinesCost = (breakdown.VirtualMachinesCost ?? 0) + cost;
                break;
            case ResourceSubCategory.Relational:
            case ResourceSubCategory.NoSQL:
            case ResourceSubCategory.DatabaseStorage:
                breakdown.DatabasesCost = (breakdown.DatabasesCost ?? 0) + cost;
                break;
            case ResourceSubCategory.LoadBalancer:
                breakdown.LoadBalancersCost = (breakdown.LoadBalancersCost ?? 0) + cost;
                break;
            case ResourceSubCategory.Monitoring:
                breakdown.MonitoringCost = (breakdown.MonitoringCost ?? 0) + cost;
                break;
            case ResourceSubCategory.ObjectStorage:
            case ResourceSubCategory.BlobStorage:
            case ResourceSubCategory.FileStorage:
            case ResourceSubCategory.Backup:
                breakdown.StorageCost = (breakdown.StorageCost ?? 0) + cost;
                break;
            case ResourceSubCategory.CloudFunctions:
                breakdown.CloudFunctionsCost = (breakdown.CloudFunctionsCost ?? 0) + cost;
                break;
            case ResourceSubCategory.Kubernetes:
                breakdown.KubernetesCost = (breakdown.KubernetesCost ?? 0) + cost;
                break;
            case ResourceSubCategory.ContainerInstances:
                breakdown.ContainerInstancesCost = (breakdown.ContainerInstancesCost ?? 0) + cost;
                break;
            case ResourceSubCategory.Caching:
                breakdown.CachingCost = (breakdown.CachingCost ?? 0) + cost;
                break;
            case ResourceSubCategory.VpnGateway:
                breakdown.VpnGatewayCost = (breakdown.VpnGatewayCost ?? 0) + cost;
                break;
            case ResourceSubCategory.ApiGateway:
                breakdown.ApiGatewayCost = (breakdown.ApiGatewayCost ?? 0) + cost;
                break;
            case ResourceSubCategory.Dns:
                breakdown.DnsCost = (breakdown.DnsCost ?? 0) + cost;
                break;
            case ResourceSubCategory.CDN:
                breakdown.CdnCost = (breakdown.CdnCost ?? 0) + cost;
                break;
            case ResourceSubCategory.DataWarehouse:
                breakdown.DataWarehouseCost = (breakdown.DataWarehouseCost ?? 0) + cost;
                break;
            case ResourceSubCategory.Streaming:
                breakdown.StreamingCost = (breakdown.StreamingCost ?? 0) + cost;
                break;
            case ResourceSubCategory.MachineLearning:
                breakdown.MachineLearningCost = (breakdown.MachineLearningCost ?? 0) + cost;
                break;
            case ResourceSubCategory.Queueing:
                breakdown.QueueingCost = (breakdown.QueueingCost ?? 0) + cost;
                break;
            case ResourceSubCategory.Messaging:
                breakdown.MessagingCost = (breakdown.MessagingCost ?? 0) + cost;
                break;
            case ResourceSubCategory.Secrets:
                breakdown.SecretsCost = (breakdown.SecretsCost ?? 0) + cost;
                break;
            case ResourceSubCategory.Compliance:
                breakdown.ComplianceCost = (breakdown.ComplianceCost ?? 0) + cost;
                break;
        }
    }

    private static decimal CalculateVirtualMachineCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        var specs = GetVirtualMachineSpecs(usage);
        var instances = resources.ComputeInstances
            .Where(i => i.Cloud == cloud)
            .Where(i => i.VCpu.HasValue && i.Memory != null)
            .ToList();

        var matchedInstance = FindCheapestInstance(instances, specs.MinCpu, specs.MinMemory, cloud);
        if (matchedInstance != null)
        {
            return CalculateMonthlyPrice(matchedInstance.PricePerHour);
        }

        return 0m;
    }

    private static decimal CalculateCloudFunctionsCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateKubernetesCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateContainerInstancesCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateDatabaseCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        var specs = GetDatabaseSpecs(usage);
        var databases = resources.Databases
            .Where(d => d.Cloud == cloud)
            .Where(d => d.VCpu.HasValue && d.Memory != null)
            .ToList();

        var matchedDatabase = FindCheapestInstance(databases, specs.MinCpu, specs.MinMemory, cloud);
        if (matchedDatabase != null)
        {
            return CalculateMonthlyPrice(matchedDatabase.PricePerHour);
        }

        return 0m;
    }

    private static decimal CalculateDatabaseStorageCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateCachingCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateStorageCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateBackupCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateVpnGatewayCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateLoadBalancerCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        var loadBalancers = resources.LoadBalancers
            .Where(lb => lb.Cloud == cloud)
            .ToList();

        if (loadBalancers.Any())
        {
            return loadBalancers.First().PricePerMonth ?? 0m;
        }

        return 0m;
    }

    private static decimal CalculateApiGatewayCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateDnsCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateCdnCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateDataWarehouseCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateStreamingCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateMachineLearningCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateQueueingCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateMessagingCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateSecretsCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateComplianceCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        return 0m;
    }

    private static decimal CalculateMonitoringCost(CategorizedResourcesDto resources, CloudProvider cloud, UsageSize usage)
    {
        var monitoring = resources.Monitoring
            .Where(m => m.Cloud == cloud)
            .ToList();

        if (monitoring.Any())
        {
            return monitoring.First().PricePerMonth ?? 0m;
        }

        return 0m;
    }
}