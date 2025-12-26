using Application.Models.Dtos;
using Application.Models.Enums;
using Application.Services.Calculator;

namespace Tests.UnitTests;

public class PriceProviderTests
{
    private readonly PriceProvider _priceProvider;

    public PriceProviderTests()
    {
        _priceProvider = new PriceProvider();
    }

    [Fact]
    public void GetCheapestComputeInstance_Returns_Cheapest_Instance_Meeting_Requirements()
    {
        // Arrange
        var instances = new List<NormalizedComputeInstanceDto>
        {
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Compute,
                SubCategory = ResourceSubCategory.VirtualMachines,
                InstanceName = "t2.medium",
                Region = "us-east-1",
                VCpu = 2,
                Memory = "4 GB",
                PricePerHour = 0.05m
            },
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Compute,
                SubCategory = ResourceSubCategory.VirtualMachines,
                InstanceName = "t2.large",
                Region = "us-east-1",
                VCpu = 2,
                Memory = "8 GB",
                PricePerHour = 0.10m
            },
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Compute,
                SubCategory = ResourceSubCategory.VirtualMachines,
                InstanceName = "t2.xlarge",
                Region = "us-east-1",
                VCpu = 4,
                Memory = "16 GB",
                PricePerHour = 0.20m
            }
        };

        // Act
        var result = _priceProvider.GetCheapestComputeInstance(instances, 2, 4, CloudProvider.AWS);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("t2.medium", result.InstanceName);
        Assert.Equal(0.05m, result.PricePerHour);
    }

    [Fact]
    public void GetCheapestComputeInstance_Returns_Null_When_No_Instance_Meets_Requirements()
    {
        // Arrange
        var instances = new List<NormalizedComputeInstanceDto>
        {
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Compute,
                SubCategory = ResourceSubCategory.VirtualMachines,
                InstanceName = "t2.micro",
                Region = "us-east-1",
                VCpu = 1,
                Memory = "1 GB",
                PricePerHour = 0.01m
            }
        };

        // Act
        var result = _priceProvider.GetCheapestComputeInstance(instances, 2, 4, CloudProvider.AWS);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCheapestComputeInstance_Filters_By_MinCpu()
    {
        // Arrange
        var instances = new List<NormalizedComputeInstanceDto>
        {
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Compute,
                SubCategory = ResourceSubCategory.VirtualMachines,
                InstanceName = "t2.small",
                Region = "us-east-1",
                VCpu = 1,
                Memory = "4 GB",
                PricePerHour = 0.03m
            },
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Compute,
                SubCategory = ResourceSubCategory.VirtualMachines,
                InstanceName = "t2.medium",
                Region = "us-east-1",
                VCpu = 2,
                Memory = "4 GB",
                PricePerHour = 0.05m
            }
        };

        // Act
        var result = _priceProvider.GetCheapestComputeInstance(instances, 2, 4, CloudProvider.AWS);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("t2.medium", result.InstanceName);
        Assert.Equal(2, result.VCpu);
    }

    [Fact]
    public void GetCheapestComputeInstance_Filters_By_MinMemory()
    {
        // Arrange
        var instances = new List<NormalizedComputeInstanceDto>
        {
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Compute,
                SubCategory = ResourceSubCategory.VirtualMachines,
                InstanceName = "t2.small",
                Region = "us-east-1",
                VCpu = 2,
                Memory = "2 GB",
                PricePerHour = 0.03m
            },
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Compute,
                SubCategory = ResourceSubCategory.VirtualMachines,
                InstanceName = "t2.medium",
                Region = "us-east-1",
                VCpu = 2,
                Memory = "4 GB",
                PricePerHour = 0.05m
            }
        };

        // Act
        var result = _priceProvider.GetCheapestComputeInstance(instances, 2, 4, CloudProvider.AWS);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("t2.medium", result.InstanceName);
    }

    [Fact]
    public void GetCheapestDatabase_Returns_Cheapest_Database_Meeting_Requirements()
    {
        // Arrange
        var databases = new List<NormalizedDatabaseDto>
        {
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Database,
                SubCategory = ResourceSubCategory.Relational,
                InstanceName = "db.t2.small",
                Region = "us-east-1",
                VCpu = 1,
                Memory = "2 GB",
                PricePerHour = 0.034m
            },
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Database,
                SubCategory = ResourceSubCategory.Relational,
                InstanceName = "db.t2.medium",
                Region = "us-east-1",
                VCpu = 2,
                Memory = "4 GB",
                PricePerHour = 0.068m
            }
        };

        // Act
        var result = _priceProvider.GetCheapestDatabase(databases, 1, 2, CloudProvider.AWS);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("db.t2.small", result.InstanceName);
        Assert.Equal(0.034m, result.PricePerHour);
    }

    [Fact]
    public void GetLoadBalancer_Returns_LoadBalancer_For_Specified_Cloud()
    {
        // Arrange
        var loadBalancers = new List<NormalizedLoadBalancerDto>
        {
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Networking,
                SubCategory = ResourceSubCategory.LoadBalancer,
                Name = "Application Load Balancer",
                PricePerMonth = 16.51m
            },
            new()
            {
                Cloud = CloudProvider.Azure,
                Category = ResourceCategory.Networking,
                SubCategory = ResourceSubCategory.LoadBalancer,
                Name = "Load Balancer",
                PricePerMonth = 0m
            }
        };

        // Act
        var result = _priceProvider.GetLoadBalancer(loadBalancers, CloudProvider.AWS);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Application Load Balancer", result.Name);
        Assert.Equal(16.51m, result.PricePerMonth);
    }

    [Fact]
    public void GetMonitoring_Returns_Monitoring_For_Specified_Cloud()
    {
        // Arrange
        var monitoring = new List<NormalizedMonitoringDto>
        {
            new()
            {
                Cloud = CloudProvider.AWS,
                Category = ResourceCategory.Management,
                SubCategory = ResourceSubCategory.Monitoring,
                Name = "CloudWatch",
                PricePerMonth = 5m
            },
            new()
            {
                Cloud = CloudProvider.GCP,
                Category = ResourceCategory.Management,
                SubCategory = ResourceSubCategory.Monitoring,
                Name = "Cloud Ops",
                PricePerMonth = 4m
            }
        };

        // Act
        var result = _priceProvider.GetMonitoring(monitoring, CloudProvider.GCP);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cloud Ops", result.Name);
        Assert.Equal(4m, result.PricePerMonth);
    }
}
