using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Migration.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_MigrationTests
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
    public async Task Mapear_Identity_Y_Publisher()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/migracion/mapear", new
        {
            assemblyName = "VentasDesktop",
            publisher = "Mi Empresa, S.L.",
            version = "2.4.1",
        });

        var j = await Json(r);
        Assert.Equal("MiEmpresaSL.VentasDesktop", j.GetProperty("identityName").GetString());
        Assert.Equal("2.4.1.0", j.GetProperty("version").GetString());
        Assert.StartsWith("CN=", j.GetProperty("publisher").GetString());
    }

    [Fact]
    public async Task Compatibilidad_Bloqueador_Para_Kernel_Driver()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/migracion/compatibilidad", new
        {
            comportamientos = new[] { "Wpf", "KernelDriver" },
        });
        Assert.Equal("Bloqueador",
            (await Json(r)).GetProperty("riesgo").GetString());
    }

    [Fact]
    public async Task Fase_Empaquetado_Tiene_Criterios()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/migracion/fase?fase=Empaquetado");
        var j = await Json(r);
        Assert.Equal("Empaquetado", j.GetProperty("fase").GetString());
        Assert.True(j.GetProperty("criteriosSalida").GetArrayLength() >= 4);
    }

    [Fact]
    public async Task Siguiente_Fase_Avanza_Si_Todos_Ok()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/migracion/siguiente-fase", new
        {
            faseActual = "Empaquetado",
            criteriosOk = new[] { true, true, true, true, true },
        });
        var j = await Json(r);
        Assert.True(j.GetProperty("avanza").GetBoolean());
        Assert.Equal("Piloto", j.GetProperty("siguiente").GetString());
    }

    [Fact]
    public async Task Plan_Compone_Mapeo_Compatibilidad_Y_Fase()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/migracion/plan", new
        {
            clickOnce = new { assemblyName = "App", publisher = "MiEmpresa", version = "1.0" },
            comportamientos = new[] { "Wpf", "EscribeHKLM" },
            faseActual = "Piloto",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("MiEmpresa.App",
            j.GetProperty("manifest").GetProperty("identityName").GetString());
        Assert.Equal("Precaucion",
            j.GetProperty("compatibilidad").GetProperty("riesgo").GetString());
        Assert.Equal("Piloto",
            j.GetProperty("fase").GetProperty("fase").GetString());
    }
}
