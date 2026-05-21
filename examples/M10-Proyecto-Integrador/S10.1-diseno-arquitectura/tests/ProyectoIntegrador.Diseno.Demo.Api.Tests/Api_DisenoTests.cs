using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ProyectoIntegrador.Diseno.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_DisenoTests
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
    public async Task Arquitectura_Devuelve_10_Componentes()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/diseno/arquitectura", new { });
        Assert.Equal(10, (await Json(r)).GetArrayLength());
    }

    [Fact]
    public async Task Porcentaje_Sin_Estado_Es_0()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync(
            "/diseno/arquitectura/porcentaje", new { });
        Assert.Equal(0, (await Json(r)).GetProperty("porcentaje").GetInt32());
    }

    [Fact]
    public async Task Bloque_Siguiente_Sin_Bicep_Devuelve_A()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/diseno/bloque-siguiente", new { });
        Assert.Equal("A_Infraestructura",
            (await Json(r)).GetProperty("bloque").GetString());
    }

    [Fact]
    public async Task Entrega_Todo_Cumplido_Aprueba()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/diseno/entrega", new
        {
            bicepDesplegadoConWhatIf = true,
            apiCrudDevuelve2xx = true,
            jwtValidaConEntra = true,
            datosPersistenEnCosmos = true,
            changeFeedTriggerFunctions = true,
            sinConnectionStringConPassword = true,
            pipelineDesplegaAStaging = true,
            appInsightsTieneTelemetryYAlertas = true,
        });
        var j = await Json(r);
        Assert.Equal(100, j.GetProperty("porcentajeTotal").GetInt32());
        Assert.True(j.GetProperty("aprobada").GetBoolean());
    }

    [Fact]
    public async Task Retos_Devuelve_5_Items()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/diseno/retos");
        Assert.Equal(5, (await Json(r)).GetArrayLength());
    }

    [Fact]
    public async Task Plan_Compone_Arquitectura_Bloque_Entrega_Retos()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/diseno/plan", new
        {
            sistema = new
            {
                bicep = "Desplegado",
                appService = "Desplegado",
            },
            entrega = new
            {
                bicepDesplegadoConWhatIf = true,
                apiCrudDevuelve2xx = true,
            },
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal(10, j.GetProperty("arquitectura").GetArrayLength());
        Assert.Equal(20, j.GetProperty("porcentajeDesplegado").GetInt32());
        Assert.Equal("B_ApiYAuth",
            j.GetProperty("bloqueSiguiente").GetProperty("bloque").GetString());
        Assert.Equal(30,
            j.GetProperty("entrega").GetProperty("porcentajeTotal").GetInt32());
        Assert.Equal(5, j.GetProperty("retos").GetArrayLength());
    }
}
