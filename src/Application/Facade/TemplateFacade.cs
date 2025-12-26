using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services;
using Application.Services.Normalization;

namespace Application.Facade;

public interface ITemplateFacade
{
    Task<TemplateDto> GetTemplateAsync(TemplateType templateType);

}

public class TemplateFacade(
    ITemplateService templateService) : ITemplateFacade
{
    public async Task<TemplateDto> GetTemplateAsync(TemplateType templateType)
    {
        return templateService.GetTemplate(templateType);
    }

}