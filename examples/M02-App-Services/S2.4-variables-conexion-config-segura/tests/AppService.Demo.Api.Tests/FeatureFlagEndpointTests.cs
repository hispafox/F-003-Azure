using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AppService.Demo.Api.Tests;

public class FeatureFlagEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task NewUI_Returns_V1_When_Feature_Disabled()
    {
        using var disabled = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:NewUI"] = "false"
                }));
        });

        using var client = disabled.CreateClient();
        var response = await client.GetAsync("/features/new-ui");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.False(root.GetProperty("enabled").GetBoolean());
        Assert.Equal("v1", root.GetProperty("payload").GetProperty("version").GetString());
    }

    [Fact]
    public async Task NewUI_Returns_V2_When_Feature_Enabled()
    {
        using var enabled = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:NewUI"] = "true"
                }));
        });

        using var client = enabled.CreateClient();
        var response = await client.GetAsync("/features/new-ui");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.GetProperty("enabled").GetBoolean());
        Assert.Equal("v2", root.GetProperty("payload").GetProperty("version").GetString());
    }
}
