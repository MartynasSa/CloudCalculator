using Application.Models.Enums;

namespace Application.Models.Dtos;

public class CategorizedResourcesDto
{
    public required Dictionary<ResourceCategory, CategoryResourcesDto> Categories { get; set; } = new();
}

public class NormalizedResourceDto<T>
{   
    public required ResourceCategory Category { get; set; }
    public required ResourceSubCategory SubCategory { get; set; }   
    public required T Details { get; set; }
}

public class CategoryResourcesDto
{
    public required ResourceCategory Category { get; set; }
    public List<NormalizedComputeInstanceDto> ComputeInstances { get; set; } = new();
    public List<NormalizedDatabaseDto> Databases { get; set; } = new();
    public List<NormalizedLoadBalancerDto> LoadBalancers { get; set; } = new();
    public List<NormalizedMonitoringDto> Monitoring { get; set; } = new();
    public List<NormalizedResourceDto> Storage { get; set; } = new();
    public List<NormalizedResourceDto> Analytics { get; set; } = new();
    public List<NormalizedResourceDto> AI { get; set; } = new();
    public List<NormalizedResourceDto> Security { get; set; } = new();
    public List<NormalizedResourceDto> ApplicationServices { get; set; } = new();
    public List<NormalizedResourceDto> DeveloperTools { get; set; } = new();
    public List<NormalizedResourceDto> IoT { get; set; } = new();
    public List<NormalizedResourceDto> Data { get; set; } = new();
    public List<NormalizedResourceDto> Integration { get; set; } = new();
    public List<NormalizedResourceDto> Web { get; set; } = new();
    public List<NormalizedResourceDto> EnterpriseApplications { get; set; } = new();
    public List<NormalizedResourceDto> Licensing { get; set; } = new();
    public List<NormalizedResourceDto> Other { get; set; } = new();
    public List<NormalizedResourceDto> Networking { get; set; } = new();
    public List<NormalizedResourceDto> Management { get; set; } = new();
}
