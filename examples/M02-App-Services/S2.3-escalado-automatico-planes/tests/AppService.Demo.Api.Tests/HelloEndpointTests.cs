using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AppService.Demo.Api.Tests;

public class HelloEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Root_Returns_Greeting_And_InstanceInfo()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("greeting", out var greeting));
        Assert.False(string.IsNullOrWhiteSpace(greeting.GetString()));

        Assert.True(root.TryGetProperty("instanceId", out var instanceId));
        Assert.False(string.IsNullOrWhiteSpace(instanceId.GetString()));

        Assert.True(root.TryGetProperty("machineName", out var machineName));
        Assert.False(string.IsNullOrWhiteSpace(machineName.GetString()));
    }
}
