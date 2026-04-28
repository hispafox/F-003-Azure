using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HelloWorld.Tests;

public class ApiInfoTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Info_Reads_Curso_Env_Vars_From_Configuration()
    {
        using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CURSO_MODULO"] = "1",
                    ["CURSO_SESION"] = "Introduccion",
                    ["CURSO_FECHA"] = "2026-04-22"
                }));
        });

        using var client = custom.CreateClient();
        var response = await client.GetAsync("/api/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("1", root.GetProperty("modulo").GetString());
        Assert.Equal("Introduccion", root.GetProperty("sesion").GetString());
        Assert.Equal("2026-04-22", root.GetProperty("fecha").GetString());
    }

    [Fact]
    public async Task Info_Returns_Defaults_When_Vars_Missing()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("no definido", root.GetProperty("modulo").GetString());
        Assert.Equal("no definida", root.GetProperty("sesion").GetString());
        Assert.Equal("sin fecha", root.GetProperty("fecha").GetString());
    }
}
