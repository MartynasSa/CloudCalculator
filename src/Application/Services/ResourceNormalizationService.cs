using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Ports;
using System.Text.RegularExpressions;

namespace Application.Services;

public interface IResourceNormalizationService
{
    Task<CategorizedResourcesDto> GetResourcesAsync(IReadOnlyCollection<ResourceCategory> neededResources, UsageSize usage, CancellationToken cancellationToken = default);
    Task<ProductFamilyMappingsDto> GetProductFamilyMappingsAsync(CancellationToken cancellationToken = default);
}

public class ResourceNormalizationService(ICloudPricingRepository cloudPricingRepository) : IResourceNormalizationService
{
    // GCP machine type memory ratios (GB per vCPU)
    private const double GCP_HIGHCPU_MEMORY_RATIO = 2.0;
    private const double GCP_HIGHMEM_MEMORY_RATIO = 8.0;
    private const double GCP_ULTRAMEM_MEMORY_RATIO = 25.0;
    private const double GCP_STANDARD_MEMORY_RATIO = 4.0;

    // Mapping based on productFamily and service combination
    private static readonly Dictionary<(string ProductFamily, string Service), (ResourceCategory Category, ResourceSubCategory SubCategory)> ProductFamilyServiceMap =
        new()
        {
            // Compute - Virtual Machines
            [("Compute", "Virtual Machines")] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            [("Compute", "Compute Engine")] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            [("Compute Instance", "AmazonEC2")] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            [("Compute Instance", "AWSOutposts")] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            [("Compute Instance", "AmazonDeadline")] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            [("Compute Instance", "Compute Engine")] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            
            // Compute - Bare Metal
            [("Compute Instance (bare metal)", "AmazonEC2")] = (ResourceCategory.Compute, ResourceSubCategory.BareMetalServers),
            
            // Compute - Dedicated Hosts
            [("Dedicated Host", "AmazonEC2")] = (ResourceCategory.Compute, ResourceSubCategory.DedicatedHosts),
            
            // Compute - Containers
            [("Compute", "Azure Kubernetes Service")] = (ResourceCategory.Compute, ResourceSubCategory.Containers),
            [("Compute", "Azure Container Apps")] = (ResourceCategory.Compute, ResourceSubCategory.Containers),
            [("Containers", "Container Registry")] = (ResourceCategory.Compute, ResourceSubCategory.Containers),
            [("Compute", "Azure App Service")] = (ResourceCategory.Compute, ResourceSubCategory.Containers),
            [("Compute", "Cloud Services")] = (ResourceCategory.Compute, ResourceSubCategory.Containers),
            
            // Compute - Other
            [("Compute", "Azure Local")] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            [("Compute", "Azure Modeling and Simulation Workbench")] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            [("Compute", "Specialized Compute")] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            [("Compute", "Virtual Machines Licenses")] = (ResourceCategory.Licensing, ResourceSubCategory.SoftwareLicenses),
            
            // Databases - Relational
            [("Databases", "Azure Database for PostgreSQL")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Databases", "Azure Database for MySQL")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Databases", "Azure Database for MariaDB")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Databases", "SQL Database")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Databases", "SQL Managed Instance")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Databases", "Azure Cosmos DB")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Databases", "Azure Managed Instance for Apache Cassandra")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Databases", "Redis Cache")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Databases", "SQL Data Warehouse")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Databases", "Azure Arc Enabled Databases")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Databases", "Azure Database Migration Service")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            [("Database Instance", "AmazonRDS")] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            
            // Databases - Storage
            [("Database Storage", "AmazonRDS")] = (ResourceCategory.Databases, ResourceSubCategory.DatabaseStorage),
            
            // Storage
            [("Storage", "AmazonS3")] = (ResourceCategory.Storage, ResourceSubCategory.BlockStorage),
            [("Storage", "Cloud Storage")] = (ResourceCategory.Storage, ResourceSubCategory.BlockStorage),
            [("Storage", "Compute Engine")] = (ResourceCategory.Storage, ResourceSubCategory.BlockStorage),
            [("Storage", "Storage")] = (ResourceCategory.Storage, ResourceSubCategory.BlockStorage),
            [("Storage", "Azure NetApp Files")] = (ResourceCategory.Storage, ResourceSubCategory.BlockStorage),
            [("Storage", "Backup")] = (ResourceCategory.Storage, ResourceSubCategory.BlockStorage),
            [("Provisioned IOPS", "AmazonRDS")] = (ResourceCategory.Storage, ResourceSubCategory.PerformanceStorage),
            [("Provisioned Throughput", "AmazonRDS")] = (ResourceCategory.Storage, ResourceSubCategory.PerformanceStorage),
            
            // Networking
            [("Network", "Cloud SQL")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Network", "Cloud Storage")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Network", "Compute Engine")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Network", "Firebase Realtime Database")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Networking", "Application Gateway")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Networking", "Azure Bastion")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Networking", "Azure Firewall")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Networking", "Bandwidth")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Networking", "Content Delivery Network")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Networking", "ExpressRoute")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Networking", "VPN Gateway")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("Networking", "Virtual Network")] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            [("IP Address", "AmazonEC2")] = (ResourceCategory.Networking, ResourceSubCategory.IPAddresses),
            
            // Analytics
            [("Analytics", "Azure Data Factory v2")] = (ResourceCategory.Analytics, ResourceSubCategory.DataAnalytics),
            [("Analytics", "Azure Data Share")] = (ResourceCategory.Analytics, ResourceSubCategory.DataAnalytics),
            [("Analytics", "Azure Databricks")] = (ResourceCategory.Analytics, ResourceSubCategory.DataAnalytics),
            [("Analytics", "Azure Synapse Analytics")] = (ResourceCategory.Analytics, ResourceSubCategory.DataAnalytics),
            [("Analytics", "HDInsight")] = (ResourceCategory.Analytics, ResourceSubCategory.DataAnalytics),
            [("Analytics", "Power BI Embedded")] = (ResourceCategory.Analytics, ResourceSubCategory.DataAnalytics),
            [("AWS Lake Formation", "AWSLakeFormation")] = (ResourceCategory.Analytics, ResourceSubCategory.DataLakes),
            
            // AI
            [("AI + Machine Learning", "Cognitive Services")] = (ResourceCategory.AI, ResourceSubCategory.MachineLearning),
            [("AI + Machine Learning", "Foundry Models")] = (ResourceCategory.AI, ResourceSubCategory.MachineLearning),
            [("AI + Machine Learning", "Foundry Tools")] = (ResourceCategory.AI, ResourceSubCategory.MachineLearning),
            
            // Security
            [("Security", "Key Vault")] = (ResourceCategory.Security, ResourceSubCategory.SecurityServices),
            [("Security", "Microsoft Defender for Cloud")] = (ResourceCategory.Security, ResourceSubCategory.SecurityServices),
            [("Amazon Inspector", "AmazonInspectorV2")] = (ResourceCategory.Security, ResourceSubCategory.VulnerabilityScanning),
            [("Web Application Firewall", "awswaf")] = (ResourceCategory.Security, ResourceSubCategory.WebApplicationFirewall),
            
            // Application Services
            [("ApplicationServices", "Cloud SQL")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            [("ApplicationServices", "Cloud Workstations")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            [("ApplicationServices", "Collibra collibra-data-intelligence-cloud-gcp")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            [("ApplicationServices", "Firebase Phone Number Verification")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            [("ApplicationServices", "Firebase Test Lab")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            [("ApplicationServices", "FortiGate Next-Generation Firewall (PAYG)")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            [("ApplicationServices", "Fortinet FortiADC Application Delivery Controller PAYG 10Gbps")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            [("ApplicationServices", "Fortinet FortiWeb Web Application Firewall WAF (BYOL)")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            [("ApplicationServices", "Gemini API")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            [("AmazonConnect", "AmazonConnect")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ContactCenter),
            [("Azure Communication Services", "Messaging")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.CommunicationServices),
            [("Azure Communication Services", "Phone Numbers")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.CommunicationServices),
            [("Azure Communication Services", "SMS")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.CommunicationServices),
            [("Azure Communication Services", "Voice")] = (ResourceCategory.ApplicationServices, ResourceSubCategory.CommunicationServices),
            
            // Management
            [("Management and Governance", "Application Insights")] = (ResourceCategory.Management, ResourceSubCategory.CloudManagement),
            [("Management and Governance", "Azure Monitor")] = (ResourceCategory.Management, ResourceSubCategory.CloudManagement),
            [("Management and Governance", "Azure Site Recovery")] = (ResourceCategory.Management, ResourceSubCategory.CloudManagement),
            [("Management and Governance", "Sentinel")] = (ResourceCategory.Management, ResourceSubCategory.CloudManagement),
            [("System Operation", "AmazonEC2")] = (ResourceCategory.Management, ResourceSubCategory.Operations),
            
            // Developer Tools
            [("Developer Tools", "API Management")] = (ResourceCategory.DeveloperTools, ResourceSubCategory.Development),
            [("Developer Tools", "Microsoft Dev Box")] = (ResourceCategory.DeveloperTools, ResourceSubCategory.Development),
            
            // IoT
            [("Internet of Things", "Azure Maps")] = (ResourceCategory.IoT, ResourceSubCategory.IoTServices),
            [("Internet of Things", "Event Hubs")] = (ResourceCategory.IoT, ResourceSubCategory.IoTServices),
            [("Internet of Things", "IoT Central")] = (ResourceCategory.IoT, ResourceSubCategory.IoTServices),
            
            // Data
            [("Data", "Microsoft Fabric")] = (ResourceCategory.Data, ResourceSubCategory.DataServices),
            
            // Integration
            [("Integration", "Service Bus")] = (ResourceCategory.Integration, ResourceSubCategory.IntegrationServices),
            [("AWS Transfer Family", "AWSTransfer")] = (ResourceCategory.Integration, ResourceSubCategory.FileTransfer),
            
            // Web
            [("Web", "Azure Cognitive Search")] = (ResourceCategory.Web, ResourceSubCategory.WebServices),
            [("Web", "Media Services")] = (ResourceCategory.Web, ResourceSubCategory.WebServices),
            
            // Enterprise Applications
            [("Enterprise Applications", "AmazonWorkSpaces")] = (ResourceCategory.EnterpriseApplications, ResourceSubCategory.BusinessApplications),
            [("Microsoft Syntex", "Syntex")] = (ResourceCategory.EnterpriseApplications, ResourceSubCategory.ContentServices),
            
            // Licensing
            [("License", "Compute Engine")] = (ResourceCategory.Licensing, ResourceSubCategory.SoftwareLicenses),
            
            // Other
            [("Other", "Azure API for FHIR")] = (ResourceCategory.Other, ResourceSubCategory.Uncategorized),
            [("Other", "Azure Spring Cloud")] = (ResourceCategory.Other, ResourceSubCategory.Uncategorized)
        };

    // Fallback mapping based on productFamily only (for backward compatibility and unmapped combinations)
    private static readonly Dictionary<string, (ResourceCategory Category, ResourceSubCategory SubCategory)> ProductFamilyFallbackMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Compute"] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            ["Compute Instance"] = (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            ["Compute Instance (bare metal)"] = (ResourceCategory.Compute, ResourceSubCategory.BareMetalServers),
            ["Dedicated Host"] = (ResourceCategory.Compute, ResourceSubCategory.DedicatedHosts),
            ["Containers"] = (ResourceCategory.Compute, ResourceSubCategory.Containers),

            ["Databases"] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            ["Database Instance"] = (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            ["Database Storage"] = (ResourceCategory.Databases, ResourceSubCategory.DatabaseStorage),

            ["Storage"] = (ResourceCategory.Storage, ResourceSubCategory.BlockStorage),
            ["Provisioned IOPS"] = (ResourceCategory.Storage, ResourceSubCategory.PerformanceStorage),
            ["Provisioned Throughput"] = (ResourceCategory.Storage, ResourceSubCategory.PerformanceStorage),

            ["Network"] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            ["Networking"] = (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            ["IP Address"] = (ResourceCategory.Networking, ResourceSubCategory.IPAddresses),

            ["Analytics"] = (ResourceCategory.Analytics, ResourceSubCategory.DataAnalytics),
            ["AWS Lake Formation"] = (ResourceCategory.Analytics, ResourceSubCategory.DataLakes),

            ["AI + Machine Learning"] = (ResourceCategory.AI, ResourceSubCategory.MachineLearning),

            ["Security"] = (ResourceCategory.Security, ResourceSubCategory.SecurityServices),
            ["Amazon Inspector"] = (ResourceCategory.Security, ResourceSubCategory.VulnerabilityScanning),
            ["Web Application Firewall"] = (ResourceCategory.Security, ResourceSubCategory.WebApplicationFirewall),

            ["ApplicationServices"] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            ["AmazonConnect"] = (ResourceCategory.ApplicationServices, ResourceSubCategory.ContactCenter),
            ["Azure Communication Services"] = (ResourceCategory.ApplicationServices, ResourceSubCategory.CommunicationServices),

            ["Management and Governance"] = (ResourceCategory.Management, ResourceSubCategory.CloudManagement),
            ["System Operation"] = (ResourceCategory.Management, ResourceSubCategory.Operations),

            ["Developer Tools"] = (ResourceCategory.DeveloperTools, ResourceSubCategory.Development),

            ["Internet of Things"] = (ResourceCategory.IoT, ResourceSubCategory.IoTServices),

            ["Data"] = (ResourceCategory.Data, ResourceSubCategory.DataServices),

            ["Integration"] = (ResourceCategory.Integration, ResourceSubCategory.IntegrationServices),
            ["AWS Transfer Family"] = (ResourceCategory.Integration, ResourceSubCategory.FileTransfer),

            ["Web"] = (ResourceCategory.Web, ResourceSubCategory.WebServices),

            ["Enterprise Applications"] = (ResourceCategory.EnterpriseApplications, ResourceSubCategory.BusinessApplications),
            ["Microsoft Syntex"] = (ResourceCategory.EnterpriseApplications, ResourceSubCategory.ContentServices),

            ["License"] = (ResourceCategory.Licensing, ResourceSubCategory.SoftwareLicenses),

            ["Other"] = (ResourceCategory.Other, ResourceSubCategory.Uncategorized)
        };

    public async Task<CategorizedResourcesDto> GetResourcesAsync(
        IReadOnlyCollection<ResourceCategory> neededResources,
        UsageSize usage,
        CancellationToken cancellationToken = default)
    {
        var data = await cloudPricingRepository.GetAllAsync(cancellationToken);

        var categories = new Dictionary<ResourceCategory, CategoryResourcesDto>();

        foreach (var product in data.Data.Products)
        {
            if (string.IsNullOrWhiteSpace(product.ProductFamily))
                continue;

            var (category, subCategory) = MapProductFamilyToCategoryAndSubCategory(product.ProductFamily, product.Service);

            // Override category for GCP Cloud SQL - treat it as a database
            if (product.VendorName == CloudProvider.GCP && product.Service == "Cloud SQL")
            {
                category = ResourceCategory.Databases;
                subCategory = ResourceSubCategory.RelationalDatabases;
            }

            if (!neededResources.Contains(category))
                continue;

            var dto = GetOrCreateCategory(categories, category);

            switch (category)
            {
                case ResourceCategory.Compute:
                    {
                        var instanceName = GetInstanceName(product);
                        if (string.IsNullOrWhiteSpace(instanceName))
                            break;

                        var vcpu = GetVCpu(product);
                        var memory = GetMemory(product);
                        var pricePerHour = GetPricePerHour(product);

                        (dto.ComputeInstances ??= new List<NormalizedComputeInstanceDto>()).Add(new NormalizedComputeInstanceDto
                        {
                            Cloud = product.VendorName,
                            InstanceName = instanceName!,
                            Region = product.Region ?? "unknown",
                            VCpu = vcpu,
                            Memory = memory,
                            PricePerHour = pricePerHour
                        });
                        break;
                    }

                case ResourceCategory.Databases:
                    {
                        var instanceName = GetInstanceName(product);
                        if (string.IsNullOrWhiteSpace(instanceName))
                            break;

                        var vcpu = GetVCpu(product);
                        var memory = GetMemory(product);
                        var pricePerHour = GetPricePerHour(product);
                        var databaseEngine = GetDatabaseEngine(product);

                        (dto.Databases ??= new List<NormalizedDatabaseDto>()).Add(new NormalizedDatabaseDto
                        {
                            Cloud = product.VendorName,
                            InstanceName = instanceName!,
                            Region = product.Region ?? "unknown",
                            DatabaseEngine = databaseEngine,
                            VCpu = vcpu,
                            Memory = memory,
                            PricePerHour = pricePerHour
                        });
                        break;
                    }

                default:
                    {
                        var resources = GetOrInitNormalizedResourceList(dto, category);
                        var resourceName = GetInstanceName(product) ?? product.Service ?? "unknown";
                        var pricePerHour = GetPricePerHour(product);

                        resources.Add(new NormalizedResourceDto
                        {
                            Cloud = product.VendorName,
                            Service = product.Service ?? "unknown",
                            Region = product.Region ?? "unknown",
                            Category = category,
                            SubCategory = subCategory,
                            ProductFamily = product.ProductFamily,
                            ResourceName = resourceName,
                            PricePerHour = pricePerHour,
                            Attributes = product.Attributes.ToDictionary(a => a.Key, a => a.Value)
                        });
                        break;
                    }
            }
        }

        // Add computed Networking and Management categories if requested
        if (neededResources.Contains(ResourceCategory.Networking))
        {
            var lbs = GetNormalizedLoadBalancers(usage);
            if (lbs.Any())
            {
                var dto = GetOrCreateCategory(categories, ResourceCategory.Networking);
                dto.LoadBalancers = lbs;
            }
        }

        if (neededResources.Contains(ResourceCategory.Management))
        {
            var monitoring = GetNormalizedMonitoring(usage);
            if (monitoring.Any())
            {
                var dto = GetOrCreateCategory(categories, ResourceCategory.Management);
                dto.Monitoring = monitoring;
            }
        }

        return new CategorizedResourcesDto { Categories = categories };
    }

    public async Task<ProductFamilyMappingsDto> GetProductFamilyMappingsAsync(CancellationToken cancellationToken = default)
    {
        var data = await cloudPricingRepository.GetAllAsync(cancellationToken);

        var processedCombinations = new HashSet<(string ProductFamily, string Service)>();
        var mappings = new List<ProductFamilyMappingDto>();

        foreach (var product in data.Data.Products)
        {
            var family = product.ProductFamily;
            var service = product.Service;
            
            if (string.IsNullOrWhiteSpace(family) || string.IsNullOrWhiteSpace(service))
                continue;

            var key = (family, service);
            if (processedCombinations.Add(key))
            {
                var (category, subCategory) = MapProductFamilyToCategoryAndSubCategory(family, service);
                mappings.Add(new ProductFamilyMappingDto
                {
                    ProductFamily = family,
                    Service = service,
                    Category = category,
                    SubCategory = subCategory
                });
            }
        }

        return new ProductFamilyMappingsDto
        {
            Mappings = mappings
                .OrderBy(m => m.Category)
                .ThenBy(m => m.SubCategory)
                .ThenBy(m => m.ProductFamily)
                .ThenBy(m => m.Service)
                .ToList()
        };
    }

    private static CategoryResourcesDto GetOrCreateCategory(Dictionary<ResourceCategory, CategoryResourcesDto> categories, ResourceCategory category)
    {
        if (!categories.TryGetValue(category, out var dto))
        {
            dto = new CategoryResourcesDto { Category = category };
            categories[category] = dto;
        }
        return dto;
    }

    private static List<NormalizedResourceDto> GetOrInitNormalizedResourceList(CategoryResourcesDto dto, ResourceCategory category)
    {
        return category switch
        {
            ResourceCategory.Storage => dto.Storage ??= new List<NormalizedResourceDto>(),
            ResourceCategory.Analytics => dto.Analytics ??= new List<NormalizedResourceDto>(),
            ResourceCategory.AI => dto.AI ??= new List<NormalizedResourceDto>(),
            ResourceCategory.Security => dto.Security ??= new List<NormalizedResourceDto>(),
            ResourceCategory.ApplicationServices => dto.ApplicationServices ??= new List<NormalizedResourceDto>(),
            ResourceCategory.DeveloperTools => dto.DeveloperTools ??= new List<NormalizedResourceDto>(),
            ResourceCategory.IoT => dto.IoT ??= new List<NormalizedResourceDto>(),
            ResourceCategory.Data => dto.Data ??= new List<NormalizedResourceDto>(),
            ResourceCategory.Integration => dto.Integration ??= new List<NormalizedResourceDto>(),
            ResourceCategory.Web => dto.Web ??= new List<NormalizedResourceDto>(),
            ResourceCategory.EnterpriseApplications => dto.EnterpriseApplications ??= new List<NormalizedResourceDto>(),
            ResourceCategory.Licensing => dto.Licensing ??= new List<NormalizedResourceDto>(),
            ResourceCategory.Other => dto.Other ??= new List<NormalizedResourceDto>(),
            ResourceCategory.Networking => dto.Networking ??= new List<NormalizedResourceDto>(),
            ResourceCategory.Management => dto.Management ??= new List<NormalizedResourceDto>(),
            _ => throw new ArgumentOutOfRangeException(nameof(category), $"Unsupported category {category} for generic resources.")
        };
    }

    private static (ResourceCategory Category, ResourceSubCategory SubCategory) MapProductFamilyToCategoryAndSubCategory(string productFamily, string? service)
    {
        // Try to find mapping using both productFamily and service
        if (!string.IsNullOrWhiteSpace(service) && 
            ProductFamilyServiceMap.TryGetValue((productFamily, service), out var exactMapping))
        {
            return exactMapping;
        }

        // Fall back to productFamily-only mapping
        if (ProductFamilyFallbackMap.TryGetValue(productFamily, out var fallbackMapping))
        {
            return fallbackMapping;
        }

        // Default to Other/Uncategorized if no mapping found
        return (ResourceCategory.Other, ResourceSubCategory.Uncategorized);
    }

    private static string? GetInstanceName(CloudPricingProductDto product)
    {
        var name = GetFirstAttribute(product, "instanceType", "armSkuName", "skuName", "machineType");
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        if (product.VendorName == CloudProvider.GCP && product.Service == "Cloud SQL")
        {
            var resourceGroup = GetFirstAttribute(product, "resourceGroup");
            if (!string.IsNullOrWhiteSpace(resourceGroup))
                return resourceGroup;
        }

        return null;
    }

    private static int? GetVCpu(CloudPricingProductDto product)
    {
        var vcpu = GetFirstIntAttribute(product, "vcpu", "vCpusAvailable", "numberOfCores");
        if (vcpu.HasValue)
            return vcpu;

        if (product.VendorName == CloudProvider.GCP)
        {
            var description = GetFirstAttribute(product, "description");
            if (!string.IsNullOrWhiteSpace(description))
            {
                var match = Regex.Match(description, @"(\d+)\s+vCPU");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var gcpVcpu))
                    return gcpVcpu;
            }

            var vcpuFromMachineType = ExtractVCpuFromGcpMachineType(product);
            if (vcpuFromMachineType.HasValue)
                return vcpuFromMachineType;
        }

        return null;
    }

    private static string? GetMemory(CloudPricingProductDto product)
    {
        var memory = GetFirstAttribute(product, "memory");
        if (!string.IsNullOrWhiteSpace(memory))
            return memory;

        var memoryInGB = GetFirstAttribute(product, "memoryInGB");
        if (!string.IsNullOrWhiteSpace(memoryInGB))
            return $"{memoryInGB} GB";

        if (product.VendorName == CloudProvider.GCP)
        {
            var description = GetFirstAttribute(product, "description");
            if (!string.IsNullOrWhiteSpace(description))
            {
                var match = Regex.Match(description, @"(\d+(?:\.\d+)?)\s*GB RAM");
                if (match.Success)
                    return $"{match.Groups[1].Value} GB";
            }

            var vcpus = ExtractVCpuFromGcpMachineType(product);
            var machineType = GetFirstAttribute(product, "machineType");
            if (vcpus.HasValue && !string.IsNullOrWhiteSpace(machineType))
            {
                var memoryGb = EstimateGcpMemoryFromMachineType(machineType!, vcpus.Value);
                return $"{memoryGb} GB";
            }
        }

        return null;
    }

    private static int? ExtractVCpuFromGcpMachineType(CloudPricingProductDto product)
    {
        var machineType = GetFirstAttribute(product, "machineType");
        if (!string.IsNullOrWhiteSpace(machineType))
        {
            var parts = machineType!.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[^1], out var machineVcpu))
                return machineVcpu;
        }
        return null;
    }

    private static double EstimateGcpMemoryFromMachineType(string machineType, int vcpus)
    {
        if (machineType.Contains("highcpu", StringComparison.OrdinalIgnoreCase))
            return vcpus * GCP_HIGHCPU_MEMORY_RATIO;
        if (machineType.Contains("highmem", StringComparison.OrdinalIgnoreCase))
            return vcpus * GCP_HIGHMEM_MEMORY_RATIO;
        if (machineType.Contains("ultramem", StringComparison.OrdinalIgnoreCase))
            return vcpus * GCP_ULTRAMEM_MEMORY_RATIO;

        return vcpus * GCP_STANDARD_MEMORY_RATIO;
    }

    private static decimal? GetPricePerHour(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault(p => IsOnDemand(p.PurchaseOption))?.Usd
                    ?? product.Prices.FirstOrDefault()?.Usd;
        return price;
    }

    private static bool IsOnDemand(string? purchaseOption)
    {
        var po = purchaseOption?.ToLowerInvariant();
        return po is "on_demand" or "ondemand" or "consumption";
    }

    private static string? GetDatabaseEngine(CloudPricingProductDto product)
    {
        var engine = GetFirstAttribute(product, "databaseEngine");
        if (!string.IsNullOrWhiteSpace(engine))
            return engine;

        if (!string.IsNullOrWhiteSpace(product.Service))
        {
            if (product.Service!.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase)) return "PostgreSQL";
            if (product.Service!.Contains("MySQL", StringComparison.OrdinalIgnoreCase)) return "MySQL";
        }

        if (product.VendorName == CloudProvider.GCP)
        {
            var description = GetFirstAttribute(product, "description");
            if (!string.IsNullOrWhiteSpace(description))
            {
                if (description!.Contains("MySQL", StringComparison.OrdinalIgnoreCase)) return "MySQL";
                if (description!.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                    description!.Contains("Postgres", StringComparison.OrdinalIgnoreCase)) return "PostgreSQL";
            }
        }

        return null;
    }

    private static List<NormalizedLoadBalancerDto> GetNormalizedLoadBalancers(UsageSize usage)
    {
        var gcpPricing = usage switch
        {
            UsageSize.Small => 18.41m,
            UsageSize.Medium => 19.85m,
            UsageSize.Large => 34.25m,
            _ => 0m
        };

        var awsPricing = usage switch
        {
            UsageSize.Small => 16.51m,
            UsageSize.Medium => 17.23m,
            UsageSize.Large => 24.43m,
            _ => 0m
        };

        return new List<NormalizedLoadBalancerDto>
        {
            new() { Cloud = CloudProvider.GCP,  Name = "Cloud Load Balancing", PricePerMonth = gcpPricing },
            new() { Cloud = CloudProvider.Azure, Name = "Azure Load Balancer",   PricePerMonth = 0m },
            new() { Cloud = CloudProvider.AWS,  Name = "Elastic Load Balancing", PricePerMonth = awsPricing }
        };
    }

    private static List<NormalizedMonitoringDto> GetNormalizedMonitoring(UsageSize usage)
    {
        var gcpPricing = usage switch
        {
            UsageSize.Small => 4m,
            UsageSize.Medium => 12m,
            UsageSize.Large => 40m,
            _ => 0m
        };

        var azurePricing = usage switch
        {
            UsageSize.Small => 6m,
            UsageSize.Medium => 18m,
            UsageSize.Large => 60m,
            _ => 0m
        };

        var awsPricing = usage switch
        {
            UsageSize.Small => 5m,
            UsageSize.Medium => 15m,
            UsageSize.Large => 50m,
            _ => 0m
        };

        return new List<NormalizedMonitoringDto>
        {
            new() { Cloud = CloudProvider.GCP,  Name = "Cloud Ops",     PricePerMonth = gcpPricing },
            new() { Cloud = CloudProvider.Azure, Name = "Azure Monitor", PricePerMonth = azurePricing },
            new() { Cloud = CloudProvider.AWS,  Name = "CloudWatch",    PricePerMonth = awsPricing }
        };
    }

    private static string? GetFirstAttribute(CloudPricingProductDto product, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = product.Attributes.FirstOrDefault(a => a.Key == key)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }

    private static int? GetFirstIntAttribute(CloudPricingProductDto product, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = product.Attributes.FirstOrDefault(a => a.Key == key)?.Value;
            if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, out var parsed))
                return parsed;
        }
        return null;
    }
}