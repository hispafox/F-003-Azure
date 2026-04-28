using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AppService.Demo.Api.Tests;

public class ConfigEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Config_Endpoint_Redacts_Sensitive_Keys()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/config");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("AppOptions:ApiKey", out var apiKey));
        Assert.Equal("***REDACTED***", apiKey.GetString());

        Assert.True(root.TryGetProperty("AppOptions:ConnectionString", out var connStr));
        Assert.Equal("***REDACTED***", connStr.GetString());

        Assert.True(root.TryGetProperty("AppOptions:Greeting", out var greeting));
        Assert.NotEqual("***REDACTED***", greeting.GetString());
    }

    [Fact]
    public async Task Connection_Endpoint_Returns_Safe_Fields_Only()
    {
        using var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] =
                        "Server=tcp:srv.database.windows.net;Database=demo;User=admin;Password=topsecret;Encrypt=true"
                });
            });
        });

        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/connection");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("topsecret", body);

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("hasConnectionString").GetBoolean());
        Assert.False(root.GetProperty("isKeyVaultReferenceLiteral").GetBoolean());

        var safe = root.GetProperty("safeFields");
        Assert.Equal("tcp:srv.database.windows.net", safe.GetProperty("Server").GetString());
        Assert.Equal("demo", safe.GetProperty("Database").GetString());
    }

    [Fact]
    public async Task Connection_Endpoint_Detects_Unresolved_KeyVault_Reference()
    {
        using var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] =
                        "@Microsoft.KeyVault(VaultName=kv;SecretName=Db)"
                });
            });
        });

        using var client = customFactory.CreateClient();

        var response = await client.GetAsync("/connection");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("isKeyVaultReferenceLiteral").GetBoolean());
    }
}
