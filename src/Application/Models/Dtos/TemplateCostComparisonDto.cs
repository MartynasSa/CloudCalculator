using Application.Models.Enums;

namespace Application.Models.Dtos;

public class TemplateCostComparisonDto
{
    public TemplateType Template { get; set; }
    public List<UsageCostBreakdownDto> UsageBreakdowns { get; set; } = new();
}

public class UsageCostBreakdownDto
{
    public UsageSize Usage { get; set; }
    public Dictionary<CloudProvider, CloudProviderCostDto> CloudProviderCosts { get; set; } = new();
}

public class CloudProviderCostDto
{
    public decimal TotalMonthlyPrice { get; set; }
    public CostBreakdownDto Breakdown { get; set; } = new();
}

public class CostBreakdownDto
{
    public decimal? VirtualMachinesCost { get; set; }
    public decimal? DatabasesCost { get; set; }
    public decimal? LoadBalancersCost { get; set; }
    public decimal? MonitoringCost { get; set; }
}
