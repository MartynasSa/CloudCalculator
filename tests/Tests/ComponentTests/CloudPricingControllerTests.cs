using Application.Models.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Tests.ComponentTests;

public class CloudPricingControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CloudPricingControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_CloudPricingEndpoint_Returns_CloudPricingDto()
    {
        // Ensure the factory uses the repository's content root so the Data files are discoverable
        var contentRoot = FindWebApiContentRoot() ?? throw new InvalidOperationException("Could not locate WebApi content root.");
        var client = _factory.WithWebHostBuilder(builder => builder.UseContentRoot(contentRoot)).CreateClient();

        var response = await client.GetAsync("/api/cloud-pricing");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
        var dto = await JsonSerializer.DeserializeAsync<CloudPricingDto>(stream, options);

        Assert.NotNull(dto);
        Assert.NotNull(dto.Data);
        Assert.NotNull(dto.Data.Products);
        Assert.Equal(3000, dto.Data.Products.Count);
    }

    private static string? FindWebApiContentRoot()
    {
        // Walk up the directory tree from the test host and look for the WebApi project folder.
        var dir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(dir);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "src", "WebApi");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}