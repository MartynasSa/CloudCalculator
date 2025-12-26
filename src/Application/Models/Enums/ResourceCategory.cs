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

public enum ResourceSubCategory
{
    Unknown = 0,
    Uncategorized = 1,
    
    // Compute (100-199)
    VirtualMachines = 100,
    CloudFunctions = 101, // Lambda
    Kubernetes = 102,     // EKS/GKE
    ContainerInstances = 103,

    // Database (200-299)
    Relational = 200,
    NoSQL = 202,
    DatabaseStorage = 203,
    Caching = 204,

    // Storage (300-399)
    ObjectStorage = 300,
    BlobStorage = 301,
    FileStorage = 303,
    Backup = 304,

    VpnGateway = 400,
    LoadBalancer = 401,
    ApiGateway = 402,
    Dns = 403,
    CDN = 404,

    DataWarehouse = 500,
    Streaming = 501,
    MachineLearning = 502,

    Queueing = 600,
    Messaging = 601,
    Secrets = 602,
    Compliance = 603,
    Monitoring = 604,
}