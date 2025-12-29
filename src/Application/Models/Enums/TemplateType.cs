using System.ComponentModel;
using System.Runtime.Serialization;

namespace Application.Models.Enums;

[TypeConverter(typeof(EnumMemberTypeConverter<TemplateType>))]
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
    [EnumMember(Value = "mobile_app_backend")]
    MobileAppBackend = 6,
    [EnumMember(Value = "headless_frontend_api")]
    HeadlessFrontendApi = 7,
    [EnumMember(Value = "data_analytics")]
    DataAnalytics = 8,
    [EnumMember(Value = "machine_learning")]
    MachineLearning = 9,
    [EnumMember(Value = "serverless_event_driven")]
    ServerlessEventDriven = 10,
    [EnumMember(Value = "blank")]
    Blank = 11,
}

[TypeConverter(typeof(EnumMemberTypeConverter<UsageSize>))]
public enum UsageSize
{
    [EnumMember(Value = "small")]
    Small = 1,
    [EnumMember(Value = "medium")]
    Medium = 2,
    [EnumMember(Value = "large")]
    Large = 3,
    [EnumMember(Value = "xlarge")]
    ExtraLarge = 4,
}

[TypeConverter(typeof(EnumMemberTypeConverter<CloudProvider>))]
public enum CloudProvider
{
    [EnumMember(Value = "aws")]
    AWS,
    [EnumMember(Value = "azure")]
    Azure,
    [EnumMember(Value = "gcp")]
    GCP,
}