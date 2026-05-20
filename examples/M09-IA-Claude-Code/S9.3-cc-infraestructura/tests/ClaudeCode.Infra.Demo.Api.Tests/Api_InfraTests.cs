using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaudeCode.Infra.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_InfraTests
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
    public async Task Requisitos_Multi_Region_Gdpr_Slots_Devuelve_Flags_True()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/infra/requisitos", new
        {
            descripcion = "App Service con slots, multi-region en West Europe + " +
                "North Europe, GDPR, Managed Identity, HTTPS only.",
        });

        var j = await Json(r);
        Assert.True(j.GetProperty("multiRegion").GetBoolean());
        Assert.True(j.GetProperty("complianceEuropa").GetBoolean());
        Assert.True(j.GetProperty("conSlots").GetBoolean());
        Assert.True(j.GetProperty("conHttpsOnly").GetBoolean());
        Assert.True(j.GetProperty("conManagedIdentity").GetBoolean());
    }

    [Fact]
    public async Task Prompt_Dockerfile_Menciona_Multi_Stage()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/infra/prompt/DockerfileMultiStage");
        var texto = (await Json(r)).GetProperty("texto").GetString();
        Assert.Contains("multi-stage", texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Audit_Storage_Con_Acceso_Publico_Es_Critico()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/infra/audit", new
        {
            recursos = new[]
            {
                new
                {
                    nombre = "st-bad",
                    tipo = "Microsoft.Storage/storageAccounts",
                    accesoPublico = true,
                    tieneTags = true,
                },
            },
        });

        var j = await Json(r);
        Assert.False(j.GetProperty("limpio").GetBoolean());
        Assert.True(j.GetProperty("criticos").GetInt32() >= 1);
    }

    [Fact]
    public async Task Plan_Compone_Requisitos_Prompts_Audit_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/infra/plan", new
        {
            descripcion = "App Service con HTTPS only, Managed Identity y Cosmos DB. " +
                "Multi-region UE con GDPR.",
            recursos = new[]
            {
                new
                {
                    nombre = "app-bad",
                    tipo = "Microsoft.Web/sites",
                    httpsOnly = false,
                    tieneManagedIdentity = false,
                    tieneTags = true,
                    tlsVersion = "1.2",
                },
            },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("requisitos").GetProperty("multiRegion").GetBoolean());
        Assert.Equal("BicepDesdeRequirements",
            j.GetProperty("promptBicep").GetProperty("escenario").GetString());
        Assert.Equal("GhActionsPipeline",
            j.GetProperty("promptPipeline").GetProperty("escenario").GetString());
        Assert.False(j.GetProperty("audit").GetProperty("limpio").GetBoolean());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }
}
