using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AppService.Demo.Api.Tests;

public class CorsConfigurationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Preflight_From_AllowedOrigin_Returns_AllowOriginHeader()
    {
        using var allowingFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AppOptions:AllowedOrigins:0"] = "https://allowed.example.com"
                });
            });
        });

        using var client = allowingFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/");
        request.Headers.Add("Origin", "https://allowed.example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        var allowed = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Origin"));
        Assert.Equal("https://allowed.example.com", allowed);
    }

    [Fact]
    public async Task Preflight_From_DisallowedOrigin_Has_No_AllowOriginHeader()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/");
        request.Headers.Add("Origin", "https://evil.example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
