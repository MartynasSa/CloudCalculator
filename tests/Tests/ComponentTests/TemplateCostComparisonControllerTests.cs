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
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.Relational, ResourceSubCategory.LoadBalancer, ResourceSubCategory.Monitoring }
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        Assert.NotNull(result);
        Assert.Equal(4, result.Resources.Count);
        Assert.NotEmpty(result.CloudCosts);
        
        // Verify we have all cloud providers and usage sizes (3 providers x 4 sizes = 12 entries)
        Assert.Equal(12, result.CloudCosts.Count);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithSaasTemplate_ReturnsAllCloudProviders()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.Relational, ResourceSubCategory.LoadBalancer, ResourceSubCategory.Monitoring }
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        Assert.NotNull(result);
        Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == CloudProvider.AWS);
        Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == CloudProvider.Azure);
        Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == CloudProvider.GCP);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithSaasTemplate_HasValidCostBreakdown()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.Relational, ResourceSubCategory.LoadBalancer, ResourceSubCategory.Monitoring }
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        Assert.NotNull(result);
        var smallAwsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS && cc.UsageSize == UsageSize.Small);
        
        // Verify cost breakdown is present
        var expectedTotal = smallAwsCost.CostDetails.Sum(cd => cd.Cost);
        Assert.Equal(expectedTotal, smallAwsCost.TotalMonthlyPrice);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithBlankTemplate_Returns_ZeroCosts()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Blank,
            Resources = { }
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        Assert.NotNull(result);
        Assert.Empty(result.Resources);
        
        Assert.All(result.CloudCosts, cloudCost =>
        {
            Assert.Equal(0m, cloudCost.TotalMonthlyPrice);
        });
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithStaticSiteTemplate_OnlyHasLoadBalancerCosts()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.StaticSite,
            Resources = { ResourceSubCategory.LoadBalancer }
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        Assert.NotNull(result);
        
        var mediumAwsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS && cc.UsageSize == UsageSize.Medium);
        
        // Static site should not have VMs, Databases, or Monitoring
        Assert.DoesNotContain(mediumAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.VirtualMachines);
        Assert.DoesNotContain(mediumAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Relational);
        Assert.DoesNotContain(mediumAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Monitoring);
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
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Ecommerce,
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.Relational, ResourceSubCategory.LoadBalancer, ResourceSubCategory.Monitoring }
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        Assert.NotNull(result);

        // For each cloud provider, costs should generally increase with usage size
        foreach (var cloudProvider in new[] { CloudProvider.AWS, CloudProvider.Azure, CloudProvider.GCP })
        {
            var smallCost = result.CloudCosts.First(cc => cc.CloudProvider == cloudProvider && cc.UsageSize == UsageSize.Small).TotalMonthlyPrice;
            var mediumCost = result.CloudCosts.First(cc => cc.CloudProvider == cloudProvider && cc.UsageSize == UsageSize.Medium).TotalMonthlyPrice;
            var largeCost = result.CloudCosts.First(cc => cc.CloudProvider == cloudProvider && cc.UsageSize == UsageSize.Large).TotalMonthlyPrice;

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
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.Relational, ResourceSubCategory.LoadBalancer }
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        Assert.NotNull(result);
        Assert.DoesNotContain(ResourceSubCategory.Monitoring, result.Resources);
        
        var mediumAwsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS && cc.UsageSize == UsageSize.Medium);
        
        // WordPress template should not have Monitoring
        Assert.DoesNotContain(mediumAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Monitoring);
    }

    [Fact]
    public async Task Post_TemplatesCostComparison_WithMachineLearningTemplate_HasCorrectComponents()
    {
        // Arrange
        var templateDto = new TemplateDto
        {
            Template = TemplateType.MachineLearning,
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.LoadBalancer, ResourceSubCategory.Monitoring }
        };
        var json = JsonSerializer.Serialize(templateDto, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/calculator/calculate", content);

        // Assert
        response.EnsureSuccessStatusCode();
        
        await using var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<TemplateCostComparisonResultDto>(stream, JsonOptions);

        Assert.NotNull(result);
        Assert.DoesNotContain(ResourceSubCategory.Relational, result.Resources);
        
        var largeAwsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS && cc.UsageSize == UsageSize.Large);
        
        // Machine Learning template should not have Databases
        Assert.DoesNotContain(largeAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Relational);
    }
}
