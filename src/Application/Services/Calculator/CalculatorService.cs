using Application.Models.Dtos;
using Application.Models.Enums;

namespace Application.Services.Calculator;

public interface ICalculatorService
{
    decimal CalculateVmCost(NormalizedComputeInstanceDto? vm, int usageHoursPerMonth);
    decimal CalculateDatabaseCost(NormalizedDatabaseDto? database, int usageHoursPerMonth);
    decimal CalculateLoadBalancerCost(NormalizedLoadBalancerDto? loadBalancer);
    decimal CalculateMonitoringCost(NormalizedMonitoringDto? monitoring);
}

public class CalculatorService : ICalculatorService
{
    public const int HoursPerMonth = 730;

    public decimal CalculateVmCost(NormalizedComputeInstanceDto? vm, int usageHoursPerMonth)
    {
        if (vm?.PricePerHour is null || vm.PricePerHour <= 0)
        {
            return 0m;
        }

        return vm.PricePerHour.Value * usageHoursPerMonth;
    }

    public decimal CalculateDatabaseCost(NormalizedDatabaseDto? database, int usageHoursPerMonth)
    {
        if (database?.PricePerHour is null || database.PricePerHour <= 0)
        {
            return 0m;
        }

        return database.PricePerHour.Value * usageHoursPerMonth;
    }

    public decimal CalculateLoadBalancerCost(NormalizedLoadBalancerDto? loadBalancer)
    {
        if (loadBalancer?.PricePerMonth is null || loadBalancer.PricePerMonth <= 0)
        {
            return 0m;
        }

        return loadBalancer.PricePerMonth.Value;
    }

    public decimal CalculateMonitoringCost(NormalizedMonitoringDto? monitoring)
    {
        if (monitoring?.PricePerMonth is null || monitoring.PricePerMonth <= 0)
        {
            return 0m;
        }

        return monitoring.PricePerMonth.Value;
    }
}