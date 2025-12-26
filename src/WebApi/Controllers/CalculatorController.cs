using Application.Facade;
using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public class CalculatorController(ICalculatorFacade calculatorFacade) : Controller
{
    [HttpPost("calculator/calculate")]
    public async Task<IActionResult> GetCostComparison([FromBody] TemplateDto templateDto, CancellationToken ct)
    {
        if (templateDto.Template == TemplateType.None)
        {
            return BadRequest("Template must be specified with valid values");
        }

        var result = await calculatorFacade.CalculateCostComparisonsAsync(templateDto, ct);
        return Ok(result);
    }
}
