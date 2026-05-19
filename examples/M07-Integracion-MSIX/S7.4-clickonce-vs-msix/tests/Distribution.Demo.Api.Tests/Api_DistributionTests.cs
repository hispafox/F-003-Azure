using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Distribution.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory. Sin Azure:
// S7.4 es decisión de distribución desktop, pura.
[Trait("Category", "Component")]
public class Api_DistributionTests
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
    public async Task Soporta_Sandboxing_Solo_En_Msix()
    {
        await using var f = new WebApplicationFactory<Program>();
        var click = await f.CreateClient().GetAsync(
            "/distribution/soporta?formato=ClickOnce&caracteristica=Sandboxing");
        var msix = await f.CreateClient().GetAsync(
            "/distribution/soporta?formato=Msix&caracteristica=Sandboxing");

        Assert.False((await Json(click)).GetProperty("soporta").GetBoolean());
        Assert.True((await Json(msix)).GetProperty("soporta").GetBoolean());
    }

    [Fact]
    public async Task Comparar_Msix_Gana_A_ClickOnce()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/distribution/comparar?a=ClickOnce&b=Msix");

        var j = await Json(r);
        int ganaA = j.GetProperty("ganaA").GetArrayLength();
        int ganaB = j.GetProperty("ganaB").GetArrayLength();
        Assert.True(ganaB > ganaA, $"MSIX debería ganar (B={ganaB}, A={ganaA})");
    }

    [Fact]
    public async Task Escenario_Migracion_C_App_Nueva()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/distribution/escenario?esAppNueva=true");
        Assert.Equal("C_AppNuevaDirectaMsix",
            (await Json(r)).GetProperty("escenario").GetString());
    }

    [Fact]
    public async Task Cert_Distribucion_Interna_Es_Enterprise_Ca()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/distribution/cert?escenario=DistribucionInterna");
        Assert.Equal("EnterpriseCa",
            (await Json(r)).GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task Plan_Compone_Migracion_Escenario_Y_Cert()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/distribution/plan", new
        {
            intunePlaneado = true,
            dotNet8Planeado = true,
            sobreDotNetFramework = true,
            tieneTiempoEquipo = true,
            clickOnceFuncionaBien = false,
            escenarioFirma = "DistribucionInterna",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("migrarRecomendado").GetBoolean());
        Assert.Equal("B_DotNet8MasMsix", j.GetProperty("escenario").GetString());
        Assert.Equal("EnterpriseCa",
            j.GetProperty("certificado").GetProperty("tipo").GetString());
    }
}
