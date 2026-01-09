using Application.Models.Dtos;
using Application.Models.Enums;
using System.Text.RegularExpressions;

namespace Application.Mappers;

public static class NormalizationMapper
{
    // Compiled regex patterns for better performance
    private static readonly Regex AzureVCoreRegex = new(@"(\d+)\s*vCore", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GcpVCpuMemoryRegex = new(@"(\d+)\s*vCPU\s*\+\s*(\d+)GB\s*RAM", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static NormalizedComputeInstanceDto MapToComputeInstance(
        CloudPricingProductDto product,
        ResourceCategory Category,
        ResourceSubCategory SubCategory)
    {
        var instanceName = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "vmSize")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "armSkuName")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "machineType")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value;

        // Fallback to Service if instanceName is still empty
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            instanceName = !string.IsNullOrWhiteSpace(product.Service) ? product.Service : product.ProductFamily;
        }

        var vcpuStr = product.Attributes.FirstOrDefault(a => a.Key == "vcpu")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "numberOfCores")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "vCpusAvailable")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "vCPUs")?.Value;

        var memory = product.Attributes.FirstOrDefault(a => a.Key == "memory")?.Value
                    ?? product.Attributes.FirstOrDefault(a => a.Key == "memoryInGB")?.Value
                    ?? product.Attributes.FirstOrDefault(a => a.Key == "memoryGb")?.Value;

        return new NormalizedComputeInstanceDto
        {
            Category = Category,
            SubCategory = SubCategory,
            Cloud = product.VendorName,
            InstanceName = instanceName,
            Region = product.Region,
            VCpu = int.TryParse(vcpuStr, out var cpu) ? cpu : null,
            Memory = !string.IsNullOrWhiteSpace(memory) ? memory : null,
            PricePerHour = GetPricePerHour(product)
        };
    }

    public static NormalizedDatabaseDto MapToDatabase(CloudPricingProductDto product,
        ResourceCategory Category, ResourceSubCategory SubCategory)
    {
        var instanceName = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "databaseEngine")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "armSkuName")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "machineType")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value;

        // Fallback to Service if instanceName is still empty
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            instanceName = !string.IsNullOrWhiteSpace(product.Service) ? product.Service : product.ProductFamily;
        }

        var vcpuStr = product.Attributes.FirstOrDefault(a => a.Key == "vcpu")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "numberOfCores")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "vCpusAvailable")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "vCPUs")?.Value;

        var memory = product.Attributes.FirstOrDefault(a => a.Key == "memory")?.Value
                    ?? product.Attributes.FirstOrDefault(a => a.Key == "memoryInGB")?.Value
                    ?? product.Attributes.FirstOrDefault(a => a.Key == "memoryGb")?.Value;

        var databaseEngine = product.Attributes.FirstOrDefault(a => a.Key == "databaseEngine")?.Value
                            ?? product.Attributes.FirstOrDefault(a => a.Key == "engine")?.Value
                            ?? product.Attributes.FirstOrDefault(a => a.Key == "databaseFamily")?.Value;

        // Extract vCPU and memory from Azure skuName if not already set
        if (product.VendorName == CloudProvider.Azure && string.IsNullOrWhiteSpace(vcpuStr))
        {
            var skuName = product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value;
            if (!string.IsNullOrWhiteSpace(skuName))
            {
                // Extract vCore from patterns like "4 vCore", "10 vCore", etc.
                var vCoreMatch = AzureVCoreRegex.Match(skuName);
                if (vCoreMatch.Success)
                {
                    vcpuStr = vCoreMatch.Groups[1].Value;
                }
            }
        }

        // Extract vCPU and memory from GCP description if not already set
        if (product.VendorName == CloudProvider.GCP && string.IsNullOrWhiteSpace(vcpuStr))
        {
            var description = product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value;
            if (!string.IsNullOrWhiteSpace(description))
            {
                // Extract vCPU and RAM from patterns like "4 vCPU + 15GB RAM", "16 vCPU + 104GB RAM"
                var match = GcpVCpuMemoryRegex.Match(description);
                if (match.Success)
                {
                    vcpuStr = match.Groups[1].Value;
                    // GCP uses GB in descriptions which is approximately GiB for memory sizing
                    memory = $"{match.Groups[2].Value} GiB";
                }
            }
        }

        // Extract database engine from GCP description if not already set
        if (product.VendorName == CloudProvider.GCP && string.IsNullOrWhiteSpace(databaseEngine))
        {
            var description = product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value;
            if (!string.IsNullOrWhiteSpace(description))
            {
                // Extract database engine from patterns like "Cloud SQL for MySQL", "Cloud SQL for PostgreSQL"
                if (description.Contains("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    databaseEngine = "MySQL";
                }
                else if (description.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                {
                    databaseEngine = "PostgreSQL";
                }
                else if (description.Contains("SQL Server", StringComparison.OrdinalIgnoreCase))
                {
                    databaseEngine = "SQL Server";
                }
            }
        }

        // Infer database engine for Azure SQL Database if not already set
        if (product.VendorName == CloudProvider.Azure && string.IsNullOrWhiteSpace(databaseEngine))
        {
            if (product.Service.Contains("SQL Database", StringComparison.OrdinalIgnoreCase))
            {
                databaseEngine = "SQL Server";
            }
        }

        return new NormalizedDatabaseDto
        {
            Category = Category,
            SubCategory = SubCategory,
            Cloud = product.VendorName,
            InstanceName = instanceName,
            Region = product.Region,
            DatabaseEngine = databaseEngine,
            VCpu = int.TryParse(vcpuStr, out var cpu) ? cpu : null,
            Memory = !string.IsNullOrWhiteSpace(memory) ? memory : null,
            PricePerHour = GetPricePerHour(product)
        };
    }

    public static NormalizedCloudFunctionDto MapToCloudFunction(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var functionName = product.Attributes.FirstOrDefault(a => a.Key == "group")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value
                          ?? product.Service;

        return new NormalizedCloudFunctionDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            FunctionName = functionName,
            Region = product.Region,
            PricePerRequest = GetPricePerRequest(product),
            PricePerGbSecond = GetPricePerGbSecond(product)
        };
    }

    public static NormalizedKubernetesDto MapToKubernetes(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        // For AWS, use the service name (AmazonEKS)
        // For Azure and GCP, use meterName or productName which contains the cluster tier/type
        var clusterName = product.VendorName switch
        {
            CloudProvider.AWS => product.Service,
            CloudProvider.Azure => product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                                    ?? product.Attributes.FirstOrDefault(a => a.Key == "productName")?.Value
                                    ?? product.Service,
            CloudProvider.GCP => product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value
                                 ?? product.Attributes.FirstOrDefault(a => a.Key == "machineType")?.Value
                                 ?? product.Service,
            _ => product.Attributes.FirstOrDefault(a => a.Key == "usageType")?.Value
                ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                ?? product.Service
        };

        // Fallback to service name if cluster name is still empty
        if (string.IsNullOrWhiteSpace(clusterName))
        {
            clusterName = product.Service;
        }

        var nodeType = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value
                      ?? product.Attributes.FirstOrDefault(a => a.Key == "productName")?.Value
                      ?? product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value;

        return new NormalizedKubernetesDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            ClusterName = clusterName,
            Region = product.Region,
            NodeType = nodeType,
            PricePerHour = GetPricePerHour(product)
        };
    }

    public static NormalizedApiGatewayDto MapToApiGateway(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var name = product.Attributes.FirstOrDefault(a => a.Key == "group")?.Value
                  ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                  ?? product.Service;

        return new NormalizedApiGatewayDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            Name = name,
            Region = product.Region,
            PricePerRequest = GetPricePerRequest(product),
            PricePerMonth = GetPricePerMonth(product)
        };
    }

    public static NormalizedLoadBalancerDto MapToLoadBalancer(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var name = product.Attributes.FirstOrDefault(a => a.Key == "usageType")?.Value
                  ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                  ?? product.Attributes.FirstOrDefault(a => a.Key == "group")?.Value
                  ?? product.Service;

        return new NormalizedLoadBalancerDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            Name = name,
            PricePerMonth = GetPricePerMonth(product)
        };
    }

    public static NormalizedBlobStorageDto MapToBlobStorage(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var name = product.Attributes.FirstOrDefault(a => a.Key == "storageClass")?.Value
                  ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                  ?? product.Service;

        var storageClass = product.Attributes.FirstOrDefault(a => a.Key == "storageClass")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "volumeType")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value;

        return new NormalizedBlobStorageDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            Name = name,
            Region = product.Region,
            StorageClass = storageClass,
            PricePerGbMonth = GetPricePerGbMonth(product),
            PricePerRequest = GetPricePerRequest(product)
        };
    }

    public static NormalizedResourceDto MapToNormalizedResource(
        CloudPricingProductDto product, ResourceCategory category, ResourceSubCategory subCategory)
    {
        var attributes = product.Attributes.ToDictionary(a => a.Key, a => a.Value);

        return new NormalizedResourceDto
        {
            Cloud = product.VendorName,
            Service = product.Service,
            Region = product.Region,
            Category = category,
            SubCategory = subCategory,
            ProductFamily = product.ProductFamily,
            ResourceName = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value,
            PricePerHour = GetPricePerHour(product),
            Attributes = attributes
        };
    }

    private static decimal? GetPricePerHour(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        if (price == null) return null;

        // Convert daily pricing to hourly
        if (price.Unit?.Contains("Day", StringComparison.OrdinalIgnoreCase) == true)
        {
            return price.Usd / 24m; // Convert daily to hourly
        }

        // Assume hourly pricing if unit contains "hour" or "Hrs"
        // Other pricing units (per-request, per-GB, etc.) are handled by specific methods
        return price.Usd;
    }

    private static decimal? GetPricePerRequest(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        if (price?.Unit?.Contains("request", StringComparison.OrdinalIgnoreCase) == true)
        {
            return price.Usd;
        }
        return null;
    }

    private static decimal? GetPricePerGbSecond(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        if (price?.Unit?.Contains("GB-s", StringComparison.OrdinalIgnoreCase) == true ||
            price?.Unit?.Contains("GBs", StringComparison.OrdinalIgnoreCase) == true)
        {
            return price.Usd;
        }
        return null;
    }

    private static decimal? GetPricePerMonth(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        if (price?.Unit?.Contains("month", StringComparison.OrdinalIgnoreCase) == true)
        {
            return price.Usd;
        }
        // Convert hourly price to monthly if available
        var hourlyPrice = GetPricePerHour(product);
        if (hourlyPrice.HasValue)
        {
            return hourlyPrice.Value * 730; // Average hours per month
        }
        return null;
    }

    private static decimal? GetPricePerGbMonth(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        if (price?.Unit?.Contains("GB", StringComparison.OrdinalIgnoreCase) == true)
        {
            return price.Usd;
        }
        return null;
    }

    public static NormalizedContainerInstanceDto MapToContainerInstance(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var containerName = product.Attributes.FirstOrDefault(a => a.Key == "usageType")?.Value
                           ?? product.Attributes.FirstOrDefault(a => a.Key == "productName")?.Value
                           ?? product.Service;

        return new NormalizedContainerInstanceDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            ContainerName = containerName,
            Region = product.Region,
            PricePerHour = GetPricePerHour(product)
        };
    }

    public static NormalizedDataWarehouseDto MapToDataWarehouse(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var warehouseName = product.Attributes.FirstOrDefault(a => a.Key == "servicename")?.Value
                           ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                           ?? product.Attributes.FirstOrDefault(a => a.Key == "resourceGroup")?.Value
                           ?? product.Service;

        var nodeType = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value
                      ?? product.Attributes.FirstOrDefault(a => a.Key == "productName")?.Value
                      ?? product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value;

        return new NormalizedDataWarehouseDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            WarehouseName = warehouseName,
            Region = product.Region,
            NodeType = nodeType,
            PricePerHour = GetPricePerHour(product)
        };
    }

    public static NormalizedCachingDto MapToCaching(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var cacheName = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value
                       ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                       ?? product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value
                       ?? product.Service;

        var cacheEngine = "Redis";

        var vcpuStr = product.Attributes.FirstOrDefault(a => a.Key == "vcpu")?.Value;

        var memory = product.Attributes.FirstOrDefault(a => a.Key == "memory")?.Value;

        return new NormalizedCachingDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            CacheName = cacheName,
            Region = product.Region,
            CacheEngine = cacheEngine,
            VCpu = int.TryParse(vcpuStr, out var cpu) ? cpu : null,
            Memory = !string.IsNullOrWhiteSpace(memory) ? memory : null,
            PricePerHour = GetPricePerHour(product)
        };
    }

    public static NormalizedMessagingDto MapToMessaging(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var messagingService = product.Attributes.FirstOrDefault(a => a.Key == "servicename")?.Value
                              ?? product.Attributes.FirstOrDefault(a => a.Key == "productName")?.Value
                              ?? product.Service;

        var messageType = product.Attributes.FirstOrDefault(a => a.Key == "group")?.Value
                         ?? product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value
                         ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value;

        return new NormalizedMessagingDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            MessagingService = messagingService,
            Region = product.Region,
            MessageType = messageType,
            PricePerMonth = GetPricePerMonth(product)
        };
    }

    public static NormalizedQueuingDto MapToQueueing(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var queuingService = product.Attributes.FirstOrDefault(a => a.Key == "servicename")?.Value
                            ?? product.Attributes.FirstOrDefault(a => a.Key == "productName")?.Value
                            ?? product.Service;

        var operationType = product.Attributes.FirstOrDefault(a => a.Key == "group")?.Value
                           ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value;

        var queueType = product.Attributes.FirstOrDefault(a => a.Key == "queueType")?.Value;

        return new NormalizedQueuingDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            QueuingService = queuingService,
            Region = product.Region,
            OperationType = operationType,
            QueueType = queueType,
            PricePerMonth = GetPricePerMonth(product)
        };
    }

    public static NormalizedMonitoringDto MapToMonitoring(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var monitoringService = product.Attributes.FirstOrDefault(a => a.Key == "servicename")?.Value
                               ?? product.Attributes.FirstOrDefault(a => a.Key == "productName")?.Value
                               ?? product.Service;

        var metricType = product.Attributes.FirstOrDefault(a => a.Key == "group")?.Value
                        ?? product.Attributes.FirstOrDefault(a => a.Key == "resourceGroup")?.Value
                        ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value;

        return new NormalizedMonitoringDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            Region = product.Region,
            MetricType = metricType,
            PricePerMonth = GetPricePerMonth(product),
            MonitoringService = monitoringService
        };
    }

    public static NormalizedCdnDto MapToCdn(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var cdnName = product.Attributes.FirstOrDefault(a => a.Key == "group")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value
                     ?? product.Service;

        var edgeLocation = product.Attributes.FirstOrDefault(a => a.Key == "location")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "region")?.Value;

        return new NormalizedCdnDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            CdnName = cdnName,
            Region = product.Region,
            EdgeLocation = edgeLocation,
            PricePerGbOut = GetPricePerGbOut(product),
            PricePerRequest = GetPricePerRequest(product)
        };
    }

    public static NormalizedIdentityManagementDto MapToIdentityManagement(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var serviceName = product.Attributes.FirstOrDefault(a => a.Key == "servicename")?.Value
                         ?? product.Attributes.FirstOrDefault(a => a.Key == "productName")?.Value
                         ?? product.Service;

        var operationType = product.Attributes.FirstOrDefault(a => a.Key == "usagetype")?.Value
                           ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                           ?? product.Attributes.FirstOrDefault(a => a.Key == "resourceGroup")?.Value;

        return new NormalizedIdentityManagementDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            ServiceName = serviceName,
            Region = product.Region,
            OperationType = operationType,
            PricePerUser = GetPricePerUser(product),
            PricePerRequest = GetPricePerRequest(product),
            PricePerAuthentication = GetPricePerAuthentication(product)
        };
    }

    public static NormalizedWebApplicationFirewallDto MapToWebApplicationFirewall(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var firewallName = product.Attributes.FirstOrDefault(a => a.Key == "servicename")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "productName")?.Value
                          ?? product.Service;

        var firewallType = product.Attributes.FirstOrDefault(a => a.Key == "subcategory")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "resourceGroup")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value;

        var skuTier = product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value;

        return new NormalizedWebApplicationFirewallDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            FirewallName = firewallName,
            Region = product.Region,
            FirewallType = firewallType,
            SkuTier = skuTier,
            PricePerHour = GetPricePerHourForFirewall(product),
            PricePerGb = GetPricePerGbForFirewall(product),
            PricePerRule = GetPricePerRule(product)
        };
    }

    private static decimal? GetPricePerGbOut(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        if (price?.Unit?.Contains("GB", StringComparison.OrdinalIgnoreCase) == true &&
            !price.Unit.Contains("request", StringComparison.OrdinalIgnoreCase))
        {
            return price.Usd;
        }
        return null;
    }

    private static decimal? GetPricePerUser(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        if (price?.Unit?.Contains("user", StringComparison.OrdinalIgnoreCase) == true ||
            price?.Unit?.Contains("MAU", StringComparison.OrdinalIgnoreCase) == true)
        {
            return price.Usd;
        }
        return null;
    }

    private static decimal? GetPricePerAuthentication(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        var description = product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value ?? string.Empty;

        if (price?.Unit?.Contains("count", StringComparison.OrdinalIgnoreCase) == true &&
            (description.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
             description.Contains("verification", StringComparison.OrdinalIgnoreCase) ||
             description.Contains("SMS", StringComparison.OrdinalIgnoreCase)))
        {
            return price.Usd;
        }
        return null;
    }

    private static decimal? GetPricePerHourForFirewall(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        if (price?.Unit?.Contains("hour", StringComparison.OrdinalIgnoreCase) == true ||
            price?.Unit?.Contains("hourly", StringComparison.OrdinalIgnoreCase) == true)
        {
            return price.Usd;
        }
        return null;
    }

    private static decimal? GetPricePerGbForFirewall(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        if (price?.Unit?.Contains("gibibyte", StringComparison.OrdinalIgnoreCase) == true ||
            (price?.Unit?.Contains("GB", StringComparison.OrdinalIgnoreCase) == true &&
             !price.Unit.Contains("hour", StringComparison.OrdinalIgnoreCase)))
        {
            return price.Usd;
        }
        return null;
    }

    private static decimal? GetPricePerRule(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        var description = product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value ?? string.Empty;

        if (price?.Unit?.Contains("month", StringComparison.OrdinalIgnoreCase) == true &&
            (description.Contains("rule", StringComparison.OrdinalIgnoreCase) ||
             description.Contains("policy", StringComparison.OrdinalIgnoreCase)))
        {
            return price.Usd;
        }
        return null;
    }

    public static NormalizedBlockStorageDto MapToBlockStorage(
        CloudPricingProductDto product,
        ResourceCategory category,
        ResourceSubCategory subCategory)
    {
        var name = product.Attributes.FirstOrDefault(a => a.Key == "volumeType")?.Value
                  ?? product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value
                  ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                  ?? product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value
                  ?? product.Service;

        var volumeType = product.Attributes.FirstOrDefault(a => a.Key == "volumeType")?.Value
                        ?? product.Attributes.FirstOrDefault(a => a.Key == "skuName")?.Value
                        ?? product.Attributes.FirstOrDefault(a => a.Key == "resourceGroup")?.Value;

        var storageMedia = product.Attributes.FirstOrDefault(a => a.Key == "storageMedia")?.Value
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value;

        var maxIopsVolume = product.Attributes.FirstOrDefault(a => a.Key == "maxIopsvolume")?.Value
                           ?? product.Attributes.FirstOrDefault(a => a.Key == "maxIops")?.Value;

        var maxVolumeSize = product.Attributes.FirstOrDefault(a => a.Key == "maxVolumeSize")?.Value
                           ?? product.Attributes.FirstOrDefault(a => a.Key == "volumeSize")?.Value;

        var maxThroughputVolume = product.Attributes.FirstOrDefault(a => a.Key == "maxThroughputvolume")?.Value
                                 ?? product.Attributes.FirstOrDefault(a => a.Key == "maxThroughput")?.Value;

        return new NormalizedBlockStorageDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            Name = name,
            Region = product.Region,
            VolumeType = volumeType,
            StorageMedia = storageMedia,
            MaxIopsVolume = maxIopsVolume,
            MaxVolumeSize = maxVolumeSize,
            MaxThroughputVolume = maxThroughputVolume,
            PricePerGbMonth = GetPricePerGbMonth(product),
            PricePerIops = GetPricePerIops(product),
            PricePerSnapshot = GetPricePerSnapshot(product)
        };
    }

    private static decimal? GetPricePerIops(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        if (price?.Unit?.Contains("IOPS", StringComparison.OrdinalIgnoreCase) == true)
        {
            return price.Usd;
        }
        return null;
    }

    private static decimal? GetPricePerSnapshot(CloudPricingProductDto product)
    {
        var price = product.Prices.FirstOrDefault();
        var description = product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value ?? string.Empty;

        if (price?.Unit?.Contains("GB", StringComparison.OrdinalIgnoreCase) == true &&
            (description.Contains("snapshot", StringComparison.OrdinalIgnoreCase) ||
             description.Contains("backup", StringComparison.OrdinalIgnoreCase)))
        {
            return price.Usd;
        }
        return null;
    }
}