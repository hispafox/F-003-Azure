using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AutoUpdate.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_AutoUpdateTests
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
    public async Task Construir_AppInstaller()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/update/appinstaller", new
        {
            appInstallerUri = "https://x/MiApp.appinstaller",
            version = "1.0.0.0",
            mainPackage = new
            {
                name = "MiEmpresa.App",
                version = "1.0.0.0",
                publisher = "CN=MiEmpresa",
                processorArchitecture = "x64",
                packageUri = "https://x/MiApp_1.0.0.0_x64.msix",
            },
            updateSettings = new { hoursBetweenUpdateChecks = 1 },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var xml = (await Json(r)).GetProperty("xml").GetString();
        Assert.Contains("MiEmpresa.App", xml);
    }

    [Fact]
    public async Task Canary_Misma_Cohorte_Para_El_Mismo_User()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r1 = await f.CreateClient().GetAsync("/update/canary?userId=alice&porcentaje=25");
        var r2 = await f.CreateClient().GetAsync("/update/canary?userId=alice&porcentaje=25");
        var j1 = await Json(r1);
        var j2 = await Json(r2);
        Assert.Equal(j1.GetProperty("hash").GetInt32(), j2.GetProperty("hash").GetInt32());
    }

    [Fact]
    public async Task Siguiente_Etapa_Con_Salud_Ok()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/update/siguiente-etapa?etapaActual=25&saludOk=true");
        Assert.Equal(50, (await Json(r)).GetProperty("siguiente").GetInt32());
    }

    [Fact]
    public async Task Comparar_Disponible_Mayor()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/update/comparar?actual=2.4.1.0&disponible=2.4.2.0");
        Assert.True((await Json(r)).GetProperty("debeActualizar").GetBoolean());
    }

    [Fact]
    public async Task Rollback_Republica_Previa()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/update/rollback", new
        {
            versionMala = "2.4.147.0",
            historial = new[] { "2.4.145.0", "2.4.146.0", "2.4.147.0" },
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("2.4.146.0", j.GetProperty("versionPreviaBuena").GetString());
        Assert.Equal("2.4.148.0", j.GetProperty("etiquetaRollback").GetString());
    }

    [Fact]
    public async Task Rollback_404_Si_No_Hay_Previa()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/update/rollback", new
        {
            versionMala = "2.4.145.0",
            historial = new[] { "2.4.145.0" },
        });
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Plan_Critica_Bloquea_Activacion()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/update/plan", new
        {
            canal = "Stable",
            actualizacionCritica = true,
        });
        var j = await Json(r);
        Assert.Equal("Stable", j.GetProperty("canal").GetString());
        Assert.True(j.GetProperty("updateSettings").GetProperty("updateBlocksActivation").GetBoolean());
    }
}
