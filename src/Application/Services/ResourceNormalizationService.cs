using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Ports;

namespace Application.Services;

public interface IResourceNormalizationService
{
    Task<List<NormalizedComputeInstanceDto>> GetNormalizedComputeInstancesAsync(CancellationToken cancellationToken = default);
    Task<List<NormalizedDatabaseDto>> GetNormalizedDatabasesAsync(CancellationToken cancellationToken = default);
    List<NormalizedLoadBalancerDto> GetNormalizedLoadBalancers(UsageSize usage);
    List<NormalizedMonitoringDto> GetNormalizedMonitoring(UsageSize usage);
    Task<ProductFamilyMappingsDto> GetProductFamilyMappingsAsync(CancellationToken cancellationToken = default);
}

public class ResourceNormalizationService(ICloudPricingRepository cloudPricingRepository) : IResourceNormalizationService
{
    // GCP machine type memory ratios (GB per vCPU)
    private const double GCP_HIGHCPU_MEMORY_RATIO = 2.0;
    private const double GCP_HIGHMEM_MEMORY_RATIO = 8.0;
    private const double GCP_ULTRAMEM_MEMORY_RATIO = 25.0;
    private const double GCP_STANDARD_MEMORY_RATIO = 4.0;
    public async Task<List<NormalizedComputeInstanceDto>> GetNormalizedComputeInstancesAsync(CancellationToken cancellationToken = default)
    {
        var data = await cloudPricingRepository.GetAllAsync(cancellationToken);
        var normalizedInstances = new List<NormalizedComputeInstanceDto>();

        foreach (var product in data.Data.Products)
        {
            // Filter for compute instances
            if (!IsComputeInstance(product))
                continue;

            var instanceName = GetInstanceName(product);
            if (string.IsNullOrWhiteSpace(instanceName))
                continue;

            var vcpu = GetVCpu(product);
            var memory = GetMemory(product);
            var pricePerHour = GetPricePerHour(product);

            normalizedInstances.Add(new NormalizedComputeInstanceDto
            {
                Cloud = product.VendorName,
                InstanceName = instanceName,
                Region = product.Region ?? "unknown",
                VCpu = vcpu,
                Memory = memory,
                PricePerHour = pricePerHour
            });
        }

        return normalizedInstances;
    }

    public async Task<List<NormalizedDatabaseDto>> GetNormalizedDatabasesAsync(CancellationToken cancellationToken = default)
    {
        var data = await cloudPricingRepository.GetAllAsync(cancellationToken);
        var normalizedDatabases = new List<NormalizedDatabaseDto>();

        foreach (var product in data.Data.Products)
        {
            // Filter for database instances
            if (!IsDatabaseInstance(product))
                continue;

            var instanceName = GetInstanceName(product);
            if (string.IsNullOrWhiteSpace(instanceName))
                continue;

            var vcpu = GetVCpu(product);
            var memory = GetMemory(product);
            var pricePerHour = GetPricePerHour(product);
            var databaseEngine = GetDatabaseEngine(product);

            normalizedDatabases.Add(new NormalizedDatabaseDto
            {
                Cloud = product.VendorName,
                InstanceName = instanceName,
                Region = product.Region ?? "unknown",
                DatabaseEngine = databaseEngine,
                VCpu = vcpu,
                Memory = memory,
                PricePerHour = pricePerHour
            });
        }

        return normalizedDatabases;
    }

    private static bool IsComputeInstance(CloudPricingProductDto product)
    {
        return product.ProductFamily switch
        {
            "Compute Instance" => true,
            "Compute Instance (bare metal)" => true,
            "Compute" when product.VendorName == CloudProvider.Azure && product.Service == "Virtual Machines" => true,
            _ => false
        };
    }

    private static bool IsDatabaseInstance(CloudPricingProductDto product)
    {
        return product.ProductFamily switch
        {
            "Database Instance" => true,
            "Databases" when product.VendorName == CloudProvider.Azure => true,
            "ApplicationServices" when product.VendorName == CloudProvider.GCP && product.Service == "Cloud SQL" => true,
            _ => false
        };
    }

