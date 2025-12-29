using Application.Models.Dtos;
using Application.Models.Enums;

namespace Application.Services;

public interface ITemplateService
{
    TemplateDto GetTemplate(TemplateType template, UsageSize usageSize);
    List<TemplateDto> GetTemplates();
}

public class TemplateService : ITemplateService
{
    // Centralized mapping: (TemplateType, UsageSize) -> Complete resource configuration
    private readonly Dictionary<(TemplateType Template, UsageSize Size), List<ResourceSubCategory>> _templateResourceMappings = new()
    {
        // ========== SaaS Template ==========
        // Small: VirtualMachines (stateless app), 1× Relational (standard), 1× small Caching
        [(TemplateType.Saas, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Medium: VirtualMachines (small cluster, autoscaling); Relational (multi-AZ read replica 1×); Caching (1× medium)
        [(TemplateType.Saas, UsageSize.Medium)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Large: Kubernetes (1–2 medium clusters), Relational (multi-AZ + 2–3 read replicas), Caching (clustered), CDN
        [(TemplateType.Saas, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Kubernetes (multi-region clusters), Relational (sharding or partitioned, replicas in each region), Caching (multi-region), Streaming to Analytics for BI
        [(TemplateType.Saas, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // ========== WordPress Template ==========
        // Small: 1× VM, 1× Relational (standard), CDN optional
        [(TemplateType.WordPress, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.BlobStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Medium: 2–3× VMs behind LoadBalancer or small Kubernetes; Relational with 1× read replica; Caching (page/object)
        [(TemplateType.WordPress, UsageSize.Medium)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.BlobStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Large: Kubernetes (medium cluster), Relational (multi-AZ + 2× replicas), CDN required; Caching (Redis cluster)
        [(TemplateType.WordPress, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.BlobStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Kubernetes (multi-zone), Relational (partition or read-heavy replicas), CDN + WAF, object storage offload for media
        [(TemplateType.WordPress, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.BlobStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // ========== REST API Template ==========
        // Small: CloudFunctions; 1× Relational or NoSQL; small Caching
        [(TemplateType.RestApi, UsageSize.Small)] =
        [
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Medium: Kubernetes (small), DB multi-AZ/read replica; Caching (1× medium), ApiGateway with throttling
        [(TemplateType.RestApi, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Large: Kubernetes (medium–large), DB (replicas + partition if needed), Caching (cluster), CDN for edge response caching
        [(TemplateType.RestApi, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Multi-region API deployments, global CDN, DB sharding or NoSQL partitioning, Streaming for logs/metrics analytics
        [(TemplateType.RestApi, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // ========== Static Site Template ==========
        // Small: Object/Blob storage hosting + CDN + LoadBalancer
        [(TemplateType.StaticSite, UsageSize.Small)] =
        [
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.CDN,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Monitoring
        ],

        // Medium: CDN with edge caching rules, optional CloudFunctions for personalization
        [(TemplateType.StaticSite, UsageSize.Medium)] =
        [
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.CDN,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.Monitoring
        ],

        // Large: Multi-region CDN, image optimization at edge, can add Queueing for build/deploy pipeline orchestration, LoadBalancer
        [(TemplateType.StaticSite, UsageSize.Large)] =
        [
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.CDN,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Monitoring
        ],

        // XLarge: Global CDN with multiple POP optimizations, edge CloudFunctions, advanced Monitoring and synthetic testing
        [(TemplateType.StaticSite, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.CDN,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.Monitoring
        ],

        // ========== Ecommerce Template ==========
        // Small: VirtualMachines; 1× Relational; small Caching; CDN
        [(TemplateType.Ecommerce, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Compliance,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Medium: Kubernetes (medium), Relational (multi-AZ + 1–2 replicas), Caching (cluster), Streaming for clickstream
        [(TemplateType.Ecommerce, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Compliance,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Large: Kubernetes (large), Relational (partition + replicas), Caching (multi-node), CDN, DataWarehouse for analytics
        [(TemplateType.Ecommerce, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Compliance,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Multi-region deployment, global CDN/WAF, relational sharding or scale-out, Streaming + DataWarehouse, robust Queueing/Messaging for order/event processing
        [(TemplateType.Ecommerce, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Compliance,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // ========== Mobile App Backend Template ==========
        // Small: VirtualMachines; 1× Relational; CDN for app assets
        [(TemplateType.MobileAppBackend, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Medium: Kubernetes (small), NoSQL partition scaling, Caching (hot keys), Streaming for analytics
        [(TemplateType.MobileAppBackend, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Large: Kubernetes (medium–large), NoSQL (multi-region or multi-partition), CDN, Messaging at scale
        [(TemplateType.MobileAppBackend, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Multi-region active-active, edge CDN, NoSQL global tables/replication, Streaming into analytics lake
        [(TemplateType.MobileAppBackend, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // ========== Headless Frontend API Template ==========
        // Small: CloudFunctions; 1× DB; small Caching; CDN
        [(TemplateType.HeadlessFrontendApi, UsageSize.Small)] =
        [
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Medium: Kubernetes (small), DB with read replicas or NoSQL partitioning, CDN with cache rules
        [(TemplateType.HeadlessFrontendApi, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Large: Kubernetes (medium–large), Caching (cluster), CDN, Streaming for audit/log events
        [(TemplateType.HeadlessFrontendApi, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Multi-region, global CDN, DB sharding/partitioning, aggressive caching & edge logic
        [(TemplateType.HeadlessFrontendApi, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // ========== Data Analytics Template ==========
        // Small: ObjectStorage + single DataWarehouse instance; basic Streaming (ETL)
        [(TemplateType.DataAnalytics, UsageSize.Small)] =
        [
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.Compliance
        ],

        // Medium: Larger DataWarehouse, Streaming with checkpoints, Kubernetes small cluster for batch transformations
        [(TemplateType.DataAnalytics, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.Compliance
        ],

        // Large: DataWarehouse with partitioning/materialized views, Streaming at scale, Kubernetes medium–large cluster
        [(TemplateType.DataAnalytics, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.Compliance
        ],

        // XLarge: Multi-zone DataWarehouse, tiered ObjectStorage, Streaming (multi-region), job orchestration, robust Monitoring/Compliance
        [(TemplateType.DataAnalytics, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Relational,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.Compliance
        ],

        // ========== Machine Learning Template ==========
        // Small: 1× VM (GPU) or small K8s with 1–2 GPU nodes; ObjectStorage; simple DB; CloudFunctions for inference
        [(TemplateType.MachineLearning, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.Compliance,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Medium: K8s with GPU autoscaling, feature store in NoSQL, Streaming for features/logs, CDN for model artifacts
        [(TemplateType.MachineLearning, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.MLPlatforms,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.Compliance,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Large: K8s (GPU pools across zones), batch/online training, model versioning, multi-stage Streaming, Relational + NoSQL combo
        [(TemplateType.MachineLearning, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.MLPlatforms,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.Compliance,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Multi-region GPU clusters, global inference endpoints behind ApiGateway/LB/CDN, advanced Monitoring (drift), Compliance controls
        [(TemplateType.MachineLearning, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.MLPlatforms,
            ResourceSubCategory.AIServices,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.Compliance,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // ========== Serverless Event Driven Template ==========
        // Small: CloudFunctions; 1× NoSQL; lightweight Queueing/Messaging
        [(TemplateType.ServerlessEventDriven, UsageSize.Small)] =
        [
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Medium: More functions with concurrency tuning; NoSQL partition scaling; Streaming for event pipelines
        [(TemplateType.ServerlessEventDriven, UsageSize.Medium)] =
        [
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Large: Event routing across multiple services, cross-account Queueing/Messaging, Streaming with consumer groups
        [(TemplateType.ServerlessEventDriven, UsageSize.Large)] =
        [
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Global event mesh, multi-region NoSQL replication, strict throttling/quota, extensive Monitoring
        [(TemplateType.ServerlessEventDriven, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // ========== Blank Template ==========
        // Small: Empty template
        [(TemplateType.Blank, UsageSize.Small)] =
        [
        ],

        // Medium: Empty template
        [(TemplateType.Blank, UsageSize.Medium)] =
        [
        ],

        // Large: Empty template
        [(TemplateType.Blank, UsageSize.Large)] =
        [
        ],

        // XLarge: Empty template
        [(TemplateType.Blank, UsageSize.ExtraLarge)] =
        [
        ],
    };

    public List<TemplateDto> GetTemplates()
    {
        return _templateResourceMappings
            .Select(kvp => kvp.Key.Template)
            .Distinct()
            .Select(template => new TemplateDto
            {
                Template = template,
                Resources = []
            })
            .ToList();
    }

    public TemplateDto GetTemplate(TemplateType template, UsageSize usageSize)
    {
        var resources = _templateResourceMappings.GetValueOrDefault((template, usageSize), []);

        return new TemplateDto
        {
            Template = template,
            Resources = resources
        };
    }
}