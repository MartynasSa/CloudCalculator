using Application.Models.Dtos;
using Application.Ports;
using System.Text.Json;

namespace Infrastructure;

public class CloudPricingRepository : ICloudPricingRepository
{
    private readonly string? _dataDirectory;

    public CloudPricingRepository(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory;
    }

    /// <summary>
    /// Loads pricing JSON files (aws.json, azure.json, gcp.json) from the repository Data folder,
    /// deserializes them into CloudPricingDto and returns a single combined CloudPricingDto
    /// with all products merged.
    /// </summary>
    public async Task<CloudPricingDto> GetAllAsync(CancellationToken cancellationToken )
    {
        var filesToLoad = new[] { "aws.json", "azure.json", "gcp.json" };

        var dataDir = ResolveDataDirectory();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
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
                // skip missing files
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
        if (!string.IsNullOrWhiteSpace(_dataDirectory))
        {
            if (Directory.Exists(_dataDirectory))
            {
                return _dataDirectory;
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
