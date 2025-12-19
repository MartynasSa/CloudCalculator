using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests;

public abstract class TestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly HttpClient Client;

    protected TestBase(WebApplicationFactory<Program> factory)
    {
        var contentRoot = FindWebApiContentRoot() ?? throw new InvalidOperationException("Could not locate WebApi content root.");
        Client = factory.WithWebHostBuilder(builder => builder.UseContentRoot(contentRoot)).CreateClient();
    }

    private static string? FindWebApiContentRoot()
    {
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