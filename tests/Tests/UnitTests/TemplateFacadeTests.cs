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
        var templateDto = new CalculationRequest
        {
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.VirtualMachines],
                Databases = [DatabaseType.Relational],
                Networks = [NetworkingType.LoadBalancer],
                Management = [ManagementType.Monitoring]
            }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Resources);
        Assert.Contains(ComputeType.VirtualMachines, result.Resources.Computes);
        Assert.Contains(DatabaseType.Relational, result.Resources.Databases);
        Assert.Contains(NetworkingType.LoadBalancer, result.Resources.Networks);
        Assert.Contains(ManagementType.Monitoring, result.Resources.Management);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithSaasTemplate_ReturnsAllCloudProviders()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.VirtualMachines],
                Databases = [DatabaseType.Relational],
                Networks = [NetworkingType.LoadBalancer],
                Management = [ManagementType.Monitoring]
            }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.CloudCosts.Count);
        Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == CloudProvider.AWS);
        Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == CloudProvider.Azure);
        Assert.Contains(result.CloudCosts, cc => cc.CloudProvider == CloudProvider.GCP);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithSaasTemplate_HasCorrectCostBreakdown()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.VirtualMachines],
                Databases = [DatabaseType.Relational],
                Networks = [NetworkingType.LoadBalancer],
                Management = [ManagementType.Monitoring]
            }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        var awsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS);

        // Total should be sum of all cost details
        var expectedTotal = awsCost.CostDetails.Sum(cd => cd.Cost);
        Assert.Equal(expectedTotal, awsCost.TotalMonthlyPrice);

        // Verify that we have costs for each resource category
        Assert.Contains(awsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.VirtualMachines);
        Assert.Contains(awsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Relational);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithBlankTemplate_HasZeroCosts()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = new ResourcesDto()
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Resources);
        Assert.Empty(result.Resources.Computes);
        Assert.Empty(result.Resources.Databases);
        Assert.Empty(result.Resources.Storages);
        Assert.Empty(result.Resources.Networks);
        Assert.Empty(result.Resources.Analytics);
        Assert.Empty(result.Resources.Management);
        Assert.Empty(result.Resources.Security);
        Assert.Empty(result.Resources.AI);

        // Verify all cloud providers have zero costs
        foreach (var cloudCost in result.CloudCosts)
        {
            Assert.Equal(0m, cloudCost.TotalMonthlyPrice);
            Assert.Empty(cloudCost.CostDetails);
        }
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithStaticSiteTemplate_OnlyHasLoadBalancerCosts()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = new ResourcesDto
            {
                Networks = [NetworkingType.LoadBalancer]
            }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Resources.Networks);
        Assert.Contains(NetworkingType.LoadBalancer, result.Resources.Networks);

        var awsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS);

        // Static site template should only have load balancer costs
        Assert.DoesNotContain(awsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.VirtualMachines);
        Assert.DoesNotContain(awsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Relational);
        Assert.DoesNotContain(awsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Monitoring);
        Assert.Single(awsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.LoadBalancer);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_CostsIncreaseWithUsageSize()
    {
        // Arrange
        var service = GetService();
        var smallTemplateDto = new CalculationRequest
        {
            Usage = UsageSize.Small,
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.VirtualMachines],
                Databases = [DatabaseType.Relational]
            }
        };

        var largeTemplateDto = new CalculationRequest
        {
            Usage = UsageSize.Large,
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.VirtualMachines],
                Databases = [DatabaseType.Relational]
            }
        };

        // Act
        var smallResult = await service.CalculateCostComparisonsAsync(smallTemplateDto);
        var largeResult = await service.CalculateCostComparisonsAsync(largeTemplateDto);

        // Assert
        var smallAwsCost = smallResult.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS);
        var largeAwsCost = largeResult.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS);

        Assert.True(largeAwsCost.TotalMonthlyPrice >= smallAwsCost.TotalMonthlyPrice,
            "Larger usage size should have equal or higher costs");
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithWordPressTemplate_ReturnsValidBreakdown()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.VirtualMachines],
                Databases = [DatabaseType.Relational],
                Networks = [NetworkingType.LoadBalancer]
            }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Resources.Computes);
        Assert.Single(result.Resources.Databases);
        Assert.Single(result.Resources.Networks);
        Assert.Empty(result.Resources.Management);

        var awsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS);

        // WordPress template should not have Monitoring
        Assert.DoesNotContain(awsCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.Monitoring);
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
            ResourceSubCategory.Caching,

            // Storage (300-399)
            ResourceSubCategory.ObjectStorage,
            ResourceSubCategory.BlobStorage,
            ResourceSubCategory.BlockStorage,
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

            // Security (700-799)
            ResourceSubCategory.WebApplicationFirewall,
            ResourceSubCategory.IdentityManagement,

            // AI/ML (801-803)
            ResourceSubCategory.AIServices,
            ResourceSubCategory.MLPlatforms,
            ResourceSubCategory.IntelligentSearch,
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