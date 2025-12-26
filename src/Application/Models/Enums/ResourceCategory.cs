namespace Application.Models.Enums;

public enum ResourceCategory
{
    Unknown,
    Compute,
    Storage,
    Database,
    Network,
    Analytics,
    AI_ML,
    Management,
    Security
}

public enum ResourceSubCategory
{
    Unknown,
    // Compute
    VirtualMachines,
    CloudFunctions, // Lambda
    Kubernetes,     // EKS/GKE
    ContainerInstances,

    // Storage
    ObjectStorage,  // S3
    BlockStorage,   // EBS
    FileStorage,    // EFS/FSx
    Backup,

    // Database
    Relational,     // RDS/SQL
    NoSQL,          // DynamoDB
    Caching,        // ElastiCache

    // Network
    VpnGateway,
    LoadBalancer,
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
}