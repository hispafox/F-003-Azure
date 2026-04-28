using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MiPrimeraWebApp.Tests;

public class RootEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Root_Returns_Application_Info()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("Mi Primera Web App", root.GetProperty("aplicacion").GetString());
        Assert.Equal("1.0", root.GetProperty("version").GetString());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("entorno").GetString()));
        Assert.True(root.TryGetProperty("hora_servidor", out _));
    }
}
