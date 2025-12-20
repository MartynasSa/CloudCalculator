using Application.Models.Dtos;
using Application.Models.Enums;

namespace Application.Facade;

public interface ITemplateFacade
{
    TemplateDto GetTemplate(TemplateRequest request);
}

public class TemplateFacade : ITemplateFacade
{
    public TemplateDto GetTemplate(TemplateRequest request)
    {
        var result = new TemplateDto()
        {
            Template = request.Template,
            Usage = request.Usage,
        };

        switch (request.Template)
        {
            case TemplateType.Saas:
                result.VirtualMachines = GetVirtualMachines(request.Usage);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return result;
    }

    private Dictionary<CloudProvider, TemplateVirtualMachineDto> GetVirtualMachines(UsageSize usage)
    {
        return new Dictionary<CloudProvider, TemplateVirtualMachineDto>()
                {
                    {
                        CloudProvider.AWS,
                        usage switch
                        {
                            UsageSize.Small => new TemplateVirtualMachineDto()
                            {
                                InstanceName = "t4g.medium",
                                CpuCores = 2,
                                Memory = 4,
                                PricePerMonth = 12.19m,
                            },
                            UsageSize.Medium => new TemplateVirtualMachineDto()
                            {
                                InstanceName = "c6g.xlarge",
                                CpuCores = 4,
                                Memory = 8,
                                PricePerMonth = 47.96m,
                            },
                            UsageSize.Large => new TemplateVirtualMachineDto()
                            {
                                InstanceName = "c6g.2xlarge",
                                CpuCores = 8,
                                Memory = 16,
                                PricePerMonth = 95.85m,
                            },
                        }
                    },
                    {
                        CloudProvider.Azure,
                        usage switch
                        {
                            UsageSize.Small => new TemplateVirtualMachineDto()
                            {
                                InstanceName = "Standard_B2s_v2",
                                CpuCores = 2,
                                Memory = 4,
                                PricePerMonth = 15.11m,
                            },
                            UsageSize.Medium => new TemplateVirtualMachineDto()
                            {
                                InstanceName = "Standard_B2as_v2",
                                CpuCores = 4,
                                Memory = 8,
                                PricePerMonth = 30.37m,
                            },
                            UsageSize.Large => new TemplateVirtualMachineDto()
                            {
                                InstanceName = "Standard_B2ms",
                                CpuCores = 8,
                                Memory = 16,
                                PricePerMonth = 60.74m,
                            },
                        }
                    },
                    {
                        CloudProvider.GCP,
                        usage switch
                        {
                            UsageSize.Small => new TemplateVirtualMachineDto()
                            {
                                InstanceName = "Compute Engine t2d-standard-1",
                                CpuCores = 1,
                                Memory = 4,
                                PricePerMonth = 31.84m,
                            },
                            UsageSize.Medium => new TemplateVirtualMachineDto()
                            {
                                InstanceName = "Compute Engine t2d-standard-2",
                                CpuCores = 4,
                                Memory = 8,
                                PricePerMonth = 62.68m,
                            },
                            UsageSize.Large => new TemplateVirtualMachineDto()
                            {
                                InstanceName = "Compute Engine t2d-standard-4",
                                CpuCores = 8,
                                Memory = 16,
                                PricePerMonth = 124.36m,
                            },
                        }
                    },
                };
    }
}
