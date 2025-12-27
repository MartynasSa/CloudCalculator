using Application.Models.Dtos;
using Application.Models.Enums;

namespace Application.Services.Calculator;

public interface IPriceProvider
{
    NormalizedComputeInstanceDto? GetVm(
        List<NormalizedComputeInstanceDto> instances,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedDatabaseDto? GetDatabase(
        List<NormalizedDatabaseDto> databases,
        CloudProvider cloud,
        UsageSize usageSize,
        int minCpu,
        double minMemory);

    NormalizedCloudFunctionDto? GetCloudFunction(
        List<NormalizedCloudFunctionDto> cloudFunctions,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedKubernetesDto? GetKubernetesCluster(
        List<NormalizedKubernetesDto> kubernetes,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedLoadBalancerDto? GetLoadBalancer(
        List<NormalizedLoadBalancerDto> loadBalancers,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedApiGatewayDto? GetApiGateway(
        List<NormalizedApiGatewayDto> apiGateways,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedBlobStorageDto? GetBlobStorage(
        List<NormalizedBlobStorageDto> blobStorage,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedBlobStorageDto? GetObjectStorage(
        List<NormalizedBlobStorageDto> objectStorage,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetContainerInstance(
        List<NormalizedResourceDto> containerInstances,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetDatabaseStorage(
        List<NormalizedResourceDto> databaseStorage,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetCaching(
        List<NormalizedResourceDto> caching,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetFileStorage(
        List<NormalizedResourceDto> fileStorage,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetBackup(
        List<NormalizedResourceDto> backups,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetVpnGateway(
        List<NormalizedResourceDto> vpnGateways,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetDns(
        List<NormalizedResourceDto> dns,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetCdn(
        List<NormalizedResourceDto> cdn,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetDataWarehouse(
        List<NormalizedResourceDto> dataWarehouses,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetStreaming(
        List<NormalizedResourceDto> streaming,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetMachineLearning(
        List<NormalizedResourceDto> machineLearning,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetQueueing(
        List<NormalizedResourceDto> queueing,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetMessaging(
        List<NormalizedResourceDto> messaging,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetSecrets(
        List<NormalizedResourceDto> secrets,
        CloudProvider cloud,
        UsageSize usageSize);

    NormalizedResourceDto? GetCompliance(
        List<NormalizedResourceDto> compliance,
        CloudProvider cloud,
        UsageSize usageSize);

    decimal GetLoadBalancerPrice(
        List<NormalizedLoadBalancerDto> loadBalancers,
        CloudProvider cloud,
        UsageSize usageSize,
        int usageHours);

    NormalizedMonitoringDto? GetMonitoring(
        List<NormalizedMonitoringDto> monitoring,
        CloudProvider cloud,
        UsageSize usageSize);
}

public class PriceProvider : IPriceProvider
{
    public NormalizedComputeInstanceDto? GetVm(
        List<NormalizedComputeInstanceDto> instances,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        var specs = GetVirtualMachineSpecs(usageSize);

        return instances
            .Where(i => i.Cloud == cloud)
            .Where(i => (i.VCpu ?? 0) >= specs.MinCpu)
            .Where(i => (ResourceParsingUtils.ParseMemory(i.Memory) ?? 0) >= specs.MinMemory)
            .Where(i => (i.PricePerHour ?? 0m) > 0m)
            .OrderBy(i => i.PricePerHour ?? decimal.MaxValue)
            .FirstOrDefault();
    }

    public NormalizedDatabaseDto? GetDatabase(
        List<NormalizedDatabaseDto> databases,
        CloudProvider cloud,
        UsageSize usageSize,
        int minCpu,
        double minMemory)
    {
        return databases
            .Where(i => i.Cloud == cloud)
            .Where(i => (i.VCpu ?? 0) >= minCpu)
            .Where(i => (ResourceParsingUtils.ParseMemory(i.Memory) ?? 0) >= minMemory)
            .Where(i => (i.PricePerHour ?? 0m) > 0m)
            .OrderBy(i => i.PricePerHour ?? decimal.MaxValue)
            .FirstOrDefault();
    }

    public NormalizedCloudFunctionDto? GetCloudFunction(
        List<NormalizedCloudFunctionDto> cloudFunctions,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return cloudFunctions
            .Where(f => f.Cloud == cloud)
            .OrderBy(GetCloudFunctionPriceScore)
            .FirstOrDefault();
    }

    public NormalizedKubernetesDto? GetKubernetesCluster(
        List<NormalizedKubernetesDto> kubernetes,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return kubernetes
            .Where(k => k.Cloud == cloud)
            .Where(k => (k.PricePerHour ?? 0m) > 0m)
            .OrderBy(k => k.PricePerHour ?? decimal.MaxValue)
            .FirstOrDefault();
    }

    public NormalizedLoadBalancerDto? GetLoadBalancer(
        List<NormalizedLoadBalancerDto> loadBalancers,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return loadBalancers
            .Where(lb => lb.Cloud == cloud)
            .FirstOrDefault();
    }

    public NormalizedApiGatewayDto? GetApiGateway(
        List<NormalizedApiGatewayDto> apiGateways,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return apiGateways
            .Where(g => g.Cloud == cloud)
            .OrderBy(GetApiGatewayPriceScore)
            .FirstOrDefault();
    }

    public NormalizedBlobStorageDto? GetBlobStorage(
        List<NormalizedBlobStorageDto> blobStorage,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetBlobLikeResource(blobStorage, cloud, ResourceSubCategory.BlobStorage);
    }

    public NormalizedBlobStorageDto? GetObjectStorage(
        List<NormalizedBlobStorageDto> objectStorage,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetBlobLikeResource(objectStorage, cloud, ResourceSubCategory.ObjectStorage);
    }

    public NormalizedResourceDto? GetContainerInstance(
        List<NormalizedResourceDto> containerInstances,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(containerInstances, cloud, ResourceSubCategory.ContainerInstances);
    }

    public NormalizedResourceDto? GetDatabaseStorage(
        List<NormalizedResourceDto> databaseStorage,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(databaseStorage, cloud, ResourceSubCategory.DatabaseStorage);
    }

    public NormalizedResourceDto? GetCaching(
        List<NormalizedResourceDto> caching,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(caching, cloud, ResourceSubCategory.Caching);
    }

    public NormalizedResourceDto? GetFileStorage(
        List<NormalizedResourceDto> fileStorage,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(fileStorage, cloud, ResourceSubCategory.FileStorage);
    }

    public NormalizedResourceDto? GetBackup(
        List<NormalizedResourceDto> backups,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(backups, cloud, ResourceSubCategory.Backup);
    }

    public NormalizedResourceDto? GetVpnGateway(
        List<NormalizedResourceDto> vpnGateways,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(vpnGateways, cloud, ResourceSubCategory.VpnGateway);
    }

    public NormalizedResourceDto? GetDns(
        List<NormalizedResourceDto> dns,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(dns, cloud, ResourceSubCategory.Dns);
    }

    public NormalizedResourceDto? GetCdn(
        List<NormalizedResourceDto> cdn,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(cdn, cloud, ResourceSubCategory.CDN);
    }

    public NormalizedResourceDto? GetDataWarehouse(
        List<NormalizedResourceDto> dataWarehouses,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(dataWarehouses, cloud, ResourceSubCategory.DataWarehouse);
    }

    public NormalizedResourceDto? GetStreaming(
        List<NormalizedResourceDto> streaming,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(streaming, cloud, ResourceSubCategory.Streaming);
    }

    public NormalizedResourceDto? GetMachineLearning(
        List<NormalizedResourceDto> machineLearning,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(machineLearning, cloud, ResourceSubCategory.MachineLearning);
    }

    public NormalizedResourceDto? GetQueueing(
        List<NormalizedResourceDto> queueing,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(queueing, cloud, ResourceSubCategory.Queueing);
    }

    public NormalizedResourceDto? GetMessaging(
        List<NormalizedResourceDto> messaging,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(messaging, cloud, ResourceSubCategory.Messaging);
    }

    public NormalizedResourceDto? GetSecrets(
        List<NormalizedResourceDto> secrets,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(secrets, cloud, ResourceSubCategory.Secrets);
    }

    public NormalizedResourceDto? GetCompliance(
        List<NormalizedResourceDto> compliance,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return GetGenericResource(compliance, cloud, ResourceSubCategory.Compliance);
    }

    public decimal GetLoadBalancerPrice(
        List<NormalizedLoadBalancerDto> loadBalancers,
        CloudProvider cloud,
        UsageSize usageSize,
        int usageHours)
    {
        var loadBalancer = loadBalancers
            .Where(lb => lb.Cloud == cloud)
            .FirstOrDefault();

        if (loadBalancer?.PricePerMonth is null)
        {
            return 0m;
        }

        const decimal hoursPerMonth = 730m;
        return (loadBalancer.PricePerMonth.Value / hoursPerMonth) * usageHours;
    }

    public NormalizedMonitoringDto? GetMonitoring(
        List<NormalizedMonitoringDto> monitoring,
        CloudProvider cloud,
        UsageSize usageSize)
    {
        return monitoring
            .Where(m => m.Cloud == cloud)
            .FirstOrDefault();
    }

    private static (int MinCpu, double MinMemory) GetVirtualMachineSpecs(UsageSize usageSize)
    {
        return usageSize switch
        {
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