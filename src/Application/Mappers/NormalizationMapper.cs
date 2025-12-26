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
                          ?? product.Attributes.FirstOrDefault(a => a.Key == "usageType")?.Value
                          ?? product.Service;

        if (string.IsNullOrWhiteSpace(functionName))
        {
            functionName = "Unknown";
        }

        var runtime = product.Attributes.FirstOrDefault(a => a.Key == "runtime")?.Value
                     ?? product.Attributes.FirstOrDefault(a => a.Key == "productDescription")?.Value;

        var memory = product.Attributes.FirstOrDefault(a => a.Key == "memory")?.Value
                    ?? product.Attributes.FirstOrDefault(a => a.Key == "memorySize")?.Value;

        return new NormalizedCloudFunctionDto
        {
            Category = category,
            SubCategory = subCategory,
            Cloud = product.VendorName,
            FunctionName = functionName,
            Region = product.Region,
            Runtime = runtime,
            Memory = memory,
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
                         ?? product.Service;

        if (string.IsNullOrWhiteSpace(clusterName))
        {
            clusterName = "Unknown";
        }

        var nodeType = product.Attributes.FirstOrDefault(a => a.Key == "instanceType")?.Value
                      ?? product.Attributes.FirstOrDefault(a => a.Key == "vmSize")?.Value
                      ?? product.Attributes.FirstOrDefault(a => a.Key == "machineType")?.Value;

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
}
