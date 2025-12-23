namespace Application.Models.Enums;

public enum ResourceCategory
{
    None = 0,
    Compute,
    Databases,
    Storage,
    Networking,
    Analytics,
    AI,
    Security,
    ApplicationServices,
    Management,
    DeveloperTools,
    IoT,
    Data,
    Integration,
    Web,
    EnterpriseApplications,
    Licensing,
    Other
}

// Compute subcategories: 100-199
public enum ComputeSubCategory
{
    VirtualMachines = 100,
    BareMetalServers = 101,
    DedicatedHosts = 102,
    Containers = 103
}

// Database subcategories: 200-299
public enum DatabaseSubCategory
{
    RelationalDatabases = 200,
    DatabaseStorage = 201
}

// Storage subcategories: 300-399
public enum StorageSubCategory
{
    BlockStorage = 300,
    PerformanceStorage = 301
}

// Networking subcategories: 400-499
public enum NetworkingSubCategory
{
    NetworkServices = 400,
    IPAddresses = 401
}

// Analytics subcategories: 500-599
public enum AnalyticsSubCategory
{
    DataAnalytics = 500,
    DataLakes = 501
}

// AI subcategories: 600-699
public enum AISubCategory
{
    MachineLearning = 600
}

// Security subcategories: 700-799
public enum SecuritySubCategory
{
    SecurityServices = 700,
    VulnerabilityScanning = 701,
    WebApplicationFirewall = 702
}

// Application Services subcategories: 800-899
public enum ApplicationServicesSubCategory
{
    ManagedServices = 800,
    ContactCenter = 801,
    CommunicationServices = 802
}

// Management subcategories: 900-999
public enum ManagementSubCategory
{
    CloudManagement = 900,
    Operations = 901
}

// Developer Tools subcategories: 1000-1099
public enum DeveloperToolsSubCategory
{
    Development = 1000
}

// IoT subcategories: 1100-1199
public enum IoTSubCategory
{
    IoTServices = 1100
}

// Data subcategories: 1200-1299
public enum DataSubCategory
{
    DataServices = 1200
}

// Integration subcategories: 1300-1399
public enum IntegrationSubCategory
{
    IntegrationServices = 1300,
    FileTransfer = 1301
}

// Web subcategories: 1400-1499
public enum WebSubCategory
{
    WebServices = 1400
}

// Enterprise Applications subcategories: 1500-1599
public enum EnterpriseApplicationsSubCategory
{
    BusinessApplications = 1500,
    ContentServices = 1501
}

// Licensing subcategories: 1600-1699
public enum LicensingSubCategory
{
    SoftwareLicenses = 1600
}

// Other subcategories: 1700-1799
public enum OtherSubCategory
{
    Uncategorized = 1700
}

// Combined ResourceSubCategory enum that includes all subcategories from all categories
public enum ResourceSubCategory
{
    None = 0,
    
    // Compute (100-199)
    VirtualMachines = 100,
    BareMetalServers = 101,
    DedicatedHosts = 102,
    Containers = 103,
    
    // Databases (200-299)
    RelationalDatabases = 200,
    DatabaseStorage = 201,
    
    // Storage (300-399)
    BlockStorage = 300,
    PerformanceStorage = 301,
    
    // Networking (400-499)
    NetworkServices = 400,
    IPAddresses = 401,
    
    // Analytics (500-599)
    DataAnalytics = 500,
    DataLakes = 501,
    
    // AI (600-699)
    MachineLearning = 600,
    
    // Security (700-799)
    SecurityServices = 700,
    VulnerabilityScanning = 701,
    WebApplicationFirewall = 702,
    
    // Application Services (800-899)
    ManagedServices = 800,
    ContactCenter = 801,
    CommunicationServices = 802,
    
    // Management (900-999)
    CloudManagement = 900,
    Operations = 901,
    
    // Developer Tools (1000-1099)
    Development = 1000,
    
    // IoT (1100-1199)
    IoTServices = 1100,
    
    // Data (1200-1299)
    DataServices = 1200,
    
    // Integration (1300-1399)
    IntegrationServices = 1300,
    FileTransfer = 1301,
    
    // Web (1400-1499)
    WebServices = 1400,
    
    // Enterprise Applications (1500-1599)
    BusinessApplications = 1500,
    ContentServices = 1501,
    
    // Licensing (1600-1699)
    SoftwareLicenses = 1600,
    
    // Other (1700-1799)
    Uncategorized = 1700
}
