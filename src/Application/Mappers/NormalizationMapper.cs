using Application.Models.Dtos;
using Application.Models.Enums;

namespace Application.Mappers;

public static class NormalizationMapper
{
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

        // Fallback to "Unknown" if still empty
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            instanceName = "Unknown";
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

        // Fallback to "Unknown" if still empty
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            instanceName = "Unknown";
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
                          ?? "Unknown";

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
        var clusterName = product.Attributes.FirstOrDefault(a => a.Key == "usageType")?.Value
                         ?? product.Attributes.FirstOrDefault(a => a.Key == "meterName")?.Value
                         ?? product.Attributes.FirstOrDefault(a => a.Key == "description")?.Value
                         ?? "Unknown";

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

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Unknown";
        }

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

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Unknown";
        }

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

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Unknown";
        }

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
        return product.Prices.FirstOrDefault()?.Usd;
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
                           ?? "Unknown";

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
                           ?? "Unknown";

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
                       ?? "Unknown";

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
                              ?? product.Service
                              ?? "Unknown";

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
                            ?? product.Service
                            ?? "Unknown";

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
                               ?? product.Service
                               ?? "Unknown";

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
                     ?? product.Service
                     ?? "Unknown";

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
                         ?? product.Service
                         ?? "Unknown";

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
                          ?? product.Service
                          ?? "Unknown";

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

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Unknown";
        }

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