    private static string? GetInstanceName(CloudPricingProductDto product)
    {
        // AWS uses instanceType
        var instanceType = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value;
        if (!string.IsNullOrWhiteSpace(instanceType))
            return instanceType;

        // Azure uses armSkuName
        var armSkuName = product.Attributes.FirstOrDefault(a => a.Key == "armSkuName")?.Value;
        if (!string.IsNullOrWhiteSpace(armSkuName))
            return armSkuName;

        // Azure can also use skuName
        var skuName = product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value;
        if (!string.IsNullOrWhiteSpace(skuName))
            return skuName;

        // GCP uses machineType
        var machineType = product.Attributes.FirstOrDefault(a => a.Key == "machineType")?.Value;
        if (!string.IsNullOrWhiteSpace(machineType))
            return machineType;

        // GCP Cloud SQL uses resourceGroup
        if (product.VendorName == CloudProvider.GCP && product.Service == "Cloud SQL")
        {
            var resourceGroup = product.Attributes.FirstOrDefault(a => a.Key == "resourceGroup")?.Value;
            if (!string.IsNullOrWhiteSpace(resourceGroup))
                return resourceGroup;
        }

        return null;
    }

    private static int? GetVCpu(CloudPricingProductDto product)
    {
        // Try vcpu (AWS, some Azure)
        var vcpuStr = product.Attributes.FirstOrDefault(a => a.Key == "vcpu")?.Value;
        if (!string.IsNullOrWhiteSpace(vcpuStr) && int.TryParse(vcpuStr, out var vcpu))
            return vcpu;

        // Try vCpusAvailable (Azure)
        var vCpusAvailableStr = product.Attributes.FirstOrDefault(a => a.Key == "vCpusAvailable")?.Value;
        if (!string.IsNullOrWhiteSpace(vCpusAvailableStr) && int.TryParse(vCpusAvailableStr, out var vCpusAvailable))
            return vCpusAvailable;

        // Try numberOfCores (Azure)
        var numberOfCoresStr = product.Attributes.FirstOrDefault(a => a.Key == "numberOfCores")?.Value;
        if (!string.IsNullOrWhiteSpace(numberOfCoresStr) && int.TryParse(numberOfCoresStr, out var numberOfCores))
            return numberOfCores;

        // Parse from description for GCP (Cloud SQL, etc.)
        if (product.VendorName == CloudProvider.GCP)
        {
            var description = product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value;
            if (!string.IsNullOrWhiteSpace(description))
            {
                var match = System.Text.RegularExpressions.Regex.Match(description, @"(\d+)\s+vCPU");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var gcpVcpu))
                    return gcpVcpu;
            }
            
