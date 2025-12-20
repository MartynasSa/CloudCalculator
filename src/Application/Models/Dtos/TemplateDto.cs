using Application.Models.Enums;

namespace Application.Models.Dtos;

public class TemplateDto
{
    public TemplateType Template { get; set; }
    public UsageSize Usage { get; set; }
    public Dictionary<CloudProvider, TemplateVirtualMachineDto>? VirtualMachines { get; set; }
}

public class TemplateVirtualMachineDto : TemplateResourceDtoBase
{
    public required string InstanceName { get; set; }
    public double CpuCores { get; set; }
    public double Memory { get; set; }
}

public class TemplateResourceDtoBase
{
    public required decimal PricePerMonth { get; set; }
}