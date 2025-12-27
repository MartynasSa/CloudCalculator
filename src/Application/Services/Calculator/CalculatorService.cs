using Application.Models.Dtos;
using Application.Models.Enums;

namespace Application.Services.Calculator;

public interface ICalculatorService
{
    (int MinCpu, double MinMemory) GetVirtualMachineSpecs(UsageSize usage);
    (int MinCpu, double MinMemory) GetDatabaseSpecs(UsageSize usage);
    decimal CalculateMonthlyPrice(decimal? pricePerHour);
    decimal CalculateVirtualMachineCost(NormalizedComputeInstanceDto? instance);
    decimal CalculateDatabaseCost(NormalizedDatabaseDto? database);
    decimal CalculateLoadBalancerCost(NormalizedLoadBalancerDto? loadBalancer);
    decimal CalculateMonitoringCost(NormalizedMonitoringDto? monitoring);
}

public class CalculatorService : ICalculatorService
{
    public (int MinCpu, double MinMemory) GetVirtualMachineSpecs(UsageSize usage)
    {
        return usage switch
        {
            UsageSize.Small => (MinCpu: 2, MinMemory: 4),
            UsageSize.Medium => (MinCpu: 4, MinMemory: 8),
            UsageSize.Large => (MinCpu: 8, MinMemory: 16),
            UsageSize.ExtraLarge => (MinCpu: 16, MinMemory: 32),
            _ => throw new ArgumentOutOfRangeException(nameof(usage)),
        };
    }

    public (int MinCpu, double MinMemory) GetDatabaseSpecs(UsageSize usage)
    {
        return usage switch
        {
            UsageSize.Small => (MinCpu: 1, MinMemory: 2),
            UsageSize.Medium => (MinCpu: 2, MinMemory: 4),
            UsageSize.Large => (MinCpu: 4, MinMemory: 8),
            UsageSize.ExtraLarge => (MinCpu: 8, MinMemory: 16),
            _ => throw new ArgumentOutOfRangeException(nameof(usage)),
        };
    }

    public decimal CalculateMonthlyPrice(decimal? pricePerHour)
    {
        if (!pricePerHour.HasValue)
        {
            return 0m;
        }

        return pricePerHour.Value * 730m;
    }

    public decimal CalculateVirtualMachineCost(NormalizedComputeInstanceDto? instance)
    {
        if (instance == null)
        {
            return 0m;
        }

        return CalculateMonthlyPrice(instance.PricePerHour);
    }

    public decimal CalculateDatabaseCost(NormalizedDatabaseDto? database)
    {
        if (database == null)
        {
            return 0m;
        }

        return CalculateMonthlyPrice(database.PricePerHour);
    }

    public decimal CalculateLoadBalancerCost(NormalizedLoadBalancerDto? loadBalancer)
    {
        if (loadBalancer == null)
        {
            return 0m;
        }

        return loadBalancer.PricePerMonth ?? 0m;
    }

    public decimal CalculateMonitoringCost(NormalizedMonitoringDto? monitoring)
    {
        if (monitoring == null)
        {
            return 0m;
        }

        return monitoring.PricePerMonth ?? 0m;
    }
}