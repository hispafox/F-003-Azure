using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaudeCode.Intro.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_ClaudeCodeTests
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
    public async Task Comparativa_Devuelve_Filas()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/cc/comparativa");
        Assert.True((await Json(r)).GetArrayLength() >= 6);
    }

    [Fact]
    public async Task Recomendar_Agente_Mas_Ide_Devuelve_Combinacion()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/cc/recomendar", new
        {
            quieresAutocompletadoEnIde = true,
            necesitasAgenteQueEjecuta = true,
            necesitasMcp = true,
        });
        Assert.Equal("Combinacion",
            (await Json(r)).GetProperty("herramienta").GetString());
    }

    [Fact]
    public async Task Feature_Analisis_Logs_Devuelve_Modo_Pipe()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/cc/feature", new
        {
            tarea = "AnalisisLogs",
        });
        Assert.Equal("Pipe",
            (await Json(r)).GetProperty("modo").GetString());
    }

    [Fact]
    public async Task Feature_Arquitectura_Activa_Extended_Thinking()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/cc/feature", new
        {
            tarea = "Arquitectura",
        });
        Assert.True((await Json(r)).GetProperty("usarExtendedThinking").GetBoolean());
    }

    [Fact]
    public async Task Settings_Con_Infraestructura_Incluye_Bash()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/cc/settings", new
        {
            lenguajePrincipal = "csharp",
            framework = "net10.0",
            tocaInfraestructura = true,
        });

        bool tieneBash = false;
        foreach (var t in (await Json(r)).GetProperty("allowedTools").EnumerateArray())
            if (t.GetString() == "Bash") tieneBash = true;
        Assert.True(tieneBash);
    }

    [Fact]
    public async Task Plan_Compone_Herramienta_Feature_Settings_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/cc/plan", new
        {
            herramienta = new
            {
                quieresAutocompletadoEnIde = true,
                necesitasAgenteQueEjecuta = true,
                necesitasMcp = true,
            },
            equipo = new
            {
                lenguajePrincipal = "csharp",
                framework = "net10.0",
                cursoEnProduccion = true,
                tocaInfraestructura = true,
            },
            tareaConcreta = new { tarea = "GenerarIac", esRecurrente = true },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("Combinacion",
            j.GetProperty("herramienta").GetProperty("herramienta").GetString());
        Assert.True(j.GetProperty("feature").ValueKind == JsonValueKind.Object);
        Assert.True(j.GetProperty("settings").GetProperty("allowedTools").GetArrayLength() >= 5);
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }
}
