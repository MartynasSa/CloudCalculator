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
        // Small: Basic setup with VM, DB, and monitoring
        [(TemplateType.Saas, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Medium: Added caching, queuing, and API management
        [(TemplateType.Saas, UsageSize.Medium)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Large: Kubernetes for scalability, multi-AZ DB, CDN
        [(TemplateType.Saas, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Multi-region, advanced analytics, streaming
        [(TemplateType.Saas, UsageSize.ExtraLarge)] =
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
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // ========== WordPress Template ==========
        // Small: Single VM with DB and CDN
        [(TemplateType.WordPress, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Monitoring,
        ],

        // Medium: Multiple VMs with caching
        [(TemplateType.WordPress, UsageSize.Medium)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Large: Kubernetes, multi-AZ database, WAF
        [(TemplateType.WordPress, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Multi-zone deployment with media offload
        [(TemplateType.WordPress, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // ========== REST API Template ==========
        // Small: VM with DB and basic routing
        [(TemplateType.RestApi, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Medium: Kubernetes with API Gateway
        [(TemplateType.RestApi, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Large: Kubernetes cluster with CDN and advanced routing
        [(TemplateType.RestApi, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Multi-region with NoSQL and streaming analytics
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
        // Small: Minimal storage and CDN
        [(TemplateType.StaticSite, UsageSize.Small)] =
        [
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Monitoring
        ],

        // Medium: CDN with edge functions
        [(TemplateType.StaticSite, UsageSize.Medium)] =
        [
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.CDN,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.Monitoring
        ],

        // Large: Multi-region CDN with edge computing
        [(TemplateType.StaticSite, UsageSize.Large)] =
        [
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.CDN,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Monitoring
        ],

        // XLarge: Global CDN with advanced edge optimization
        [(TemplateType.StaticSite, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.CDN,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.Monitoring
        ],

        // ========== Ecommerce Template ==========
        // Small: Basic e-commerce with VM, DB, and WAF
        [(TemplateType.Ecommerce, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Compliance,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // Medium: Kubernetes with caching and streaming analytics
        [(TemplateType.Ecommerce, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
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

        // Large: Advanced analytics and data warehouse
        [(TemplateType.Ecommerce, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
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

        // XLarge: Multi-region with advanced analytics
        [(TemplateType.Ecommerce, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.Relational,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
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
        // Small: VM with NoSQL and basic messaging
        [(TemplateType.MobileAppBackend, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Medium: Kubernetes with caching and streaming
        [(TemplateType.MobileAppBackend, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Large: Multi-partition NoSQL with comprehensive stack
        [(TemplateType.MobileAppBackend, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Multi-region with global tables and analytics
        [(TemplateType.MobileAppBackend, UsageSize.ExtraLarge)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Backup,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
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
        // Small: VM with caching and basic DB
        [(TemplateType.HeadlessFrontendApi, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.Relational,
            ResourceSubCategory.Caching,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Medium: Kubernetes with NoSQL and CDN
        [(TemplateType.HeadlessFrontendApi, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Large: Kubernetes cluster with messaging and streaming
        [(TemplateType.HeadlessFrontendApi, UsageSize.Large)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
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

        // XLarge: Multi-region with sharding and advanced caching
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
        // Small: VM with basic data warehouse and streaming
        [(TemplateType.DataAnalytics, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Medium: Kubernetes with larger data warehouse
        [(TemplateType.DataAnalytics, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.Compliance
        ],

        // Large: Multi-zone data warehouse with comprehensive pipeline
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

        // XLarge: Multi-region tiered storage with analytics
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
        // Small: VM with GPU or Cloud Functions for inference
        [(TemplateType.MachineLearning, UsageSize.Small)] =
        [
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Medium: Kubernetes with GPU autoscaling and ML platform
        [(TemplateType.MachineLearning, UsageSize.Medium)] =
        [
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.MLPlatforms,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Large: GPU pools with batch and online training
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
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Multi-region GPU clusters with advanced monitoring
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
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall,
            ResourceSubCategory.Compliance
        ],

        // ========== Serverless Event Driven Template ==========
        // Small: Cloud Functions with NoSQL and basic messaging
        [(TemplateType.ServerlessEventDriven, UsageSize.Small)] =
        [
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
        ],

        // Medium: Cloud Functions with caching and streaming
        [(TemplateType.ServerlessEventDriven, UsageSize.Medium)] =
        [
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
        ],

        // Large: Event mesh with multiple services and monitoring
        [(TemplateType.ServerlessEventDriven, UsageSize.Large)] =
        [
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.Caching,
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.CDN,
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Monitoring,
            ResourceSubCategory.WebApplicationFirewall
        ],

        // XLarge: Global event mesh with multi-region NoSQL
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
            .Select(template => GetTemplate(template, UsageSize.Small))
            .ToList();
    }

    public TemplateDto GetTemplate(TemplateType template, UsageSize usageSize)
    {
        var subCategories = _templateResourceMappings.GetValueOrDefault((template, usageSize), []);

        return new TemplateDto
        {
            Template = template,
            Resources = MapToResourcesDto(subCategories)
        };
    }

    private static ResourcesDto MapToResourcesDto(IEnumerable<ResourceSubCategory> subCategories)
    {
        var resources = new ResourcesDto();

        foreach (var subCategory in subCategories)
        {
            switch (subCategory)
            {
                case ResourceSubCategory.VirtualMachines:
                case ResourceSubCategory.CloudFunctions:
                case ResourceSubCategory.Kubernetes:
                case ResourceSubCategory.ContainerInstances:
                    resources.Computes.Add((ComputeType)subCategory);
                    break;

                case ResourceSubCategory.Relational:
                case ResourceSubCategory.NoSQL:
                case ResourceSubCategory.Caching:
                    resources.Databases.Add((DatabaseType)subCategory);
                    break;

                case ResourceSubCategory.ObjectStorage:
                case ResourceSubCategory.BlobStorage:
                case ResourceSubCategory.BlockStorage:
                case ResourceSubCategory.FileStorage:
                case ResourceSubCategory.Backup:
                    resources.Storages.Add((StorageType)subCategory);
                    break;

                case ResourceSubCategory.VpnGateway:
                case ResourceSubCategory.LoadBalancer:
                case ResourceSubCategory.ApiGateway:
                case ResourceSubCategory.Dns:
                case ResourceSubCategory.CDN:
                    resources.Networks.Add((NetworkingType)subCategory);
                    break;

                case ResourceSubCategory.DataWarehouse:
                case ResourceSubCategory.Streaming:
                case ResourceSubCategory.MachineLearning:
                    resources.Analytics.Add((AnalyticsType)subCategory);
                    break;

                case ResourceSubCategory.Queueing:
                case ResourceSubCategory.Messaging:
                case ResourceSubCategory.Secrets:
                case ResourceSubCategory.Compliance:
                case ResourceSubCategory.Monitoring:
                    resources.Management.Add((ManagementType)subCategory);
                    break;

                case ResourceSubCategory.WebApplicationFirewall:
                case ResourceSubCategory.IdentityManagement:
                    resources.Security.Add((SecurityType)subCategory);
                    break;

                case ResourceSubCategory.AIServices:
                case ResourceSubCategory.MLPlatforms:
                case ResourceSubCategory.IntelligentSearch:
                    resources.AI.Add((AIType)subCategory);
                    break;
                default:
                    break;
            }
        }

        return resources;
    }
}