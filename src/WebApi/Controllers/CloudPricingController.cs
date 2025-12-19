using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Infrastructure;

namespace WebApi.Controllers;
[Route("api/cloud-pricing")]
public class CloudPricingController(ICloudPricingFileFacade cloudPricingFileFacade) : Controller
{
    [HttpGet]
    public async Task<IActionResult> GetPricing([FromQuery] PricingRequest? pagination, CancellationToken ct)
    {
        pagination ??= new PricingRequest();
        var result = await cloudPricingFileFacade.GetOrCreatePagedAsync(pagination, ct);
        return Ok(result);
    }
}
