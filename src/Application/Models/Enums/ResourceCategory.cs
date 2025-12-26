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
    VirtualMachines = ComputeSubCategory.VirtualMachines,
    BareMetalServers = ComputeSubCategory.BareMetalServers,
    DedicatedHosts = ComputeSubCategory.DedicatedHosts,
    Containers = ComputeSubCategory.Containers,

    // Databases (200-299)
    RelationalDatabases = DatabaseSubCategory.RelationalDatabases,
    DatabaseStorage = DatabaseSubCategory.DatabaseStorage,

    // Storage (300-399)
    BlockStorage = StorageSubCategory.BlockStorage,
    PerformanceStorage = StorageSubCategory.PerformanceStorage,

    // Networking (400-499)
    NetworkServices = NetworkingSubCategory.NetworkServices,
    IPAddresses = NetworkingSubCategory.IPAddresses,

    // Analytics (500-599)
    DataAnalytics = AnalyticsSubCategory.DataAnalytics,
    DataLakes = AnalyticsSubCategory.DataLakes,

    // AI (600-699)
    MachineLearning = AISubCategory.MachineLearning,

    // Security (700-799)
    SecurityServices = SecuritySubCategory.SecurityServices,
    VulnerabilityScanning = SecuritySubCategory.VulnerabilityScanning,
    WebApplicationFirewall = SecuritySubCategory.WebApplicationFirewall,

    // Application Services (800-899)
    ManagedServices = ApplicationServicesSubCategory.ManagedServices,
    ContactCenter = ApplicationServicesSubCategory.ContactCenter,
    CommunicationServices = ApplicationServicesSubCategory.CommunicationServices,

    // Management (900-999)
    CloudManagement = ManagementSubCategory.CloudManagement,
    Operations = ManagementSubCategory.Operations,

    // Developer Tools (1000-1099)
    Development = DeveloperToolsSubCategory.Development,

    // IoT (1100-1199)
    IoTServices = IoTSubCategory.IoTServices,

    // Data (1200-1299)
    DataServices = DataSubCategory.DataServices,

    // Integration (1300-1399)
    IntegrationServices = IntegrationSubCategory.IntegrationServices,
    FileTransfer = IntegrationSubCategory.FileTransfer,

    // Web (1400-1499)
    WebServices = WebSubCategory.WebServices,

    // Enterprise Applications (1500-1599)
    BusinessApplications = EnterpriseApplicationsSubCategory.BusinessApplications,
    ContentServices = EnterpriseApplicationsSubCategory.ContentServices,

    // Licensing (1600-1699)
    SoftwareLicenses = LicensingSubCategory.SoftwareLicenses,

    // Other (1700-1799)
    Uncategorized = OtherSubCategory.Uncategorized
}
