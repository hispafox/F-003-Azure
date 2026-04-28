using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AppService.Demo.Api.Tests;

public class SecretsEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Check_Returns_Metadata_But_Never_The_Value()
    {
        const string secretValue = "super-secret-12345678";

        using var custom = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AppOptions:ApiKey"] = secretValue
                }));
        });

        using var client = custom.CreateClient();
        var response = await client.GetAsync("/secrets/api-key/check");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(secretValue, body);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("isPresent").GetBoolean());
        Assert.Equal(secretValue.Length, root.GetProperty("length").GetInt32());
        Assert.Equal(16, root.GetProperty("fingerprint").GetString()!.Length);
        Assert.Equal("explicit", root.GetProperty("source").GetString());
    }
}
