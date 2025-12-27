using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services.Calculator;

namespace Tests.UnitTests;

public class CalculatorServiceTests
{
    private readonly CalculatorService _calculatorService;

    public CalculatorServiceTests()
    {
        _calculatorService = new CalculatorService();
    }

    [Theory]
    [InlineData(UsageSize.Small, 2, 4)]
    [InlineData(UsageSize.Medium, 4, 8)]
    [InlineData(UsageSize.Large, 8, 16)]
    [InlineData(UsageSize.ExtraLarge, 16, 32)]
    public void GetVirtualMachineSpecs_Returns_Correct_Specs(UsageSize usage, int expectedCpu, double expectedMemory)
    {
        // Act
        var result = _calculatorService.GetVirtualMachineSpecs(usage);

        // Assert
        Assert.Equal(expectedCpu, result.MinCpu);
        Assert.Equal(expectedMemory, result.MinMemory);
    }

    [Theory]
    [InlineData(UsageSize.Small, 1, 2)]
    [InlineData(UsageSize.Medium, 2, 4)]
    [InlineData(UsageSize.Large, 4, 8)]
    [InlineData(UsageSize.ExtraLarge, 8, 16)]
    public void GetDatabaseSpecs_Returns_Correct_Specs(UsageSize usage, int expectedCpu, double expectedMemory)
    {
        // Act
        var result = _calculatorService.GetDatabaseSpecs(usage);

        // Assert
        Assert.Equal(expectedCpu, result.MinCpu);
        Assert.Equal(expectedMemory, result.MinMemory);
    }

    [Fact]
    public void CalculateMonthlyPrice_Returns_Correct_Monthly_Price()
    {
        // Arrange
        decimal pricePerHour = 0.10m;

        // Act
        var result = _calculatorService.CalculateMonthlyPrice(pricePerHour);

        // Assert
        Assert.Equal(73m, result); // 0.10 * 730 = 73
    }

    [Fact]
    public void CalculateMonthlyPrice_Returns_Zero_When_Null()
    {
        // Act
        var result = _calculatorService.CalculateMonthlyPrice(null);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateVirtualMachineCost_Returns_Monthly_Cost()
    {
        // Arrange
        var instance = new NormalizedComputeInstanceDto
        {
            Cloud = CloudProvider.AWS,
            Category = ResourceCategory.Compute,
            SubCategory = ResourceSubCategory.VirtualMachines,
            InstanceName = "t2.medium",
            Region = "us-east-1",
            VCpu = 2,
            Memory = "4 GB",
            PricePerHour = 0.05m
        };

        // Act
        var result = _calculatorService.CalculateVirtualMachineCost(instance);

        // Assert
        Assert.Equal(36.5m, result); // 0.05 * 730 = 36.5
    }

    [Fact]
    public void CalculateVirtualMachineCost_Returns_Zero_When_Null()
    {
        // Act
        var result = _calculatorService.CalculateVirtualMachineCost(null);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateDatabaseCost_Returns_Monthly_Cost()
    {
        // Arrange
        var database = new NormalizedDatabaseDto
        {
            Cloud = CloudProvider.AWS,
            Category = ResourceCategory.Database,
            SubCategory = ResourceSubCategory.Relational,
            InstanceName = "db.t2.small",
            Region = "us-east-1",
            VCpu = 1,
            Memory = "2 GB",
            PricePerHour = 0.034m
        };

        // Act
        var result = _calculatorService.CalculateDatabaseCost(database);

        // Assert
        Assert.Equal(24.82m, result); // 0.034 * 730 = 24.82
    }

    [Fact]
    public void CalculateDatabaseCost_Returns_Zero_When_Null()
    {
        // Act
        var result = _calculatorService.CalculateDatabaseCost(null);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateLoadBalancerCost_Returns_Price_Per_Month()
    {
        // Arrange
        var loadBalancer = new NormalizedLoadBalancerDto
        {
            Cloud = CloudProvider.AWS,
            Category = ResourceCategory.Networking,
            SubCategory = ResourceSubCategory.LoadBalancer,
            Name = "Application Load Balancer",
            PricePerMonth = 16.51m
        };

        // Act
        var result = _calculatorService.CalculateLoadBalancerCost(loadBalancer);

        // Assert
        Assert.Equal(16.51m, result);
    }

    [Fact]
    public void CalculateLoadBalancerCost_Returns_Zero_When_Null()
    {
        // Act
        var result = _calculatorService.CalculateLoadBalancerCost(null);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateMonitoringCost_Returns_Price_Per_Month()
    {
        // Arrange
        var monitoring = new NormalizedMonitoringDto
        {
            Cloud = CloudProvider.AWS,
            Category = ResourceCategory.Management,
            SubCategory = ResourceSubCategory.Monitoring,
            Name = "CloudWatch",
            PricePerMonth = 5m
        };

        // Act
        var result = _calculatorService.CalculateMonitoringCost(monitoring);

        // Assert
        Assert.Equal(5m, result);
    }

    [Fact]
    public void CalculateMonitoringCost_Returns_Zero_When_Null()
    {
        // Act
        var result = _calculatorService.CalculateMonitoringCost(null);

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public void CalculateLoadBalancerCost_Returns_Zero_When_PricePerMonth_Is_Null()
    {
        // Arrange
        var loadBalancer = new NormalizedLoadBalancerDto
        {
            Cloud = CloudProvider.AWS,
            Category = ResourceCategory.Networking,
            SubCategory = ResourceSubCategory.LoadBalancer,
            Name = "Application Load Balancer",
            PricePerMonth = null
        };

        // Act
        var result = _calculatorService.CalculateLoadBalancerCost(loadBalancer);

        // Assert
        Assert.Equal(0m, result);
    }
}
