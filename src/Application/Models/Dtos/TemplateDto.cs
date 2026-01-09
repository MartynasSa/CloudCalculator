using Application.Models.Enums;

namespace Application.Models.Dtos;

public class TemplateDto
{
    public TemplateType Template { get; set; }
    public ResourcesDto Resources { get; set; } = new();
}

public class CalculationRequest
{
    public UsageSize Usage { get; set; }
    public ResourcesDto Resources { get; set; } = new();
}

public class CalculateTemplateRequest
{
    public TemplateType Template { get; set; }
    public UsageSize Usage { get; set; }
}

public class ResourcesDto
{
    public List<ComputeType> Computes { get; set; } = new();

    public List<DatabaseType> Databases { get; set; } = new();

    public List<StorageType> Storages { get; set; } = new();

    public List<NetworkingType> Networks { get; set; } = new();

    public List<AnalyticsType> Analytics { get; set; } = new();

    public List<ManagementType> Management { get; set; } = new();

    public List<SecurityType> Security { get; set; } = new();

    public List<AIType> AI { get; set; } = new();
}