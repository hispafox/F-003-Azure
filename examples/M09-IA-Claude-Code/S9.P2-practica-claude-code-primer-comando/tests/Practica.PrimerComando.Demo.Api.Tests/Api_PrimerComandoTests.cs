using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Practica.PrimerComando.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_PrimerComandoTests
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
    public async Task Preflight_Sin_Auth_Bloquea()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/primercomando/preflight", new
        {
            tieneNode18OSuperior = true,
            tieneCuentaAnthropic = true,
            auth = "Ninguno",
            tieneRepoPracticar = true,
        });
        Assert.False((await Json(r)).GetProperty("listoParaArrancar").GetBoolean());
    }

    [Fact]
    public async Task Paso_Compila_Y_Output_Es_Pasa()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/primercomando/paso", new
        {
            paso = "InstalarCli",
            comandoEjecutado = true,
            outputEsperadoVisible = true,
        });
        Assert.Equal("Pasa", (await Json(r)).GetProperty("resultado").GetString());
    }

    [Fact]
    public async Task Slash_Commands_Devuelve_8_Items()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/primercomando/slash-commands");
        Assert.Equal(8, (await Json(r)).GetArrayLength());
    }

    [Fact]
    public async Task Prompt_Mejora_Codigo_Es_Anti_Pattern()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/primercomando/prompt", new
        {
            prompt = "Mejora el código",
        });
        Assert.True((await Json(r)).GetProperty("tieneAntiPatterns").GetBoolean());
    }

    [Fact]
    public async Task Prompt_Confirmacion_Previa_No_Tiene_Anti_Patterns()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/primercomando/prompt", new
        {
            prompt = "Antes de implementar, dime cómo lo harías",
        });
        Assert.False((await Json(r)).GetProperty("tieneAntiPatterns").GetBoolean());
    }

    [Fact]
    public async Task Plan_Compone_Preflight_Pasos_Prompt_Slash_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/primercomando/plan", new
        {
            preflight = new
            {
                tieneNode18OSuperior = true,
                tieneCuentaAnthropic = true,
                auth = "ClaudeAi",
                tieneTerminalModerna = true,
                tieneGit = true,
                tieneRepoPracticar = true,
            },
            evidencias = new[]
            {
                new
                {
                    paso = "InstalarCli",
                    comandoEjecutado = true,
                    outputEsperadoVisible = true,
                },
            },
            promptDelAlumno = "Antes de implementar, dime cómo lo harías",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("preflight").GetProperty("listoParaArrancar").GetBoolean());
        Assert.Equal(1, j.GetProperty("pasos").GetArrayLength());
        Assert.False(j.GetProperty("analisisDelPromptDelAlumno")
            .GetProperty("tieneAntiPatterns").GetBoolean());
        Assert.Equal(8, j.GetProperty("slashCommandsEsenciales").GetArrayLength());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 10);
    }
}
