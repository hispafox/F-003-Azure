using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Demo.Api.Tests;

public class StaticEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Products_Returns_CacheControl_Public_For_60s()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products?limit=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Cache-Control puede aparecer en Headers o en Content.Headers según el .NET runtime;
        // verificamos ambos para ser robustos.
        var cacheControl = string.Join(",",
            response.Headers.TryGetValues("Cache-Control", out var fromHeaders) ? fromHeaders : []);
        if (string.IsNullOrEmpty(cacheControl))
        {
            cacheControl = string.Join(",",
                response.Content.Headers.TryGetValues("Cache-Control", out var fromContent) ? fromContent : []);
        }

        Assert.Contains("public", cacheControl);
        Assert.Contains("max-age=60", cacheControl);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(3, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Categorias_Returns_CacheControl_Public_For_3600s()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/categorias");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cacheControl = string.Join(",",
            response.Headers.TryGetValues("Cache-Control", out var fromHeaders) ? fromHeaders : []);
        if (string.IsNullOrEmpty(cacheControl))
        {
            cacheControl = string.Join(",",
                response.Content.Headers.TryGetValues("Cache-Control", out var fromContent) ? fromContent : []);
        }

        Assert.Contains("max-age=3600", cacheControl);
    }
}
