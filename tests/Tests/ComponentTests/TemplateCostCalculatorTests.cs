using System.Net;
using System.Text;
using System.Text.Json;
using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.ComponentTests;

public class TemplateCostCalculatorTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumMemberConverter() }
    };

    [Fact]
    public async Task Post_CalculateTemplate_WithSaasMedium_Returns_CostBreakdown()
    {
        // Arrange
        var request = new CalculateTemplateRequest
        {
            Template = TemplateType.Saas,
            Usage = UsageSize.Medium
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate-template", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_CalculateTemplate_WithSaasMedium_ReturnsAllCloudProviders()
    {
        // Arrange
        var request = new CalculateTemplateRequest
        {
            Template = TemplateType.Saas,
            Usage = UsageSize.Medium
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate-template", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_CalculateTemplate_WithBlankTemplate_Returns_ZeroCosts()
    {
        // Arrange
        var request = new CalculateTemplateRequest
        {
            Template = TemplateType.Blank,
            Usage = UsageSize.Small
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate-template", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_CalculateTemplate_WithStaticSiteTemplate_HasExpectedComponents()
    {
        // Arrange
        var request = new CalculateTemplateRequest
        {
            Template = TemplateType.StaticSite,
            Usage = UsageSize.Small
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate-template", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_CalculateTemplate_CostsIncreaseWithUsageSize()
    {
        // Arrange
        var request = new CalculateTemplateRequest
        {
            Template = TemplateType.Saas,
            Usage = UsageSize.Small
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate-template", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_CalculateTemplate_WithNullBody_Returns_BadRequest()
    {
        // Arrange
        var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate-template", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}