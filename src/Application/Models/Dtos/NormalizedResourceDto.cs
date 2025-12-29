using Application.Models.Enums;

namespace Application.Models.Dtos;

public class NormalizedComputeInstanceDto : NormalizedResource
{
    public required string InstanceName { get; set; }
    public required string Region { get; set; }
    public int? VCpu { get; set; }
    public string? Memory { get; set; }
    public decimal? PricePerHour { get; set; }
}

public class NormalizedDatabaseDto : NormalizedResource
{
    public required string InstanceName { get; set; }
    public required string Region { get; set; }
    public string? DatabaseEngine { get; set; }
    public int? VCpu { get; set; }
    public string? Memory { get; set; }
    public decimal? PricePerHour { get; set; }
}

public class NormalizedLoadBalancerDto : NormalizedResource
{
    public required string Name { get; set; }
    public decimal? PricePerMonth { get; set; }
}

public class NormalizedCloudFunctionDto : NormalizedResource
{
    public required string FunctionName { get; set; }
    public required string Region { get; set; }
    public decimal? PricePerRequest { get; set; }
    public decimal? PricePerGbSecond { get; set; }
}

public class NormalizedKubernetesDto : NormalizedResource
{
    public required string ClusterName { get; set; }
    public required string Region { get; set; }
    public int? NodeCount { get; set; }
    public string? NodeType { get; set; }
    public decimal? PricePerHour { get; set; }
}

public class NormalizedApiGatewayDto : NormalizedResource
{
    public required string Name { get; set; }
    public required string Region { get; set; }
    public decimal? PricePerRequest { get; set; }
    public decimal? PricePerMonth { get; set; }
}

public class NormalizedBlobStorageDto : NormalizedResource
{
    public required string Name { get; set; }
    public required string Region { get; set; }
    public string? StorageClass { get; set; }
    public decimal? PricePerGbMonth { get; set; }
    public decimal? PricePerRequest { get; set; }
}

public class NormalizedResourceDto : NormalizedResource
{
    public required string Service { get; set; }
    public required string Region { get; set; }
    public string? ProductFamily { get; set; }
    public string? ResourceName { get; set; }
    public decimal? PricePerHour { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
}

public class NormalizedContainerInstanceDto : NormalizedResource
{
    public required string ContainerName { get; set; }
    public required string Region { get; set; }
    public decimal? PricePerHour { get; set; }
}

public class NormalizedDataWarehouseDto : NormalizedResource
{
    public required string WarehouseName { get; set; }
    public required string Region { get; set; }
    public int? NodeCount { get; set; }
    public string? NodeType { get; set; }
    public decimal? PricePerHour { get; set; }
}

public class NormalizedCachingDto : NormalizedResource
{
    public required string CacheName { get; set; }
    public required string Region { get; set; }
    public string? CacheEngine { get; set; }
    public int? VCpu { get; set; }
    public string? Memory { get; set; }
    public decimal? PricePerHour { get; set; }
}

public class NormalizedMessagingDto : NormalizedResource
{
    public required string MessagingService { get; set; }
    public required string Region { get; set; }
    public string? MessageType { get; set; }
    public decimal? PricePerMonth { get; set; }
}

public class NormalizedQueuingDto : NormalizedResource
{
    public required string QueuingService { get; set; }
    public required string Region { get; set; }
    public string? OperationType { get; set; }
    public string? QueueType { get; set; }
    public decimal? PricePerMonth { get; set; }
}

public class NormalizedMonitoringDto : NormalizedResource
{
    public required string MonitoringService { get; set; }
    public required string Region { get; set; }
    public string? MetricType { get; set; }
    public decimal? PricePerMonth { get; set; }
}

public class NormalizedCdnDto : NormalizedResource
{
    public required string CdnName { get; set; }
    public required string Region { get; set; }
    public string? EdgeLocation { get; set; }
    public decimal? PricePerGbOut { get; set; }
    public decimal? PricePerRequest { get; set; }
}

public class NormalizedIdentityManagementDto : NormalizedResource
{
    public required string ServiceName { get; set; }
    public required string Region { get; set; }
    public string? OperationType { get; set; }
    public decimal? PricePerUser { get; set; }
    public decimal? PricePerRequest { get; set; }
    public decimal? PricePerAuthentication { get; set; }
}

public class NormalizedWebApplicationFirewallDto : NormalizedResource
{
    public required string FirewallName { get; set; }
    public required string Region { get; set; }
    public string? FirewallType { get; set; }
    public string? SkuTier { get; set; }
    public decimal? PricePerHour { get; set; }
    public decimal? PricePerGb { get; set; }
    public decimal? PricePerRule { get; set; }
}

public class NormalizedBlockStorageDto : NormalizedResource
{
    public required string Name { get; set; }
    public required string Region { get; set; }
    public string? VolumeType { get; set; }
    public string? StorageMedia { get; set; }
    public string? MaxIopsVolume { get; set; }
    public string? MaxVolumeSize { get; set; }
    public string? MaxThroughputVolume { get; set; }
    public decimal? PricePerGbMonth { get; set; }
    public decimal? PricePerIops { get; set; }
    public decimal? PricePerSnapshot { get; set; }
}