using Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;
[Route("api/cloud-pricing")]
public class CloudPricingController(ICloudPricingRepository cloudPricingRepository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Calculate(CancellationToken ct)
    {
        var result = await cloudPricingRepository.GetAllAsync(ct);
        return Ok(result);
    }
}
