using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HelloWorld.Tests;

public class RootEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Root_Returns_All_Required_Fields()
    {
        using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Asistente"] = "Pedro"
                }));
        });

        using var client = custom.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        // Slide 27 — los siete campos esperados deben existir
        Assert.Equal("Hello Azure — Curso AZ-204", root.GetProperty("mensaje").GetString());
        Assert.Equal("Pedro", root.GetProperty("asistente").GetString());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("entorno").GetString()));
        Assert.False(string.IsNullOrEmpty(root.GetProperty("servidor").GetString()));
        Assert.False(string.IsNullOrEmpty(root.GetProperty("hora_utc").GetString()));
        Assert.False(string.IsNullOrEmpty(root.GetProperty("runtime").GetString()));
        Assert.False(string.IsNullOrEmpty(root.GetProperty("os").GetString()));
    }

    [Fact]
    public async Task Root_Falls_Back_To_Placeholder_When_Asistente_Not_Configured()
    {
        using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Asistente"] = null
                }));
        });

        using var client = custom.CreateClient();
        var response = await client.GetAsync("/");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var asistente = doc.RootElement.GetProperty("asistente").GetString()!;

        // Sin valor explícito viene del appsettings.json o del placeholder del código
        Assert.Contains("TU-NOMBRE-AQUÍ", asistente);
    }
}
