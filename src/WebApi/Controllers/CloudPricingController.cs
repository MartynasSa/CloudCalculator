using Application.Models.Dtos;
using Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;
[Route("api/cloud-pricing")]
public class CloudPricingController(ICloudPricingRepository cloudPricingRepository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Calculate([FromQuery] PaginationParameters? pagination, CancellationToken ct)
    {
        pagination ??= new PaginationParameters();
        var result = await cloudPricingRepository.GetAllAsync(pagination, ct);
        return Ok(result);
    }
}
