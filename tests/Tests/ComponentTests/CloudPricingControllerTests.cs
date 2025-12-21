using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;

namespace Tests.ComponentTests;

public class CloudPricingControllerTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithPagination_Returns_PagedResult()
    {
        var response = await Client.GetAsync("/api/cloud-pricing?page=1&pageSize=100");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, JsonOptions);

        AssertValidPagedResult(paged, expectedPageSize: 100, expectedTotalCount: 3000, expectedPage: 1);
        await Verify(paged);
    }

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithVendorFilter_Returns_FilteredResults()
    {
        var basePaged = await GetBaselinePagedResult();

        var vendor = "AWS";
        var response = await Client.GetAsync($"/api/cloud-pricing?vendorName={vendor}&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, JsonOptions);

        AssertFilteredResults(paged, basePaged, item => item.VendorName.ToString(), vendor);
        await Verify(paged);
    }

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithServiceFilter_Returns_FilteredResults()
    {
        var basePaged = await GetBaselinePagedResult();

        var service = "compute";
        var response = await Client.GetAsync($"/api/cloud-pricing?service={service}&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, JsonOptions);

        AssertFilteredResults(paged, basePaged, item => item.Service, service, StringComparison.OrdinalIgnoreCase);
        await Verify(paged);
    }

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithRegionFilter_Returns_FilteredResults()
    {
        var basePaged = await GetBaselinePagedResult();

        var region = "us";
        var response = await Client.GetAsync($"/api/cloud-pricing?region={region}&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, JsonOptions);

        AssertFilteredResults(paged, basePaged, item => item.Region, region, StringComparison.OrdinalIgnoreCase);
        await Verify(paged);
    }

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithProductFamilyFilter_Returns_FilteredResults()
    {
        var basePaged = await GetBaselinePagedResult();

        var family = "storage";
        var response = await Client.GetAsync($"/api/cloud-pricing?productFamily={family}&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, JsonOptions);

        AssertFilteredResults(paged, basePaged, item => item.ProductFamily, family, StringComparison.OrdinalIgnoreCase);
        await Verify(paged);
    }

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithCombinedFilters_Returns_Intersection()
    {
        var basePaged = await GetBaselinePagedResult();

        var vendor = "aws";
        var service = "compute";
        var response = await Client.GetAsync($"/api/cloud-pricing?vendorName={vendor}&service={service}&page=1&pageSize=20");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, JsonOptions);

        Assert.NotNull(paged);
        Assert.NotNull(paged.Items);
        Assert.True(paged.TotalCount <= basePaged.TotalCount);

        if (paged.TotalCount > 0)
        {
            Assert.All(paged.Items, item =>
            {
                Assert.Contains(vendor, item.VendorName.ToString(), StringComparison.OrdinalIgnoreCase);
                Assert.Contains(service, item.Service, StringComparison.OrdinalIgnoreCase);
            });
        }

        await Verify(paged);
    }

    private async Task<PagedResult<CloudPricingProductDto>> GetBaselinePagedResult()
    {
        var response = await Client.GetAsync("/api/cloud-pricing?page=1&pageSize=1");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var basePaged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, JsonOptions);
        Assert.NotNull(basePaged);

        return basePaged;
    }

    private static void AssertValidPagedResult(
        PagedResult<CloudPricingProductDto> paged,
        int expectedPageSize,
        int expectedTotalCount,
        int expectedPage)
    {
        Assert.NotNull(paged);
        Assert.NotNull(paged.Items);
        Assert.Equal(expectedPageSize, paged.Items.Count);
        Assert.Equal(expectedTotalCount, paged.TotalCount);
        Assert.Equal(expectedPage, paged.Page);
        Assert.Equal(expectedPageSize, paged.PageSize);
    }

    private static void AssertFilteredResults(
        PagedResult<CloudPricingProductDto> paged,
        PagedResult<CloudPricingProductDto> basePaged,
        Func<CloudPricingProductDto, string> selector,
        string filterValue,
        StringComparison comparison = StringComparison.Ordinal)
    {
        Assert.NotNull(paged);
        Assert.NotNull(paged.Items);
        Assert.True(paged.TotalCount <= basePaged.TotalCount);

        if (paged.TotalCount > 0)
        {
            Assert.All(paged.Items, item =>
                Assert.Contains(filterValue, selector(item), comparison));
        }
    }
}