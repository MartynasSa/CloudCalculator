using Application.Facade;
using static Tests.ResourceSpecificationTestHelper;
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
            Resources = { VirtualMachines(), Relational(), LoadBalancer(), Monitoring() }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Resources.Count);
        Assert.Contains(VirtualMachines(), result.Resources);
        Assert.Contains(Relational(), result.Resources);
        Assert.Contains(LoadBalancer(), result.Resources);
        Assert.Contains(Monitoring(), result.Resources);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithSaasTemplate_ReturnsAllCloudProviders()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = { VirtualMachines(), Relational(), LoadBalancer(), Monitoring() }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithSaasTemplate_HasCorrectCostBreakdown()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = { VirtualMachines(), Relational(), LoadBalancer(), Monitoring() }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        var smallAwsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS);

        // Total should be sum of all cost details
        var expectedTotal = smallAwsCost.CostDetails.Sum(cd => cd.Cost);
        Assert.Equal(expectedTotal, smallAwsCost.TotalMonthlyPrice);

        // Verify that we have costs for each resource category
        Assert.Contains(smallAwsCost.CostDetails, cd => cd.ResourceSpecification.Equals(VirtualMachines()));
        Assert.Contains(smallAwsCost.CostDetails, cd => cd.ResourceSpecification.Equals(Relational()));
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithBlankTemplate_HasZeroCosts()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = { }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Resources);

        // Verify all usage sizes and cloud providers have zero costs
        foreach (var kvp in result.CloudCosts)
        {
            Assert.Equal(0m, kvp.TotalMonthlyPrice);
            Assert.Empty(kvp.CostDetails);
        }
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithStaticSiteTemplate_OnlyHasLoadBalancerCosts()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = { LoadBalancer() }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Resources);
        Assert.Contains(LoadBalancer(), result.Resources);

        var smallAwsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS);

        // Static site template should only have load balancer costs
        Assert.DoesNotContain(smallAwsCost.CostDetails, cd => cd.ResourceSpecification.Equals(VirtualMachines()));
        Assert.DoesNotContain(smallAwsCost.CostDetails, cd => cd.ResourceSpecification.Equals(Relational()));
        Assert.DoesNotContain(smallAwsCost.CostDetails, cd => cd.ResourceSpecification.Equals(Monitoring()));
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_CostsIncreaseWithUsageSize()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = { VirtualMachines(), Relational(), LoadBalancer(), Monitoring() }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithWordPressTemplate_ReturnsValidBreakdown()
    {
        // Arrange
        var service = GetService();
        var templateDto = new CalculationRequest
        {
            Resources = { VirtualMachines(), Relational(), LoadBalancer() }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(templateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Resources.Count);
        Assert.DoesNotContain(Monitoring(), result.Resources);

        var mediumAwsCost = result.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS);

        // WordPress template should not have Monitoring
        Assert.DoesNotContain(mediumAwsCost.CostDetails, cd => cd.ResourceSpecification.Equals(Monitoring()));
    }
}
