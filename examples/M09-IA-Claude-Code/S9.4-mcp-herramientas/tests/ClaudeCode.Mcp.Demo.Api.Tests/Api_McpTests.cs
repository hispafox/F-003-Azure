using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaudeCode.Mcp.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_McpTests
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
    public async Task Config_Parsear_Devuelve_Servers()
    {
        await using var f = new WebApplicationFactory<Program>();
        const string cfg = "{\"mcpServers\":{\"filesystem\":{\"command\":\"npx\"," +
            "\"args\":[\"-y\",\"x\",\"/home/dev/repo\"]}}}";
        var r = await f.CreateClient().PostAsJsonAsync("/mcp/config/parsear", new { json = cfg });
        Assert.Equal(1, (await Json(r)).GetProperty("servers").GetArrayLength());
    }

    [Fact]
    public async Task Recomendar_Equipo_Ado_Y_Github_Incluye_Ambos()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/mcp/recomendar", new
        {
            usaAzureDevOps = true,
            usaGitHub = true,
        });

        var nombres = new List<string>();
        foreach (var s in (await Json(r)).EnumerateArray())
            nombres.Add(s.GetProperty("nombre").GetString() ?? "");

        Assert.Contains("filesystem", nombres);
        Assert.Contains("azure-devops", nombres);
        Assert.Contains("github", nombres);
    }

    [Fact]
    public async Task Seguridad_Token_Hardcoded_Devuelve_No_Seguro()
    {
        await using var f = new WebApplicationFactory<Program>();
        const string cfg = "{\"mcpServers\":{\"github\":{\"command\":\"npx\"," +
            "\"args\":[\"-y\",\"x\"]," +
            "\"env\":{\"GITHUB_TOKEN\":\"ghp_abcdefghijklmnopqrstuvwxyz0123456789\"}}}}";
        var r = await f.CreateClient().PostAsJsonAsync("/mcp/seguridad", new { json = cfg });

        var j = await Json(r);
        Assert.False(j.GetProperty("seguro").GetBoolean());
        Assert.True(j.GetProperty("criticos").GetInt32() >= 1);
    }

    [Fact]
    public async Task Plan_Compone_Recomendados_Config_Seguridad_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        const string cfg = "{\"mcpServers\":{\"filesystem\":{\"command\":\"npx\"," +
            "\"args\":[\"-y\",\"x\",\"/home/dev/repo\"]}," +
            "\"github\":{\"command\":\"npx\",\"args\":[\"-y\",\"x\"]," +
            "\"env\":{\"GITHUB_TOKEN\":\"${GH}\"}}}}";

        var r = await f.CreateClient().PostAsJsonAsync("/mcp/plan", new
        {
            escenario = new
            {
                usaGitHub = true,
                usaCosmosDb = true,
            },
            configJson = cfg,
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("serversRecomendados").GetArrayLength() >= 3);
        Assert.Equal(2,
            j.GetProperty("configActual").GetProperty("servers").GetArrayLength());
        Assert.True(j.GetProperty("seguridad").TryGetProperty("seguro", out _));
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 6);
    }
}
