using System.Net;
using System.Text.Json;
using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.IntegrationTests;

public class CloudPricingControllerTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumMemberConverter() }
    };

    [Fact]
    public async Task Get_CloudPricingEndpoint_Returns_CloudPricingDto()
    {
        var response = await Client.GetAsync("/api/cloud-pricing");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<CloudPricingDto>(stream, JsonOptions);

        Assert.NotNull(dto);
        Assert.NotNull(dto.Data);
        Assert.NotNull(dto.Data.Products);
    }

    [Fact]
    public async Task Get_CategorizedResourcesEndpoint_Returns_CategorizedResourcesDto()
    {
        var response = await Client.GetAsync("/api/cloud-pricing/categorized-resources");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<CategorizedResourcesDto>(stream, JsonOptions);

        Assert.NotNull(dto);
        Assert.NotNull(dto.ComputeInstances);
        Assert.NotNull(dto.Databases);
        Assert.NotNull(dto.LoadBalancers);
    }
}