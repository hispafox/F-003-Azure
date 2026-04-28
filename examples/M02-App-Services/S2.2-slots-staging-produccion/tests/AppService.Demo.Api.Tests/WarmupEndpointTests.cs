using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Demo.Api.Tests;

public class WarmupEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Warmup_Returns_200_With_Check_List_When_All_Dependencies_Are_Ready()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/warmup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("warm", root.GetProperty("status").GetString());

        var checks = root.GetProperty("checks");
        Assert.True(checks.GetArrayLength() >= 3);

        foreach (var check in checks.EnumerateArray())
        {
            Assert.True(check.GetProperty("ok").GetBoolean(),
                $"check '{check.GetProperty("name").GetString()}' should be ok");
        }
    }
}
