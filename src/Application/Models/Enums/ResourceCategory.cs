namespace Application.Models.Enums;

public enum ResourceCategory
{
    Unknown,
    Compute,
    Storage,
    Database,
    Databases,
    Network,
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
    BareMetalServers = 104,
    DedicatedHosts = 105,
    Containers = 106,

    // Database (200-299)
    Relational = 200,     // RDS/SQL
    RelationalDatabases = 201, // SQL Databases
    NoSQL = 202,          // DynamoDB
    DatabaseStorage = 203,
    Caching = 204,        // ElastiCache

    // Storage (300-399)
    ObjectStorage = 300,  // S3
    BlobStorage = 301,    // Blob storage
    BlockStorage = 302,   // EBS
    FileStorage = 303,    // EFS/FSx
    Backup = 304,
    PerformanceStorage = 305,

    // Network (400-499)
    VpnGateway = 400,
    LoadBalancer = 401,
    ApiGateway = 402,
    Dns = 403,            // Route53
    ContentDelivery = 404,// CloudFront
    DataTransfer = 405,   // Bandwidth/Egress

    // Analytics & AI (500-599)
    DataWarehouse = 500,  // Redshift
    Streaming = 501,      // Kinesis/MSK
    MachineLearning = 502,// SageMaker

    // Management/Security (600-699)
    Queueing = 600,       // SQS
    Messaging = 601,      // SNS
    Secrets = 602,        // SecretsManager
    Compliance = 603,     // Config/CloudTrail
    Monitoring = 604,
}