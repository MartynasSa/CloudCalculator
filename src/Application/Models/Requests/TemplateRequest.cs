using Application.Models.Enums;

namespace Application.Models.Dtos;

public class TemplateRequest
{   
    public TemplateType Template { get; set; }
    public UsageSize Usage { get; set; }
}
