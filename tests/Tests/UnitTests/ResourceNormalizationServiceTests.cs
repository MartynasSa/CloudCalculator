using Application.Models.Enums;
using Application.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.UnitTests;

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
        var categorized = await service.GetResourcesAsync(new[] { ResourceCategory.Compute }, UsageSize.Small);
        var result = categorized.Categories.TryGetValue(ResourceCategory.Compute, out var computeCategory)
            ? (computeCategory.ComputeInstances ?? [])
            : [];

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify we have instances from different clouds
        Assert.Contains(result, r => r.Cloud == CloudProvider.AWS);
        Assert.Contains(result, r => r.Cloud == CloudProvider.Azure);
        Assert.Contains(result, r => r.Cloud == CloudProvider.GCP);

        // Verify all instances have required properties
        Assert.All(result, instance =>
        {
            Assert.False(string.IsNullOrWhiteSpace(instance.Cloud.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(instance.InstanceName));
            Assert.False(string.IsNullOrWhiteSpace(instance.Region));
        });

        await Verify(result);
    }

    [Fact]
    public async Task GetNormalizedComputeInstancesAsync_AWS_Instances_Have_CPU_And_Memory()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync(new[] { ResourceCategory.Compute }, UsageSize.Small);
        var result = categorized.Categories.TryGetValue(ResourceCategory.Compute, out var computeCategory)
            ? (computeCategory.ComputeInstances ?? [])
            : [];
        var awsInstances = result.Where(r => r.Cloud == CloudProvider.AWS).ToList();

        // Assert
        Assert.NotEmpty(awsInstances);
        Assert.Contains(awsInstances, i => i.VCpu.HasValue && i.VCpu.Value > 0);
        Assert.Contains(awsInstances, i => !string.IsNullOrWhiteSpace(i.Memory));

        await Verify(awsInstances);
    }

    [Fact]
    public async Task GetNormalizedComputeInstancesAsync_Azure_Instances_Have_CPU_And_Memory()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync(new[] { ResourceCategory.Compute }, UsageSize.Small);
        var result = categorized.Categories.TryGetValue(ResourceCategory.Compute, out var computeCategory)
            ? (computeCategory.ComputeInstances ?? [])
            : [];
        var azureInstances = result.Where(r => r.Cloud == CloudProvider.Azure).ToList();

        // Assert
        Assert.NotEmpty(azureInstances);
        Assert.Contains(azureInstances, i => i.VCpu.HasValue && i.VCpu.Value > 0);

        await Verify(azureInstances);
    }

    [Fact]
    public async Task GetNormalizedDatabasesAsync_Returns_DatabaseInstances()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync(new[] { ResourceCategory.Databases }, UsageSize.Small);
        var result = categorized.Categories.TryGetValue(ResourceCategory.Databases, out var dbCategory)
            ? (dbCategory.Databases ?? [])
            : [];

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify we have databases from different clouds
        Assert.Contains(result, r => r.Cloud == CloudProvider.AWS);
        Assert.Contains(result, r => r.Cloud == CloudProvider.Azure);
        Assert.Contains(result, r => r.Cloud == CloudProvider.GCP);

        // Verify all databases have required properties
        Assert.All(result, db =>
        {
            Assert.False(string.IsNullOrWhiteSpace(db.Cloud.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(db.InstanceName));
            Assert.False(string.IsNullOrWhiteSpace(db.Region));
        });

        await Verify(result);
    }

    [Fact]
    public async Task GetNormalizedDatabasesAsync_AWS_Databases_Have_DatabaseEngine()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync(new[] { ResourceCategory.Databases }, UsageSize.Small);
        var result = categorized.Categories.TryGetValue(ResourceCategory.Databases, out var dbCategory)
            ? (dbCategory.Databases ?? [])
            : [];
        var awsDatabases = result.Where(r => r.Cloud == CloudProvider.AWS).ToList();

        // Assert
        Assert.NotEmpty(awsDatabases);
        Assert.Contains(awsDatabases, db => !string.IsNullOrWhiteSpace(db.DatabaseEngine));

        await Verify(awsDatabases);
    }

    [Fact]
    public async Task GetNormalizedDatabasesAsync_Databases_Have_CPU_And_Memory()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync(new[] { ResourceCategory.Databases }, UsageSize.Small);
        var result = categorized.Categories.TryGetValue(ResourceCategory.Databases, out var dbCategory)
            ? (dbCategory.Databases ?? [])
            : [];

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, db => db.VCpu.HasValue && db.VCpu.Value > 0);
        Assert.Contains(result, db => !string.IsNullOrWhiteSpace(db.Memory));

        await Verify(result);
    }

    [Fact]
    public async Task GetNormalizedLoadBalancers_Returns_LoadBalancers_For_All_Clouds()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync(new[] { ResourceCategory.Networking }, UsageSize.Small);
        var result = categorized.Categories.TryGetValue(ResourceCategory.Networking, out var netCategory)
            ? (netCategory.LoadBalancers ?? [])
            : [];

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, lb => lb.Cloud == CloudProvider.AWS);
        Assert.Contains(result, lb => lb.Cloud == CloudProvider.Azure);
        Assert.Contains(result, lb => lb.Cloud == CloudProvider.GCP);

        // Verify all load balancers have required properties
        Assert.All(result, lb =>
        {
            Assert.False(string.IsNullOrWhiteSpace(lb.Cloud.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(lb.Name));
            Assert.NotNull(lb.PricePerMonth);
        });
    }

    [Fact]
    public async Task GetNormalizedLoadBalancers_Small_Has_Correct_Pricing()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync(new[] { ResourceCategory.Networking }, UsageSize.Small);
        var result = categorized.Categories.TryGetValue(ResourceCategory.Networking, out var netCategory)
            ? (netCategory.LoadBalancers ?? [])
            : [];

        // Assert
        var awsLb = result.First(lb => lb.Cloud == CloudProvider.AWS);
        var azureLb = result.First(lb => lb.Cloud == CloudProvider.Azure);
        var gcpLb = result.First(lb => lb.Cloud == CloudProvider.GCP);

        Assert.Equal(16.51m, awsLb.PricePerMonth);
        Assert.Equal(0m, azureLb.PricePerMonth);
        Assert.Equal(18.41m, gcpLb.PricePerMonth);
    }

    [Fact]
    public async Task GetNormalizedMonitoring_Returns_Monitoring_For_All_Clouds()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync(new[] { ResourceCategory.Management }, UsageSize.Small);
        var result = categorized.Categories.TryGetValue(ResourceCategory.Management, out var mgmtCategory)
            ? (mgmtCategory.Monitoring ?? [])
            : [];

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, mon => mon.Cloud == CloudProvider.AWS);
        Assert.Contains(result, mon => mon.Cloud == CloudProvider.Azure);
        Assert.Contains(result, mon => mon.Cloud == CloudProvider.GCP);

        // Verify all monitoring services have required properties
        Assert.All(result, mon =>
        {
            Assert.False(string.IsNullOrWhiteSpace(mon.Cloud.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(mon.Name));
            Assert.NotNull(mon.PricePerMonth);
        });
    }

    [Fact]
    public async Task GetNormalizedMonitoring_Small_Has_Correct_Pricing()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync(new[] { ResourceCategory.Management }, UsageSize.Small);
        var result = categorized.Categories.TryGetValue(ResourceCategory.Management, out var mgmtCategory)
            ? (mgmtCategory.Monitoring ?? [])
            : [];

        // Assert
        var awsMon = result.First(mon => mon.Cloud == CloudProvider.AWS);
        var azureMon = result.First(mon => mon.Cloud == CloudProvider.Azure);
        var gcpMon = result.First(mon => mon.Cloud == CloudProvider.GCP);

        Assert.Equal(5m, awsMon.PricePerMonth);
        Assert.Equal(6m, azureMon.PricePerMonth);
        Assert.Equal(4m, gcpMon.PricePerMonth);
    }

    [Fact]
    public async Task GetCategorizedResourcesAsync_Returns_ResourcesByCategory()
    {
        // Arrange
        var service = GetService();

        // Act
        var result = await service.GetResourcesAsync(
            new[] { ResourceCategory.Compute, ResourceCategory.Databases, ResourceCategory.Networking, ResourceCategory.Management },
            UsageSize.Small);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Categories);
        Assert.NotEmpty(result.Categories);
        
        // Verify we have compute category with instances
        Assert.True(result.Categories.ContainsKey(ResourceCategory.Compute));
        var computeCategory = result.Categories[ResourceCategory.Compute];
        Assert.NotEmpty(computeCategory.ComputeInstances);
        
        // Verify we have database category with databases
        Assert.True(result.Categories.ContainsKey(ResourceCategory.Databases));
        var databaseCategory = result.Categories[ResourceCategory.Databases];
        Assert.NotEmpty(databaseCategory.Databases);
        
        // Verify we have networking category with load balancers
        Assert.True(result.Categories.ContainsKey(ResourceCategory.Networking));
        var networkingCategory = result.Categories[ResourceCategory.Networking];
        Assert.NotEmpty(networkingCategory.LoadBalancers);
        
        // Verify we have management category with monitoring
        Assert.True(result.Categories.ContainsKey(ResourceCategory.Management));
        var managementCategory = result.Categories[ResourceCategory.Management];
        Assert.NotEmpty(managementCategory.Monitoring);
    }
}