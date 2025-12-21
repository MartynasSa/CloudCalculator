using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Application.Models.Enums;

[TypeConverter(typeof(JsonStringEnumConverter<TemplateType>))]
public enum TemplateType
{
    None = 0,
    [EnumMember(Value = "saas")]
    Saas = 1,
    [EnumMember(Value = "wordpress")]
    WordPress = 2,
    [EnumMember(Value = "rest_api")]
    RestApi = 3,
    [EnumMember(Value = "static_site")]
    StaticSite = 4,
    [EnumMember(Value = "ecommerce")]
    Ecommerce = 5,
}

[TypeConverter(typeof(JsonStringEnumConverter<UsageSize>))]
public enum UsageSize
{
    None = 0,
    [EnumMember(Value = "small")]
    Small = 1,
    [EnumMember(Value = "medium")]
    Medium = 2,
    [EnumMember(Value = "large")]
    Large = 3,
    [EnumMember(Value = "xlarge")]
    ExtraLarge = 4,
}

[TypeConverter(typeof(JsonStringEnumConverter<CloudProvider>))]
public enum CloudProvider
{
    None = 0,
    [EnumMember(Value = "aws")]
    AWS,
    [EnumMember(Value = "azure")]
    Azure,
    [EnumMember(Value = "gcp")]
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