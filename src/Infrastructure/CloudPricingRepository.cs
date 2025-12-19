using Application.Models.Dtos;
using Application.Ports;
using System.Text.Json;

namespace Infrastructure;

public class CloudPricingRepository(string? dataDirectory = null) : ICloudPricingRepository
{
    public async Task<CloudPricingDto> GetAllAsync(CancellationToken cancellationToken )
    {
        var filesToLoad = new[] { "aws.json", "azure.json", "gcp.json" };

        var dataDir = ResolveDataDirectory();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        var combined = new CloudPricingDto
        {
            Data = new CloudPricingDataDto()
        };

        foreach (var fileName in filesToLoad)
        {
            var path = Path.Combine(dataDir, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            await using var stream = File.OpenRead(path);
            var dto = await JsonSerializer.DeserializeAsync<CloudPricingDto>(stream, options, cancellationToken)
                      ?? new CloudPricingDto { Data = new CloudPricingDataDto() };

            if (dto.Data?.Products is { } products)
            {
                combined.Data.Products.AddRange(products);
            }
        }

        return combined;
    }

    private string ResolveDataDirectory()
    {
        if (!string.IsNullOrWhiteSpace(dataDirectory))
        {
            if (Directory.Exists(dataDirectory))
            {
                return dataDirectory;
            }
        }

        // 1) Prefer a Data folder next to the running assembly
        var baseDir = AppContext.BaseDirectory ?? Environment.CurrentDirectory;
        var candidate = Path.Combine(baseDir, "Data");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        // 2) Try common development layout relative to repo root
        candidate = Path.Combine(Environment.CurrentDirectory, "src", "Infrastructure", "Data");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        // 3) Fallback to a Data folder in current directory
        candidate = Path.Combine(Environment.CurrentDirectory, "Data");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        // If nothing found, use current directory (will result in no files loaded)
        return Environment.CurrentDirectory;
    }
}
