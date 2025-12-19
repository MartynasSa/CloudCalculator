using System.Text.Json.Serialization;

namespace Application.Models.Dtos;

public class CloudPricingDto
{
    public required CloudPricingDataDto Data { get; set; }
}

public class CloudPricingDataDto
{
    public List<CloudPricingProductDto> Products { get; set; } = new List<CloudPricingProductDto>();
}

public class CloudPricingProductDto
{
    public required string VendorName { get; set; }
    public required string Service { get; set; }
    public required string Region { get; set; }
    public required string ProductFamily { get; set; }
    public List<CloudPricingAttributeDto> Attributes { get; set; } = new List<CloudPricingAttributeDto>();
    public List<CloudPricingPriceDto> Prices { get; set; } = new List<CloudPricingPriceDto>();
}

public class CloudPricingAttributeDto
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}

public class CloudPricingPriceDto
{
    [JsonPropertyName("USD")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? Usd { get; set; }

    public string? Unit { get; set; }
}

public class CloudPricingAttributeSummary
{
    public required string Key { get; set; }
    public required List<string> Values { get; set; } = new List<string>();
}

public class DistinctFiltersDto
{
    public required List<string> VendorNames { get; set; } = new List<string>();
    public required List<string> Services { get; set; } = new List<string>();
    public required List<string> Regions { get; set; } = new List<string>();
    public required List<string> ProductFamilies { get; set; } = new List<string>();
    public required List<CloudPricingAttributeSummary> AttributeSummaries { get; set; } = new List<CloudPricingAttributeSummary>();
}
