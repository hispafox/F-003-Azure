using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MiPrimeraWebApp.Tests;

public class UsuariosEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Post_Usuarios_Returns_201_For_Valid_Email()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/usuarios", new
        {
            nombre = "Pedro",
            email = "pedro@example.com"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("pedro@example.com", doc.RootElement.GetProperty("email").GetString());
        Assert.True(doc.RootElement.TryGetProperty("id", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-arroba")]
    public async Task Post_Usuarios_Returns_400_For_Invalid_Email(string email)
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/usuarios", new
        {
            nombre = "Pedro",
            email
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
