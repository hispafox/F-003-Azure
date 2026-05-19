using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Practica.Demo.Api.Practica;

namespace Practica.Demo.Api.Tests;

// CAPA E2E — la API completa vía WebApplicationFactory, SIMULANDO las
// cabeceras X-MS-CLIENT-PRINCIPAL-* que Easy Auth inyecta en Azure
// (slide 9/11). No es integración con Entra (no emulable): es la
// verificación funcional del comportamiento 401 vs 200.
[Trait("Category", "Component")]
public class Api_EasyAuthTests
{
    [Fact]
    public async Task Health_Es_Publico()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Perfil_Sin_Cabeceras_Es_401()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/api/perfil");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Perfil_Con_Cabeceras_EasyAuth_Es_200()
    {
        await using var f = new WebApplicationFactory<Program>();
        var client = f.CreateClient();
        client.DefaultRequestHeaders.Add(
            EasyAuthPrincipal.HeaderNombre, "pedro@empresa.com");
        client.DefaultRequestHeaders.Add(EasyAuthPrincipal.HeaderIdp, "aad");

        var r = await client.GetAsync("/api/perfil");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("pedro@empresa.com", await r.Content.ReadAsStringAsync());
    }
}
