using Application.Facade;
using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public class TemplatesController(ITemplateFacade templateFacade) : Controller
{
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplate([FromQuery] TemplateRequest request, CancellationToken ct)
    {
        var result = await templateFacade.GetTemplateAsync(request);
        return Ok(result);
    }
}