namespace Application.Models.Enums;

public enum ResourceCategory
{
    Unknown,
    Compute,
    Storage,
    Database,
    Networking,
    Analytics,
    AI_ML,
    Management,
    Security,
    Other
}

public enum ComputeType
{
    None = 0,
    VirtualMachines = 100,
    CloudFunctions = 101,
    Kubernetes = 102,
    ContainerInstances = 103,
}

public enum DatabaseType
{
    None = 0,
    Relational = 200,
    NoSQL = 202,
    DatabaseStorage = 203,
    Caching = 204,
}

public enum StorageType
{
    None = 0,
    ObjectStorage = 300,
    BlobStorage = 301,
    FileStorage = 303,
    Backup = 304,
}

public enum NetworkingType
{
    None = 0,
    VpnGateway = 400,
    LoadBalancer = 401,
    ApiGateway = 402,
    Dns = 403,
    CDN = 404,
}

public enum AnalyticsType
{
    None = 0,
    DataWarehouse = 500,
    Streaming = 501,
    MachineLearning = 502,
}

public enum ManagementType
{
    None = 0,
    Queueing = 600,
    Messaging = 601,
    Secrets = 602,
    Compliance = 603,
    Monitoring = 604,
}

public enum ResourceSubCategory
{
    None = 0,
    Uncategorized = 1,
    
    // Compute (100-199)
    VirtualMachines = ComputeType.VirtualMachines,
    CloudFunctions = ComputeType.CloudFunctions,
    Kubernetes = ComputeType.Kubernetes,
    ContainerInstances = ComputeType.ContainerInstances,

    // Database (200-299)
    Relational = DatabaseType.Relational,
    NoSQL = DatabaseType.NoSQL,
    DatabaseStorage = DatabaseType.DatabaseStorage,
    Caching = DatabaseType.Caching,

    // Storage (300-399)
    ObjectStorage = StorageType.ObjectStorage,
    BlobStorage = StorageType.BlobStorage,
    FileStorage = StorageType.FileStorage,
    Backup = StorageType.Backup,

    // Networking (400-499)
    VpnGateway = NetworkingType.VpnGateway,
    LoadBalancer = NetworkingType.LoadBalancer,
    ApiGateway = NetworkingType.ApiGateway,
    Dns = NetworkingType.Dns,
    CDN = NetworkingType.CDN,

    // Analytics / AI (500-599)
    DataWarehouse = AnalyticsType.DataWarehouse,
    Streaming = AnalyticsType.Streaming,
    MachineLearning = AnalyticsType.MachineLearning,

    // Management / Security (600-699)
    Queueing = ManagementType.Queueing,
    Messaging = ManagementType.Messaging,
    Secrets = ManagementType.Secrets,
    Compliance = ManagementType.Compliance,
    Monitoring = ManagementType.Monitoring,
}