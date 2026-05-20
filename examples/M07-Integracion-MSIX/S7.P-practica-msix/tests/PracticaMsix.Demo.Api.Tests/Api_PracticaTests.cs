using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PracticaMsix.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_PracticaTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Health_Ok()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Listar_8_Pasos()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/practica/pasos");
        Assert.Equal(8, (await Json(r)).GetArrayLength());
    }

    [Fact]
    public async Task Avanzar_OK_De_Crear_A_Personalizar()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/practica/avanzar", new
        {
            pasoActual = "CrearSolucion",
            criteriosOk = new[] { true, true, true },
        });
        var j = await Json(r);
        Assert.True(j.GetProperty("avanza").GetBoolean());
        Assert.Equal("PersonalizarApp", j.GetProperty("siguiente").GetString());
    }

    [Fact]
    public async Task Cert_No_Coincide_Detectado()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/practica/cert-coincide", new
        {
            publisherManifest = "CN=MsixDemoCurso",
            subjectCertificado = "CN=Otro",
        });
        Assert.False((await Json(r)).GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task Manifest_Canonico_Como_Texto_Xml()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/practica/artefactos/manifest?empresa=Empresa&app=MsixDemo&version=1.0.0.0");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("application/xml",
            r.Content.Headers.ContentType?.MediaType);
        var xml = await r.Content.ReadAsStringAsync();
        Assert.Contains("Empresa.MsixDemo", xml);
        Assert.Contains("CN=Empresa", xml);
    }

    [Fact]
    public async Task AppInstaller_Canonico_Como_Texto_Xml()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/practica/artefactos/appinstaller?empresa=Empresa&app=MsixDemo&version=1.0.0.0&baseUri=https://x/msix");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var xml = await r.Content.ReadAsStringAsync();
        Assert.Contains("Empresa.MsixDemo_1.0.0.0_x64.msix", xml);
    }

    [Fact]
    public async Task Plan_Devuelve_Pasos_Cert_Y_Artefactos()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/practica/plan", new
        {
            parametros = new
            {
                empresa = "Empresa",
                app = "MsixDemo",
                version = "1.0.0.0",
                baseUri = "https://x/msix",
            },
            subjectCertificado = "CN=Empresa",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal(8, j.GetProperty("pasos").GetArrayLength());
        Assert.True(j.GetProperty("publisherCertCheck").GetProperty("ok").GetBoolean());
        Assert.Contains("Empresa.MsixDemo",
            j.GetProperty("manifestEjemplo").GetString());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 10);
    }
}
