using Application.Models.Dtos;

namespace Application.Ports;

public interface ICloudPricingRepository
{
    Task<CloudPricingDto> GetAllAsync(CancellationToken cancellationToken);

    Task<PagedResult<CloudPricingProductDto>> GetAllAsync(PaginationParameters pagination, CancellationToken cancellationToken);
}
