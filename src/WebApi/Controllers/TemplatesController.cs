using Application.Facade;
using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public class TemplatesController(ITemplateFacade templateFacade) : Controller
{
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplate([FromQuery] TemplateRequest request, CancellationToken ct)
    {
        if (request.Template == TemplateType.None || request.Usage == UsageSize.None)
        {
            return BadRequest("Template and Usage must be specified with valid values");
        }
        
        var result = await templateFacade.GetTemplateAsync(request);
        return Ok(result);
    }

    [HttpPost("templates/cost-comparison")]
    public async Task<IActionResult> GetCostComparison([FromBody] TemplateDto templateDto, CancellationToken ct)
    {
        if (templateDto.Template == TemplateType.None)
        {
            return BadRequest("Template and Usage must be specified with valid values");
        }
        
        var result = await templateFacade.CalculateCostComparisonsAsync(templateDto, ct);
        return Ok(result);
    }
}