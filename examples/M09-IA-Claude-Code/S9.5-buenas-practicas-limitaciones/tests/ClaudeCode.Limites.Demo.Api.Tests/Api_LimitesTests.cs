using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaudeCode.Limites.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_LimitesTests
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
    public async Task Reglas_Devuelve_Las_7_Reglas_De_Oro()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/limites/reglas");
        Assert.Equal(7, (await Json(r)).GetArrayLength());
    }

    [Fact]
    public async Task AntiPatterns_Workflow_Sucio_Devuelve_No_Limpio()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/limites/antipatterns", new
        {
            descripcion = "Le paso la connection string real y Claude mergea directo a main.",
        });

        var j = await Json(r);
        Assert.False(j.GetProperty("limpio").GetBoolean());
        Assert.True(j.GetProperty("hallazgos").GetArrayLength() >= 2);
    }

    [Fact]
    public async Task Estructura_Prompt_Completo_Llega_A_100()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/limites/estructura", new
        {
            prompt = "CONTEXTO: .NET 10. OBJETIVO: crea endpoint. " +
                "Constraints: no añadir deps. INPUT: src/X.cs. " +
                "OUTPUT: archivos en src/Y/. Ejemplo: como en Z.cs. " +
                "Criterio éxito: tests verdes.",
        });

        Assert.Equal(100, (await Json(r)).GetProperty("puntuacion").GetInt32());
    }

    [Fact]
    public async Task Acelera_Boilerplate_Es_Acelera()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/limites/acelera-o-frena/Boilerplate");
        Assert.Equal("Acelera", (await Json(r)).GetProperty("impacto").GetString());
    }

    [Fact]
    public async Task Acelera_SeguridadCritica_Es_Frena()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/limites/acelera-o-frena/SeguridadCritica");
        Assert.Equal("Frena", (await Json(r)).GetProperty("impacto").GetString());
    }

    [Fact]
    public async Task Plan_Compone_AntiPatterns_Estructura_Clasificacion_Reglas_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/limites/plan", new
        {
            descripcionUso = "Itero en chunks, reviso cada línea.",
            promptDelAlumno = "CONTEXTO: .NET 10. OBJETIVO: crea endpoint. " +
                "Constraints: no añadir deps. OUTPUT: archivos. " +
                "Criterio éxito: tests verdes.",
            tipoTarea = "Boilerplate",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("antiPatterns").GetProperty("limpio").GetBoolean());
        Assert.True(j.GetProperty("estructura").GetProperty("puntuacion").GetInt32() >= 60);
        Assert.Equal("Acelera",
            j.GetProperty("clasificacion").GetProperty("impacto").GetString());
        Assert.Equal(7, j.GetProperty("reglasDeOro").GetArrayLength());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 10);
    }
}
