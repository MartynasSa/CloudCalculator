using Application.Models.Enums;

namespace Application.Models.Dtos;

public class CategorizedResourcesDto
{
    public required Dictionary<ResourceCategory, CategoryResourcesDto> Categories { get; set; } = new();
}

public class CategoryResourcesDto
{
    public required ResourceCategory Category { get; set; }
    public List<NormalizedComputeInstanceDto> ComputeInstances { get; set; } = new();
    public List<NormalizedDatabaseDto> Databases { get; set; } = new();
    public List<NormalizedLoadBalancerDto> LoadBalancers { get; set; } = new();
    public List<NormalizedMonitoringDto> Monitoring { get; set; } = new();
}
