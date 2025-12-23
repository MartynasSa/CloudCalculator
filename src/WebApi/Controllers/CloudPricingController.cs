using Application.Facade;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api")]
public class CloudPricingController(ICloudPricingFileFacade cloudPricingFileFacade) : Controller
{
    [HttpGet("cloud-pricing:product-family-mappings")]
    public async Task<IActionResult> GetProductFamilyMappings([FromQuery] UsageSize? usage, CancellationToken ct)
    {
        var result = await cloudPricingFileFacade.GetCategorizedResourcesAsync(usage ?? UsageSize.Small, ct);
        return Ok(result);
    }
}
