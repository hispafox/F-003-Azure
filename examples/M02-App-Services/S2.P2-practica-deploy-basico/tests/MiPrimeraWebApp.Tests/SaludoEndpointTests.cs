using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MiPrimeraWebApp.Tests;

public class SaludoEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Saludo_Returns_Greeting_From_Settings()
    {
        using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Saludo:Base"] = "Hola desde el test,",
                    ["Saludo:MaxLength"] = "50"
                }));
        });

        using var client = custom.CreateClient();
        var response = await client.GetAsync("/saludo/Pedro");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Hola desde el test, Pedro", doc.RootElement.GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task Saludo_Returns_400_When_Name_Exceeds_MaxLength()
    {
        using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Saludo:MaxLength"] = "5"
                }));
        });

        using var client = custom.CreateClient();
        var response = await client.GetAsync("/saludo/NombreLargo");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
