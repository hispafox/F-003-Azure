using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Deploy.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory.
[Trait("Category", "Component")]
public class Api_DeployTests
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
    public async Task Estrategia_AppService_Con_Slots_Es_SlotSwap()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/deploy/estrategia?tipoApp=AppService&tieneSlots=true");
        Assert.Equal("SlotSwap",
            (await Json(r)).GetProperty("estrategia").GetString());
    }

    [Fact]
    public async Task Estrategia_Msix_Es_AppInstaller()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/deploy/estrategia?tipoApp=Msix");
        Assert.Equal("AppInstaller",
            (await Json(r)).GetProperty("estrategia").GetString());
    }

    [Fact]
    public async Task HealthCheck_Pasa_Al_Segundo_Intento()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/deploy/healthcheck", new
        {
            statusEsperado = 200,
            maxIntentos = 5,
            intentos = new[]
            {
                new { intento = 1, statusObservado = 503 },
                new { intento = 2, statusObservado = 200 },
            },
        });
        var j = await Json(r);
        Assert.True(j.GetProperty("pasa").GetBoolean());
        Assert.Equal(2, j.GetProperty("intentosUsados").GetInt32());
    }

    [Fact]
    public async Task Smoke_Falla_Si_Un_Endpoint_Cae()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/deploy/smoke", new
        {
            requests = new[]
            {
                new { endpoint = "/a", statusObservado = 200 },
                new { endpoint = "/b", statusObservado = 500 },
            },
        });
        var j = await Json(r);
        Assert.False(j.GetProperty("pasa").GetBoolean());
        Assert.Equal(1, j.GetProperty("endpointsFallidos").GetArrayLength());
    }

    [Fact]
    public async Task Rollback_AppService_Slots_Es_Swap()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/deploy/rollback?tipoApp=AppService&tieneSlots=true");
        Assert.Contains("Swap",
            (await Json(r)).GetProperty("metodo").GetString());
    }

    [Fact]
    public async Task Rollback_Feature_Flag_Lista_El_Flag()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/deploy/rollback/feature-flag?flag=FEATURE_X");
        var j = await Json(r);
        Assert.Contains("FEATURE_X",
            j.GetProperty("pasos")[0].GetString());
    }

    [Fact]
    public async Task Plan_Compone_Estrategia_Rollback_Y_Checklist()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/deploy/plan", new
        {
            tipoApp = "AppService",
            tieneSlots = true,
            critico = true,
        });
        var j = await Json(r);
        Assert.Equal("SlotSwap",
            j.GetProperty("estrategia").GetProperty("estrategia").GetString());
        Assert.True(j.GetProperty("checklist").GetArrayLength() >= 8);
    }
}
