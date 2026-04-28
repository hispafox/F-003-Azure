using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HelloWorld.Tests;

public class ApiVersionTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Version_Returns_Assembly_Info()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.False(string.IsNullOrEmpty(root.GetProperty("version").GetString()));
        Assert.Equal("hello-world", root.GetProperty("assembly").GetString());
        Assert.Contains(".NET", root.GetProperty("framework").GetString());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("buildTime").GetString()));
    }
}
