using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Practica.Pipeline.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_PipelineTests
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
    public async Task Preflight_Sin_Slot_Bloquea_La_Practica()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/pipeline/preflight", new
        {
            tieneOrgADO = true,
            tieneRepoConPushAccess = true,
            tieneSuscripcionAzure = true,
            esAdminProyectoADO = true,
            esOwnerOUserAccessAdmin = true,
            planS1OSuperior = true,
            slotStagingExiste = false,
        });

        var j = await Json(r);
        Assert.False(j.GetProperty("listoParaArrancar").GetBoolean());
    }

    [Fact]
    public async Task Etapas_Ado_Por_Defecto_Devuelve_Build_DeployStaging_SwapProduction()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/pipeline/etapas", new
        {
            plataforma = "AzureDevOps",
            usarOidc = true,
            aprobacionEnProduccion = true,
            autoRollbackEnFallo = true,
        });

        var nombres = (await Json(r)).GetProperty("etapas")
            .EnumerateArray()
            .Select(e => e.GetProperty("nombre").GetString())
            .ToArray();
        Assert.Contains("Build", nombres);
        Assert.Contains("DeployStaging", nombres);
        Assert.Contains("SwapProduction", nombres);
    }

    [Fact]
    public async Task Smoke_Http_200_Latencia_Baja_Devuelve_Continuar()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/pipeline/smoke", new
        {
            medidas = new
            {
                httpCode = 200,
                latenciaMediaSegundos = 0.4,
                errorRatePorcentaje = 0.1,
            },
        });
        Assert.Equal("Continuar",
            (await Json(r)).GetProperty("decision").GetString());
    }

    [Fact]
    public async Task Smoke_Http_503_Devuelve_RollbackNecesario()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/pipeline/smoke", new
        {
            medidas = new
            {
                httpCode = 503,
                latenciaMediaSegundos = 0.4,
                errorRatePorcentaje = 0.0,
            },
        });
        Assert.Equal("RollbackNecesario",
            (await Json(r)).GetProperty("decision").GetString());
    }

    [Fact]
    public async Task Plan_Compone_Preflight_Pipeline_Smoke_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/pipeline/plan", new
        {
            preflight = new
            {
                tieneOrgADO = true, tieneRepoConPushAccess = true,
                tieneSuscripcionAzure = true, esAdminProyectoADO = true,
                esOwnerOUserAccessAdmin = true, planS1OSuperior = true,
                slotStagingExiste = true, tieneServiceConnectionOidc = true,
                tieneAppRegistration = true, tieneAzCliInstalado = true,
            },
            opciones = new
            {
                plataforma = "AzureDevOps",
                usarOidc = true,
                aprobacionEnProduccion = true,
                autoRollbackEnFallo = true,
            },
            simulacionSmoke = new
            {
                httpCode = 200, latenciaMediaSegundos = 0.5, errorRatePorcentaje = 0.2,
            },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("preflight").GetProperty("listoParaArrancar").GetBoolean());
        Assert.True(j.GetProperty("pipeline").GetProperty("etapas").GetArrayLength() >= 3);
        Assert.Equal("Continuar",
            j.GetProperty("smokeTest").GetProperty("decision").GetString());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 10);
    }
}
