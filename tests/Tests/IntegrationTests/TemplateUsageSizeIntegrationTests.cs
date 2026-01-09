using Application.Models.Dtos;
using static Tests.ResourceSpecificationTestHelper;
using Application.Models.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Tests.IntegrationTests;

public class TemplateUsageSizeIntegrationTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumMemberConverter() }
    };

    [Fact]
    public async Task GetTemplate_WithSaasAndSmallSize_ReturnsVirtualMachines()
    {
        // Act
        var response = await Client.GetAsync("/api/template?template=saas&usage=small");
        
        // Assert
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.Contains(VirtualMachines(), template.Resources);
        Assert.DoesNotContain(Kubernetes(), template.Resources);
    }

    [Fact]
    public async Task GetTemplate_WithSaasAndMediumSize_ReturnsVirtualMachines()
    {
        // Act
        var response = await Client.GetAsync("/api/template?template=saas&usage=medium");
        
        // Assert
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.Contains(VirtualMachines(), template.Resources);
        Assert.DoesNotContain(Kubernetes(), template.Resources);
    }

    [Fact]
    public async Task GetTemplate_WithSaasAndLargeSize_ReturnsKubernetes()
    {
        // Act
        var response = await Client.GetAsync("/api/template?template=saas&usage=large");
        
        // Assert
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.Contains(Kubernetes(), template.Resources);
        Assert.DoesNotContain(VirtualMachines(), template.Resources);
        // Verify other resources are still present
        Assert.Contains(Relational(), template.Resources);
        Assert.Contains(LoadBalancer(), template.Resources);
        Assert.Contains(Monitoring(), template.Resources);
    }

    [Fact]
    public async Task GetTemplate_WithSaasAndExtraLargeSize_ReturnsKubernetes()
    {
        // Act
        var response = await Client.GetAsync("/api/template?template=saas&usage=xlarge");
        
        // Assert
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.Saas, template.Template);
        Assert.Contains(Kubernetes(), template.Resources);
        Assert.DoesNotContain(VirtualMachines(), template.Resources);
    }

    [Fact]
    public async Task GetTemplate_WithWordPressAndLargeSize_ReturnsKubernetes()
    {
        // Act
        var response = await Client.GetAsync("/api/template?template=wordpress&usage=large");
        
        // Assert
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.WordPress, template.Template);
        Assert.Contains(Kubernetes(), template.Resources);
        Assert.DoesNotContain(VirtualMachines(), template.Resources);
    }

    [Fact]
    public async Task GetTemplate_WithStaticSiteAndLargeSize_DoesNotHaveComputeResources()
    {
        // Act
        var response = await Client.GetAsync("/api/template?template=static_site&usage=large");
        
        // Assert
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        var template = await JsonSerializer.DeserializeAsync<TemplateDto>(stream, JsonOptions);

        Assert.NotNull(template);
        Assert.Equal(TemplateType.StaticSite, template.Template);
        // Static site doesn't have VirtualMachines, so should not have Kubernetes either
        Assert.DoesNotContain(Kubernetes(), template.Resources);
        Assert.DoesNotContain(VirtualMachines(), template.Resources);
        Assert.Contains(LoadBalancer(), template.Resources);
    }

    [Fact]
    public async Task GetTemplate_WithMissingUsageSize_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/template?template=saas");
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTemplate_WithMissingTemplate_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/template?usage=small");
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
