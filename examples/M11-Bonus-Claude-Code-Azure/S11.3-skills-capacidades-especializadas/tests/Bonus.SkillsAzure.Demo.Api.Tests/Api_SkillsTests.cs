using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bonus.SkillsAzure.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_SkillsTests
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
    public async Task Frontmatter_Bien_Formado_Es_Valido()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/skills/frontmatter", new
        {
            skillMd = "---\nname: deploy\ndescription: \"Deploy a .NET app to Azure App Service\"\n" +
                "allowed-tools: Bash(az *), Read\n---\n\n# Deploy",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("valido").GetBoolean());
        Assert.Equal("deploy", j.GetProperty("frontmatter").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Frontmatter_Sin_Name_Description_Es_Invalido()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/skills/frontmatter", new
        {
            skillMd = "---\nallowed-tools: Read\n---\n\n# Algo",
        });

        var j = await Json(r);
        Assert.False(j.GetProperty("valido").GetBoolean());
    }

    [Fact]
    public async Task Description_Especifica_Es_Fiable()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/skills/description", new
        {
            description = "Deploy a .NET 8 application to Azure App Service with Bicep " +
                "validation, what-if preview, and smoke tests",
        });

        var j = await Json(r);
        Assert.True(j.GetProperty("seActivaraFiable").GetBoolean());
        Assert.True(j.GetProperty("puntuacion").GetInt32() >= 60);
    }

    [Fact]
    public async Task Description_Vaga_No_Es_Fiable()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/skills/description", new
        {
            description = "Helps with deployments and maybe other things",
        });

        var j = await Json(r);
        Assert.False(j.GetProperty("seActivaraFiable").GetBoolean());
    }

    [Fact]
    public async Task AntiPatterns_Credencial_Devuelve_Error()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/skills/antipatterns", new
        {
            skillMd = "---\nname: deploy\ndescription: \"Deploy\"\nallowed-tools: Read\n---\n\n" +
                "Connection string: Server=tcp:sql;Password=secret123;",
        });

        var j = await Json(r);
        Assert.False(j.GetProperty("limpio").GetBoolean());
        var severidades = j.GetProperty("hallazgos").EnumerateArray()
            .Select(h => h.GetProperty("severidad").GetString()!)
            .ToList();
        Assert.Contains("Error", severidades);
    }

    [Fact]
    public async Task Microsoft_Devuelve_8_Skills()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/skills/microsoft");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(8, (await Json(r)).GetArrayLength());
    }

    [Fact]
    public async Task Plan_Con_SkillMd_Compone_Todo()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/skills/plan", new
        {
            skillMd = "---\nname: convenciones-equipo\n" +
                "description: \"Apply our team .NET and Azure coding conventions when reviewing code\"\n" +
                "allowed-tools: Read\n---\n\n# Convenciones\n\n- async/await",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("frontmatter").GetProperty("valido").GetBoolean());
        Assert.True(j.GetProperty("description").GetProperty("seActivaraFiable").GetBoolean());
        Assert.True(j.GetProperty("antiPatrones").GetProperty("limpio").GetBoolean());
        Assert.Equal(8, j.GetProperty("skillsMicrosoft").GetArrayLength());
        Assert.Equal(5, j.GetProperty("skillsRecomendadosEquipo").GetArrayLength());
        Assert.Equal(4, j.GetProperty("roadmap").GetArrayLength());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }

    [Fact]
    public async Task Plan_Sin_SkillMd_Devuelve_Catalogo()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/skills/plan", new { });

        var j = await Json(r);
        Assert.Equal(JsonValueKind.Null, j.GetProperty("frontmatter").ValueKind);
        Assert.Equal(8, j.GetProperty("skillsMicrosoft").GetArrayLength());
        Assert.Equal(4, j.GetProperty("roadmap").GetArrayLength());
    }
}
