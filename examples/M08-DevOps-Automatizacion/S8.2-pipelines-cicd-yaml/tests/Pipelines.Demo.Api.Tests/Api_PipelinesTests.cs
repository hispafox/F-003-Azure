using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Pipelines.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_PipelinesTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    private const string YamlSimple = """
        trigger: { branches: { include: [main] } }
        pool: { vmImage: 'ubuntu-latest' }
        stages:
        - stage: Build
          jobs:
          - job: B
            steps:
            - script: dotnet build
            - script: dotnet test
        """;

    [Fact]
    public async Task Health_Ok()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Fact]
    public async Task Parsear_Devuelve_Estructura()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync(
            "/pipeline/parsear", new { yaml = YamlSimple });
        var j = await Json(r);
        Assert.Equal(1, j.GetProperty("stages").GetArrayLength());
        Assert.Equal("ubuntu-latest", j.GetProperty("poolVmImage").GetString());
    }

    [Fact]
    public async Task Validar_Pipeline_OK_Es_Valido()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync(
            "/pipeline/validar", new { yaml = YamlSimple });
        Assert.True((await Json(r)).GetProperty("valido").GetBoolean());
    }

    [Fact]
    public async Task Validar_Detecta_DependsOn_Roto()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/pipeline/validar", new
        {
            yaml = """
                stages:
                - stage: A
                  dependsOn: NoExiste
                  jobs:
                  - job: J
                    steps: [{ script: echo }]
                """,
        });
        Assert.False((await Json(r)).GetProperty("valido").GetBoolean());
    }

    [Fact]
    public async Task Trigger_Estandar_Incluye_3_Recomendaciones()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/pipeline/trigger/estandar");
        Assert.Equal(3, (await Json(r)).GetArrayLength());
    }

    [Fact]
    public async Task Trigger_Recomendado_Manual_Es_None()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/pipeline/trigger/recomendado?escenario=ManualOnly");
        Assert.Contains("trigger: none",
            (await Json(r)).GetProperty("yaml").GetString());
    }

    [Fact]
    public async Task Plan_Compone_Estructura_Validacion_Triggers_Y_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync(
            "/pipeline/plan", new { yaml = YamlSimple });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal(1,
            j.GetProperty("estructura").GetProperty("stages").GetArrayLength());
        Assert.True(j.GetProperty("validacion").GetProperty("valido").GetBoolean());
        Assert.Equal(3, j.GetProperty("triggersEstandar").GetArrayLength());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }
}
