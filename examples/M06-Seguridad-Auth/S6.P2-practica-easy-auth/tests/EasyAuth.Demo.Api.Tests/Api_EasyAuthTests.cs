using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using EasyAuth.Demo.Api.EasyAuth;

namespace EasyAuth.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory, SIMULANDO las
// cabeceras X-MS-CLIENT-PRINCIPAL-* que Easy Auth inyecta en Azure
// (slide 4-6). No es integración con Entra (no emulable).
[Trait("Category", "Component")]
public class Api_EasyAuthTests
{
    // AllowAutoRedirect=false para poder afirmar el 302 del sitio web.
    private static HttpClient NoRedirect(WebApplicationFactory<Program> f) =>
        f.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    [Fact]
    public async Task Health_Es_Publico()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Raiz_Sin_Sesion_Redirige_302_Al_Login()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await NoRedirect(f).GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);   // 302
        Assert.Contains("/.auth/login/aad", r.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Raiz_Con_Cabeceras_EasyAuth_Es_200()
    {
        await using var f = new WebApplicationFactory<Program>();
        var c = NoRedirect(f);
        c.DefaultRequestHeaders.Add(EasyAuthHeaders.Nombre, "pedro@empresa.com");
        c.DefaultRequestHeaders.Add(EasyAuthHeaders.Idp, "aad");

        var r = await c.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("pedro@empresa.com", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Auth_Me_Vacio_Sin_Sesion()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/.auth/me");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("[]", await r.Content.ReadAsStringAsync());
    }
}
