using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Ports;

namespace Application.Services;

public interface IResourceNormalizationService
{
    Task<CategorizedResourcesDto> GetResourcesAsync(IReadOnlyCollection<ResourceCategory> neededResources, UsageSize usage, CancellationToken cancellationToken = default);
}

public class ResourceNormalizationService(ICloudPricingRepository cloudPricingRepository) : IResourceNormalizationService
{
    // GCP machine type memory ratios (GB per vCPU)
    private const double GCP_HIGHCPU_MEMORY_RATIO = 2.0;
    private const double GCP_HIGHMEM_MEMORY_RATIO = 8.0;
    private const double GCP_ULTRAMEM_MEMORY_RATIO = 25.0;
    private const double GCP_STANDARD_MEMORY_RATIO = 4.0;

    public async Task<CategorizedResourcesDto> GetResourcesAsync(IReadOnlyCollection<ResourceCategory> neededResources, UsageSize usage, CancellationToken cancellationToken = default)
    {
        var data = await cloudPricingRepository.GetAllAsync(cancellationToken);

        var categories = new Dictionary<ResourceCategory, CategoryResourcesDto>();

        var needCompute = neededResources.Contains(ResourceCategory.Compute);
        var needDatabases = neededResources.Contains(ResourceCategory.Databases);

        List<NormalizedComputeInstanceDto>? computeInstances = needCompute ? [] : null;
        List<NormalizedDatabaseDto>? normalizedDatabases = needDatabases ? [] : null;

        if (needCompute || needDatabases)
        {
            foreach (var product in data.Data.Products)
            {
                if (needCompute && IsComputeInstance(product))
                {
                    var instanceName = GetInstanceName(product);
                    if (string.IsNullOrWhiteSpace(instanceName))
                        continue;

                    var vcpu = GetVCpu(product);
                    var memory = GetMemory(product);
                    var pricePerHour = GetPricePerHour(product);

                    computeInstances!.Add(new NormalizedComputeInstanceDto
                    {
                        Cloud = product.VendorName,
                        InstanceName = instanceName,
                        Region = product.Region ?? "unknown",
                        VCpu = vcpu,
                        Memory = memory,
                        PricePerHour = pricePerHour
                    });
                    continue;
                }

                if (needDatabases && IsDatabaseInstance(product))
                {
                    var instanceName = GetInstanceName(product);
                    if (string.IsNullOrWhiteSpace(instanceName))
                        continue;

                    var vcpu = GetVCpu(product);
                    var memory = GetMemory(product);
                    var pricePerHour = GetPricePerHour(product);
                    var databaseEngine = GetDatabaseEngine(product);

                    normalizedDatabases!.Add(new NormalizedDatabaseDto
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
            }
        }

        if (needCompute && computeInstances!.Any())
        {
            categories[ResourceCategory.Compute] = new CategoryResourcesDto
            {
                Category = ResourceCategory.Compute,
                ComputeInstances = computeInstances
            };
        }

        if (needDatabases && normalizedDatabases!.Any())
        {
            categories[ResourceCategory.Databases] = new CategoryResourcesDto
            {
                Category = ResourceCategory.Databases,
                Databases = normalizedDatabases
            };
        }

        if (neededResources.Contains(ResourceCategory.Networking))
        {
            var loadBalancers = GetNormalizedLoadBalancers(usage);
            if (loadBalancers.Any())
            {
                categories[ResourceCategory.Networking] = new CategoryResourcesDto
                {
                    Category = ResourceCategory.Networking,
                    LoadBalancers = loadBalancers
                };
            }
        }

        if (neededResources.Contains(ResourceCategory.Management))
        {
            var monitoring = GetNormalizedMonitoring(usage);
            if (monitoring.Any())
            {
                categories[ResourceCategory.Management] = new CategoryResourcesDto
                {
                    Category = ResourceCategory.Management,
                    Monitoring = monitoring
                };
            }
        }

        return new CategorizedResourcesDto
        {
            Categories = categories
        };
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
        var instanceType = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value;
        if (!string.IsNullOrWhiteSpace(instanceType))
            return instanceType;

        var armSkuName = product.Attributes.FirstOrDefault(a => a.Key == "armSkuName")?.Value;
        if (!string.IsNullOrWhiteSpace(armSkuName))
            return armSkuName;

        var skuName = product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value;
        if (!string.IsNullOrWhiteSpace(skuName))
            return skuName;

        var machineType = product.Attributes.FirstOrDefault(a => a.Key == "machineType")?.Value;
        if (!string.IsNullOrWhiteSpace(machineType))
            return machineType;

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
        var vcpuStr = product.Attributes.FirstOrDefault(a => a.Key == "vcpu")?.Value;
        if (!string.IsNullOrWhiteSpace(vcpuStr) && int.TryParse(vcpuStr, out var vcpu))
            return vcpu;

        var vCpusAvailableStr = product.Attributes.FirstOrDefault(a => a.Key == "vCpusAvailable")?.Value;
        if (!string.IsNullOrWhiteSpace(vCpusAvailableStr) && int.TryParse(vCpusAvailableStr, out var vCpusAvailable))
            return vCpusAvailable;

        var numberOfCoresStr = product.Attributes.FirstOrDefault(a => a.Key == "numberOfCores")?.Value;
        if (!string.IsNullOrWhiteSpace(numberOfCoresStr) && int.TryParse(numberOfCoresStr, out var numberOfCores))
            return numberOfCores;

        if (product.VendorName == CloudProvider.GCP)
        {
            var description = product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value;
            if (!string.IsNullOrWhiteSpace(description))
            {
                var match = System.Text.RegularExpressions.Regex.Match(description, @"(\d+)\s+vCPU");
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
        var memory = product.Attributes.FirstOrDefault(a => a.Key == "memory")?.Value;
        if (!string.IsNullOrWhiteSpace(memory))
            return memory;

        var memoryInGB = product.Attributes.FirstOrDefault(a => a.Key == "memoryInGB")?.Value;
        if (!string.IsNullOrWhiteSpace(memoryInGB))
            return $"{memoryInGB} GB";

        if (product.VendorName == CloudProvider.GCP)
        {
            var description = product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value;
            if (!string.IsNullOrWhiteSpace(description))
            {
                var match = System.Text.RegularExpressions.Regex.Match(description, @"(\d+(?:\.\d+)?)\s*GB RAM");
                if (match.Success)
                    return $"{match.Groups[1].Value} GB";
            }

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
            var parts = machineType.Split('-');
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
        var price = product.Prices
            .Where(p => p.PurchaseOption?.ToLower() == "on_demand" ||
                        p.PurchaseOption?.ToLower() == "ondemand" ||
                        p.PurchaseOption?.ToLower() == "consumption")
            .FirstOrDefault()?.Usd;

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

        if (product.Service != null && product.Service.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            return "PostgreSQL";
        if (product.Service != null && product.Service.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
            return "MySQL";

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

    private static List<NormalizedLoadBalancerDto> GetNormalizedLoadBalancers(UsageSize usage)
    {
        var loadBalancers = new List<NormalizedLoadBalancerDto>();

        var gcpPricing = usage switch
        {
            UsageSize.Small => 18.41m,
            UsageSize.Medium => 19.85m,
            UsageSize.Large => 34.25m,
            _ => 0m
        };

        var azurePricing = 0m;

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

    private static List<NormalizedMonitoringDto> GetNormalizedMonitoring(UsageSize usage)
    {
        var monitoring = new List<NormalizedMonitoringDto>();

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

    private static (ResourceCategory Category, ResourceSubCategory SubCategory) MapProductFamilyToCategoryAndSubCategory(string productFamily)
    {
        return productFamily switch
        {
            "Compute" => (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            "Compute Instance" => (ResourceCategory.Compute, ResourceSubCategory.VirtualMachines),
            "Compute Instance (bare metal)" => (ResourceCategory.Compute, ResourceSubCategory.BareMetalServers),
            "Dedicated Host" => (ResourceCategory.Compute, ResourceSubCategory.DedicatedHosts),
            "Containers" => (ResourceCategory.Compute, ResourceSubCategory.Containers),
            "Databases" => (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            "Database Instance" => (ResourceCategory.Databases, ResourceSubCategory.RelationalDatabases),
            "Database Storage" => (ResourceCategory.Databases, ResourceSubCategory.DatabaseStorage),
            "Storage" => (ResourceCategory.Storage, ResourceSubCategory.BlockStorage),
            "Provisioned IOPS" => (ResourceCategory.Storage, ResourceSubCategory.PerformanceStorage),
            "Provisioned Throughput" => (ResourceCategory.Storage, ResourceSubCategory.PerformanceStorage),
            "Network" => (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            "Networking" => (ResourceCategory.Networking, ResourceSubCategory.NetworkServices),
            "IP Address" => (ResourceCategory.Networking, ResourceSubCategory.IPAddresses),
            "Analytics" => (ResourceCategory.Analytics, ResourceSubCategory.DataAnalytics),
            "AWS Lake Formation" => (ResourceCategory.Analytics, ResourceSubCategory.DataLakes),
            "AI + Machine Learning" => (ResourceCategory.AI, ResourceSubCategory.MachineLearning),
            "Security" => (ResourceCategory.Security, ResourceSubCategory.SecurityServices),
            "Amazon Inspector" => (ResourceCategory.Security, ResourceSubCategory.VulnerabilityScanning),
            "Web Application Firewall" => (ResourceCategory.Security, ResourceSubCategory.WebApplicationFirewall),
            "ApplicationServices" => (ResourceCategory.ApplicationServices, ResourceSubCategory.ManagedServices),
            "AmazonConnect" => (ResourceCategory.ApplicationServices, ResourceSubCategory.ContactCenter),
            "Azure Communication Services" => (ResourceCategory.ApplicationServices, ResourceSubCategory.CommunicationServices),
            "Management and Governance" => (ResourceCategory.Management, ResourceSubCategory.CloudManagement),
            "System Operation" => (ResourceCategory.Management, ResourceSubCategory.Operations),
            "Developer Tools" => (ResourceCategory.DeveloperTools, ResourceSubCategory.Development),
            "Internet of Things" => (ResourceCategory.IoT, ResourceSubCategory.IoTServices),
            "Data" => (ResourceCategory.Data, ResourceSubCategory.DataServices),
            "Integration" => (ResourceCategory.Integration, ResourceSubCategory.IntegrationServices),
            "AWS Transfer Family" => (ResourceCategory.Integration, ResourceSubCategory.FileTransfer),
            "Web" => (ResourceCategory.Web, ResourceSubCategory.WebServices),
            "Enterprise Applications" => (ResourceCategory.EnterpriseApplications, ResourceSubCategory.BusinessApplications),
            "Microsoft Syntex" => (ResourceCategory.EnterpriseApplications, ResourceSubCategory.ContentServices),
            "License" => (ResourceCategory.Licensing, ResourceSubCategory.SoftwareLicenses),
            "Other" => (ResourceCategory.Other, ResourceSubCategory.Uncategorized),
            _ => (ResourceCategory.Other, ResourceSubCategory.Uncategorized)
        };
    }
}
