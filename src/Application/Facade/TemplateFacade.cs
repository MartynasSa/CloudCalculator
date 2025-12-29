using Application.Facade;
using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services;
using Application.Services.Normalization;

namespace Application.Facade;

public interface ITemplateFacade
{
    Task<TemplateDto> GetTemplateAsync(TemplateType templateType, UsageSize usageSize);

    List<TemplateDto> GetTemplates();
}

public class TemplateFacade(
    ITemplateService templateService) : ITemplateFacade
{
    public async Task<TemplateDto> GetTemplateAsync(TemplateType templateType, UsageSize usageSize)
    {
        return templateService.GetTemplate(templateType, usageSize);
    }

    public List<TemplateDto> GetTemplates()
    {
        return templateService.GetTemplates();
    }
}