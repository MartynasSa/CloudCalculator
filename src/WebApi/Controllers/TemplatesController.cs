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
    public async Task<IActionResult> GetTemplate([FromQuery] TemplateType template, CancellationToken ct)
    {
        if (template == TemplateType.None)
        {
            return BadRequest("Template and Usage must be specified with valid values");
        }
        
        var result = await templateFacade.GetTemplateAsync(template);
        return Ok(result);
    }
}