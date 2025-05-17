using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;
[Route("api/cloud-cost")]
public class CalculatorController : Controller
{
    [HttpPost(":calculate")]
    public async Task<IActionResult> Calculate()
    {
        return Ok();
    }
}
