using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services.Calculator;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.UnitTests.Calculator;

public class CalculatorServiceCloudFunctionsTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private ICalculatorService GetService()
    {
        var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ICalculatorService>();
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithCloudFunctions_SmallUsage_ReturnsValidResult()
    {
        // Arrange
        var service = GetService();
        var calculationRequest = new CalculationRequest
        {
            Usage = UsageSize.Small,
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.CloudFunctions]
            }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(calculationRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(UsageSize.Small, result.Usage);
        Assert.Contains(ComputeType.CloudFunctions, result.Resources.Computes);
        Assert.Equal(3, result.CloudCosts.Count);
        
        foreach (var cloudCost in result.CloudCosts)
        {
            Assert.Contains(cloudCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.CloudFunctions);
        }

        await Verify(result);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithCloudFunctions_MediumUsage_ReturnsValidResult()
    {
        // Arrange
        var service = GetService();
        var calculationRequest = new CalculationRequest
        {
            Usage = UsageSize.Medium,
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.CloudFunctions]
            }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(calculationRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(UsageSize.Medium, result.Usage);
        Assert.Contains(ComputeType.CloudFunctions, result.Resources.Computes);
        Assert.Equal(3, result.CloudCosts.Count);
        
        foreach (var cloudCost in result.CloudCosts)
        {
            Assert.Contains(cloudCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.CloudFunctions);
        }

        await Verify(result);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithCloudFunctions_LargeUsage_ReturnsValidResult()
    {
        // Arrange
        var service = GetService();
        var calculationRequest = new CalculationRequest
        {
            Usage = UsageSize.Large,
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.CloudFunctions]
            }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(calculationRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(UsageSize.Large, result.Usage);
        Assert.Contains(ComputeType.CloudFunctions, result.Resources.Computes);
        Assert.Equal(3, result.CloudCosts.Count);
        
        foreach (var cloudCost in result.CloudCosts)
        {
            Assert.Contains(cloudCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.CloudFunctions);
        }

        await Verify(result);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithCloudFunctions_ExtraLargeUsage_ReturnsValidResult()
    {
        // Arrange
        var service = GetService();
        var calculationRequest = new CalculationRequest
        {
            Usage = UsageSize.ExtraLarge,
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.CloudFunctions]
            }
        };

        // Act
        var result = await service.CalculateCostComparisonsAsync(calculationRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(UsageSize.ExtraLarge, result.Usage);
        Assert.Contains(ComputeType.CloudFunctions, result.Resources.Computes);
        Assert.Equal(3, result.CloudCosts.Count);
        
        foreach (var cloudCost in result.CloudCosts)
        {
            Assert.Contains(cloudCost.CostDetails, cd => cd.ResourceSubCategory == ResourceSubCategory.CloudFunctions);
        }

        await Verify(result);
    }

    [Fact]
    public async Task CalculateCostComparisonsAsync_WithCloudFunctions_CostsIncreaseWithUsageSize()
    {
        // Arrange
        var service = GetService();
        var smallRequest = new CalculationRequest
        {
            Usage = UsageSize.Small,
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.CloudFunctions]
            }
        };

        var largeRequest = new CalculationRequest
        {
            Usage = UsageSize.Large,
            Resources = new ResourcesDto
            {
                Computes = [ComputeType.CloudFunctions]
            }
        };

        // Act
        var smallResult = await service.CalculateCostComparisonsAsync(smallRequest);
        var largeResult = await service.CalculateCostComparisonsAsync(largeRequest);

        // Assert
        var smallAwsCost = smallResult.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS);
        var largeAwsCost = largeResult.CloudCosts.First(cc => cc.CloudProvider == CloudProvider.AWS);

        Assert.True(largeAwsCost.TotalMonthlyPrice >= smallAwsCost.TotalMonthlyPrice,
            "Larger usage size should have equal or higher costs for CloudFunctions");

        await Verify(new { smallResult, largeResult });
    }
}
