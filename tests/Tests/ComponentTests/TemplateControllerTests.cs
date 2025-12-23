using Application.Models.Dtos;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tests.ComponentTests;

public class TemplateControllerTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumMemberConverter() }
    };

    [Fact]
    public async Task Get_Templates_WithSaasSmall_Returns_Template_With_VirtualMachines()
    {
        var response = await Client.GetAsync("/api/templates?template=saas&usage=small");
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
        var response = await Client.GetAsync("/api/templates?template=saas&usage=medium");
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
        var response = await Client.GetAsync("/api/templates?template=saas&usage=large");
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
    [InlineData("small")]
    [InlineData("medium")]
    [InlineData("large")]
    public async Task Get_Templates_WithSaasAllGranularities_Returns_All_CloudProviders(string usage)
    {
        var response = await Client.GetAsync($"/api/templates?template=saas&usage={usage}");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        AssertValidTemplate(template);

        // Verify all major clouds are represented in databases
        if (template.Databases != null)
        {
            Assert.Contains(CloudProvider.AWS, template.Databases.Keys);
            Assert.Contains(CloudProvider.Azure, template.Databases.Keys);
            Assert.Contains(CloudProvider.GCP, template.Databases.Keys);
        }
        
        // Verify AWS and Azure VMs are always present
        if (template.VirtualMachines != null)
        {
            Assert.Contains(CloudProvider.AWS, template.VirtualMachines.Keys);
            Assert.Contains(CloudProvider.Azure, template.VirtualMachines.Keys);
        }
    }

    [Fact]
    public async Task Get_Templates_WithSaasSmall_Databases_Have_DatabaseEngine()
    {
        var response = await Client.GetAsync("/api/templates?template=saas&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.NotNull(template.Databases);

        Assert.All(template.Databases.Values, db =>
        {
            Assert.False(string.IsNullOrWhiteSpace(db.DatabaseEngine));
        });

        await Verify(template.Databases);
    }

    [Fact]
    public async Task Get_Templates_WithSaasSmall_Instances_Have_Required_Properties()
    {
        var response = await Client.GetAsync("/api/templates?template=saas&usage=small");
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
        var response = await Client.GetAsync("/api/templates?template=invalid&usage=small");

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
        var response = await Client.GetAsync("/api/templates?template=saas&usage=small");
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

    [Fact]
    public async Task Get_Templates_WithSaasSmall_Has_LoadBalancers_And_Monitoring()
    {
        var response = await Client.GetAsync("/api/templates?template=saas&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotNull(template.Monitoring);

        // Verify all major clouds are represented
        Assert.Contains(CloudProvider.AWS, template.LoadBalancers.Keys);
        Assert.Contains(CloudProvider.Azure, template.LoadBalancers.Keys);
        Assert.Contains(CloudProvider.GCP, template.LoadBalancers.Keys);

        Assert.Contains(CloudProvider.AWS, template.Monitoring.Keys);
        Assert.Contains(CloudProvider.Azure, template.Monitoring.Keys);
        Assert.Contains(CloudProvider.GCP, template.Monitoring.Keys);
    }

    [Fact]
    public async Task Get_Templates_WithSaasSmall_LoadBalancers_Have_Correct_Pricing()
    {
        var response = await Client.GetAsync("/api/templates?template=saas&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.NotNull(template.LoadBalancers);

        var awsLb = template.LoadBalancers[CloudProvider.AWS];
        var azureLb = template.LoadBalancers[CloudProvider.Azure];
        var gcpLb = template.LoadBalancers[CloudProvider.GCP];

        Assert.Equal(16.51m, awsLb.PricePerMonth);
        Assert.Equal(0m, azureLb.PricePerMonth);
        Assert.Equal(18.41m, gcpLb.PricePerMonth);
    }

    [Fact]
    public async Task Get_Templates_WithSaasSmall_Monitoring_Has_Correct_Pricing()
    {
        var response = await Client.GetAsync("/api/templates?template=saas&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.NotNull(template.Monitoring);

        var awsMon = template.Monitoring[CloudProvider.AWS];
        var azureMon = template.Monitoring[CloudProvider.Azure];
        var gcpMon = template.Monitoring[CloudProvider.GCP];

        Assert.Equal(5m, awsMon.PricePerMonth);
        Assert.Equal(6m, azureMon.PricePerMonth);
        Assert.Equal(4m, gcpMon.PricePerMonth);
    }

    private static void AssertValidTemplate(TemplateDto template)
    {
        Assert.NotNull(template);
        Assert.NotNull(template.VirtualMachines);
        Assert.NotEmpty(template.VirtualMachines);
        Assert.NotNull(template.Databases);
        Assert.NotEmpty(template.Databases);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotEmpty(template.LoadBalancers);
        Assert.NotNull(template.Monitoring);
        Assert.NotEmpty(template.Monitoring);
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

    // WordPress template tests
    [Fact]
    public async Task Get_Templates_WithWordPressSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=wordpress&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.WordPress, template.Template);
        Assert.Equal(UsageSize.Small, template.Usage);
        Assert.NotNull(template.VirtualMachines);
        Assert.NotEmpty(template.VirtualMachines);
        Assert.NotNull(template.Databases);
        Assert.NotEmpty(template.Databases);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotEmpty(template.LoadBalancers);
    }

    [Fact]
    public async Task Get_Templates_WithWordPressMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=wordpress&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.WordPress, template.Template);
        Assert.Equal(UsageSize.Medium, template.Usage);
    }

    [Fact]
    public async Task Get_Templates_WithWordPressLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=wordpress&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.WordPress, template.Template);
        Assert.Equal(UsageSize.Large, template.Usage);
    }

    // REST API template tests
    [Fact]
    public async Task Get_Templates_WithRestApiSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=rest_api&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.RestApi, template.Template);
        Assert.Equal(UsageSize.Small, template.Usage);
        Assert.NotNull(template.VirtualMachines);
        Assert.NotEmpty(template.VirtualMachines);
        Assert.NotNull(template.Databases);
        Assert.NotEmpty(template.Databases);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotEmpty(template.LoadBalancers);
        Assert.NotNull(template.Monitoring);
        Assert.NotEmpty(template.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithRestApiMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=rest_api&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.RestApi, template.Template);
        Assert.Equal(UsageSize.Medium, template.Usage);
    }

    [Fact]
    public async Task Get_Templates_WithRestApiLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=rest_api&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.RestApi, template.Template);
        Assert.Equal(UsageSize.Large, template.Usage);
    }

    // Static Site template tests
    [Fact]
    public async Task Get_Templates_WithStaticSiteSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=static_site&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.StaticSite, template.Template);
        Assert.Equal(UsageSize.Small, template.Usage);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotEmpty(template.LoadBalancers);
        // Static sites don't need VMs or databases
        Assert.True(template.VirtualMachines == null || template.VirtualMachines.Count == 0);
        Assert.True(template.Databases == null || template.Databases.Count == 0);
    }

    [Fact]
    public async Task Get_Templates_WithStaticSiteMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=static_site&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.StaticSite, template.Template);
        Assert.Equal(UsageSize.Medium, template.Usage);
    }

    [Fact]
    public async Task Get_Templates_WithStaticSiteLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=static_site&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.StaticSite, template.Template);
        Assert.Equal(UsageSize.Large, template.Usage);
    }

    // E-commerce template tests
    [Fact]
    public async Task Get_Templates_WithEcommerceSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=ecommerce&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Ecommerce, template.Template);
        Assert.Equal(UsageSize.Small, template.Usage);
        Assert.NotNull(template.VirtualMachines);
        Assert.NotEmpty(template.VirtualMachines);
        Assert.NotNull(template.Databases);
        Assert.NotEmpty(template.Databases);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotEmpty(template.LoadBalancers);
        Assert.NotNull(template.Monitoring);
        Assert.NotEmpty(template.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithEcommerceMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=ecommerce&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Ecommerce, template.Template);
        Assert.Equal(UsageSize.Medium, template.Usage);
    }

    [Fact]
    public async Task Get_Templates_WithEcommerceLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=ecommerce&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Ecommerce, template.Template);
        Assert.Equal(UsageSize.Large, template.Usage);
    }

    // Mobile App Backend template tests
    [Fact]
    public async Task Get_Templates_WithMobileAppBackendSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=mobile_app_backend&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MobileAppBackend, template.Template);
        Assert.Equal(UsageSize.Small, template.Usage);
        Assert.NotNull(template.VirtualMachines);
        Assert.NotEmpty(template.VirtualMachines);
        Assert.NotNull(template.Databases);
        Assert.NotEmpty(template.Databases);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotEmpty(template.LoadBalancers);
        Assert.NotNull(template.Monitoring);
        Assert.NotEmpty(template.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithMobileAppBackendMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=mobile_app_backend&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MobileAppBackend, template.Template);
        Assert.Equal(UsageSize.Medium, template.Usage);
    }

    [Fact]
    public async Task Get_Templates_WithMobileAppBackendLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=mobile_app_backend&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MobileAppBackend, template.Template);
        Assert.Equal(UsageSize.Large, template.Usage);
    }

    // Headless Frontend + API template tests
    [Fact]
    public async Task Get_Templates_WithHeadlessFrontendApiSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=headless_frontend_api&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.HeadlessFrontendApi, template.Template);
        Assert.Equal(UsageSize.Small, template.Usage);
        Assert.NotNull(template.VirtualMachines);
        Assert.NotEmpty(template.VirtualMachines);
        Assert.NotNull(template.Databases);
        Assert.NotEmpty(template.Databases);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotEmpty(template.LoadBalancers);
        Assert.NotNull(template.Monitoring);
        Assert.NotEmpty(template.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithHeadlessFrontendApiMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=headless_frontend_api&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.HeadlessFrontendApi, template.Template);
        Assert.Equal(UsageSize.Medium, template.Usage);
    }

    [Fact]
    public async Task Get_Templates_WithHeadlessFrontendApiLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=headless_frontend_api&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.HeadlessFrontendApi, template.Template);
        Assert.Equal(UsageSize.Large, template.Usage);
    }

    // Data Analytics & Reporting Platform template tests
    [Fact]
    public async Task Get_Templates_WithDataAnalyticsSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=data_analytics&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.DataAnalytics, template.Template);
        Assert.Equal(UsageSize.Small, template.Usage);
        Assert.NotNull(template.VirtualMachines);
        Assert.NotEmpty(template.VirtualMachines);
        Assert.NotNull(template.Databases);
        Assert.NotEmpty(template.Databases);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotEmpty(template.LoadBalancers);
        Assert.NotNull(template.Monitoring);
        Assert.NotEmpty(template.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithDataAnalyticsMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=data_analytics&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.DataAnalytics, template.Template);
        Assert.Equal(UsageSize.Medium, template.Usage);
    }

    [Fact]
    public async Task Get_Templates_WithDataAnalyticsLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=data_analytics&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.DataAnalytics, template.Template);
        Assert.Equal(UsageSize.Large, template.Usage);
    }

    // Machine Learning Inference Service template tests
    [Fact]
    public async Task Get_Templates_WithMachineLearningSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=machine_learning&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MachineLearning, template.Template);
        Assert.Equal(UsageSize.Small, template.Usage);
        Assert.NotNull(template.VirtualMachines);
        Assert.NotEmpty(template.VirtualMachines);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotEmpty(template.LoadBalancers);
        Assert.NotNull(template.Monitoring);
        Assert.NotEmpty(template.Monitoring);
        // Machine Learning typically doesn't need a traditional database
        Assert.True(template.Databases == null || template.Databases.Count == 0);
    }

    [Fact]
    public async Task Get_Templates_WithMachineLearningMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=machine_learning&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MachineLearning, template.Template);
        Assert.Equal(UsageSize.Medium, template.Usage);
    }

    [Fact]
    public async Task Get_Templates_WithMachineLearningLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=machine_learning&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MachineLearning, template.Template);
        Assert.Equal(UsageSize.Large, template.Usage);
    }

    // Serverless Event-Driven Application template tests
    [Fact]
    public async Task Get_Templates_WithServerlessEventDrivenSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=serverless_event_driven&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.ServerlessEventDriven, template.Template);
        Assert.Equal(UsageSize.Small, template.Usage);
        Assert.NotNull(template.LoadBalancers);
        Assert.NotEmpty(template.LoadBalancers);
        // Serverless doesn't need VMs or databases
        Assert.True(template.VirtualMachines == null || template.VirtualMachines.Count == 0);
        Assert.True(template.Databases == null || template.Databases.Count == 0);
        Assert.True(template.Monitoring == null || template.Monitoring.Count == 0);
    }

    [Fact]
    public async Task Get_Templates_WithServerlessEventDrivenMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=serverless_event_driven&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.ServerlessEventDriven, template.Template);
        Assert.Equal(UsageSize.Medium, template.Usage);
    }

    [Fact]
    public async Task Get_Templates_WithServerlessEventDrivenLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=serverless_event_driven&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.ServerlessEventDriven, template.Template);
        Assert.Equal(UsageSize.Large, template.Usage);
    }
}