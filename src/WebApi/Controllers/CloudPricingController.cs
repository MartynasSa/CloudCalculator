using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Infrastructure;

namespace WebApi.Controllers;
[Route("api/cloud-pricing")]
public class CloudPricingController(ICloudPricingFileFacade cloudPricingFileFacade) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Calculate([FromQuery] PaginationParameters? pagination, CancellationToken ct)
    {
        pagination ??= new PaginationParameters();
        var result = await cloudPricingFileFacade.GetOrCreatePagedAsync(pagination, ct);
        return Ok(result);
    }
}
