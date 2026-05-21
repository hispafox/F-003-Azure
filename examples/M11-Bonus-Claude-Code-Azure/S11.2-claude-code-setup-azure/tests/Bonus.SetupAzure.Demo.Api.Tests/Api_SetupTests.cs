using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bonus.SetupAzure.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_SetupTests
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
    public async Task Estructura_Equipo_Completo_Incluye_Agents_Skills_Y_Mcp()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/setup/estructura", new
        {
            tieneAgentsCustom = true,
            tieneSkillsPropios = true,
            usaMcpServers = true,
            quiereHooks = true,
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        var rutas = j.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("ruta").GetString()!)
            .ToList();
        Assert.Contains("CLAUDE.md", rutas);
        Assert.Contains(".claude/settings.json", rutas);
        Assert.Contains(".claude/agents/", rutas);
        Assert.Contains(".claude/skills/", rutas);
        Assert.Contains(".mcp.json", rutas);
        Assert.Contains(".claude/hooks/", rutas);
    }

    [Fact]
    public async Task Settings_Allow_Amplio_Y_Sin_Deny_Es_Inseguro()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/setup/settings", new
        {
            allow = new[] { "Bash(*)", "Write(**)" },
            deny = Array.Empty<string>(),
            model = "claude-sonnet-4-6",
        });

        var j = await Json(r);
        Assert.False(j.GetProperty("seguro").GetBoolean());
        var niveles = j.GetProperty("hallazgos").EnumerateArray()
            .Select(h => h.GetProperty("nivel").GetString()!)
            .ToList();
        Assert.Contains("Critico", niveles);
        Assert.Contains("Alto", niveles);
    }

    [Fact]
    public async Task Settings_Bien_Configurados_Es_Seguro()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/setup/settings", new
        {
            allow = new[] { "Bash(dotnet *)", "Read(**)" },
            deny = new[]
            {
                "Bash(rm -rf *)",
                "Bash(az group delete *)",
                "Bash(az resource delete *)",
                "Bash(drop database *)",
                "Read(**/*.env)",
                "Read(**/*.pfx)",
                "Read(**/*.key)",
                "Read(**/local.settings.json)",
            },
            model = "claude-sonnet-4-6",
        });

        var j = await Json(r);
        Assert.True(j.GetProperty("seguro").GetBoolean());
    }

    [Fact]
    public async Task ClaudeMd_Vago_Tiene_Puntuacion_Baja()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/setup/claudemd", new
        {
            contenido = "# Mi proyecto\n\nUna app.",
        });

        var j = await Json(r);
        Assert.True(j.GetProperty("puntuacion").GetInt32() < 30);
    }

    [Fact]
    public async Task ClaudeMd_Con_Secreto_Genera_Aviso_AntiPatron()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/setup/claudemd", new
        {
            contenido = "# Proyecto\n## Stack\n.NET 8.\n## Convenciones\nasync.\n" +
                "Connection string: Server=tcp:sql;Password=secret;",
        });

        var j = await Json(r);
        Assert.True(j.GetProperty("avisosDeAntiPatrones").GetArrayLength() > 0);
    }

    [Fact]
    public async Task AzureSkills_Devuelve_20_Items()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/setup/azure-skills");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal(20, j.GetArrayLength());
    }

    [Fact]
    public async Task Plan_Compone_Estructura_Settings_ClaudeMd_Skills_Y_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/setup/plan", new
        {
            equipo = new
            {
                tieneAgentsCustom = true,
                tieneSkillsPropios = true,
                quiereHooks = true,
                usaMcpServers = true,
            },
            settings = new
            {
                allow = new[] { "Bash(dotnet *)", "Read(**)" },
                deny = new[]
                {
                    "Bash(rm -rf *)",
                    "Bash(az group delete *)",
                    "Bash(az resource delete *)",
                    "Bash(drop database *)",
                    "Read(**/*.env)",
                    "Read(**/*.pfx)",
                    "Read(**/*.key)",
                    "Read(**/local.settings.json)",
                },
                model = "claude-sonnet-4-6",
            },
            claudeMdContenido =
                "# Proyecto\n## Stack\n.NET 8.\n## Convenciones\nasync/await.\n" +
                "## Comandos\ndotnet build\n## No tocar sin preguntar\nrbac.bicep",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("estructura").GetProperty("items").GetArrayLength() >= 6);
        Assert.True(j.GetProperty("settings").GetProperty("seguro").GetBoolean());
        Assert.True(j.GetProperty("claudeMd").GetProperty("puntuacion").GetInt32() >= 70);
        Assert.Equal(20, j.GetProperty("azureSkillsDisponibles").GetArrayLength());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }
}
