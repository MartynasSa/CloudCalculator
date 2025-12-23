namespace Application.Models.Dtos;

public class ProductFamilyMappingDto
{
    public required string ProductFamily { get; set; }
    public required string Category { get; set; }
    public required string SubCategory { get; set; }
}

public class ProductFamilyMappingsDto
{
    public required List<ProductFamilyMappingDto> Mappings { get; set; } = new List<ProductFamilyMappingDto>();
}
