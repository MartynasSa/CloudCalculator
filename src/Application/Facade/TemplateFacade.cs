using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services.Normalization;

namespace Application.Facade;

public interface ITemplateFacade
{
    Task<TemplateDto> GetTemplateAsync(TemplateRequest request);
    Task<TemplateCostComparisonDto> CalculateCostComparisonsAsync(CostCalcualtor templateDto, CancellationToken ct = default);
}

public class TemplateFacade(IResourceNormalizationService resourceNormalizationService) : ITemplateFacade
{
    public async Task<TemplateDto> GetTemplateAsync(TemplateRequest request)
    {
        var result = new TemplateDto()
        {
            Template = request.Template,
            Usage = request.Usage,
        };

        switch (request.Template)
        {
            case TemplateType.Saas:
                result.VirtualMachines = await GetVirtualMachinesAsync(request.Usage);
                result.Databases = await GetDatabasesAsync(request.Usage);
                result.LoadBalancers = GetLoadBalancers(request.Usage);
                result.Monitoring = GetMonitoring(request.Usage);
                break;
            case TemplateType.WordPress:
                result.VirtualMachines = await GetVirtualMachinesAsync(request.Usage);
                result.Databases = await GetDatabasesAsync(request.Usage);
                result.LoadBalancers = GetLoadBalancers(request.Usage);
                break;
            case TemplateType.RestApi:
                result.VirtualMachines = await GetVirtualMachinesAsync(request.Usage);
                result.Databases = await GetDatabasesAsync(request.Usage);
                result.LoadBalancers = GetLoadBalancers(request.Usage);
                result.Monitoring = GetMonitoring(request.Usage);
                break;
            case TemplateType.StaticSite:
                result.LoadBalancers = GetLoadBalancers(request.Usage);
                break;
            case TemplateType.Ecommerce:
                result.VirtualMachines = await GetVirtualMachinesAsync(request.Usage);
                result.Databases = await GetDatabasesAsync(request.Usage);
                result.LoadBalancers = GetLoadBalancers(request.Usage);
                result.Monitoring = GetMonitoring(request.Usage);
                break;
            case TemplateType.MobileAppBackend:
                result.VirtualMachines = await GetVirtualMachinesAsync(request.Usage);
                result.Databases = await GetDatabasesAsync(request.Usage);
                result.LoadBalancers = GetLoadBalancers(request.Usage);
                result.Monitoring = GetMonitoring(request.Usage);
                break;
            case TemplateType.HeadlessFrontendApi:
                result.VirtualMachines = await GetVirtualMachinesAsync(request.Usage);
                result.Databases = await GetDatabasesAsync(request.Usage);
                result.LoadBalancers = GetLoadBalancers(request.Usage);
                result.Monitoring = GetMonitoring(request.Usage);
                break;
            case TemplateType.DataAnalytics:
                result.VirtualMachines = await GetVirtualMachinesAsync(request.Usage);
                result.Databases = await GetDatabasesAsync(request.Usage);
                result.LoadBalancers = GetLoadBalancers(request.Usage);
                result.Monitoring = GetMonitoring(request.Usage);
                break;
            case TemplateType.MachineLearning:
                result.VirtualMachines = await GetVirtualMachinesAsync(request.Usage);
                result.LoadBalancers = GetLoadBalancers(request.Usage);
                result.Monitoring = GetMonitoring(request.Usage);
                break;
            case TemplateType.ServerlessEventDriven:
                result.LoadBalancers = GetLoadBalancers(request.Usage);
                break;
            case TemplateType.Blank:
                // Blank template has no resources
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return result;
    }

    private async Task<Dictionary<CloudProvider, TemplateVirtualMachineDto>> GetVirtualMachinesAsync(UsageSize usage)
    {
        var categorized = await resourceNormalizationService.GetResourcesAsync(
            new[] { ResourceCategory.Compute },
            usage);

        var instances = categorized.Categories.TryGetValue(ResourceCategory.Compute, out var computeCategory)
            ? (computeCategory.ComputeInstances ?? [])
            : [];

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
                result[cloud] = new TemplateVirtualMachineDto()
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
        var categorized = await resourceNormalizationService.GetResourcesAsync(
            new[] { ResourceCategory.Database },
            usage);

        var databases = categorized.Categories.TryGetValue(ResourceCategory.Database, out var dbCategory)
            ? (dbCategory.Databases ?? [])
            : [];

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
                result[cloud] = new TemplateDatabaseDto()
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
            return null;

        // Handle formats like "4 GB", "4GB", "4", etc.
        var parts = memory.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && double.TryParse(parts[0], out var value))
            return value;

        return null;
    }

    private static decimal CalculateMonthlyPrice(decimal? pricePerHour)
    {
        if (!pricePerHour.HasValue)
            return 0m;

        // Assuming 730 hours per month (365 days / 12 months * 24 hours)
        return pricePerHour.Value * 730m;
    }

    private Dictionary<CloudProvider, TemplateLoadBalancerDto> GetLoadBalancers(UsageSize usage)
    {
        // Fetch networking resources (load balancers) via the categorization API
        var categorizedTask = resourceNormalizationService.GetResourcesAsync(
            new[] { ResourceCategory.Networking },
            usage);

        var categorized = categorizedTask.GetAwaiter().GetResult();

        var loadBalancers = categorized.Categories.TryGetValue(ResourceCategory.Networking, out var netCategory)
            ? (netCategory.LoadBalancers ?? [])
            : [];

        var result = new Dictionary<CloudProvider, TemplateLoadBalancerDto>();

        foreach (var lb in loadBalancers)
        {
            result[lb.Cloud] = new TemplateLoadBalancerDto()
            {
                Name = lb.Name,
                PricePerMonth = lb.PricePerMonth ?? 0m
            };
        }

        return result;
    }

    private Dictionary<CloudProvider, TemplateMonitoringDto> GetMonitoring(UsageSize usage)
    {
        // Fetch management resources (monitoring) via the categorization API
        var categorizedTask = resourceNormalizationService.GetResourcesAsync(
            new[] { ResourceCategory.Management },
            usage);

        var categorized = categorizedTask.GetAwaiter().GetResult();

        var monitoring = categorized.Categories.TryGetValue(ResourceCategory.Management, out var mgmtCategory)
            ? (mgmtCategory.Monitoring ?? [])
            : [];

        var result = new Dictionary<CloudProvider, TemplateMonitoringDto>();

        foreach (var mon in monitoring)
        {
            result[mon.Cloud] = new TemplateMonitoringDto()
            {
                Name = mon.Name,
                PricePerMonth = mon.PricePerMonth ?? 0m
            };
        }

        return result;
    }

    public async Task<TemplateCostComparisonDto> CalculateCostComparisonsAsync(CostCalcualtor templateDto, CancellationToken ct = default)
    {
        var result = new TemplateCostComparisonDto
        {

        };

        return result;
    }
}