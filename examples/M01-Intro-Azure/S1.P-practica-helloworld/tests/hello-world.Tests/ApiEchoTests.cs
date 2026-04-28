using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HelloWorld.Tests;

public class ApiEchoTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Echo_With_Msg_Returns_200_With_Eco()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/echo?msg=hola");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal("hola", root.GetProperty("recibido").GetString());
        Assert.Equal(4, root.GetProperty("longitud").GetInt32());
        Assert.Equal("Has dicho: hola", root.GetProperty("eco").GetString());
    }

    [Theory]
    [InlineData("/api/echo")]
    [InlineData("/api/echo?msg=")]
    [InlineData("/api/echo?msg=%20")]
    public async Task Echo_Returns_400_When_Msg_Missing_Or_Empty(string url)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
