using Application.Models.Enums;

namespace Application.Models.Dtos;

public class TemplateCostComparisonDto
{
    public TemplateType Template { get; set; }
    public List<UsageCostBreakdownDto> UsageBreakdowns { get; set; } = new();
}

public class TemplateCostComparisonResultDto
{
    public List<ResourceSubCategory> Resources { get; set; } = new ();
    public List<TemplateCostComparisonResultCloudProviderDto> CloudCosts { get; set; } = new();
}

public class TemplateCostComparisonResultCloudProviderDto
{
    public CloudProvider CloudProvider { get; set; }
    public UsageSize UsageSize { get; set; }
    public decimal TotalMonthlyPrice { get; set; }
    public List<TemplateCostResourceSubCategoryDetailsDto> CostDetails { get; set; } = new ();
}

public class TemplateCostResourceSubCategoryDetailsDto 
{
    public decimal Cost { get; set; }
    public ResourceSubCategory ResourceSubCategory { get; set; }
    public Dictionary<string,object> ResourceDetails { get; set; } = new();
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
    public CostBreakdownDto Breakdown { get; set; } = new();
}

public class CostBreakdownDto
{
    public decimal? VirtualMachinesCost { get; set; }
    public decimal? DatabasesCost { get; set; }
    public decimal? LoadBalancersCost { get; set; }
    public decimal? MonitoringCost { get; set; }
    public decimal? StorageCost { get; set; }
    public decimal? CloudFunctionsCost { get; set; }
    public decimal? KubernetesCost { get; set; }
    public decimal? ContainerInstancesCost { get; set; }
    public decimal? CachingCost { get; set; }
    public decimal? VpnGatewayCost { get; set; }
    public decimal? ApiGatewayCost { get; set; }
    public decimal? DnsCost { get; set; }
    public decimal? CdnCost { get; set; }
    public decimal? DataWarehouseCost { get; set; }
    public decimal? StreamingCost { get; set; }
    public decimal? MachineLearningCost { get; set; }
    public decimal? QueueingCost { get; set; }
    public decimal? MessagingCost { get; set; }
    public decimal? SecretsCost { get; set; }
    public decimal? ComplianceCost { get; set; }
}
