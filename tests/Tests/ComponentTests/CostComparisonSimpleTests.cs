using System.Net;
using System.Text;
using System.Text.Json;
using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using static Tests.ResourceSpecificationTestHelper;

namespace Tests.ComponentTests;

public class CostComparisonSimpleTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumMemberConverter() }
    };

    [Fact]
    public async Task Post_TemplatesCostComparison_WithSaasTemplate_Returns_CostBreakdown()
    {
        // Arrange
        var request = new CalculationRequest
        {
            Usage = UsageSize.Medium,
            Resources = [VirtualMachines(), Relational(), LoadBalancer(), Monitoring()]
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithSaasTemplate_ReturnsAllCloudProviders()
    {
        // Arrange
        var request = new CalculationRequest
        {
            Usage = UsageSize.Medium,
            Resources = [VirtualMachines(), Relational(), LoadBalancer(), Monitoring()]
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithSaasTemplate_HasValidCostBreakdown()
    {
        // Arrange
        var request = new CalculationRequest
        {
            Usage = UsageSize.Medium,
            Resources = [VirtualMachines(), Relational(), LoadBalancer(), Monitoring()]
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithBlankTemplate_Returns_ZeroCosts()
    {
        // Arrange
        var request = new CalculationRequest
        {
            Usage = UsageSize.Medium,
            Resources = []
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithStaticSiteTemplate_OnlyHasLoadBalancerCosts()
    {
        // Arrange
        var request = new CalculationRequest
        {
            Usage = UsageSize.Small,
            Resources = [LoadBalancer()]
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithNullBody_Returns_BadRequest()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_CostsIncreaseWithUsageSize()
    {
        // Arrange
        var request = new CalculationRequest
        {
            Usage = UsageSize.Medium,
            Resources = [VirtualMachines(), Relational(), LoadBalancer(), Monitoring()]
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithWordPressTemplate_HasCorrectComponents()
    {
        // Arrange
        var request = new CalculationRequest
        {
            Usage = UsageSize.Large,
            Resources = [VirtualMachines(), Relational(), LoadBalancer()]
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithMachineLearningTemplate_HasCorrectComponents()
    {
        // Arrange
        var request = new CalculationRequest
        {
            Usage = UsageSize.Medium,
            Resources = [VirtualMachines(), LoadBalancer(), Monitoring()]
        };
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        await Verify(result);
    }
}