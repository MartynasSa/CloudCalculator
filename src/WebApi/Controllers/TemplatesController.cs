using Application.Facade;
using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api")]
public class TemplatesController(ITemplateFacade templateFacade) : Controller
{
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplate([FromQuery] TemplateRequest request, CancellationToken ct)
    {
        var result = templateFacade.GetTemplate(request);
        return Ok(result);
    }
}
