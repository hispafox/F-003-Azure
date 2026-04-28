using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AppService.Practica.Api.Tests;

public class HomeEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Root_Reflects_Version_And_Novedad_From_Configuration()
    {
        using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Practica:Version"] = "2.0",
                    ["Practica:Novedad"] = "Slots de despliegue funcionando",
                    ["Practica:NotaEntorno"] = "production"
                }));
        });

        using var client = custom.CreateClient();
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("2.0", root.GetProperty("version").GetString());
        Assert.Equal("Slots de despliegue funcionando", root.GetProperty("novedad").GetString());
        Assert.Equal("production", root.GetProperty("nota_entorno").GetString());
        Assert.Equal("local", root.GetProperty("slot").GetString());
        Assert.NotNull(root.GetProperty("servidor").GetString());
    }

    [Fact]
    public async Task Root_Returns_Defaults_When_No_Practica_Config()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("1.0", root.GetProperty("version").GetString());
        Assert.NotEmpty(root.GetProperty("novedad").GetString()!);
    }
}
