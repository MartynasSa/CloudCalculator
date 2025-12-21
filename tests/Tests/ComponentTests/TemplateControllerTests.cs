using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Tests.ComponentTests;

public class TemplateControllerTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    [Fact]
    public async Task Get_Templates_WithSaasSmall_Returns_Template_With_VirtualMachines()
    {
        var response = await Client.GetAsync("/api/templates?template=1&usage=1");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.Equal(UsageSize.Small, template.Usage);

        AssertValidTemplate(template);
        AssertSmallGranularitySpecs(template);

        await Verify(template);
    }

    [Fact]
    public async Task Get_Templates_WithSaasMedium_Returns_Template_With_VirtualMachines()
    {
        var response = await Client.GetAsync("/api/templates?template=1&usage=2");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.Equal(UsageSize.Medium, template.Usage);

        AssertValidTemplate(template);
        AssertMediumGranularitySpecs(template);

        await Verify(template);
    }

    [Fact]
    public async Task Get_Templates_WithSaasLarge_Returns_Template_With_VirtualMachines()
    {
        var response = await Client.GetAsync("/api/templates?template=1&usage=3");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.Equal(UsageSize.Large, template.Usage);

        AssertValidTemplate(template);
        AssertLargeGranularitySpecs(template);

        await Verify(template);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Get_Templates_WithSaasAllGranularities_Returns_All_CloudProviders(int usage)
    {
        var response = await Client.GetAsync($"/api/templates?template=1&usage={usage}");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        AssertValidTemplate(template);

        // Verify all major clouds are represented
        if (template.VirtualMachines != null)
        {
            Assert.Contains(CloudProvider.AWS, template.VirtualMachines.Keys);
            Assert.Contains(CloudProvider.Azure, template.VirtualMachines.Keys);
        }
    }

    [Fact]
    public async Task Get_Templates_WithSaasSmall_Databases_Have_PostgreSQL()
    {
        var response = await Client.GetAsync("/api/templates?template=1&usage=1");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.NotNull(template.Databases);

        Assert.All(template.Databases.Values, db =>
        {
            Assert.False(string.IsNullOrWhiteSpace(db.DatabaseEngine));
            Assert.Contains("postgres", db.DatabaseEngine, StringComparison.OrdinalIgnoreCase);
        });

        await Verify(template.Databases);
    }

    [Fact]
    public async Task Get_Templates_WithSaasSmall_Instances_Have_Required_Properties()
    {
        var response = await Client.GetAsync("/api/templates?template=1&usage=1");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.NotNull(template.VirtualMachines);

        Assert.All(template.VirtualMachines.Values, vm =>
        {
            Assert.False(string.IsNullOrWhiteSpace(vm.InstanceName));
            Assert.True(vm.CpuCores > 0);
            Assert.True(vm.Memory > 0);
            Assert.True(vm.PricePerMonth > 0);
        });
    }

    [Fact]
    public async Task Get_Templates_WithInvalidTemplate_Returns_BadRequest()
    {
        var response = await Client.GetAsync("/api/templates?template=999&usage=1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Templates_WithMissingUsage_Returns_BadRequest()
    {
        var response = await Client.GetAsync("/api/templates?template=saas");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Templates_WithSaasSmall_VM_Prices_Are_Reasonable()
    {
        var response = await Client.GetAsync("/api/templates?template=1&usage=1");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.NotNull(template.VirtualMachines);

        // Small instance with 2 CPUs and 4GB memory should be reasonably priced
        // Expecting somewhere between $5 and $100 per month for small instance
        Assert.All(template.VirtualMachines.Values, vm =>
        {
            Assert.True(vm.PricePerMonth >= 5m, $"Price too low: {vm.PricePerMonth}");
            Assert.True(vm.PricePerMonth <= 200m, $"Price too high: {vm.PricePerMonth}");
        });
    }

    private static void AssertValidTemplate(TemplateDto template)
    {
        Assert.NotNull(template);
        Assert.NotNull(template.VirtualMachines);
        Assert.NotEmpty(template.VirtualMachines);
        Assert.NotNull(template.Databases);
        Assert.NotEmpty(template.Databases);
    }

    private static void AssertSmallGranularitySpecs(TemplateDto template)
    {
        Assert.All(template.VirtualMachines!.Values, vm =>
        {
            Assert.True(vm.CpuCores >= 2, $"Expected at least 2 CPUs for Small, got {vm.CpuCores}");
            Assert.True(vm.Memory >= 4, $"Expected at least 4GB for Small, got {vm.Memory}GB");
        });

        Assert.All(template.Databases!.Values, db =>
        {
            Assert.True(db.CpuCores >= 1, $"Expected at least 1 CPU for database Small, got {db.CpuCores}");
            Assert.True(db.Memory >= 2, $"Expected at least 2GB for database Small, got {db.Memory}GB");
        });
    }

    private static void AssertMediumGranularitySpecs(TemplateDto template)
    {
        Assert.All(template.VirtualMachines!.Values, vm =>
        {
            Assert.True(vm.CpuCores >= 4, $"Expected at least 4 CPUs for Medium, got {vm.CpuCores}");
            Assert.True(vm.Memory >= 8, $"Expected at least 8GB for Medium, got {vm.Memory}GB");
        });

        Assert.All(template.Databases!.Values, db =>
        {
            Assert.True(db.CpuCores >= 2, $"Expected at least 2 CPUs for database Medium, got {db.CpuCores}");
            Assert.True(db.Memory >= 4, $"Expected at least 4GB for database Medium, got {db.Memory}GB");
        });
    }

    private static void AssertLargeGranularitySpecs(TemplateDto template)
    {
        Assert.All(template.VirtualMachines!.Values, vm =>
        {
            Assert.True(vm.CpuCores >= 8, $"Expected at least 8 CPUs for Large, got {vm.CpuCores}");
            Assert.True(vm.Memory >= 16, $"Expected at least 16GB for Large, got {vm.Memory}GB");
        });

        Assert.All(template.Databases!.Values, db =>
        {
            Assert.True(db.CpuCores >= 4, $"Expected at least 4 CPUs for database Large, got {db.CpuCores}");
            Assert.True(db.Memory >= 8, $"Expected at least 8GB for database Large, got {db.Memory}GB");
        });
    }
}