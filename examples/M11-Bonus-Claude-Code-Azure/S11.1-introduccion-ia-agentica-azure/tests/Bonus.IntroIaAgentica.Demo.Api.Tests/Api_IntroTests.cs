using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bonus.IntroIaAgentica.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_IntroTests
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
    public async Task Generacion_Claude_Code_Devuelve_Gen3_Agente()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/intro/generacion", new
        {
            descripcion = "Uso Claude Code en terminal con MCP de Azure",
        });
        Assert.Equal("Gen3Agente",
            (await Json(r)).GetProperty("generacion").GetString());
    }

    [Fact]
    public async Task Comparativa_Devuelve_12_Filas()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/intro/comparativa");
        Assert.Equal(12, (await Json(r)).GetArrayLength());
    }

    [Fact]
    public async Task Recomendar_Dev_Devuelve_Claude_Code()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/intro/recomendar", new
        {
            editaCodigo = true,
            esDeveloper = true,
        });
        Assert.Equal("ClaudeCode", (await Json(r)).GetProperty("cual").GetString());
    }

    [Fact]
    public async Task Nivel_Sin_Configuracion_Es_Nivel1()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/intro/nivel", new
        {
            usaPromptsConcretos = true,
        });
        Assert.Equal("Nivel1_Ayudante", (await Json(r)).GetProperty("nivel").GetString());
    }

    [Fact]
    public async Task Objetivos_M11_Devuelve_7_Items()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/intro/objetivos");
        Assert.Equal(7, (await Json(r)).GetArrayLength());
    }

    [Fact]
    public async Task Plan_Compone_Clasificacion_Recomendacion_Nivel_Objetivos_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/intro/plan", new
        {
            uso = new { editaCodigo = true, esDeveloper = true },
            equipo = new
            {
                configuraSkills = true,
                configuraMcp = true,
                skillsEnGit = true,
            },
            descripcionHerramientaActual = "Claude Code en terminal",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("Gen3Agente",
            j.GetProperty("clasificacion").GetProperty("generacion").GetString());
        Assert.Equal("ClaudeCode",
            j.GetProperty("recomendacion").GetProperty("cual").GetString());
        Assert.Equal("Nivel2_Colega",
            j.GetProperty("nivel").GetProperty("nivel").GetString());
        Assert.Equal(7, j.GetProperty("objetivosM11").GetArrayLength());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 5);
    }
}
