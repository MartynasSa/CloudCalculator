namespace Application.Models.Enums;

public enum TemplateType
{
    None = 0,
    Saas = 1,
}

public enum UsageSize
{
    None = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
    ExtraLarge = 4,
}

public enum CloudProvider
{
    None = 0,
    AWS,
    Azure,
    GCP,
}

public enum CloudCategory
{
    None = 0,
    Compute,
    Database,
    Networking,
    Monitoring,
}

public enum ComputeType
{
    None = 0,
    VM = 1,
}

public enum DatabaseType
{
    None = 0,
    PostgreSQL = 1,
}

public enum NetworkingType
{
    None = 0,
    LoadBalancer = 1,
}

public enum MonitoringType
{
    None = 0,
    ApplicationMonitoring = 1,
}