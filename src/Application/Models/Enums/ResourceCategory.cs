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
    Unknown,
    Uncategorized,
    
    // Compute
    VirtualMachines,
    CloudFunctions, // Lambda
    Kubernetes,     // EKS/GKE
    ContainerInstances,

    // Storage
    ObjectStorage,  // S3
    BlobStorage,    // Blob storage
    BlockStorage,   // EBS
    FileStorage,    // EFS/FSx
    Backup,

    // Database
    Relational,     // RDS/SQL
    RelationalDatabases, // SQL Databases
    NoSQL,          // DynamoDB
    DatabaseStorage,
    Caching,        // ElastiCache

    // Network
    VpnGateway,
    LoadBalancer,
    ApiGateway,
    Dns,            // Route53
    ContentDelivery,// CloudFront
    DataTransfer,   // Bandwidth/Egress

    // Analytics & AI
    DataWarehouse,  // Redshift
    Streaming,      // Kinesis/MSK
    MachineLearning,// SageMaker

    // Management/Security
    Queueing,       // SQS
    Messaging,      // SNS
    Secrets,        // SecretsManager
    Compliance,     // Config/CloudTrail
    Monitoring,
    
    // Compute additional subcategories for testing
    BareMetalServers,
    DedicatedHosts,
    Containers,
    
    // Storage additional subcategories for testing
    PerformanceStorage,
}