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
    private List<TemplateDto> _templates = new List<TemplateDto>()
    {
        new TemplateDto
        {
            Template = TemplateType.Blank
        },
        new TemplateDto
        {
            Template = TemplateType.Saas,
            Resources =
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            }
        },
        new TemplateDto
        {
            Template = TemplateType.WordPress,
            Resources =
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer
            }
        },
        new TemplateDto
        {
            Template = TemplateType.RestApi,
            Resources =
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            }
        },
        new TemplateDto
        {
            Template = TemplateType.StaticSite,
            Resources =
            {
                ResourceSubCategory.LoadBalancer
            }
        },
        new TemplateDto
        {
            Template = TemplateType.Ecommerce,
            Resources =
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            }
        },
        new TemplateDto
        {
            Template = TemplateType.MobileAppBackend,
            Resources =
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            }
        },
        new TemplateDto
        {
            Template = TemplateType.HeadlessFrontendApi,
            Resources =
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            }
        },
        new TemplateDto
        {
            Template = TemplateType.DataAnalytics,
            Resources =
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.Relational,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            }
        },
        new TemplateDto
        {
            Template = TemplateType.MachineLearning,
            Resources =
            {
                ResourceSubCategory.VirtualMachines,
                ResourceSubCategory.LoadBalancer,
                ResourceSubCategory.Monitoring
            }
        },
        new TemplateDto
        {
            Template = TemplateType.ServerlessEventDriven,
            Resources =
            {
                ResourceSubCategory.LoadBalancer
            }
        },
    };

    public List<TemplateDto> GetTemplates()
    {
        return _templates;
    }

    public TemplateDto GetTemplate(TemplateType template, UsageSize usageSize)
    {
        var baseTemplate = _templates.Single(x => x.Template == template);
        
        // Create a new instance with resources adjusted based on usage size
        var adjustedTemplate = new TemplateDto
        {
            Template = baseTemplate.Template,
            Resources = AdjustResourcesForUsageSize(baseTemplate.Resources.ToList(), usageSize)
        };
        
        return adjustedTemplate;
    }
    
    private List<ResourceSubCategory> AdjustResourcesForUsageSize(List<ResourceSubCategory> resources, UsageSize usageSize)
    {
        var adjustedResources = new List<ResourceSubCategory>(resources);
        
        // For Large and ExtraLarge usage sizes, replace VirtualMachines with Kubernetes
        if (usageSize == UsageSize.Large || usageSize == UsageSize.ExtraLarge)
        {
            if (adjustedResources.Contains(ResourceSubCategory.VirtualMachines))
            {
                adjustedResources.Remove(ResourceSubCategory.VirtualMachines);
                adjustedResources.Add(ResourceSubCategory.Kubernetes);
            }
        }
        
        return adjustedResources;
    }
}

