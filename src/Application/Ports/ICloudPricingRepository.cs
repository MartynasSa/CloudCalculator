using Application.Models.Dtos;

namespace Application.Ports;

public interface ICloudPricingRepository
{
    Task<CloudPricingDto> GetAllAsync(CancellationToken cancellationToken);
    Task<IQueryable<CloudPricingProductDto>> GetProductsQueryableAsync(CancellationToken cancellationToken);
}