            // Parse from machineType for GCP Compute Engine
            var vcpuFromMachineType = ExtractVCpuFromGcpMachineType(product);
            if (vcpuFromMachineType.HasValue)
                return vcpuFromMachineType;
        }

        return null;
    }

    private static string? GetMemory(CloudPricingProductDto product)
    {
        var memory = product.Attributes.FirstOrDefault(a => a.Key == "memory")?.Value;
        if (!string.IsNullOrWhiteSpace(memory))
            return memory;
        
        // Fallback to memoryInGB for Azure
        var memoryInGB = product.Attributes.FirstOrDefault(a => a.Key == "memoryInGB")?.Value;
        if (!string.IsNullOrWhiteSpace(memoryInGB))
            return $"{memoryInGB} GB";
        
        // Parse from description for GCP (Cloud SQL, etc.)
        if (product.VendorName == CloudProvider.GCP)
        {
            var description = product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value;
            if (!string.IsNullOrWhiteSpace(description))
            {
                var match = System.Text.RegularExpressions.Regex.Match(description, @"(\d+(?:\.\d+)?)\s*GB RAM");
                if (match.Success)
                    return $"{match.Groups[1].Value} GB";
            }
            
            // Estimate from machineType for GCP Compute Engine
            var vcpus = ExtractVCpuFromGcpMachineType(product);
            if (vcpus.HasValue)
            {
                var machineType = product.Attributes.FirstOrDefault(a => a.Key == "machineType")?.Value;
                if (!string.IsNullOrWhiteSpace(machineType))
                {
                    var memoryGb = EstimateGcpMemoryFromMachineType(machineType, vcpus.Value);
                    return $"{memoryGb} GB";
                }
            }
        }
        
        return null;
    }
    
    private static int? ExtractVCpuFromGcpMachineType(CloudPricingProductDto product)
    {
        var machineType = product.Attributes.FirstOrDefault(a => a.Key == "machineType")?.Value;
        if (!string.IsNullOrWhiteSpace(machineType))
        {
            // Extract the number at the end of the machine type (e.g., "c2d-highcpu-2" -> 2)
            var parts = machineType.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[^1], out var machineVcpu))
                return machineVcpu;
        }
        return null;
    }
    
    private static double EstimateGcpMemoryFromMachineType(string machineType, int vcpus)
    {
        // Estimate memory based on machine type series
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
        // Get the on-demand or consumption price if available
        var price = product.Prices
            .Where(p => p.PurchaseOption?.ToLower() == "on_demand" || 
                       p.PurchaseOption?.ToLower() == "ondemand" ||
                       p.PurchaseOption?.ToLower() == "consumption")
            .FirstOrDefault()?.Usd;

        // If no on-demand price, try to get any price
        if (price == null)
        {
            price = product.Prices.FirstOrDefault()?.Usd;
        }

        return price;
    }

    private static string? GetDatabaseEngine(CloudPricingProductDto product)
    {
        var engine = product.Attributes.FirstOrDefault(a => a.Key == "databaseEngine")?.Value;
        if (!string.IsNullOrWhiteSpace(engine))
            return engine;
        
        // Fallback to service name for Azure databases
        if (product.Service != null && product.Service.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            return "PostgreSQL";
        if (product.Service != null && product.Service.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            return "MySQL";
        
        // Parse from description for GCP
        if (product.VendorName == CloudProvider.GCP)
        {
            var description = product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value;
            if (!string.IsNullOrWhiteSpace(description))
            {
                if (description.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                    return "MySQL";
                if (description.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase) || 
                    description.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
                    return "PostgreSQL";
            }
        }
        
        return null;
    }

    public List<NormalizedLoadBalancerDto> GetNormalizedLoadBalancers(UsageSize usage)
    {
        var loadBalancers = new List<NormalizedLoadBalancerDto>();

        // Pricing per month for Small/Medium/Large usage sizes
        var gcpPricing = usage switch
        {
            UsageSize.Small => 18.41m,
            UsageSize.Medium => 19.85m,
            UsageSize.Large => 34.25m,
            _ => 0m
        };

        var azurePricing = 0m; // Azure Load Balancer is free for Small/Medium/Large

        var awsPricing = usage switch
        {
            UsageSize.Small => 16.51m,
            UsageSize.Medium => 17.23m,
            UsageSize.Large => 24.43m,
            _ => 0m
        };

        loadBalancers.Add(new NormalizedLoadBalancerDto
        {
            Cloud = CloudProvider.GCP,
            Name = "Cloud Load Balancing",
            PricePerMonth = gcpPricing
        });

        loadBalancers.Add(new NormalizedLoadBalancerDto
        {
            Cloud = CloudProvider.Azure,
            Name = "Azure Load Balancer",
            PricePerMonth = azurePricing
        });

        loadBalancers.Add(new NormalizedLoadBalancerDto
        {
            Cloud = CloudProvider.AWS,
            Name = "Elastic Load Balancing",
            PricePerMonth = awsPricing
        });

        return loadBalancers;
    }

    public List<NormalizedMonitoringDto> GetNormalizedMonitoring(UsageSize usage)
    {
        var monitoring = new List<NormalizedMonitoringDto>();

        // Pricing per month for Small/Medium/Large usage sizes
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

        monitoring.Add(new NormalizedMonitoringDto
        {
            Cloud = CloudProvider.GCP,
            Name = "Cloud Ops",
            PricePerMonth = gcpPricing
        });

        monitoring.Add(new NormalizedMonitoringDto
        {
            Cloud = CloudProvider.Azure,
            Name = "Azure Monitor",
            PricePerMonth = azurePricing
        });

        monitoring.Add(new NormalizedMonitoringDto
        {
            Cloud = CloudProvider.AWS,
            Name = "CloudWatch",
            PricePerMonth = awsPricing
        });

        return monitoring;
    }

    public async Task<ProductFamilyMappingsDto> GetProductFamilyMappingsAsync(CancellationToken cancellationToken = default)
    {
        var data = await cloudPricingRepository.GetAllAsync(cancellationToken);
        var productFamilies = data.Data.Products
            .Select(p => p.ProductFamily)
            .Where(pf => !string.IsNullOrWhiteSpace(pf))
            .Distinct()
            .OrderBy(pf => pf)
            .ToList();

        var mappings = new List<ProductFamilyMappingDto>();

        foreach (var productFamily in productFamilies)
        {
            var (category, subCategory) = MapProductFamilyToCategoryAndSubCategory(productFamily);
            mappings.Add(new ProductFamilyMappingDto
            {
                ProductFamily = productFamily,
                Category = category,
                SubCategory = subCategory
            });
        }

        return new ProductFamilyMappingsDto { Mappings = mappings };
    }

    private static (string Category, string SubCategory) MapProductFamilyToCategoryAndSubCategory(string productFamily)
    {
        return productFamily switch
        {
            // Compute
            "Compute" => ("Compute", "VirtualMachines"),
            "Compute Instance" => ("Compute", "VirtualMachines"),
            "Compute Instance (bare metal)" => ("Compute", "BareMetalServers"),
            "Dedicated Host" => ("Compute", "DedicatedHosts"),
            "Containers" => ("Compute", "Containers"),

            // Databases
            "Databases" => ("Databases", "RelationalDatabases"),
            "Database Instance" => ("Databases", "RelationalDatabases"),
            "Database Storage" => ("Databases", "DatabaseStorage"),

            // Storage
            "Storage" => ("Storage", "BlockStorage"),
            "Provisioned IOPS" => ("Storage", "PerformanceStorage"),
            "Provisioned Throughput" => ("Storage", "PerformanceStorage"),

            // Networking
            "Network" => ("Networking", "NetworkServices"),
            "Networking" => ("Networking", "NetworkServices"),
            "IP Address" => ("Networking", "IPAddresses"),

            // Analytics
            "Analytics" => ("Analytics", "DataAnalytics"),
            "AWS Lake Formation" => ("Analytics", "DataLakes"),

            // AI and Machine Learning
            "AI + Machine Learning" => ("AI", "MachineLearning"),

            // Security
            "Security" => ("Security", "SecurityServices"),
            "Amazon Inspector" => ("Security", "VulnerabilityScanning"),
            "Web Application Firewall" => ("Security", "WebApplicationFirewall"),

            // Application Services
            "ApplicationServices" => ("ApplicationServices", "ManagedServices"),
            "AmazonConnect" => ("ApplicationServices", "ContactCenter"),
            "Azure Communication Services" => ("ApplicationServices", "CommunicationServices"),

            // Management and Governance
            "Management and Governance" => ("Management", "CloudManagement"),
            "System Operation" => ("Management", "Operations"),

            // Developer Tools
            "Developer Tools" => ("DeveloperTools", "Development"),

            // Internet of Things
            "Internet of Things" => ("IoT", "IoTServices"),

            // Data
            "Data" => ("Data", "DataServices"),

            // Integration
            "Integration" => ("Integration", "IntegrationServices"),
            "AWS Transfer Family" => ("Integration", "FileTransfer"),

            // Web
            "Web" => ("Web", "WebServices"),

            // Enterprise Applications
            "Enterprise Applications" => ("EnterpriseApplications", "BusinessApplications"),
            "Microsoft Syntex" => ("EnterpriseApplications", "ContentServices"),

            // License
            "License" => ("Licensing", "SoftwareLicenses"),

            // Other/Uncategorized
            "Other" => ("Other", "Uncategorized"),

            _ => ("Other", "Uncategorized")
        };
    }
}
