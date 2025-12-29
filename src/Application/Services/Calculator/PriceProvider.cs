using Application.Extensions;
using Application.Models.Dtos;
using Application.Models.Enums;

namespace Application.Services.Calculator;

public interface IPriceProvider
{
    FilteredResourcesDto GetPrices(CategorizedResourcesDto categorizedResources);
}

public class PriceProvider : IPriceProvider
{
    public FilteredResourcesDto GetPrices(CategorizedResourcesDto categorizedResources)
    {
        var result = new FilteredResourcesDto();

        var vmBySize = GetVm(categorizedResources.ComputeInstances);
        var databaseBySize = GetDatabase(categorizedResources.Databases, 0, 0);
        var cloudFunctionBySize = GetCloudFunction(categorizedResources.CloudFunctions);
        var kubernetsBySize = GetKubernetesCluster(categorizedResources.Kubernetes);
        var loadBalancerBySize = GetLoadBalancer(categorizedResources.LoadBalancers);
        var apiGatewayBySize = GetApiGateway(categorizedResources.ApiGateways);
        var blobStorageBySize = GetBlobStorage(categorizedResources.BlobStorage);
        var monitoringBySize = GetMonitoring(categorizedResources.Monitoring);

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            foreach (CloudProvider cloudProvider in Enum.GetValues<CloudProvider>())
            {
                var key = (usageSize, cloudProvider);

                if (vmBySize.TryGetValue(usageSize, out var vms))
                {
                    var vm = vms.FirstOrDefault(v => v.Cloud == cloudProvider);
                    if (vm != null)
                    {
                        result.ComputeInstances[key] = vm;
                    }
                }

                if (databaseBySize.TryGetValue(usageSize, out var databases))
                {
                    var db = databases.FirstOrDefault(d => d.Cloud == cloudProvider);
                    if (db != null)
                    {
                        result.Databases[key] = db;
                    }
                }

                if (cloudFunctionBySize.TryGetValue(usageSize, out var cloudFunctions))
                {
                    var cf = cloudFunctions.FirstOrDefault(c => c.Cloud == cloudProvider);
                    if (cf != null)
                    {
                        result.CloudFunctions[key] = cf;
                    }
                }

                if (kubernetsBySize.TryGetValue(usageSize, out var kubernetes))
                {
                    var k8s = kubernetes.FirstOrDefault(k => k.Cloud == cloudProvider);
                    if (k8s != null)
                    {
                        result.Kubernetes[key] = k8s;
                    }
                }

                if (loadBalancerBySize.TryGetValue(usageSize, out var loadBalancers))
                {
                    var lb = loadBalancers.FirstOrDefault(l => l.Cloud == cloudProvider);
                    if (lb != null)
                    {
                        result.LoadBalancers[key] = lb;
                    }
                }

                if (apiGatewayBySize.TryGetValue(usageSize, out var apiGateways))
                {
                    var ag = apiGateways.FirstOrDefault(a => a.Cloud == cloudProvider);
                    if (ag != null)
                    {
                        result.ApiGateways[key] = ag;
                    }
                }

                if (blobStorageBySize.TryGetValue(usageSize, out var blobStorage))
                {
                    var bs = blobStorage.FirstOrDefault(b => b.Cloud == cloudProvider);
                    if (bs != null)
                    {
                        result.BlobStorage[key] = bs;
                    }
                }

                if (monitoringBySize.TryGetValue(usageSize, out var monitoring))
                {
                    var mon = monitoring.FirstOrDefault(m => m.Cloud == cloudProvider);
                    if (mon != null)
                    {
                        result.Monitoring[key] = mon;
                    }
                }
            }
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedComputeInstanceDto>> GetVm(
        List<NormalizedComputeInstanceDto> instances)
    {
        var result = new Dictionary<UsageSize, List<NormalizedComputeInstanceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            var specs = GetVirtualMachineSpecs(usageSize);

            result[usageSize] = instances
                .Where(i => (i.VCpu ?? 0) >= specs.MinCpu)
                .Where(i => (ResourceParsingUtils.ParseMemory(i.Memory) ?? 0) >= specs.MinMemory)
                .Where(i => (i.PricePerHour ?? 0m) > 0m)
                .GroupBy(i => i.Cloud)
                .Select(g => g.OrderBy(i => i.PricePerHour ?? decimal.MaxValue).First())
                .ToList();
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedDatabaseDto>> GetDatabase(
        List<NormalizedDatabaseDto> databases,
        int minCpu,
        double minMemory)
    {
        var result = new Dictionary<UsageSize, List<NormalizedDatabaseDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = databases
                .Where(i => (i.VCpu ?? 0) >= minCpu)
                .Where(i => (ResourceParsingUtils.ParseMemory(i.Memory) ?? 0) >= minMemory)
                .Where(i => (i.PricePerHour ?? 0m) > 0m)
                .GroupBy(i => i.Cloud)
                .Select(g => g.OrderBy(i => i.PricePerHour ?? decimal.MaxValue).First())
                .ToList();
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedCloudFunctionDto>> GetCloudFunction(
        List<NormalizedCloudFunctionDto> cloudFunctions)
    {
        var result = new Dictionary<UsageSize, List<NormalizedCloudFunctionDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = cloudFunctions
                .GroupBy(f => f.Cloud)
                .Select(g => g.OrderBy(GetCloudFunctionPriceScore).First())
                .ToList();
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedKubernetesDto>> GetKubernetesCluster(
        List<NormalizedKubernetesDto> kubernetes)
    {
        var result = new Dictionary<UsageSize, List<NormalizedKubernetesDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = kubernetes
                .Where(k => (k.PricePerHour ?? 0m) > 0m)
                .GroupBy(k => k.Cloud)
                .Select(g => g.OrderBy(k => k.PricePerHour ?? decimal.MaxValue).First())
                .ToList();
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedLoadBalancerDto>> GetLoadBalancer(
        List<NormalizedLoadBalancerDto> loadBalancers)
    {
        var result = new Dictionary<UsageSize, List<NormalizedLoadBalancerDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = loadBalancers
                .GroupBy(lb => lb.Cloud)
                .Select(g => g.First())
                .ToList();
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedApiGatewayDto>> GetApiGateway(
        List<NormalizedApiGatewayDto> apiGateways)
    {
        var result = new Dictionary<UsageSize, List<NormalizedApiGatewayDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = apiGateways
                .GroupBy(g => g.Cloud)
                .Select(grp => grp.OrderBy(GetApiGatewayPriceScore).First())
                .ToList();
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedBlobStorageDto>> GetBlobStorage(
        List<NormalizedBlobStorageDto> blobStorage)
    {
        var result = new Dictionary<UsageSize, List<NormalizedBlobStorageDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetBlobLikeResource(blobStorage, ResourceSubCategory.BlobStorage);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedBlobStorageDto>> GetObjectStorage(
        List<NormalizedBlobStorageDto> objectStorage)
    {
        var result = new Dictionary<UsageSize, List<NormalizedBlobStorageDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetBlobLikeResource(objectStorage, ResourceSubCategory.ObjectStorage);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetContainerInstance(
        List<NormalizedResourceDto> containerInstances)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(containerInstances, ResourceSubCategory.ContainerInstances);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetCaching(
        List<NormalizedResourceDto> caching)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(caching, ResourceSubCategory.Caching);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetFileStorage(
        List<NormalizedResourceDto> fileStorage)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(fileStorage, ResourceSubCategory.FileStorage);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetBackup(
        List<NormalizedResourceDto> backups)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(backups, ResourceSubCategory.Backup);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetVpnGateway(
        List<NormalizedResourceDto> vpnGateways)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(vpnGateways, ResourceSubCategory.VpnGateway);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetDns(
        List<NormalizedResourceDto> dns)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(dns, ResourceSubCategory.Dns);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetCdn(
        List<NormalizedResourceDto> cdn)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(cdn, ResourceSubCategory.CDN);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetDataWarehouse(
        List<NormalizedResourceDto> dataWarehouses)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(dataWarehouses, ResourceSubCategory.DataWarehouse);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetStreaming(
        List<NormalizedResourceDto> streaming)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(streaming, ResourceSubCategory.Streaming);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetMachineLearning(
        List<NormalizedResourceDto> machineLearning)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(machineLearning, ResourceSubCategory.MachineLearning);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetQueueing(
        List<NormalizedResourceDto> queueing)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(queueing, ResourceSubCategory.Queueing);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetMessaging(
        List<NormalizedResourceDto> messaging)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(messaging, ResourceSubCategory.Messaging);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetSecrets(
        List<NormalizedResourceDto> secrets)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(secrets, ResourceSubCategory.Secrets);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedResourceDto>> GetCompliance(
        List<NormalizedResourceDto> compliance)
    {
        var result = new Dictionary<UsageSize, List<NormalizedResourceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResource(compliance, ResourceSubCategory.Compliance);
        }

        return result;
    }

    public Dictionary<UsageSize, Dictionary<CloudProvider, decimal>> GetLoadBalancerPrice(
        List<NormalizedLoadBalancerDto> loadBalancers,
        int usageHours)
    {
        var result = new Dictionary<UsageSize, Dictionary<CloudProvider, decimal>>();
        const decimal hoursPerMonth = 730m;

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            var pricesByCloud = new Dictionary<CloudProvider, decimal>();

            foreach (var loadBalancer in loadBalancers.GroupBy(lb => lb.Cloud))
            {
                var lb = loadBalancer.First();
                if (lb.PricePerMonth is null)
                {
                    pricesByCloud[lb.Cloud] = 0m;
                }
                else
                {
                    pricesByCloud[lb.Cloud] = (lb.PricePerMonth.Value / hoursPerMonth) * usageHours;
                }
            }

            result[usageSize] = pricesByCloud;
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedMonitoringDto>> GetMonitoring(
        List<NormalizedMonitoringDto> monitoring)
    {
        var result = new Dictionary<UsageSize, List<NormalizedMonitoringDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = monitoring
                .GroupBy(m => m.Cloud)
                .Select(g => g.First())
                .ToList();
        }

        return result;
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

    private static List<NormalizedResourceDto> GetGenericResource(
        IEnumerable<NormalizedResourceDto> resources,
        ResourceSubCategory subCategory)
    {
        return resources
            .Where(r => r.SubCategory == subCategory)
            .Where(r => (r.PricePerHour ?? 0m) > 0m)
            .GroupBy(r => r.Cloud)
            .Select(g => g.OrderBy(r => r.PricePerHour ?? decimal.MaxValue).First())
            .ToList();
    }

    private static List<NormalizedBlobStorageDto> GetBlobLikeResource(
        IEnumerable<NormalizedBlobStorageDto> storages,
        ResourceSubCategory subCategory)
    {
        return storages
            .Where(s => s.SubCategory == subCategory)
            .GroupBy(s => s.Cloud)
            .Select(g => g.OrderBy(GetBlobStoragePriceScore).First())
            .ToList();
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