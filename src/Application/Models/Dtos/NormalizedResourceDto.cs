namespace Application.Models.Dtos;

public class NormalizedComputeInstanceDto
{
    public required string Cloud { get; set; }
    public required string InstanceName { get; set; }
    public required string Region { get; set; }
    public int? VCpu { get; set; }
    public string? Memory { get; set; }
    public decimal? PricePerHour { get; set; }
}

public class NormalizedDatabaseDto
{
    public required string Cloud { get; set; }
    public required string InstanceName { get; set; }
    public required string Region { get; set; }
    public string? DatabaseEngine { get; set; }
    public int? VCpu { get; set; }
    public string? Memory { get; set; }
    public decimal? PricePerHour { get; set; }
}
