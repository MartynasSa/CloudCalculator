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
    /// <summary>
    /// Preferred database engines for cross-cloud comparison.
    /// PostgreSQL and MySQL are chosen because they are:
    /// 1. Open-source and available on all major cloud providers (AWS, Azure, GCP)
    /// 2. Have similar performance characteristics and pricing models across clouds
    /// 3. Most comparable for fair cost analysis compared to proprietary engines
    /// This preference ensures apples-to-apples pricing comparisons for database instances.
    /// </summary>
    private static readonly HashSet<string> PreferredDatabaseEngines = new(StringComparer.OrdinalIgnoreCase)
    {
        "SQL Server",
        "MySQL",
        "PostgreSQL",

    };

    public FilteredResourcesDto GetPrices(CategorizedResourcesDto categorizedResources)
    {
        var result = new FilteredResourcesDto();

        var vmBySize = GetVm(categorizedResources.ComputeInstances);
        var databaseBySize = GetDatabase(categorizedResources.Databases);
        var cloudFunctionBySize = GetCloudFunction(categorizedResources.CloudFunctions);
        var kubernetsBySize = GetKubernetesCluster(categorizedResources.Kubernetes);
        var loadBalancerBySize = GetLoadBalancer(categorizedResources.LoadBalancers);
        var apiGatewayBySize = GetApiGateway(categorizedResources.ApiGateways);
        var blobStorageBySize = GetBlobStorage(categorizedResources.BlobStorage);
        var monitoringBySize = GetMonitoring(categorizedResources.Monitoring);
        var cachingBySize = GetCaching(categorizedResources.Caching);
        var dataWarehouseBySize = GetDataWarehouse(categorizedResources.DataWarehouses);
        var messagingBySize = GetMessaging(categorizedResources.Messaging);
        var queueingBySize = GetQueueing(categorizedResources.Queueing);
        var cdnBySize = GetCdn(categorizedResources.CDN);
        var identityManagementBySize = GetIdentityManagement(categorizedResources.IdentityManagement);
        var webApplicationFirewallBySize = GetWebApplicationFirewall(categorizedResources.WebApplicationFirewall);
        var containerInstancesBySize = GetContainerInstance(categorizedResources.ContainerInstances);

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

                if (cachingBySize.TryGetValue(usageSize, out var caching))
                {
                    var cache = caching.FirstOrDefault(c => c.Cloud == cloudProvider);
                    if (cache != null)
                    {
                        result.Caching[key] = cache;
                    }
                }

                if (dataWarehouseBySize.TryGetValue(usageSize, out var dataWarehouses))
                {
                    var dw = dataWarehouses.FirstOrDefault(d => d.Cloud == cloudProvider);
                    if (dw != null)
                    {
                        result.DataWarehouses[key] = dw;
                    }
                }

                if (messagingBySize.TryGetValue(usageSize, out var messaging))
                {
                    var msg = messaging.FirstOrDefault(m => m.Cloud == cloudProvider);
                    if (msg != null)
                    {
                        result.Messaging[key] = msg;
                    }
                }

                if (queueingBySize.TryGetValue(usageSize, out var queueing))
                {
                    var queue = queueing.FirstOrDefault(q => q.Cloud == cloudProvider);
                    if (queue != null)
                    {
                        result.Queueing[key] = queue;
                    }
                }

                if (cdnBySize.TryGetValue(usageSize, out var cdn))
                {
                    var cdnItem = cdn.FirstOrDefault(c => c.Cloud == cloudProvider);
                    if (cdnItem != null)
                    {
                        result.CDN[key] = cdnItem;
                    }
                }

                if (identityManagementBySize.TryGetValue(usageSize, out var identityManagement))
                {
                    var idm = identityManagement.FirstOrDefault(i => i.Cloud == cloudProvider);
                    if (idm != null)
                    {
                        result.IdentityManagement[key] = idm;
                    }
                }

                if (webApplicationFirewallBySize.TryGetValue(usageSize, out var waf))
                {
                    var firewall = waf.FirstOrDefault(w => w.Cloud == cloudProvider);
                    if (firewall != null)
                    {
                        result.WebApplicationFirewall[key] = firewall;
                    }
                }

                if (containerInstancesBySize.TryGetValue(usageSize, out var containerInstances))
                {
                    var container = containerInstances.FirstOrDefault(c => c.Cloud == cloudProvider);
                    if (container != null)
                    {
                        result.ContainerInstances[key] = container;
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
            var specs = GetResourceSpecs(usageSize);

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
        List<NormalizedDatabaseDto> databases)
    {
        var result = new Dictionary<UsageSize, List<NormalizedDatabaseDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            var specs = GetResourceSpecs(usageSize);

            // Group by cloud and select the best option for each
            result[usageSize] = databases
                .Where(i => (i.PricePerHour ?? 0m) > 0m)
                .GroupBy(i => i.Cloud)
                .Select(cloudGroup =>
                {
                    // First, try to find databases with preferred engines that meet the specs
                    var withPreferredEngine = cloudGroup
                        .Where(i => i.DatabaseEngine != null && PreferredDatabaseEngines.Contains(i.DatabaseEngine))
                        .Where(i => i.VCpu != null && i.Memory != null)
                        .Where(i => (i.VCpu ?? 0) >= specs.MinCpu && 
                                    (ResourceParsingUtils.ParseMemory(i.Memory) ?? 0) >= specs.MinMemory)
                        .OrderBy(i => i.PricePerHour ?? decimal.MaxValue)
                        .FirstOrDefault();

                    if (withPreferredEngine != null) return withPreferredEngine;

                    // If no preferred engine found, try to find any database that meets the specs
                    var withSpecs = cloudGroup
                        .Where(i => i.VCpu != null && i.Memory != null)
                        .Where(i => (i.VCpu ?? 0) >= specs.MinCpu && 
                                    (ResourceParsingUtils.ParseMemory(i.Memory) ?? 0) >= specs.MinMemory)
                        .OrderBy(i => i.PricePerHour ?? decimal.MaxValue)
                        .FirstOrDefault();

                    // If we found one with specs, use it; otherwise fall back to cheapest available
                    return withSpecs ?? cloudGroup.OrderBy(i => i.PricePerHour ?? decimal.MaxValue).First();
                })
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
            // For cloud functions, we select the cheapest option but the implied usage increases with size
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
            var priceThreshold = GetKubernetesPriceThreshold(usageSize);

            result[usageSize] = kubernetes
                .Where(k => (k.PricePerHour ?? 0m) > 0m)
                .Where(k => (k.PricePerHour ?? 0m) <= priceThreshold)
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
            // For load balancers, we select the cheapest option but the implied capacity increases with size
            result[usageSize] = loadBalancers
                .GroupBy(lb => lb.Cloud)
                .Select(g => g.OrderBy(lb => lb.PricePerMonth ?? decimal.MaxValue).First())
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
            // For API gateways, we select the cheapest option but the implied usage increases with size
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
            // For blob storage, we select the cheapest option but the implied usage increases with size
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

    public Dictionary<UsageSize, List<NormalizedContainerInstanceDto>> GetContainerInstance(
        List<NormalizedContainerInstanceDto> containerInstances)
    {
        var result = new Dictionary<UsageSize, List<NormalizedContainerInstanceDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResourceTyped(containerInstances, ResourceSubCategory.ContainerInstances);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedCachingDto>> GetCaching(
        List<NormalizedCachingDto> caching)
    {
        var result = new Dictionary<UsageSize, List<NormalizedCachingDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResourceTyped(caching, ResourceSubCategory.Caching);
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

    public Dictionary<UsageSize, List<NormalizedCdnDto>> GetCdn(
        List<NormalizedCdnDto> cdn)
    {
        var result = new Dictionary<UsageSize, List<NormalizedCdnDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResourceTyped(cdn, ResourceSubCategory.CDN);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedDataWarehouseDto>> GetDataWarehouse(
        List<NormalizedDataWarehouseDto> dataWarehouses)
    {
        var result = new Dictionary<UsageSize, List<NormalizedDataWarehouseDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResourceTyped(dataWarehouses, ResourceSubCategory.DataWarehouse);
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

    public Dictionary<UsageSize, List<NormalizedMessagingDto>> GetMessaging(
        List<NormalizedMessagingDto> messaging)
    {
        var result = new Dictionary<UsageSize, List<NormalizedMessagingDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResourceTyped(messaging, ResourceSubCategory.Messaging);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedQueuingDto>> GetQueueing(
        List<NormalizedQueuingDto> queueing)
    {
        var result = new Dictionary<UsageSize, List<NormalizedQueuingDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResourceTyped(queueing, ResourceSubCategory.Queueing);
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

    public Dictionary<UsageSize, List<NormalizedIdentityManagementDto>> GetIdentityManagement(
        List<NormalizedIdentityManagementDto> identityManagement)
    {
        var result = new Dictionary<UsageSize, List<NormalizedIdentityManagementDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResourceTyped(identityManagement, ResourceSubCategory.IdentityManagement);
        }

        return result;
    }

    public Dictionary<UsageSize, List<NormalizedWebApplicationFirewallDto>> GetWebApplicationFirewall(
        List<NormalizedWebApplicationFirewallDto> webApplicationFirewall)
    {
        var result = new Dictionary<UsageSize, List<NormalizedWebApplicationFirewallDto>>();

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            result[usageSize] = GetGenericResourceTyped(webApplicationFirewall, ResourceSubCategory.WebApplicationFirewall);
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

    private const string EstimatedMonitoringMetricType = "Estimated Usage-Based Cost";

    public Dictionary<UsageSize, List<NormalizedMonitoringDto>> GetMonitoring(
        List<NormalizedMonitoringDto> monitoring)
    {
        var result = new Dictionary<UsageSize, List<NormalizedMonitoringDto>>();

        // Handle empty monitoring list gracefully
        if (monitoring == null || monitoring.Count == 0)
        {
            return result;
        }

        foreach (UsageSize usageSize in Enum.GetValues<UsageSize>())
        {
            // Calculate estimated monthly cost based on usage patterns
            var estimatedCosts = monitoring
                .GroupBy(m => m.Cloud)
                .Select(cloudGroup =>
                {
                    // Calculate total estimated cost for this cloud provider and usage size
                    decimal totalEstimatedCost = GetDefaultMonitoringCost(usageSize);
                    
                    // Return a representative monitoring item with the estimated monthly cost
                    var representative = cloudGroup.FirstOrDefault() ?? throw new InvalidOperationException($"No monitoring items found for cloud provider: {cloudGroup.Key}");
                    return new NormalizedMonitoringDto
                    {
                        Cloud = representative.Cloud,
                        Category = representative.Category,
                        SubCategory = representative.SubCategory,
                        MonitoringService = representative.MonitoringService,
                        Region = representative.Region,
                        MetricType = EstimatedMonitoringMetricType,
                        PricePerMonth = totalEstimatedCost
                    };
                })
                .ToList();

            result[usageSize] = estimatedCosts;
        }

        return result;
    }

    private static decimal GetDefaultMonitoringCost(UsageSize usageSize)
    {
        // Estimated costs based on typical monitoring usage patterns
        // These costs are reasonable across AWS CloudWatch, Azure Monitor, and GCP Cloud Logging
        // and scale with infrastructure usage size
        return usageSize switch
        {
            UsageSize.Small => 15m,       // $15/month - basic logs, metrics, and alerts
            UsageSize.Medium => 50m,      // $50/month - moderate logs, metrics, and alerts  
            UsageSize.Large => 150m,      // $150/month - high-volume logs, metrics, and alerts
            UsageSize.ExtraLarge => 350m, // $350/month - enterprise-scale monitoring
            _ => 15m
        };
    }

    private static (int MinCpu, double MinMemory) GetResourceSpecs(UsageSize usageSize)
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

    private static decimal GetKubernetesPriceThreshold(UsageSize usageSize)
    {
        return usageSize switch
        {
            UsageSize.Small => 0.2m,      // Up to $0.20/hour for small workloads
            UsageSize.Medium => 0.5m,     // Up to $0.50/hour for medium workloads
            UsageSize.Large => 1.0m,      // Up to $1.00/hour for large workloads
            UsageSize.ExtraLarge => decimal.MaxValue, // No limit for extra large
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

    private static List<T> GetGenericResourceTyped<T>(
        IEnumerable<T> resources,
        ResourceSubCategory subCategory) where T : NormalizedResource
    {
        return resources
            .Where(r => r.SubCategory == subCategory)
            .GroupBy(r => r.Cloud)
            .Select(g => g.OrderBy(r => GetResourcePrice(r)).First())
            .ToList();
    }

    private static decimal GetResourcePrice<T>(T resource) where T : NormalizedResource
    {
        return resource switch
        {
            NormalizedContainerInstanceDto ci => ci.PricePerHour ?? decimal.MaxValue,
            NormalizedCachingDto cache => cache.PricePerHour ?? decimal.MaxValue,
            NormalizedDataWarehouseDto dw => dw.PricePerHour ?? decimal.MaxValue,
            NormalizedMessagingDto msg => msg.PricePerMonth ?? decimal.MaxValue,
            NormalizedQueuingDto queue => queue.PricePerMonth ?? decimal.MaxValue,
            NormalizedCdnDto cdn => cdn.PricePerGbOut ?? cdn.PricePerRequest ?? decimal.MaxValue,
            NormalizedIdentityManagementDto idm => idm.PricePerUser ?? idm.PricePerRequest ?? idm.PricePerAuthentication ?? decimal.MaxValue,
            NormalizedWebApplicationFirewallDto waf => waf.PricePerHour ?? waf.PricePerGb ?? waf.PricePerRule ?? decimal.MaxValue,
            _ => decimal.MaxValue
        };
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