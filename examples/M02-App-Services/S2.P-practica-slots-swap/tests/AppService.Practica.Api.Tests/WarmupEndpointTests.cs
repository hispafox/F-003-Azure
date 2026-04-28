using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Practica.Api.Tests;

public class WarmupEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Warmup_Returns_200_With_Warm_Status()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/warmup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("warm", doc.RootElement.GetProperty("status").GetString());
    }
}
