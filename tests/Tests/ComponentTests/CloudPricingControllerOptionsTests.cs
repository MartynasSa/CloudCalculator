using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Tests.ComponentTests;

public class CloudPricingControllerOptionsTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    [Fact]
    public async Task Get_Options_Returns_Ok_And_AllCollections_Present()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
        var response = await ResolveOptionsEndpointAndGetAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<DistinctFiltersDto>(stream, options);

        Assert.NotNull(dto);
        Assert.NotNull(dto.VendorNames);
        Assert.NotNull(dto.Services);
        Assert.NotNull(dto.Regions);
        Assert.NotNull(dto.ProductFamilies);
        Assert.NotNull(dto.AttributeSummaries);
    }

    [Fact]
    public async Task Get_Options_VendorNames_AreDistinct_And_Sorted()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
        var response = await ResolveOptionsEndpointAndGetAsync();
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<DistinctFiltersDto>(stream, options);
        Assert.NotNull(dto);

        var vendors = dto.VendorNames;
        Assert.NotNull(vendors);

        // distinct
        Assert.Equal(vendors.Count, vendors.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        // sorted (case-insensitive)
        var sorted = vendors.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(sorted, vendors);
    }

    [Fact]
    public async Task Get_Options_Services_And_Regions_AreDistinct_And_Sorted()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
        var response = await ResolveOptionsEndpointAndGetAsync();
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<DistinctFiltersDto>(stream, options);
        Assert.NotNull(dto);

        Assert.Equal(dto.Services.Count, dto.Services.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(dto.Regions.Count, dto.Regions.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        Assert.Equal(dto.Services.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(), dto.Services);
        Assert.Equal(dto.Regions.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(), dto.Regions);
    }

    [Fact]
    public async Task Get_Options_ProductFamilies_AreDistinct_And_AttributeSummaries_Have_Keys_And_Values()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
        var response = await ResolveOptionsEndpointAndGetAsync();
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<DistinctFiltersDto>(stream, options);
        Assert.NotNull(dto);

        Assert.Equal(dto.ProductFamilies.Count, dto.ProductFamilies.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(dto.ProductFamilies.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(), dto.ProductFamilies);

        // attribute summaries checks
        Assert.All(dto.AttributeSummaries, summary =>
        {
            Assert.False(string.IsNullOrWhiteSpace(summary.Key));
            Assert.NotNull(summary.Values);
            Assert.Equal(summary.Values.Count, summary.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(summary.Values.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(), summary.Values);
        });
    }

    [Fact]
    public async Task Get_Options_Endpoint_Uses_Expected_Route_WithOrWithoutColon()
    {
        // Ensure endpoint is reachable at either "/api/cloud-pricing/options" or "/api/cloud-pricing/:options"
        var primary = await Client.GetAsync("/api/cloud-pricing/options");
        if (primary.StatusCode == HttpStatusCode.OK)
        {
            primary.EnsureSuccessStatusCode();
            return;
        }

        var alt = await Client.GetAsync("/api/cloud-pricing/:options");
        alt.EnsureSuccessStatusCode();
    }

    // Helper to try the common route and fallback to the literal route created by the current attribute.
    private async Task<HttpResponseMessage> ResolveOptionsEndpointAndGetAsync()
    {
        var primary = await Client.GetAsync("/api/cloud-pricing/options");
        if (primary.StatusCode == HttpStatusCode.OK)
            return primary;

        return await Client.GetAsync("/api/cloud-pricing/:options");
    }
}