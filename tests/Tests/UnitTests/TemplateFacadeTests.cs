using Application.Facade;
using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.UnitTests;

public class TemplateFacadeTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private ICalculatorFacade GetService()
    {
        var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ICalculatorFacade>();
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithSaasTemplate_ReturnsAllUsageSizes()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.Relational, ResourceSubCategory.LoadBalancer, ResourceSubCategory.Monitoring }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Resources.Count);
        Assert.Contains(ResourceSubCategory.VirtualMachines, result.Resources);
        Assert.Contains(ResourceSubCategory.Relational, result.Resources);
        Assert.Contains(ResourceSubCategory.LoadBalancer, result.Resources);
        Assert.Contains(ResourceSubCategory.Monitoring, result.Resources);
        
        // Verify we have all cloud providers and usage sizes (3 providers x 4 sizes = 12 entries)
        Assert.Equal(12, result.CloudCosts.Count);
        
        // Verify we have all usage sizes for each cloud provider
        foreach (var cloud in new[] { CloudProvider.AWS, CloudProvider.Azure, CloudProvider.GCP })
        {
            Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == cloud && cc.UsageSize == UsageSize.Small);
            Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == cloud && cc.UsageSize == UsageSize.Medium);
            Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == cloud && cc.UsageSize == UsageSize.Large);
        }
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithSaasTemplate_ReturnsAllCloudProviders()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.Relational, ResourceSubCategory.LoadBalancer, ResourceSubCategory.Monitoring }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == CloudProvider.AWS);
        Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == CloudProvider.Azure);
        Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == CloudProvider.GCP);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithSaasTemplate_HasCorrectCostBreakdown()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.Relational, ResourceSubCategory.LoadBalancer, ResourceSubCategory.Monitoring }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        var smallAwsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS && cc.UsageSize == UsageSize.Small);
        
        // Total should be sum of all cost details
        var expectedTotal = smallAwsCost.CostDetails.Sum(cd => cd.Cost);
        Assert.Equal(expectedTotal, smallAwsCost.TotalMonthlyPrice);
        
        // Verify that we have costs for each resource category
        Assert.Contains(smallAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.VirtualMachines);
        Assert.Contains(smallAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Relational);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithBlankTemplate_HasZeroCosts()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Blank,
            Resources = { }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Resources);
        
        Assert.All(result.CloudCosts, cloudCost =>
        {
            Assert.Equal(0m, cloudCost.TotalMonthlyPrice);
            Assert.Empty(cloudCost.CostDetails);
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
            Resources = { ResourceSubCategory.LoadBalancer }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Resources);
        Assert.Contains(ResourceSubCategory.LoadBalancer, result.Resources);
        
        var smallAwsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS && cc.UsageSize == UsageSize.Small);
        
        // Static site template should only have load balancer costs
        Assert.DoesNotContain(smallAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.VirtualMachines);
        Assert.DoesNotContain(smallAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Relational);
        Assert.DoesNotContain(smallAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Monitoring);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_CostsIncreaseWithUsageSize()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.Saas,
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.Relational, ResourceSubCategory.LoadBalancer, ResourceSubCategory.Monitoring }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
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
    public async Task CalculateCostComparisonsAsync_WithWordPressTemplate_ReturnsValidBreakdown()
    {
        // Arrange
        var service = GetService();
        var templateDto = new TemplateDto
        {
            Template = TemplateType.WordPress,
            Resources = { ResourceSubCategory.VirtualMachines, ResourceSubCategory.Relational, ResourceSubCategory.LoadBalancer }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Resources.Count);
        Assert.DoesNotContain(ResourceSubCategory.Monitoring, result.Resources);
        
        var mediumAwsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS && cc.UsageSize == UsageSize.Medium);
        
        // WordPress template should not have Monitoring
        Assert.DoesNotContain(mediumAwsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Monitoring);
    }

    [Fact]
    public void AllResourceSubCategoryEnumsHaveCalculationLogic()
    {
        // This test ensures that when a new ResourceSubCategory is added,
        // a developer must also implement calculation logic for it.
        // If this test fails, it means you added a new ResourceSubCategory
        // without implementing the corresponding calculation method.
        
        var allSubCategories = Enum.GetValues<ResourceSubCategory>()
            .Where(sc => sc != ResourceSubCategory.None && sc != ResourceSubCategory.Uncategorized)
            .ToList();

        var implementedSubCategories = new List<ResourceSubCategory>
        {
            // Compute (100-199)
            ResourceSubCategory.VirtualMachines,
            ResourceSubCategory.CloudFunctions,
            ResourceSubCategory.Kubernetes,
            ResourceSubCategory.ContainerInstances,

            // Database (200-299)
            ResourceSubCategory.Relational,
            ResourceSubCategory.NoSQL,
            ResourceSubCategory.DatabaseStorage,
            ResourceSubCategory.Caching,

            // Storage (300-399)
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.BlobStorage,
            ResourceSubCategory.FileStorage,
            ResourceSubCategory.Backup,

            // Networking (400-499)
            ResourceSubCategory.VpnGateway,
            ResourceSubCategory.LoadBalancer,
            ResourceSubCategory.ApiGateway,
            ResourceSubCategory.Dns,
            ResourceSubCategory.CDN,

            // Analytics (500-599)
            ResourceSubCategory.DataWarehouse,
            ResourceSubCategory.Streaming,
            ResourceSubCategory.MachineLearning,

            // Management & Security (600-699)
            ResourceSubCategory.Queueing,
            ResourceSubCategory.Messaging,
            ResourceSubCategory.Secrets,
            ResourceSubCategory.Compliance,
            ResourceSubCategory.Monitoring,
        };

        var missingImplementations = allSubCategories
            .Except(implementedSubCategories)
            .ToList();

        // Assert.Empty will throw if there are missing implementations
        // Provide a helpful message in the assertion
        Assert.True(
            missingImplementations.Count == 0,
            $"The following ResourceSubCategory enums do not have calculation logic implemented: {string.Join(", ", missingImplementations)}. " +
            "Please add calculation methods in TemplateFacade.CalculateSubCategoryCostAsync and update this test.");
    }
}
