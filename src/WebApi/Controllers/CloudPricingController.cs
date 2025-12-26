using Application.Facade;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api")]
public class CloudPricingController(ICloudPricingFileFacade cloudPricingFileFacade) : Controller
{
    [HttpGet("cloud-pricing:product-family-mappings")]
    public async Task<IActionResult> GetProductFamilyMappings(CancellationToken ct)
    {
        var result = await cloudPricingFileFacade.GetCategorizedResourcesAsync(ct);
        return Ok(result);
    }
}
