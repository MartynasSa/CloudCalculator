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
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumMemberConverter() }
    };

    [Fact]
    public async Task Get_Templates_WithSaasSmall_Returns_Template_With_Resources()
    {
        var response = await Client.GetAsync("/api/template?template=saas&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        AssertContainsResource(template, ComputeType.VirtualMachines);
        AssertContainsResource(template, DatabaseType.Relational);
        AssertContainsResource(template, NetworkingType.LoadBalancer);
        AssertContainsResource(template, ManagementType.Monitoring);

        await Verify(template);
    }

    [Fact]
    public async Task Get_Templates_WithSaasMedium_Returns_Template_With_Resources()
    {
        var response = await Client.GetAsync("/api/template?template=saas&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        await Verify(template);
    }

    [Fact]
    public async Task Get_Templates_WithSaasLarge_Returns_Template_With_Resources()
    {
        var response = await Client.GetAsync("/api/template?template=saas&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        await Verify(template);
    }

    [Fact]
    public async Task Get_Templates_WithInvalidTemplate_Returns_BadRequest()
    {
        var response = await Client.GetAsync("/api/template?template=invalid&usage=small");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static bool HasResources(ResourcesDto resources)
    {
        return resources != null && (
            resources.Computes.Any() ||
            resources.Databases.Any() ||
            resources.Storages.Any() ||
            resources.Networks.Any() ||
            resources.Analytics.Any() ||
            resources.Management.Any() ||
            resources.Security.Any() ||
            resources.AI.Any()
        );
    }

    private static void AssertContainsResource<T>(TemplateDto template, T resource) where T : Enum
    {
        var resourcesDto = template.Resources;
        bool found = resource switch
        {
            ComputeType computeType => resourcesDto.Computes.Contains(computeType),
            DatabaseType databaseType => resourcesDto.Databases.Contains(databaseType),
            StorageType storageType => resourcesDto.Storages.Contains(storageType),
            NetworkingType networkingType => resourcesDto.Networks.Contains(networkingType),
            AnalyticsType analyticsType => resourcesDto.Analytics.Contains(analyticsType),
            ManagementType managementType => resourcesDto.Management.Contains(managementType),
            SecurityType securityType => resourcesDto.Security.Contains(securityType),
            AIType aiType => resourcesDto.AI.Contains(aiType),
            _ => false
        };

        Assert.True(found, $"Resource {resource} not found in template resources");
    }

    private static void AssertDoesNotContainResource<T>(TemplateDto template, T resource) where T : Enum
    {
        var resourcesDto = template.Resources;
        bool found = resource switch
        {
            ComputeType computeType => resourcesDto.Computes.Contains(computeType),
            DatabaseType databaseType => resourcesDto.Databases.Contains(databaseType),
            StorageType storageType => resourcesDto.Storages.Contains(storageType),
            NetworkingType networkingType => resourcesDto.Networks.Contains(networkingType),
            AnalyticsType analyticsType => resourcesDto.Analytics.Contains(analyticsType),
            ManagementType managementType => resourcesDto.Management.Contains(managementType),
            SecurityType securityType => resourcesDto.Security.Contains(securityType),
            AIType aiType => resourcesDto.AI.Contains(aiType),
            _ => false
        };

        Assert.False(found, $"Resource {resource} should not be found in template resources");
    }

    private static void AssertEmptyTemplate(TemplateDto template)
    {
        Assert.False(HasResources(template.Resources));
    }

    // WordPress template tests
    [Fact]
    public async Task Get_Templates_WithWordPressSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=wordpress&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.WordPress, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        AssertContainsResource(template, ComputeType.VirtualMachines);
        AssertContainsResource(template, DatabaseType.Relational);
        AssertContainsResource(template, NetworkingType.LoadBalancer);
    }

    [Fact]
    public async Task Get_Templates_WithWordPressMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=wordpress&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.WordPress, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    [Fact]
    public async Task Get_Templates_WithWordPressLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=wordpress&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.WordPress, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    // REST API template tests
    [Fact]
    public async Task Get_Templates_WithRestApiSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=rest_api&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.RestApi, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        AssertContainsResource(template, ComputeType.VirtualMachines);
        AssertContainsResource(template, DatabaseType.Relational);
        AssertContainsResource(template, NetworkingType.LoadBalancer);
        AssertContainsResource(template, ManagementType.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithRestApiMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=rest_api&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.RestApi, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    [Fact]
    public async Task Get_Templates_WithRestApiLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=rest_api&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.RestApi, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    // Static Site template tests
    [Fact]
    public async Task Get_Templates_WithStaticSiteSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=static_site&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.StaticSite, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        AssertContainsResource(template, NetworkingType.LoadBalancer);
        AssertDoesNotContainResource(template, ComputeType.VirtualMachines);
        AssertDoesNotContainResource(template, DatabaseType.Relational);
    }

    [Fact]
    public async Task Get_Templates_WithStaticSiteMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=static_site&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.StaticSite, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    [Fact]
    public async Task Get_Templates_WithStaticSiteLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=static_site&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.StaticSite, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    // E-commerce template tests
    [Fact]
    public async Task Get_Templates_WithEcommerceSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=ecommerce&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Ecommerce, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        AssertContainsResource(template, ComputeType.VirtualMachines);
        AssertContainsResource(template, DatabaseType.Relational);
        AssertContainsResource(template, NetworkingType.LoadBalancer);
        AssertContainsResource(template, ManagementType.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithEcommerceMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=ecommerce&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Ecommerce, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    [Fact]
    public async Task Get_Templates_WithEcommerceLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=ecommerce&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Ecommerce, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    // Mobile App Backend template tests
    [Fact]
    public async Task Get_Templates_WithMobileAppBackendSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=mobile_app_backend&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MobileAppBackend, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        AssertContainsResource(template, ComputeType.VirtualMachines);
        AssertContainsResource(template, DatabaseType.Relational);
        AssertContainsResource(template, NetworkingType.LoadBalancer);
        AssertContainsResource(template, ManagementType.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithMobileAppBackendMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=mobile_app_backend&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MobileAppBackend, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    [Fact]
    public async Task Get_Templates_WithMobileAppBackendLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=mobile_app_backend&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MobileAppBackend, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    // Headless Frontend + API template tests
    [Fact]
    public async Task Get_Templates_WithHeadlessFrontendApiSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=headless_frontend_api&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.HeadlessFrontendApi, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        AssertContainsResource(template, ComputeType.VirtualMachines);
        AssertContainsResource(template, DatabaseType.Relational);
        AssertContainsResource(template, NetworkingType.LoadBalancer);
        AssertContainsResource(template, ManagementType.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithHeadlessFrontendApiMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=headless_frontend_api&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.HeadlessFrontendApi, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    [Fact]
    public async Task Get_Templates_WithHeadlessFrontendApiLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=headless_frontend_api&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.HeadlessFrontendApi, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    // Data Analytics & Reporting Platform template tests
    [Fact]
    public async Task Get_Templates_WithDataAnalyticsSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=data_analytics&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.DataAnalytics, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        AssertContainsResource(template, ComputeType.VirtualMachines);
        AssertContainsResource(template, DatabaseType.Relational);
        AssertContainsResource(template, NetworkingType.LoadBalancer);
        AssertContainsResource(template, ManagementType.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithDataAnalyticsMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=data_analytics&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.DataAnalytics, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    [Fact]
    public async Task Get_Templates_WithDataAnalyticsLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=data_analytics&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.DataAnalytics, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    // Machine Learning Inference Service template tests
    [Fact]
    public async Task Get_Templates_WithMachineLearningSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=machine_learning&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MachineLearning, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        AssertContainsResource(template, ComputeType.VirtualMachines);
        AssertContainsResource(template, NetworkingType.LoadBalancer);
        AssertContainsResource(template, ManagementType.Monitoring);
        AssertDoesNotContainResource(template, DatabaseType.Relational);
    }

    [Fact]
    public async Task Get_Templates_WithMachineLearningMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=machine_learning&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MachineLearning, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    [Fact]
    public async Task Get_Templates_WithMachineLearningLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=machine_learning&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.MachineLearning, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    // Serverless Event-Driven Application template tests
    [Fact]
    public async Task Get_Templates_WithServerlessEventDrivenSmall_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=serverless_event_driven&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.ServerlessEventDriven, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));

        AssertContainsResource(template, NetworkingType.LoadBalancer);
        AssertDoesNotContainResource(template, ComputeType.VirtualMachines);
        AssertDoesNotContainResource(template, DatabaseType.Relational);
        AssertDoesNotContainResource(template, ManagementType.Monitoring);
    }

    [Fact]
    public async Task Get_Templates_WithServerlessEventDrivenMedium_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=serverless_event_driven&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.ServerlessEventDriven, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    [Fact]
    public async Task Get_Templates_WithServerlessEventDrivenLarge_Returns_Template()
    {
        var response = await Client.GetAsync("/api/template?template=serverless_event_driven&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.ServerlessEventDriven, template.Template);
        Assert.NotNull(template.Resources);
        Assert.True(HasResources(template.Resources));
    }

    // Blank template tests
    [Fact]
    public async Task Get_Templates_WithBlankSmall_Returns_Empty_Template()
    {
        var response = await Client.GetAsync("/api/template?template=blank&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Blank, template.Template);
        AssertEmptyTemplate(template);
    }

    [Fact]
    public async Task Get_Templates_WithBlankMedium_Returns_Empty_Template()
    {
        var response = await Client.GetAsync("/api/template?template=blank&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Blank, template.Template);
        AssertEmptyTemplate(template);
    }

    [Fact]
    public async Task Get_Templates_WithBlankLarge_Returns_Empty_Template()
    {
        var response = await Client.GetAsync("/api/template?template=blank&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Blank, template.Template);
        AssertEmptyTemplate(template);
    }

    [Fact]
    public async Task Get_Templates_List_Returns_All_Templates_With_Small_Resources()
    {
        var response = await Client.GetAsync("/api/templates");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var templates = await JsonSerializer.DeserializeAsync<List<TemplateDto>>(stream, JsonOptions);

        await Verify(templates);
    }
}