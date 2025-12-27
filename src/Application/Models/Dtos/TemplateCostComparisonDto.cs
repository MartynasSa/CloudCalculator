using Application.Models.Enums;

namespace Application.Models.Dtos;


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
