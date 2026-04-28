using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Demo.Api.Tests;

public class HealthDetailsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task HealthDetails_Returns_Json_With_Status_And_Checks()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/details");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("totalDurationMs").GetDouble() >= 0);

        var checks = root.GetProperty("checks");
        Assert.True(checks.GetArrayLength() >= 1);

        var firstCheck = checks[0];
        Assert.Equal("app", firstCheck.GetProperty("name").GetString());
        Assert.Equal("Healthy", firstCheck.GetProperty("status").GetString());
    }
}
