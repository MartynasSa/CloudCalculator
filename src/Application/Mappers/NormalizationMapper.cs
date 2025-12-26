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
}
