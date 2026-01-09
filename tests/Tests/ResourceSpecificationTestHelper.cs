using Application.Models.Dtos;
using Application.Models.Enums;

namespace Tests;

public static class ResourceSpecificationTestHelper
{
    public static ResourceSpecificationDto VirtualMachines() => new()
    {
        Category = ResourceCategory.Compute,
        ComputeType = ComputeType.VirtualMachines
    };

    public static ResourceSpecificationDto CloudFunctions() => new()
    {
        Category = ResourceCategory.Compute,
        ComputeType = ComputeType.CloudFunctions
    };

    public static ResourceSpecificationDto Kubernetes() => new()
    {
        Category = ResourceCategory.Compute,
        ComputeType = ComputeType.Kubernetes
    };

    public static ResourceSpecificationDto ContainerInstances() => new()
    {
        Category = ResourceCategory.Compute,
        ComputeType = ComputeType.ContainerInstances
    };

    public static ResourceSpecificationDto Relational() => new()
    {
        Category = ResourceCategory.Database,
        DatabaseType = DatabaseType.Relational
    };

    public static ResourceSpecificationDto NoSQL() => new()
    {
        Category = ResourceCategory.Database,
        DatabaseType = DatabaseType.NoSQL
    };

    public static ResourceSpecificationDto Caching() => new()
    {
        Category = ResourceCategory.Database,
        DatabaseType = DatabaseType.Caching
    };

    public static ResourceSpecificationDto ObjectStorage() => new()
    {
        Category = ResourceCategory.Storage,
        StorageType = StorageType.ObjectStorage
    };

    public static ResourceSpecificationDto BlobStorage() => new()
    {
        Category = ResourceCategory.Storage,
        StorageType = StorageType.BlobStorage
    };

    public static ResourceSpecificationDto BlockStorage() => new()
    {
        Category = ResourceCategory.Storage,
        StorageType = StorageType.BlockStorage
    };

    public static ResourceSpecificationDto FileStorage() => new()
    {
        Category = ResourceCategory.Storage,
        StorageType = StorageType.FileStorage
    };

    public static ResourceSpecificationDto Backup() => new()
    {
        Category = ResourceCategory.Storage,
        StorageType = StorageType.Backup
    };

    public static ResourceSpecificationDto VpnGateway() => new()
    {
        Category = ResourceCategory.Networking,
        NetworkingType = NetworkingType.VpnGateway
    };

    public static ResourceSpecificationDto LoadBalancer() => new()
    {
        Category = ResourceCategory.Networking,
        NetworkingType = NetworkingType.LoadBalancer
    };

    public static ResourceSpecificationDto ApiGateway() => new()
    {
        Category = ResourceCategory.Networking,
        NetworkingType = NetworkingType.ApiGateway
    };

    public static ResourceSpecificationDto Dns() => new()
    {
        Category = ResourceCategory.Networking,
        NetworkingType = NetworkingType.Dns
    };

    public static ResourceSpecificationDto CDN() => new()
    {
        Category = ResourceCategory.Networking,
        NetworkingType = NetworkingType.CDN
    };

    public static ResourceSpecificationDto DataWarehouse() => new()
    {
        Category = ResourceCategory.Analytics,
        AnalyticsType = AnalyticsType.DataWarehouse
    };

    public static ResourceSpecificationDto Streaming() => new()
    {
        Category = ResourceCategory.Analytics,
        AnalyticsType = AnalyticsType.Streaming
    };

    public static ResourceSpecificationDto MachineLearning() => new()
    {
        Category = ResourceCategory.Analytics,
        AnalyticsType = AnalyticsType.MachineLearning
    };

    public static ResourceSpecificationDto Queueing() => new()
    {
        Category = ResourceCategory.Management,
        ManagementType = ManagementType.Queueing
    };

    public static ResourceSpecificationDto Messaging() => new()
    {
        Category = ResourceCategory.Management,
        ManagementType = ManagementType.Messaging
    };

    public static ResourceSpecificationDto Secrets() => new()
    {
        Category = ResourceCategory.Management,
        ManagementType = ManagementType.Secrets
    };

    public static ResourceSpecificationDto Compliance() => new()
    {
        Category = ResourceCategory.Management,
        ManagementType = ManagementType.Compliance
    };

    public static ResourceSpecificationDto Monitoring() => new()
    {
        Category = ResourceCategory.Management,
        ManagementType = ManagementType.Monitoring
    };

    public static ResourceSpecificationDto WebApplicationFirewall() => new()
    {
        Category = ResourceCategory.Security,
        SecurityType = SecurityType.WebApplicationFirewall
    };

    public static ResourceSpecificationDto IdentityManagement() => new()
    {
        Category = ResourceCategory.Security,
        SecurityType = SecurityType.IdentityManagement
    };

    public static ResourceSpecificationDto AIServices() => new()
    {
        Category = ResourceCategory.AI,
        AIType = AIType.AIServices
    };

    public static ResourceSpecificationDto MLPlatforms() => new()
    {
        Category = ResourceCategory.AI,
        AIType = AIType.MLPlatforms
    };

    public static ResourceSpecificationDto IntelligentSearch() => new()
    {
        Category = ResourceCategory.AI,
        AIType = AIType.IntelligentSearch
    };
}
