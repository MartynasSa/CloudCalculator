using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tests.ComponentTests;

public class CloudPricingControllerTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public async Task Get_ProductFamilyMappings_HasRequiredCategories()
    {
        var response = await Client.GetAsync("/api/cloud-pricing:product-family-mappings");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<CategorizedResourcesDto>(stream, JsonOptions);

        Assert.NotNull(result);
        
        // Verify we have the expected categories
        Assert.Contains(ResourceCategory.Compute, result.Categories.Keys);
        Assert.Contains(ResourceCategory.Databases, result.Categories.Keys);
        Assert.Contains(ResourceCategory.Networking, result.Categories.Keys);
        Assert.Contains(ResourceCategory.Management, result.Categories.Keys);
    }
}