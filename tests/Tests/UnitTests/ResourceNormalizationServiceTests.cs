using Application.Models.Enums;
using Application.Services.Normalization;
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
        var categorized = await service.GetResourcesAsync();
        var result = categorized.ComputeInstances;

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
        var categorized = await service.GetResourcesAsync();
        var result = categorized.ComputeInstances;
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
        var categorized = await service.GetResourcesAsync();
        var result = categorized.ComputeInstances;
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
        var categorized = await service.GetResourcesAsync();
        var result = categorized.Databases;

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
        var categorized = await service.GetResourcesAsync();
        var result = categorized.Databases;

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
        var categorized = await service.GetResourcesAsync();
        var result = categorized.Databases;

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
        var categorized = await service.GetResourcesAsync();
        var result = categorized.LoadBalancers;

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
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
        var categorized = await service.GetResourcesAsync();
        var result = categorized.LoadBalancers;

        // Assert
        var awsLb = result.First(lb => lb.Cloud == CloudProvider.AWS);
        var azureLb = result.First(lb => lb.Cloud == CloudProvider.Azure);
        var gcpLb = result.First(lb => lb.Cloud == CloudProvider.GCP);
    }

    [Fact]
    public async Task GetNormalizedCloudFunctions_Returns_CloudFunctions()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync();
        var result = categorized.CloudFunctions;

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify all functions have required properties
        Assert.All(result, cf =>
        {
            Assert.False(string.IsNullOrWhiteSpace(cf.Cloud.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(cf.FunctionName));
            Assert.False(string.IsNullOrWhiteSpace(cf.Region));
            Assert.Equal(ResourceCategory.Compute, cf.Category);
            Assert.Equal((int)ComputeType.CloudFunctions, cf.SubCategoryValue);
        });

        await Verify(result);
    }

    [Fact]
    public async Task GetNormalizedKubernetes_Returns_Kubernetes()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync();
        var result = categorized.Kubernetes;

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify all kubernetes have required properties
        Assert.All(result, k =>
        {
            Assert.False(string.IsNullOrWhiteSpace(k.Cloud.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(k.ClusterName));
            Assert.False(string.IsNullOrWhiteSpace(k.Region));
            Assert.Equal(ResourceCategory.Compute, k.Category);
            Assert.Equal((int)ComputeType.Kubernetes, k.SubCategoryValue);
        });

        await Verify(result);
    }

    [Fact]
    public async Task GetNormalizedApiGateways_Returns_ApiGateways()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync();
        var result = categorized.ApiGateways;

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify all API gateways have required properties
        Assert.All(result, ag =>
        {
            Assert.False(string.IsNullOrWhiteSpace(ag.Cloud.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(ag.Name));
            Assert.False(string.IsNullOrWhiteSpace(ag.Region));
            Assert.Equal(ResourceCategory.Networking, ag.Category);
            Assert.Equal((int)NetworkingType.ApiGateway, ag.SubCategoryValue);
        });

        await Verify(result);
    }

    [Fact]
    public async Task GetNormalizedBlobStorage_Returns_BlobStorage()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync();
        var result = categorized.BlobStorage;

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify all blob storage have required properties
        Assert.All(result, bs =>
        {
            Assert.False(string.IsNullOrWhiteSpace(bs.Cloud.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(bs.Name));
            Assert.False(string.IsNullOrWhiteSpace(bs.Region));
            Assert.Equal(ResourceCategory.Storage, bs.Category);
            Assert.Equal((int)StorageType.BlobStorage, bs.SubCategoryValue);
        });

        await Verify(result);
    }

    [Fact]
    public async Task GetNormalizedDatabases_Includes_NoSQL_Databases()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync();
        var result = categorized.Databases;

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify we have NoSQL databases
        Assert.Contains(result, db => db.SubCategoryValue == (int)DatabaseType.NoSQL);

        await Verify(result.Where(db => db.SubCategoryValue == (int)DatabaseType.NoSQL).ToList());
    }

    [Fact]
    public async Task GetNormalizedDatabases_Includes_Relational_Databases()
    {
        // Arrange
        var service = GetService();

        // Act
        var categorized = await service.GetResourcesAsync();
        var result = categorized.Databases;

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify we have Relational databases
        Assert.Contains(result, db => db.SubCategoryValue == (int)DatabaseType.Relational);

        await Verify(result.Where(db => db.SubCategoryValue == (int)DatabaseType.Relational).ToList());
    }

    [Fact]
    public async Task TypeEnums_UseCorrectIntegerRanges()
    {
        // Arrange & Act
        var computeTypes = new[]
        {
            ComputeType.VirtualMachines,
            ComputeType.Kubernetes,
            ComputeType.CloudFunctions,
        };

        var databaseTypes = new[]
        {
            DatabaseType.Relational,
        };

        var storageTypes = new[]
        {
            StorageType.BlobStorage,
            StorageType.ObjectStorage
        };

        // Assert - Compute types should be in range 100-199
        Assert.All(computeTypes, type =>
        {
            var value = (int)type;
            Assert.InRange(value, 100, 199);
        });

        // Assert - Database types should be in range 200-299
        Assert.All(databaseTypes, type =>
        {
            var value = (int)type;
            Assert.InRange(value, 200, 299);
        });

        // Assert - Storage types should be in range 300-399
        Assert.All(storageTypes, type =>
        {
            var value = (int)type;
            Assert.InRange(value, 300, 399);
        });
    }
}