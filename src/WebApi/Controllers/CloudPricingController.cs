using Application.Facade;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api")]
public class CloudPricingController(ICloudPricingFileFacade cloudPricingFileFacade) : Controller
{
    [HttpGet("cloud-pricing")]
    public async Task<IActionResult> GetCloudPricing(CancellationToken ct)
    {
        var result = await cloudPricingFileFacade.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("cloud-pricing/categorized-resources")]
    public async Task<IActionResult> GetCategorizedResources(CancellationToken ct)
    {
        var result = await cloudPricingFileFacade.GetCategorizedResourcesAsync(ct);
        return Ok(result);
    }
}
