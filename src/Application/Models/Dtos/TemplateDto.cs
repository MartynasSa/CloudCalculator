using Application.Models.Enums;

namespace Application.Models.Dtos;

public class TemplateDto
{
    public TemplateType Template { get; set; }
    public UsageSize Usage { get; set; }
    public Dictionary<CloudProvider, TemplateVirtualMachineDto>? VirtualMachines { get; set; }
    public Dictionary<CloudProvider, TemplateDatabaseDto>? Databases { get; set; }
    public Dictionary<CloudProvider, TemplateLoadBalancerDto>? LoadBalancers { get; set; }
    public Dictionary<CloudProvider, TemplateMonitoringDto>? Monitoring { get; set; }
}


public class CostCalcualtor
{
    public List<ResourceSubCategory> Resources { get; set; } = new();
}
public class TemplateVirtualMachineDto : TemplateResourceDtoBase
{
    public required string InstanceName { get; set; }
    public double CpuCores { get; set; }
    public double Memory { get; set; }
}

public class TemplateDatabaseDto : TemplateResourceDtoBase
{
    public required string InstanceName { get; set; }
    public double CpuCores { get; set; }
    public double Memory { get; set; }
    public string? DatabaseEngine { get; set; }
}

public class TemplateLoadBalancerDto : TemplateResourceDtoBase
{
    public required string Name { get; set; }
}

public class TemplateMonitoringDto : TemplateResourceDtoBase
{
    public required string Name { get; set; }
}

public class TemplateResourceDtoBase
{
    public required decimal PricePerMonth { get; set; }
}