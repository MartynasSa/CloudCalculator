using Application.Models.Dtos;
using Application.Models.Enums;

namespace Application.Services.Calculator;

public interface IPriceProvider
{
    NormalizedComputeInstanceDto? GetCheapestComputeInstance(
        List<NormalizedComputeInstanceDto> instances,
        int minCpu,
        double minMemory);

    NormalizedDatabaseDto? GetCheapestDatabase(
        List<NormalizedDatabaseDto> databases,
        int minCpu,
        double minMemory);

    NormalizedLoadBalancerDto? GetLoadBalancer(
        List<NormalizedLoadBalancerDto> loadBalancers,
        CloudProvider cloud);

    NormalizedMonitoringDto? GetMonitoring(
        List<NormalizedMonitoringDto> monitoring,
        CloudProvider cloud);
}

public class PriceProvider : IPriceProvider
{
    public NormalizedComputeInstanceDto? GetCheapestComputeInstance(
        List<NormalizedComputeInstanceDto> instances,
        int minCpu,
        double minMemory)
    {
        return instances
            .Where(i => (i.VCpu ?? 0) >= minCpu)
            .Where(i => (ParseMemory(i.Memory) ?? 0) >= minMemory)
            .Where(i => (i.PricePerHour ?? 0m) > 0m)
            .OrderBy(i => i.PricePerHour ?? decimal.MaxValue)
            .FirstOrDefault();
    }

    public NormalizedDatabaseDto? GetCheapestDatabase(
        List<NormalizedDatabaseDto> databases,
        int minCpu,
        double minMemory)
    {
        return databases
            .Where(i => (i.VCpu ?? 0) >= minCpu)
            .Where(i => (ParseMemory(i.Memory) ?? 0) >= minMemory)
            .Where(i => (i.PricePerHour ?? 0m) > 0m)
            .OrderBy(i => i.PricePerHour ?? decimal.MaxValue)
            .FirstOrDefault();
    }

    public NormalizedLoadBalancerDto? GetLoadBalancer(
        List<NormalizedLoadBalancerDto> loadBalancers,
        CloudProvider cloud)
    {
        return loadBalancers
            .Where(lb => lb.Cloud == cloud)
            .FirstOrDefault();
    }

    public NormalizedMonitoringDto? GetMonitoring(
        List<NormalizedMonitoringDto> monitoring,
        CloudProvider cloud)
    {
        return monitoring
            .Where(m => m.Cloud == cloud)
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
}
