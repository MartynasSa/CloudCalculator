using Application.Facade;
using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.UnitTests;

public class TemplateFacadeTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private ITemplateFacade GetService()
    {
        var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ITemplateFacade>();
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithSaasTemplate_ReturnsAllUsageSizes()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
            Usage = UsageSize.Small
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.Saas, result.Template);
        Assert.Equal(3, result.UsageBreakdowns.Count);
        
        // Verify we have all three usage sizes
        Assert.Contains(result.UsageBreakdowns, ub => ub.Usage == UsageSize.Small);
        Assert.Contains(result.UsageBreakdowns, ub => ub.Usage == UsageSize.Medium);
        Assert.Contains(result.UsageBreakdowns, ub => ub.Usage == UsageSize.Large);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithSaasTemplate_ReturnsAllCloudProviders()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
            Usage = UsageSize.Small
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.All(result.UsageBreakdowns, breakdown =>
        {
            Assert.Contains(CloudProvider.AWS, breakdown.CloudProviderCosts.Keys);
            Assert.Contains(CloudProvider.Azure, breakdown.CloudProviderCosts.Keys);
            Assert.Contains(CloudProvider.GCP, breakdown.CloudProviderCosts.Keys);
        });
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithSaasTemplate_HasCorrectCostBreakdown()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
            Usage = UsageSize.Small
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        var smallBreakdown = result.UsageBreakdowns.First(ub => ub.Usage == UsageSize.Small);
        
        Assert.All(smallBreakdown.CloudProviderCosts.Values, cloudCost =>
        {
            Assert.NotNull(cloudCost.Breakdown);
            
            // Total should be sum of all components
            var expectedTotal = 
                cloudCost.Breakdown.VirtualMachinesCost.GetValueOrDefault() +
                cloudCost.Breakdown.DatabasesCost.GetValueOrDefault() +
                cloudCost.Breakdown.LoadBalancersCost.GetValueOrDefault() +
                cloudCost.Breakdown.MonitoringCost.GetValueOrDefault();
            
            Assert.Equal(expectedTotal, cloudCost.TotalMonthlyPrice);
        });
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithBlankTemplate_HasZeroCosts()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Blank,
            Usage = UsageSize.Small
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
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
    public async Task CalculateCostComparisonsAsync_WithStaticSiteTemplate_OnlyHasLoadBalancerCosts()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.StaticSite,
            Usage = UsageSize.Small
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        
        var smallBreakdown = result.UsageBreakdowns.First(ub => ub.Usage == UsageSize.Small);
        
        Assert.All(smallBreakdown.CloudProviderCosts.Values, cloudCost =>
        {
            // Static site template should not have VMs, Databases, or Monitoring
            Assert.False(cloudCost.Breakdown.VirtualMachinesCost.HasValue);
            Assert.False(cloudCost.Breakdown.DatabasesCost.HasValue);
            Assert.False(cloudCost.Breakdown.MonitoringCost.HasValue);
            // Load balancers may or may not be available depending on data
        });
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_CostsIncreaseWithUsageSize()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
            Usage = UsageSize.Small
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
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
    public async Task CalculateCostComparisonsAsync_WithWordPressTemplate_ReturnsValidBreakdown()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.WordPress,
            Usage = UsageSize.Medium
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.WordPress, result.Template);
        Assert.Equal(3, result.UsageBreakdowns.Count);
        
        var mediumBreakdown = result.UsageBreakdowns.First(ub => ub.Usage == UsageSize.Medium);
        
        // WordPress template should not have Monitoring
        Assert.All(mediumBreakdown.CloudProviderCosts.Values, cloudCost =>
        {
            Assert.False(cloudCost.Breakdown.MonitoringCost.HasValue);
        });
    }
}
