using Application.Models.Enums;

namespace Application.Models.Dtos;

public class CategorizedResourcesDto
{
    public List<NormalizedComputeInstanceDto> ComputeInstances { get; set; } = new();
    public List<NormalizedCloudFunctionDto> CloudFunctions { get; set; } = new();
    public List<NormalizedKubernetesDto> Kubernetes { get; set; } = new();
    public List<NormalizedDatabaseDto> Databases { get; set; } = new();
    public List<NormalizedApiGatewayDto> ApiGateways { get; set; } = new();
    public List<NormalizedLoadBalancerDto> LoadBalancers { get; set; } = new();
    public List<NormalizedBlobStorageDto> BlobStorage { get; set; } = new();
    public List<NormalizedMonitoringDto> Monitoring { get; set; } = new();
    public List<NormalizedResourceDto> Networking { get; set; } = new();
}

public class FilteredResourcesDto
{
    public Dictionary<UsageSize, CategorizedResourcesDto> CategorizedResources { get; set; } = new();
}

public class NormalizedResource
{   
    public required CloudProvider Cloud { get; set; }
    public required ResourceCategory Category { get; set; }
    public required ResourceSubCategory SubCategory { get; set; }   
}
