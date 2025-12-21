using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Tests.ComponentTests;

public class CloudPricingControllerTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    [Fact]
    public async Task Get_CloudPricingEndpoint_WithPagination_Returns_PagedResult()
    {
        var response = await Client.GetAsync("/api/cloud-pricing?page=1&pageSize=100");
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

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithVendorFilter_Returns_FilteredResults()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

        // baseline to compare total counts
        var baseResp = await Client.GetAsync("/api/cloud-pricing?page=1&pageSize=1");
        baseResp.EnsureSuccessStatusCode();
        await using var baseStream = await baseResp.Content.ReadAsStreamAsync();
        var basePaged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(baseStream, options);
        Assert.NotNull(basePaged);

        var vendor = "AWS";
        var response = await Client.GetAsync($"/api/cloud-pricing?vendorName={vendor}&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, options);

        Assert.NotNull(paged);
        Assert.NotNull(paged.Items);
        Assert.True(paged.TotalCount <= basePaged.TotalCount);

        if (paged.TotalCount > 0)
        {
            Assert.All(paged.Items, item =>
                Assert.Contains(vendor, item.VendorName.ToString()));
        }
    }

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithServiceFilter_Returns_FilteredResults()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

        var baseResp = await Client.GetAsync("/api/cloud-pricing?page=1&pageSize=1");
        baseResp.EnsureSuccessStatusCode();
        await using var baseStream = await baseResp.Content.ReadAsStreamAsync();
        var basePaged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(baseStream, options);
        Assert.NotNull(basePaged);

        var service = "compute";
        var response = await Client.GetAsync($"/api/cloud-pricing?service={service}&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, options);

        Assert.NotNull(paged);
        Assert.NotNull(paged.Items);
        Assert.True(paged.TotalCount <= basePaged.TotalCount);

        if (paged.TotalCount > 0)
        {
            Assert.All(paged.Items, item =>
                Assert.Contains(service, item.Service, System.StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithRegionFilter_Returns_FilteredResults()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

        var baseResp = await Client.GetAsync("/api/cloud-pricing?page=1&pageSize=1");
        baseResp.EnsureSuccessStatusCode();
        await using var baseStream = await baseResp.Content.ReadAsStreamAsync();
        var basePaged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(baseStream, options);
        Assert.NotNull(basePaged);

        var region = "us";
        var response = await Client.GetAsync($"/api/cloud-pricing?region={region}&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, options);

        Assert.NotNull(paged);
        Assert.NotNull(paged.Items);
        Assert.True(paged.TotalCount <= basePaged.TotalCount);

        if (paged.TotalCount > 0)
        {
            Assert.All(paged.Items, item =>
                Assert.Contains(region, item.Region, System.StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithProductFamilyFilter_Returns_FilteredResults()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

        var baseResp = await Client.GetAsync("/api/cloud-pricing?page=1&pageSize=1");
        baseResp.EnsureSuccessStatusCode();
        await using var baseStream = await baseResp.Content.ReadAsStreamAsync();
        var basePaged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(baseStream, options);
        Assert.NotNull(basePaged);

        var family = "storage";
        var response = await Client.GetAsync($"/api/cloud-pricing?productFamily={family}&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, options);

        Assert.NotNull(paged);
        Assert.NotNull(paged.Items);
        Assert.True(paged.TotalCount <= basePaged.TotalCount);

        if (paged.TotalCount > 0)
        {
            Assert.All(paged.Items, item =>
                Assert.Contains(family, item.ProductFamily, System.StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task Get_CloudPricingEndpoint_WithCombinedFilters_Returns_Intersection()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };

        var baseResp = await Client.GetAsync("/api/cloud-pricing?page=1&pageSize=1");
        baseResp.EnsureSuccessStatusCode();
        await using var baseStream = await baseResp.Content.ReadAsStreamAsync();
        var basePaged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(baseStream, options);
        Assert.NotNull(basePaged);

        var vendor = "aws";
        var service = "compute";
        var response = await Client.GetAsync($"/api/cloud-pricing?vendorName={vendor}&service={service}&page=1&pageSize=20");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, options);

        Assert.NotNull(paged);
        Assert.NotNull(paged.Items);
        Assert.True(paged.TotalCount <= basePaged.TotalCount);

        if (paged.TotalCount > 0)
        {
            Assert.All(paged.Items, item =>
            {
                Assert.Contains(vendor, item.VendorName.ToString());
                Assert.Contains(service, item.Service, System.StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}