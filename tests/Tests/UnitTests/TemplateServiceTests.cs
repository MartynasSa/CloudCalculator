using Application.Models.Enums;
using static Tests.ResourceSpecificationTestHelper;
using Application.Services;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.UnitTests;

public class TemplateServiceTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    [Fact]
    public void GetTemplate_WithSaasAndSmallSize_ReturnsVirtualMachines()
    {
        // Arrange
        var service = new TemplateService();

        // Act
        var result = service.GetTemplate(TemplateType.Saas, UsageSize.Small);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.Saas, result.Template);
        Assert.Contains(VirtualMachines(), result.Resources);
        Assert.DoesNotContain(Kubernetes(), result.Resources);
    }

    [Fact]
    public void GetTemplate_WithSaasAndMediumSize_ReturnsVirtualMachines()
    {
        // Arrange
        var service = new TemplateService();

        // Act
        var result = service.GetTemplate(TemplateType.Saas, UsageSize.Medium);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.Saas, result.Template);
        Assert.Contains(VirtualMachines(), result.Resources);
        Assert.DoesNotContain(Kubernetes(), result.Resources);
    }

    [Fact]
    public void GetTemplate_WithSaasAndLargeSize_ReturnsKubernetes()
    {
        // Arrange
        var service = new TemplateService();

        // Act
        var result = service.GetTemplate(TemplateType.Saas, UsageSize.Large);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.Saas, result.Template);
        Assert.Contains(Kubernetes(), result.Resources);
        Assert.DoesNotContain(VirtualMachines(), result.Resources);
        Assert.Contains(Relational(), result.Resources);
        Assert.Contains(LoadBalancer(), result.Resources);
        Assert.Contains(Monitoring(), result.Resources);
    }

    [Fact]
    public void GetTemplate_WithSaasAndExtraLargeSize_ReturnsKubernetes()
    {
        // Arrange
        var service = new TemplateService();

        // Act
        var result = service.GetTemplate(TemplateType.Saas, UsageSize.ExtraLarge);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.Saas, result.Template);
        Assert.Contains(Kubernetes(), result.Resources);
        Assert.DoesNotContain(VirtualMachines(), result.Resources);
    }

    [Fact]
    public void GetTemplate_WithRestApiAndLargeSize_ReturnsKubernetes()
    {
        // Arrange
        var service = new TemplateService();

        // Act
        var result = service.GetTemplate(TemplateType.RestApi, UsageSize.Large);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.RestApi, result.Template);
        Assert.Contains(Kubernetes(), result.Resources);
        Assert.DoesNotContain(VirtualMachines(), result.Resources);
    }

    [Fact]
    public void GetTemplate_WithWordPressAndLargeSize_ReturnsKubernetes()
    {
        // Arrange
        var service = new TemplateService();

        // Act
        var result = service.GetTemplate(TemplateType.WordPress, UsageSize.Large);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.WordPress, result.Template);
        Assert.Contains(Kubernetes(), result.Resources);
        Assert.DoesNotContain(VirtualMachines(), result.Resources);
    }

    [Fact]
    public void GetTemplate_WithStaticSiteAndLargeSize_DoesNotChangeResources()
    {
        // Arrange
        var service = new TemplateService();

        // Act
        var result = service.GetTemplate(TemplateType.StaticSite, UsageSize.Large);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.StaticSite, result.Template);
        // Static site doesn't have VirtualMachines, so should not have Kubernetes either
        Assert.DoesNotContain(Kubernetes(), result.Resources);
        Assert.DoesNotContain(VirtualMachines(), result.Resources);
        Assert.Contains(LoadBalancer(), result.Resources);
    }

    [Fact]
    public void GetTemplate_WithBlankAndLargeSize_ReturnsEmptyResources()
    {
        // Arrange
        var service = new TemplateService();

        // Act
        var result = service.GetTemplate(TemplateType.Blank, UsageSize.Large);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.Blank, result.Template);
        Assert.Empty(result.Resources);
    }

    [Fact]
    public void GetTemplate_WithEcommerceAndSmallSize_ReturnsVirtualMachines()
    {
        // Arrange
        var service = new TemplateService();

        // Act
        var result = service.GetTemplate(TemplateType.Ecommerce, UsageSize.Small);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.Ecommerce, result.Template);
        Assert.Contains(VirtualMachines(), result.Resources);
        Assert.DoesNotContain(Kubernetes(), result.Resources);
    }

    [Fact]
    public void GetTemplate_WithEcommerceAndLargeSize_ReturnsKubernetes()
    {
        // Arrange
        var service = new TemplateService();

        // Act
        var result = service.GetTemplate(TemplateType.Ecommerce, UsageSize.Large);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TemplateType.Ecommerce, result.Template);
        Assert.Contains(Kubernetes(), result.Resources);
        Assert.DoesNotContain(VirtualMachines(), result.Resources);
    }
}
