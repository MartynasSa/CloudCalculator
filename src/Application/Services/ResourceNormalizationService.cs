using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Ports;

namespace Application.Services;

public interface IResourceNormalizationService
{
    Task<List<NormalizedComputeInstanceDto>> GetNormalizedComputeInstancesAsync(CancellationToken cancellationToken = default);
    Task<List<NormalizedDatabaseDto>> GetNormalizedDatabasesAsync(CancellationToken cancellationToken = default);
}

public class ResourceNormalizationService(ICloudPricingRepository cloudPricingRepository) : IResourceNormalizationService
{
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

        return null;
    }

    private static string? GetMemory(CloudPricingProductDto product)
    {
        var memory = product.Attributes.FirstOrDefault(a => a.Key == "memory")?.Value;
        return string.IsNullOrWhiteSpace(memory) ? null : memory;
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
        return string.IsNullOrWhiteSpace(engine) ? null : engine;
    }
}
