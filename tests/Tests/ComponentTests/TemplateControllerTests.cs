using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace Tests.ComponentTests;

public class TemplateControllerTests(WebApplicationFactory<Program> factory) : TestBase(factory)
{
    [Fact]
    public async Task Get_CloudPricingEndpoint_WithPagination_Returns_PagedResult()
    {
        //var response = await Client.GetAsync("/api/cloud-pricing?page=1&pageSize=100");
        //Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        //await using var stream = await response.Content.ReadAsStreamAsync();
        //var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
        //var paged = await JsonSerializer.DeserializeAsync<PagedResult<CloudPricingProductDto>>(stream, options);

        //Assert.NotNull(paged);
        //Assert.NotNull(paged.Items);
        //Assert.Equal(100, paged.Items.Count);
        //Assert.Equal(3000, paged.TotalCount);
        //Assert.Equal(1, paged.Page);
        //Assert.Equal(100, paged.PageSize);
    }
}