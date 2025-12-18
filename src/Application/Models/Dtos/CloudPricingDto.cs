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
