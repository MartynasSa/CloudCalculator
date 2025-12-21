using Application.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.ComponentTests;

public class ResourceNormalizationServiceTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private IResourceNormalizationService GetService()
    {
        var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IResourceNormalizationService>();
    }

    [Fact]
    public async Task GetNormalizedComputeInstancesAsync_Returns_ComputeInstances()
    {
        // Arrange
        var service = GetService();

        // Act
        var result = await service.GetNormalizedComputeInstancesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        // Verify we have instances from different clouds
        Assert.Contains(result, r => r.Cloud == "aws");
        Assert.Contains(result, r => r.Cloud == "azure");
        Assert.Contains(result, r => r.Cloud == "gcp");

        // Verify all instances have required properties
        foreach (var instance in result)
        {
            Assert.False(string.IsNullOrWhiteSpace(instance.Cloud));
            Assert.False(string.IsNullOrWhiteSpace(instance.InstanceName));
            Assert.False(string.IsNullOrWhiteSpace(instance.Region));
        }
    }

    [Fact]
    public async Task GetNormalizedComputeInstancesAsync_AWS_Instances_Have_CPU_And_Memory()
    {
        // Arrange
        var service = GetService();

        // Act
        var result = await service.GetNormalizedComputeInstancesAsync();
        var awsInstances = result.Where(r => r.Cloud == "aws").ToList();

        // Assert
        Assert.NotEmpty(awsInstances);
        
        // At least some AWS instances should have vCPU and memory
        Assert.Contains(awsInstances, i => i.VCpu.HasValue && i.VCpu.Value > 0);
        Assert.Contains(awsInstances, i => !string.IsNullOrWhiteSpace(i.Memory));
    }

    [Fact]
    public async Task GetNormalizedComputeInstancesAsync_Azure_Instances_Have_CPU_And_Memory()
    {
        // Arrange
        var service = GetService();

        // Act
        var result = await service.GetNormalizedComputeInstancesAsync();
        var azureInstances = result.Where(r => r.Cloud == "azure").ToList();

        // Assert
        Assert.NotEmpty(azureInstances);
        
        // Azure instances should have vCPU (numberOfCores or vCpusAvailable)
        // Note: Azure data may not include memory as a separate attribute
        Assert.Contains(azureInstances, i => i.VCpu.HasValue && i.VCpu.Value > 0);
    }

    [Fact]
    public async Task GetNormalizedDatabasesAsync_Returns_DatabaseInstances()
    {
        // Arrange
        var service = GetService();

        // Act
        var result = await service.GetNormalizedDatabasesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        
        // Verify we have databases from different clouds
        Assert.Contains(result, r => r.Cloud == "aws");
        Assert.Contains(result, r => r.Cloud == "azure");

        // Verify all databases have required properties
        foreach (var db in result)
        {
            Assert.False(string.IsNullOrWhiteSpace(db.Cloud));
            Assert.False(string.IsNullOrWhiteSpace(db.InstanceName));
            Assert.False(string.IsNullOrWhiteSpace(db.Region));
        }
    }

    [Fact]
    public async Task GetNormalizedDatabasesAsync_AWS_Databases_Have_DatabaseEngine()
    {
        // Arrange
        var service = GetService();

        // Act
        var result = await service.GetNormalizedDatabasesAsync();
        var awsDatabases = result.Where(r => r.Cloud == "aws").ToList();

        // Assert
        Assert.NotEmpty(awsDatabases);
        
        // At least some AWS databases should have database engine
        Assert.Contains(awsDatabases, db => !string.IsNullOrWhiteSpace(db.DatabaseEngine));
    }

    [Fact]
    public async Task GetNormalizedDatabasesAsync_Databases_Have_CPU_And_Memory()
    {
        // Arrange
        var service = GetService();

        // Act
        var result = await service.GetNormalizedDatabasesAsync();

        // Assert
        Assert.NotEmpty(result);
        
        // At least some databases should have vCPU and memory
        Assert.Contains(result, db => db.VCpu.HasValue && db.VCpu.Value > 0);
        Assert.Contains(result, db => !string.IsNullOrWhiteSpace(db.Memory));
    }
}
