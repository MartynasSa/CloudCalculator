using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services;

namespace Application.Facade;

public interface ITemplateFacade
{
    Task<TemplateDto> GetTemplateAsync(TemplateRequest request);
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
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return result;
    }

    private async Task<Dictionary<CloudProvider, TemplateVirtualMachineDto>> GetVirtualMachinesAsync(UsageSize usage)
    {
        var instances = await resourceNormalizationService.GetNormalizedComputeInstancesAsync();
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
        var databases = await resourceNormalizationService.GetNormalizedDatabasesAsync();
        var result = new Dictionary<CloudProvider, TemplateDatabaseDto>();

        var specs = GetDatabaseSpecs(usage);

        foreach (var cloud in new[] { CloudProvider.AWS, CloudProvider.Azure })
        {
            var cloudDatabases = databases
                .Where(d => d.Cloud == cloud)
                .Where(d => d.DatabaseEngine != null && d.DatabaseEngine.Contains("postgres", StringComparison.OrdinalIgnoreCase))
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
            .Where(i => i.VCpu >= minCpu)
            .Where(i => ParseMemory(i.Memory) >= minMemory)
            .OrderBy(i => i.PricePerHour)
            .FirstOrDefault();
    }

    private static NormalizedDatabaseDto? FindCheapestInstance(
        List<NormalizedDatabaseDto> instances,
        int minCpu,
        double minMemory,
        CloudProvider cloud)
    {
        return instances
            .Where(i => i.VCpu >= minCpu)
            .Where(i => ParseMemory(i.Memory) >= minMemory)
            .OrderBy(i => i.PricePerHour)
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
}