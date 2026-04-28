using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Demo.Api.Tests;

public class InfoEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Info_Exposes_MachineName_And_AppOptions()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("machineName", out _));
        Assert.True(root.TryGetProperty("dotnetVersion", out _));
        Assert.True(root.TryGetProperty("appOptions", out var settings));
        Assert.True(settings.TryGetProperty("greeting", out _));
        Assert.True(settings.TryGetProperty("healthy", out _));
        Assert.True(settings.TryGetProperty("allowedOrigins", out _));
    }
}
