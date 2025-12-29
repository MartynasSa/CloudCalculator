using Application.Facade;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public class TemplatesController(ITemplateFacade templateFacade) : Controller
{
    [HttpGet("template")]
    public async Task<IActionResult> GetTemplate([FromQuery] TemplateType template, [FromQuery] UsageSize usage, CancellationToken ct)
    {
        if (template == TemplateType.None)
        {
            return BadRequest("Template must be specified with a valid value");
        }

        if (!Enum.IsDefined(typeof(UsageSize), usage) || usage == default(UsageSize))
        {
            return BadRequest("Usage must be specified with a valid value");
        }
        
        var result = await templateFacade.GetTemplateAsync(template, usage);
        return Ok(result);
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates(CancellationToken ct)
    {

        var result = templateFacade.GetTemplates();
        return Ok(result);
    }
}