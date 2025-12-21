using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;

namespace Tests.ComponentTests;

public class CloudPricingControllerOptionsTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    [Fact]
    public async Task Get_Options_Returns_Ok_And_AllCollections_Present()
    {
        var response = await Client.GetAsync("/api/cloud-pricing:options");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<DistinctFiltersDto>(stream, JsonOptions);

        await Verify(dto);
    }

    [Fact]
    public async Task Get_Options_VendorNames_AreDistinct_And_Sorted()
    {
        var response = await Client.GetAsync("/api/cloud-pricing:options");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<DistinctFiltersDto>(stream, JsonOptions);

        var vendors = dto.VendorNames;
        Assert.NotNull(vendors);

        // Verify distinct and sorted
        Assert.Equal(vendors.Count, vendors.Distinct().Count());
        Assert.Equal(vendors.OrderBy(v => v).ToList(), vendors);

        await Verify(vendors);
    }

    [Fact]
    public async Task Get_Options_Services_And_Regions_AreDistinct_And_Sorted()
    {
        var response = await Client.GetAsync("/api/cloud-pricing:options");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<DistinctFiltersDto>(stream, JsonOptions);

        Assert.NotNull(dto);
        Assert.Equal(dto.Services.Count, dto.Services.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(dto.Regions.Count, dto.Regions.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        Assert.Equal(dto.Services.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(), dto.Services);
        Assert.Equal(dto.Regions.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(), dto.Regions);

        await Verify(dto);
    }

    [Fact]
    public async Task Get_Options_ProductFamilies_AreDistinct_And_AttributeSummaries_Have_Keys_And_Values()
    {
        var response = await Client.GetAsync("/api/cloud-pricing:options");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<DistinctFiltersDto>(stream, JsonOptions);
        Assert.NotNull(dto);

        Assert.Equal(dto.ProductFamilies.Count, dto.ProductFamilies.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(dto.ProductFamilies.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(), dto.ProductFamilies);

        // Verify attribute summaries structure
        Assert.All(dto.AttributeSummaries, summary =>
        {
            Assert.False(string.IsNullOrWhiteSpace(summary.Key));
            Assert.NotNull(summary.Values);
            Assert.Equal(summary.Values.Count, summary.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(summary.Values.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(), summary.Values);
        });

        await Verify(dto);
    }
}