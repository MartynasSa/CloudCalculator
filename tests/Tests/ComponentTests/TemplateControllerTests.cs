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
        var response = await Client.GetAsync("/api/templates?template=saas&usage=small");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        AssertContainsResource(template, ResourceSubCategory.VirtualMachines);
        AssertContainsResource(template, ResourceSubCategory.Relational);
        AssertContainsResource(template, ResourceSubCategory.LoadBalancer);
        AssertContainsResource(template, ResourceSubCategory.Monitoring);

        await Verify(template);
    }

    [Fact]
    public async Task Get_Templates_WithSaasMedium_Returns_Template_With_Resources()
    {
        var response = await Client.GetAsync("/api/templates?template=saas&usage=medium");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        await Verify(template);
    }

    [Fact]
    public async Task Get_Templates_WithSaasLarge_Returns_Template_With_Resources()
    {
        var response = await Client.GetAsync("/api/templates?template=saas&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        await Verify(template);
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

    private static void AssertContainsResource(TemplateDto template, ResourceSubCategory resource)
    {
        Assert.Contains(resource, template.Resources);
    }

    private static void AssertEmptyTemplate(TemplateDto template)
    {
        Assert.True(template.Resources == null || template.Resources.Count == 0);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        AssertContainsResource(template, ResourceSubCategory.VirtualMachines);
        AssertContainsResource(template, ResourceSubCategory.Relational);
        AssertContainsResource(template, ResourceSubCategory.LoadBalancer);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        AssertContainsResource(template, ResourceSubCategory.VirtualMachines);
        AssertContainsResource(template, ResourceSubCategory.Relational);
        AssertContainsResource(template, ResourceSubCategory.LoadBalancer);
        AssertContainsResource(template, ResourceSubCategory.Monitoring);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        AssertContainsResource(template, ResourceSubCategory.LoadBalancer);
        Assert.DoesNotContain(ResourceSubCategory.VirtualMachines, template.Resources);
        Assert.DoesNotContain(ResourceSubCategory.Relational, template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        AssertContainsResource(template, ResourceSubCategory.VirtualMachines);
        AssertContainsResource(template, ResourceSubCategory.Relational);
        AssertContainsResource(template, ResourceSubCategory.LoadBalancer);
        AssertContainsResource(template, ResourceSubCategory.Monitoring);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        AssertContainsResource(template, ResourceSubCategory.VirtualMachines);
        AssertContainsResource(template, ResourceSubCategory.Relational);
        AssertContainsResource(template, ResourceSubCategory.LoadBalancer);
        AssertContainsResource(template, ResourceSubCategory.Monitoring);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        AssertContainsResource(template, ResourceSubCategory.VirtualMachines);
        AssertContainsResource(template, ResourceSubCategory.Relational);
        AssertContainsResource(template, ResourceSubCategory.LoadBalancer);
        AssertContainsResource(template, ResourceSubCategory.Monitoring);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        AssertContainsResource(template, ResourceSubCategory.VirtualMachines);
        AssertContainsResource(template, ResourceSubCategory.Relational);
        AssertContainsResource(template, ResourceSubCategory.LoadBalancer);
        AssertContainsResource(template, ResourceSubCategory.Monitoring);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        AssertContainsResource(template, ResourceSubCategory.VirtualMachines);
        AssertContainsResource(template, ResourceSubCategory.LoadBalancer);
        AssertContainsResource(template, ResourceSubCategory.Monitoring);
        Assert.DoesNotContain(ResourceSubCategory.Relational, template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);

        AssertContainsResource(template, ResourceSubCategory.LoadBalancer);
        Assert.DoesNotContain(ResourceSubCategory.VirtualMachines, template.Resources);
        Assert.DoesNotContain(ResourceSubCategory.Relational, template.Resources);
        Assert.DoesNotContain(ResourceSubCategory.Monitoring, template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
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
        Assert.NotNull(template.Resources);
        Assert.NotEmpty(template.Resources);
    }

    // Blank template tests
    [Fact]
    public async Task Get_Templates_WithBlankSmall_Returns_Empty_Template()
    {
        var response = await Client.GetAsync("/api/templates?template=blank&usage=small");
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
        var response = await Client.GetAsync("/api/templates?template=blank&usage=medium");
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
        var response = await Client.GetAsync("/api/templates?template=blank&usage=large");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Blank, template.Template);
        AssertEmptyTemplate(template);
    }
}