using Application.Facade;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public class TemplatesController(ITemplateFacade templateFacade) : Controller
{
    [HttpGet("template")]
    public async Task<IActionResult> GetTemplate([FromQuery] TemplateType template, CancellationToken ct)
    {
        if (template == TemplateType.None)
        {
            return BadRequest("Template and Usage must be specified with valid values");
        }
        
        var result = await templateFacade.GetTemplateAsync(template);
        return Ok(result);
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates(CancellationToken ct)
    {

        var result = templateFacade.GetTemplates();
        return Ok(result);
    }
}