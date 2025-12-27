using Application.Models.Dtos;
using Application.Models.Enums;

namespace Application.Services.Calculator;

public interface IPriceProvider
{
    Dictionary<UsageSize, NormalizedComputeInstanceDto?> GetVm(
        List<NormalizedComputeInstanceDto> instances,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedDatabaseDto?> GetDatabase(
        List<NormalizedDatabaseDto> databases,
        CloudProvider cloud,
        int minCpu,
        double minMemory);

    Dictionary<UsageSize, NormalizedCloudFunctionDto?> GetCloudFunction(
        List<NormalizedCloudFunctionDto> cloudFunctions,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedKubernetesDto?> GetKubernetesCluster(
        List<NormalizedKubernetesDto> kubernetes,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedLoadBalancerDto?> GetLoadBalancer(
        List<NormalizedLoadBalancerDto> loadBalancers,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedApiGatewayDto?> GetApiGateway(
        List<NormalizedApiGatewayDto> apiGateways,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedBlobStorageDto?> GetBlobStorage(
        List<NormalizedBlobStorageDto> blobStorage,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedBlobStorageDto?> GetObjectStorage(
        List<NormalizedBlobStorageDto> objectStorage,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetContainerInstance(
        List<NormalizedResourceDto> containerInstances,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetDatabaseStorage(
        List<NormalizedResourceDto> databaseStorage,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetCaching(
        List<NormalizedResourceDto> caching,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetFileStorage(
        List<NormalizedResourceDto> fileStorage,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetBackup(
        List<NormalizedResourceDto> backups,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetVpnGateway(
        List<NormalizedResourceDto> vpnGateways,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetDns(
        List<NormalizedResourceDto> dns,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetCdn(
        List<NormalizedResourceDto> cdn,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetDataWarehouse(
        List<NormalizedResourceDto> dataWarehouses,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetStreaming(
        List<NormalizedResourceDto> streaming,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetMachineLearning(
        List<NormalizedResourceDto> machineLearning,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetQueueing(
        List<NormalizedResourceDto> queueing,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetMessaging(
        List<NormalizedResourceDto> messaging,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetSecrets(
        List<NormalizedResourceDto> secrets,
        CloudProvider cloud);

    Dictionary<UsageSize, NormalizedResourceDto?> GetCompliance(
        List<NormalizedResourceDto> compliance,
        CloudProvider cloud);

    Dictionary<UsageSize, decimal> GetLoadBalancerPrice(
        List<NormalizedLoadBalancerDto> loadBalancers,
        CloudProvider cloud,
        int usageHours);

    Dictionary<UsageSize, NormalizedMonitoringDto?> GetMonitoring(
        List<NormalizedMonitoringDto> monitoring,
        CloudProvider cloud);
}

public class PriceProvider : IPriceProvider
{
    public Dictionary<UsageSize, NormalizedComputeInstanceDto?> GetVm(
        List<NormalizedComputeInstanceDto> instances,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedComputeInstanceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            var specs = GetVirtualMachineSpecs(usageSize);

            result[usageSize] = instances
                .Where(i => i.Cloud == cloud)
                .Where(i => (i.VCpu ?? 0) >= specs.MinCpu)
                .Where(i => (ResourceParsingUtils.ParseMemory(i.Memory) ?? 0) >= specs.MinMemory)
                .Where(i => (i.PricePerHour ?? 0m) > 0m)
                .OrderBy(i => i.PricePerHour ?? decimal.MaxValue)
                .FirstOrDefault();
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedDatabaseDto?> GetDatabase(
        List<NormalizedDatabaseDto> databases,
        CloudProvider cloud,
        int minCpu,
        double minMemory)
    {
        var result = new Dictionary<UsageSize, NormalizedDatabaseDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = databases
                .Where(i => i.Cloud == cloud)
                .Where(i => (i.VCpu ?? 0) >= minCpu)
                .Where(i => (ResourceParsingUtils.ParseMemory(i.Memory) ?? 0) >= minMemory)
                .Where(i => (i.PricePerHour ?? 0m) > 0m)
                .OrderBy(i => i.PricePerHour ?? decimal.MaxValue)
                .FirstOrDefault();
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedCloudFunctionDto?> GetCloudFunction(
        List<NormalizedCloudFunctionDto> cloudFunctions,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedCloudFunctionDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = cloudFunctions
                .Where(f => f.Cloud == cloud)
                .OrderBy(GetCloudFunctionPriceScore)
                .FirstOrDefault();
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedKubernetesDto?> GetKubernetesCluster(
        List<NormalizedKubernetesDto> kubernetes,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedKubernetesDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = kubernetes
                .Where(k => k.Cloud == cloud)
                .Where(k => (k.PricePerHour ?? 0m) > 0m)
                .OrderBy(k => k.PricePerHour ?? decimal.MaxValue)
                .FirstOrDefault();
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedLoadBalancerDto?> GetLoadBalancer(
        List<NormalizedLoadBalancerDto> loadBalancers,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedLoadBalancerDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = loadBalancers
                .Where(lb => lb.Cloud == cloud)
                .FirstOrDefault();
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedApiGatewayDto?> GetApiGateway(
        List<NormalizedApiGatewayDto> apiGateways,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedApiGatewayDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = apiGateways
                .Where(g => g.Cloud == cloud)
                .OrderBy(GetApiGatewayPriceScore)
                .FirstOrDefault();
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedBlobStorageDto?> GetBlobStorage(
        List<NormalizedBlobStorageDto> blobStorage,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedBlobStorageDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetBlobLikeResource(blobStorage, cloud, ResourceSubCategory.BlobStorage);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedBlobStorageDto?> GetObjectStorage(
        List<NormalizedBlobStorageDto> objectStorage,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedBlobStorageDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetBlobLikeResource(objectStorage, cloud, ResourceSubCategory.ObjectStorage);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetContainerInstance(
        List<NormalizedResourceDto> containerInstances,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(containerInstances, cloud, ResourceSubCategory.ContainerInstances);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetDatabaseStorage(
        List<NormalizedResourceDto> databaseStorage,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(databaseStorage, cloud, ResourceSubCategory.DatabaseStorage);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetCaching(
        List<NormalizedResourceDto> caching,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(caching, cloud, ResourceSubCategory.Caching);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetFileStorage(
        List<NormalizedResourceDto> fileStorage,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(fileStorage, cloud, ResourceSubCategory.FileStorage);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetBackup(
        List<NormalizedResourceDto> backups,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(backups, cloud, ResourceSubCategory.Backup);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetVpnGateway(
        List<NormalizedResourceDto> vpnGateways,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(vpnGateways, cloud, ResourceSubCategory.VpnGateway);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetDns(
        List<NormalizedResourceDto> dns,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(dns, cloud, ResourceSubCategory.Dns);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetCdn(
        List<NormalizedResourceDto> cdn,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(cdn, cloud, ResourceSubCategory.CDN);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetDataWarehouse(
        List<NormalizedResourceDto> dataWarehouses,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(dataWarehouses, cloud, ResourceSubCategory.DataWarehouse);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetStreaming(
        List<NormalizedResourceDto> streaming,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(streaming, cloud, ResourceSubCategory.Streaming);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetMachineLearning(
        List<NormalizedResourceDto> machineLearning,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(machineLearning, cloud, ResourceSubCategory.MachineLearning);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetQueueing(
        List<NormalizedResourceDto> queueing,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(queueing, cloud, ResourceSubCategory.Queueing);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetMessaging(
        List<NormalizedResourceDto> messaging,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(messaging, cloud, ResourceSubCategory.Messaging);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetSecrets(
        List<NormalizedResourceDto> secrets,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(secrets, cloud, ResourceSubCategory.Secrets);
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedResourceDto?> GetCompliance(
        List<NormalizedResourceDto> compliance,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedResourceDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(compliance, cloud, ResourceSubCategory.Compliance);
        }

        return result;
    }

    public Dictionary<UsageSize, decimal> GetLoadBalancerPrice(
        List<NormalizedLoadBalancerDto> loadBalancers,
        CloudProvider cloud,
        int usageHours)
    {
        var result = new Dictionary<UsageSize, decimal>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            var loadBalancer = loadBalancers
                .Where(lb => lb.Cloud == cloud)
                .FirstOrDefault();

            if (loadBalancer?.PricePerMonth is null)
            {
                result[usageSize] = 0m;
            }
            else
            {
                const decimal hoursPerMonth = 730m;
                result[usageSize] = (loadBalancer.PricePerMonth.Value / hoursPerMonth) * usageHours;
            }
        }

        return result;
    }

    public Dictionary<UsageSize, NormalizedMonitoringDto?> GetMonitoring(
        List<NormalizedMonitoringDto> monitoring,
        CloudProvider cloud)
    {
        var result = new Dictionary<UsageSize, NormalizedMonitoringDto?>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = monitoring
                .Where(m => m.Cloud == cloud)
                .FirstOrDefault();
        }

        return result;
    }

    private static (int MinCpu, double MinMemory) GetVirtualMachineSpecs(UsageSize usageSize)
    {
        return usageSize switch
        {
            UsageSize.None => (0, 0),
            UsageSize.Small => (2, 4),
            UsageSize.Medium => (4, 8),
            UsageSize.Large => (8, 16),
            UsageSize.ExtraLarge => (16, 32),
            _ => throw new ArgumentOutOfRangeException(nameof(usageSize)),
        };
    }

    private static NormalizedResourceDto? GetGenericResource(
        IEnumerable<NormalizedResourceDto> resources,
        CloudProvider cloud,
        ResourceSubCategory subCategory)
    {
        return resources
            .Where(r => r.Cloud == cloud)
            .Where(r => r.SubCategory == subCategory)
            .Where(r => (r.PricePerHour ?? 0m) > 0m)
            .OrderBy(r => r.PricePerHour ?? decimal.MaxValue)
            .FirstOrDefault();
    }

    private static NormalizedBlobStorageDto? GetBlobLikeResource(
        IEnumerable<NormalizedBlobStorageDto> storages,
        CloudProvider cloud,
        ResourceSubCategory subCategory)
    {
        return storages
            .Where(s => s.Cloud == cloud)
            .Where(s => s.SubCategory == subCategory)
            .OrderBy(GetBlobStoragePriceScore)
            .FirstOrDefault();
    }

    private static decimal GetCloudFunctionPriceScore(NormalizedCloudFunctionDto function)
    {
        if ((function.PricePerRequest ?? 0m) > 0m)
        {
            return function.PricePerRequest!.Value;
        }

        if ((function.PricePerGbSecond ?? 0m) > 0m)
        {
            return function.PricePerGbSecond!.Value;
        }

        return decimal.MaxValue;
    }

    private static decimal GetApiGatewayPriceScore(NormalizedApiGatewayDto gateway)
    {
        if ((gateway.PricePerMonth ?? 0m) > 0m)
        {
            return gateway.PricePerMonth!.Value;
        }

        if ((gateway.PricePerRequest ?? 0m) > 0m)
        {
            return gateway.PricePerRequest!.Value;
        }

        return decimal.MaxValue;
    }

    private static decimal GetBlobStoragePriceScore(NormalizedBlobStorageDto storage)
    {
        if ((storage.PricePerGbMonth ?? 0m) > 0m)
        {
            return storage.PricePerGbMonth!.Value;
        }

        if ((storage.PricePerRequest ?? 0m) > 0m)
        {
            return storage.PricePerRequest!.Value;
        }

        return decimal.MaxValue;
    }
}