using System.Net;
using System.Text;
using System.Text.Json;
using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.ComponentTests;

public class TemplateCostComparisonControllerTests(WebApplicationFactory<Program> factory) : TestBase(factory)
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
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/templates/cost-comparison", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonDto>(stream, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(TemplateType.Saas, result.Template);
        Assert.NotEmpty(result.UsageBreakdowns);
        
        // Verify we have all three usage sizes
        Assert.Contains(result.UsageBreakdowns, ub => ub.Usage == UsageSize.Small);
        Assert.Contains(result.UsageBreakdowns, ub => ub.Usage == UsageSize.Medium);
        Assert.Contains(result.UsageBreakdowns, ub => ub.Usage == UsageSize.Large);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithSaasTemplate_ReturnsAllCloudProviders()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/templates/cost-comparison", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonDto>(stream, JsonOptions);

        Assert.NotNull(result);
        Assert.All(result.UsageBreakdowns, breakdown =>
        {
            Assert.Contains(CloudProvider.AWS, breakdown.CloudProviderCosts.Keys);
            Assert.Contains(CloudProvider.Azure, breakdown.CloudProviderCosts.Keys);
            Assert.Contains(CloudProvider.GCP, breakdown.CloudProviderCosts.Keys);
        });
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithSaasTemplate_HasValidCostBreakdown()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/templates/cost-comparison", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonDto>(stream, JsonOptions);

        Assert.NotNull(result);
        var smallBreakdown = result.UsageBreakdowns.First(ub => ub.Usage == UsageSize.Small);
        
        // Verify cost breakdown is present
        Assert.All(smallBreakdown.CloudProviderCosts.Values, cloudCost =>
        {
            Assert.NotNull(cloudCost.Breakdown);
            // Total should equal sum of all components
            var expectedTotal = 
                cloudCost.Breakdown.VirtualMachinesCost.GetValueOrDefault() +
                cloudCost.Breakdown.DatabasesCost.GetValueOrDefault() +
                cloudCost.Breakdown.LoadBalancersCost.GetValueOrDefault() +
                cloudCost.Breakdown.MonitoringCost.GetValueOrDefault();
            Assert.Equal(expectedTotal, cloudCost.TotalMonthlyPrice);
        });
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithBlankTemplate_Returns_ZeroCosts()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Blank,
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/templates/cost-comparison", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonDto>(stream, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(TemplateType.Blank, result.Template);
        
        Assert.All(result.UsageBreakdowns, breakdown =>
        {
            Assert.All(breakdown.CloudProviderCosts.Values, cloudCost =>
            {
                Assert.Equal(0m, cloudCost.TotalMonthlyPrice);
            });
        });
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithStaticSiteTemplate_OnlyHasLoadBalancerCosts()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.StaticSite,
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/templates/cost-comparison", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonDto>(stream, JsonOptions);

        Assert.NotNull(result);
        
        var mediumBreakdown = result.UsageBreakdowns.First(ub => ub.Usage == UsageSize.Medium);
        
        Assert.All(mediumBreakdown.CloudProviderCosts.Values, cloudCost =>
        {
            // Static site should not have VMs, Databases, or Monitoring
            Assert.False(cloudCost.Breakdown.VirtualMachinesCost.HasValue);
            Assert.False(cloudCost.Breakdown.DatabasesCost.HasValue);
            Assert.False(cloudCost.Breakdown.MonitoringCost.HasValue);
        });
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithInvalidTemplate_Returns_BadRequest()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.None,
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/templates/cost-comparison", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithNullBody_Returns_BadRequest()
    {
        // Arrange
        var content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/templates/cost-comparison", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_CostsIncreaseWithUsageSize()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Ecommerce,
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/templates/cost-comparison", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonDto>(stream, JsonOptions);

        Assert.NotNull(result);
        
        var smallBreakdown = result.UsageBreakdowns.First(ub => ub.Usage == UsageSize.Small);
        var mediumBreakdown = result.UsageBreakdowns.First(ub => ub.Usage == UsageSize.Medium);
        var largeBreakdown = result.UsageBreakdowns.First(ub => ub.Usage == UsageSize.Large);

        // For each cloud provider, costs should generally increase with usage size
        foreach (var cloudProvider in new[] { CloudProvider.AWS, CloudProvider.Azure, CloudProvider.GCP })
        {
            var smallCost = smallBreakdown.CloudProviderCosts[cloudProvider].TotalMonthlyPrice;
            var mediumCost = mediumBreakdown.CloudProviderCosts[cloudProvider].TotalMonthlyPrice;
            var largeCost = largeBreakdown.CloudProviderCosts[cloudProvider].TotalMonthlyPrice;

            Assert.True(mediumCost >= smallCost, $"{cloudProvider} medium cost should be >= small cost");
            Assert.True(largeCost >= mediumCost, $"{cloudProvider} large cost should be >= medium cost");
        }
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithWordPressTemplate_HasCorrectComponents()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.WordPress,
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/templates/cost-comparison", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonDto>(stream, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(TemplateType.WordPress, result.Template);
        
        var mediumBreakdown = result.UsageBreakdowns.First(ub => ub.Usage == UsageSize.Medium);
        
        // WordPress template should not have Monitoring
        Assert.All(mediumBreakdown.CloudProviderCosts.Values, cloudCost =>
        {
            Assert.False(cloudCost.Breakdown.MonitoringCost.HasValue);
        });
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithMachineLearningTemplate_HasCorrectComponents()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.MachineLearning,
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/templates/cost-comparison", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonDto>(stream, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(TemplateType.MachineLearning, result.Template);
        
        var largeBreakdown = result.UsageBreakdowns.First(ub => ub.Usage == UsageSize.Large);
        
        // Machine Learning template should not have Databases
        Assert.All(largeBreakdown.CloudProviderCosts.Values, cloudCost =>
        {
            Assert.False(cloudCost.Breakdown.DatabasesCost.HasValue);
        });
    }
}
