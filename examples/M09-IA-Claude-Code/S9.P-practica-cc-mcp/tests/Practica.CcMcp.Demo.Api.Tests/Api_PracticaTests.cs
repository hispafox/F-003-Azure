using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Practica.CcMcp.Demo.Api.Tests;

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
    public async Task Preflight_Sin_Node_Bloquea()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/practica/preflight", new
        {
            tieneRepoLocal = true,
        });
        Assert.False((await Json(r)).GetProperty("listoParaArrancar").GetBoolean());
    }

    [Fact]
    public async Task Ejercicio_Compila_Y_Tests_Devuelve_Pasa()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/practica/ejercicio", new
        {
            ejercicio = "GenerarServicioCompleto",
            compilaOLintOk = true,
            testsOValidatePasa = true,
            outputAplicaConvenciones = true,
        });
        Assert.Equal("Pasa", (await Json(r)).GetProperty("resultado").GetString());
    }

    [Fact]
    public async Task Comparativa_Devuelve_Delta_Positivo()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/practica/comparativa", new
        {
            vago = "crea algo",
            medio = "Crea un servicio en .NET 10",
            detallado = "CONTEXTO: .NET 10. Mantén convenciones. " +
                "Output: archivos. Criterio éxito: tests verdes.",
        });
        Assert.True((await Json(r)).GetProperty("deltaVagoADetallado").GetInt32() > 0);
    }

    [Fact]
    public async Task Plan_Compone_Preflight_Ejercicios_Comparativa_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/practica/plan", new
        {
            preflight = new
            {
                tieneNode18OSuperior = true,
                claudeInstaladoYAutenticado = true,
                tieneApiKey = true,
                tieneAzCli = true,
                tieneRepoLocal = true,
                claudeMdConfigurado = true,
            },
            evidencias = new[]
            {
                new
                {
                    ejercicio = "GenerarServicioCompleto",
                    compilaOLintOk = true,
                    testsOValidatePasa = true,
                    outputAplicaConvenciones = true,
                },
            },
            promptVago = "x",
            promptMedio = "Crea un servicio en .NET 10",
            promptDetallado = "CONTEXTO: .NET 10. Mantén. Output: archivos. " +
                "Criterio éxito: tests verdes.",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("preflight").GetProperty("listoParaArrancar").GetBoolean());
        Assert.Equal(1, j.GetProperty("ejercicios").GetArrayLength());
        Assert.True(j.GetProperty("comparativa").TryGetProperty("deltaVagoADetallado", out _));
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }
}
