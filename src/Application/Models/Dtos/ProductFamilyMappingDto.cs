using Application.Models.Enums;

namespace Application.Models.Dtos;

public class ProductFamilyMappingDto
{
    public required string ProductFamily { get; set; }
    public required string Service { get; set; }
    public required ResourceCategory Category { get; set; }
    public required ResourceSubCategory SubCategory { get; set; }
}

public class ProductFamilyMappingsDto
{
    public required List<ProductFamilyMappingDto> Mappings { get; set; } = new List<ProductFamilyMappingDto>();
}
