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
    public async Task Get_CloudPricingEndpoint_WithPagination_Returns_PagedResult()
    {
        // Ensure the factory uses the repository's content root so the Data files are discoverable
        var contentRoot = FindWebApiContentRoot() ?? throw new InvalidOperationException("Could not locate WebApi content root.");
        var client = _factory.WithWebHostBuilder(builder => builder.UseContentRoot(contentRoot)).CreateClient();

        // Request first page with 100 items per page
        var response = await client.GetAsync("/api/cloud-pricing?page=1&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, options);

        Assert.NotNull(paged);
        Assert.NotNull(paged.Items);
        Assert.Equal(100, paged.Items.Count);
        Assert.Equal(3000, paged.TotalCount);
        Assert.Equal(1, paged.Page);
        Assert.Equal(100, paged.PageSize);
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