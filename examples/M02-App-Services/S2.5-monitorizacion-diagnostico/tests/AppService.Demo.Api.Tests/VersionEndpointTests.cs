using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AppService.Demo.Api.Tests;

public class VersionEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Version_Returns_Configured_Version_And_SlotName()
    {
        using var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AppOptions:Version"] = "2.4.0",
                    ["AppOptions:EnvironmentLabel"] = "production"
                });
            });
        });

        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("2.4.0", root.GetProperty("version").GetString());
        Assert.Equal("production", root.GetProperty("environmentLabel").GetString());
        Assert.Equal("local", root.GetProperty("slotName").GetString());
    }
}
