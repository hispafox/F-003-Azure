using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Devops.Repos.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_DevopsTests
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
    public async Task Parsear_Commit_Feat_Con_WorkItem()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/devops/commit/parsear", new
        {
            mensaje = "feat(pedidos): buscar por fecha #1234",
        });

        var j = await Json(r);
        Assert.True(j.GetProperty("valido").GetBoolean());
        Assert.Equal("feat", j.GetProperty("tipo").GetString());
        Assert.Equal("pedidos", j.GetProperty("scope").GetString());
        Assert.Equal(1, j.GetProperty("workItems").GetArrayLength());
    }

    [Fact]
    public async Task Parsear_Commit_Invalido()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/devops/commit/parsear", new
        {
            mensaje = "wip: jugando",
        });
        Assert.False((await Json(r)).GetProperty("valido").GetBoolean());
    }

    [Fact]
    public async Task Tipos_Commit_Incluyen_Feat_Y_Fix()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/devops/commit/tipos");
        var tipos = (await Json(r)).EnumerateArray()
            .Select(x => x.GetString())
            .ToHashSet();
        Assert.Contains("feat", tipos);
        Assert.Contains("fix", tipos);
    }

    [Fact]
    public async Task Branch_Policy_Evaluar_Sin_Build_Reporta_Faltante()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync(
            "/devops/branch-policy/evaluar", new
            {
                configuradas = new[] { "RequiredReviewers" },
            });

        var j = await Json(r);
        Assert.False(j.GetProperty("cumple").GetBoolean());
        var faltantes = j.GetProperty("faltantes").EnumerateArray()
            .Select(x => x.GetString())
            .ToHashSet();
        Assert.Contains("BuildExitoso", faltantes);
    }

    [Fact]
    public async Task Repo_Estrategia_7_Personas_5_Servicios_Es_MultiRepo()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/devops/repo/estrategia", new
        {
            personas = 7,
            servicios = 5,
            ciCdIndependiente = true,
        });
        Assert.Equal("MultiRepo",
            (await Json(r)).GetProperty("estrategia").GetString());
    }

    [Fact]
    public async Task Plan_Compone_Estrategia_Policies_Y_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/devops/plan", new
        {
            personas = 7,
            servicios = 5,
            ciCdIndependiente = true,
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("MultiRepo",
            j.GetProperty("estrategiaRecomendada").GetString());
        Assert.True(j.GetProperty("policiesMinimas").GetArrayLength() >= 4);
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 7);
    }
}
