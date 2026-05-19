using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Msix.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory. Sin Azure:
// S7.5 es validación/decisión, pura.
[Trait("Category", "Component")]
public class Api_MsixTests
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
    public async Task Validar_Manifest_Correcto()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/msix/validar", new
        {
            identityName = "MiEmpresa.App",
            publisher = "CN=MiEmpresa",
            version = "1.2.3.4",
            processorArchitecture = "x64",
            targetMinVersion = "10.0.17763.0",
            capabilities = new[] { "internetClient" },
        });

        Assert.True((await Json(r)).GetProperty("valido").GetBoolean());
    }

    [Fact]
    public async Task Validar_Manifest_Con_Problemas()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/msix/validar", new
        {
            identityName = "mal",
            publisher = "MiEmpresa",
            version = "1.2",
            processorArchitecture = "mips",
            targetMinVersion = "10.0.10000.0",
            capabilities = new[] { "runFullTrust" },
        });

        var j = await Json(r);
        Assert.False(j.GetProperty("valido").GetBoolean());
        Assert.True(j.GetProperty("problemas").GetArrayLength() >= 5);
    }

    [Fact]
    public async Task Nombre_Archivo()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/msix/nombre?identityName=MiEmpresa.App&version=2.0.0.0&arch=arm64");
        Assert.Equal("MiEmpresa.App_2.0.0.0_arm64.msix",
            (await Json(r)).GetProperty("archivo").GetString());
    }

    [Fact]
    public async Task Distribucion_Publica_Con_Power_Users()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/msix/distribucion", new
        {
            audienciaPublica = true,
            developerPowerUsers = true,
        });

        var canales = (await Json(r)).GetProperty("canales");
        var lista = canales.EnumerateArray().Select(x => x.GetString()).ToHashSet();
        Assert.Contains("MicrosoftStore", lista);
        Assert.Contains("Winget", lista);
    }

    [Fact]
    public async Task Plan_Compone_Manifest_Y_Distribucion()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/msix/plan", new
        {
            manifest = new
            {
                identityName = "MiEmpresa.App",
                publisher = "CN=MiEmpresa",
                version = "1.0.0.0",
                processorArchitecture = "x64",
                targetMinVersion = "10.0.17763.0",
                capabilities = new[] { "internetClient" },
            },
            distribucion = new { mdmIntune = true, hostingAzureBlob = true },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("manifestValido").GetBoolean());
        Assert.Equal("MiEmpresa.App_1.0.0.0_x64.msix",
            j.GetProperty("nombreArchivo").GetString());
        Assert.True(j.GetProperty("canales").GetArrayLength() >= 1);
    }
}
