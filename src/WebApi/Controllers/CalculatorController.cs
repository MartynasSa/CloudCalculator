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
    public async Task<IActionResult> GetCostComparison([FromBody] CalculationRequest templateDto, CancellationToken ct)
    {
        var result = await calculatorFacade.CalculateCostComparisonsAsync(templateDto, ct);
        return Ok(result);
    }
}
