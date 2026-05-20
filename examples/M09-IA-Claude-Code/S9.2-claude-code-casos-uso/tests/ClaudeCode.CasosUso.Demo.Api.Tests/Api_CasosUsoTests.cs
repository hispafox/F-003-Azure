using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaudeCode.CasosUso.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_CasosUsoTests
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
    public async Task Clasificar_Migracion_Devuelve_Slide_2()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/casos/clasificar", new
        {
            descripcion = "Migrar PedidoService de .NET Framework 4.8 a .NET 10",
        });
        var j = await Json(r);
        Assert.Equal("MigracionLegacyANet", j.GetProperty("caso").GetString());
        Assert.Equal("2", j.GetProperty("slide").GetString());
    }

    [Fact]
    public async Task Template_Code_Review_Pide_Output_Json()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/casos/template/CodeReview");
        var texto = (await Json(r)).GetProperty("texto").GetString();
        Assert.Contains("JSON", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Evaluar_Prompt_Vago_Devuelve_Pobre()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/casos/evaluar", new
        {
            prompt = "haz algo",
        });
        Assert.Equal("Pobre",
            (await Json(r)).GetProperty("nivel").GetString());
    }

    [Fact]
    public async Task Plan_Compone_Clasificacion_Template_Evaluacion_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/casos/plan", new
        {
            descripcion = "Optimiza el endpoint, P99 alto",
            promptDelAlumno = "En este proyecto .NET 10, analiza /api/pedidos. " +
                "No rompas los tests. Output: JSON con cambios. " +
                "Criterio éxito: P99 < 500ms.",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("OptimizacionRendimiento",
            j.GetProperty("clasificacion").GetProperty("caso").GetString());
        Assert.Equal("OptimizacionRendimiento",
            j.GetProperty("template").GetProperty("caso").GetString());
        Assert.Equal("Excelente",
            j.GetProperty("evaluacionDelPromptDelAlumno").GetProperty("nivel").GetString());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }
}
