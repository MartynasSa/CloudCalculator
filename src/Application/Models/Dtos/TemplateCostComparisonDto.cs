using Application.Models.Enums;

namespace Application.Models.Dtos;


public class TemplateCostComparisonResultDto
{
    public UsageSize Usage { get; set; }
    public List<ResourceSpecificationDto> Resources { get; set; } = new ();
    public List<TemplateCostComparisonResultCloudProviderDto> CloudCosts { get; set; } = new();
}

public class TemplateCostComparisonResultCloudProviderDto
{
    public CloudProvider CloudProvider { get; set; }
    public decimal TotalMonthlyPrice { get; set; }
    public List<TemplateCostResourceSubCategoryDetailsDto> CostDetails { get; set; } = new ();
}

public class TemplateCostResourceSubCategoryDetailsDto 
{
    public decimal Cost { get; set; }
    public ResourceSpecificationDto ResourceSpecification { get; set; } = new();
    public string? ResourceDetails { get; set; }
}
