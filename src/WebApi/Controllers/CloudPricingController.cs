using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Application.Facade;

namespace WebApi.Controllers;

[Route("api")]
public class CloudPricingController(ICloudPricingFileFacade cloudPricingFileFacade) : Controller
{
    [HttpGet("cloud-pricing")]
    public async Task<IActionResult> GetPricing([FromQuery] PricingRequest? pagination, CancellationToken ct)
    {
        pagination ??= new PricingRequest();
        var result = await cloudPricingFileFacade.GetOrCreatePagedAsync(pagination, ct);
        return Ok(result);
    }

    [HttpGet("cloud-pricing:options")]
    public async Task<IActionResult> Options(CancellationToken ct)
    {
        var result = await cloudPricingFileFacade.GetDistinctFiltersAsync(ct);
        return Ok(result);
    }

    [HttpGet("cloud-pricing:product-family-mappings")]
    public async Task<IActionResult> GetProductFamilyMappings([FromQuery] UsageSize? usage, CancellationToken ct)
    {
        var result = await cloudPricingFileFacade.GetCategorizedResourcesAsync(usage ?? UsageSize.Small, ct);
        return Ok(result);
    }
}
