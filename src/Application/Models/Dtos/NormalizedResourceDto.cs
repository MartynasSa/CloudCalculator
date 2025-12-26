using Application.Models.Enums;

namespace Application.Models.Dtos;

public class NormalizedComputeInstanceDto
{
    public required CloudProvider Cloud { get; set; }
    public required string InstanceName { get; set; }
    public required string Region { get; set; }
    public int? VCpu { get; set; }
    public string? Memory { get; set; }
    public decimal? PricePerHour { get; set; }
}

public class NormalizedDatabaseDto
{
    public required CloudProvider Cloud { get; set; }
    public required string InstanceName { get; set; }
    public required string Region { get; set; }
    public string? DatabaseEngine { get; set; }
    public int? VCpu { get; set; }
    public string? Memory { get; set; }
    public decimal? PricePerHour { get; set; }
}

public class NormalizedLoadBalancerDto
{
    public required CloudProvider Cloud { get; set; }
    public required string Name { get; set; }
    public decimal? PricePerMonth { get; set; }
}

public class NormalizedMonitoringDto
{
    public required CloudProvider Cloud { get; set; }
    public required string Name { get; set; }
    public decimal? PricePerMonth { get; set; }
}

public class NormalizedResourceDto
{
    public required CloudProvider Cloud { get; set; }
    public required string Service { get; set; }
    public required string Region { get; set; }
    public required ResourceCategory Category { get; set; }
    public required ResourceSubCategory SubCategory { get; set; }
    public string? ProductFamily { get; set; }
    public string? ResourceName { get; set; }
    public decimal? PricePerHour { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
}
