using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WizardMsix.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_WizardTests
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
    public async Task Expandir_Devuelve_4_Comandos()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/wizard/expandir", new
        {
            empresa = "MiEmpresa",
            app = "MiApp",
            version = "1.0.0.0",
            buildOutputDir = @"C:\bin",
            certPfx = @"C:\cert.pfx",
            outputMsix = @"C:\out\MiApp.msix",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(4, (await Json(r)).GetArrayLength());
    }

    [Fact]
    public async Task Elegir_Aprendizaje_Es_Wizard()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/wizard/elegir", new
        {
            aprendizajeInicial = true,
            appSimpleSingleArch = true,
        });
        Assert.Equal("Wizard",
            (await Json(r)).GetProperty("flujo").GetString());
    }

    [Fact]
    public async Task Elegir_CiCd_Es_Cli()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/wizard/elegir", new
        {
            pipelineCiCd = true,
            certDesdeKeyVault = true,
        });
        Assert.Equal("Cli",
            (await Json(r)).GetProperty("flujo").GetString());
    }

    [Fact]
    public async Task Troubleshoot_Por_Codigo_Conocido()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/wizard/troubleshoot?codigoOMensaje=0x80073CFD");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("0x80073CFD", j.GetProperty("codigo").GetString());
        Assert.Contains("TrustedPeople", j.GetProperty("causa").GetString());
    }

    [Fact]
    public async Task Troubleshoot_Codigo_Desconocido_Es_404()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/wizard/troubleshoot?codigoOMensaje=0xDEADBEEF");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task Limitaciones_Wizard_Lista_No_Vacia()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/wizard/limitaciones");
        Assert.True((await Json(r)).GetArrayLength() >= 3);
    }

    [Fact]
    public async Task Plan_Compone_Flujo_Comandos_Limitaciones_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/wizard/plan", new
        {
            contexto = new { aprendizajeInicial = true, appSimpleSingleArch = true },
            parametros = new
            {
                empresa = "MiEmpresa",
                app = "MiApp",
                version = "1.0.0.0",
                buildOutputDir = @"C:\bin",
                certPfx = @"C:\cert.pfx",
                outputMsix = @"C:\out\MiApp.msix",
            },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("Wizard", j.GetProperty("flujoRecomendado").GetString());
        Assert.Equal(4, j.GetProperty("comandosEquivalentes").GetArrayLength());
        Assert.True(j.GetProperty("limitacionesWizard").GetArrayLength() >= 3);
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 10);
    }
}
