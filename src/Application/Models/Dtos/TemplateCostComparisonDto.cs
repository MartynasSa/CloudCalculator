using Application.Models.Enums;

namespace Application.Models.Dtos;

public class TemplateCostComparisonDto
{
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
    public Dictionary<ResourceSubCategory, decimal> Details { get; set; } = new();
}
