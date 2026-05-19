using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Apim.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory. Sin Azure:
// S7.3 es la lógica del gateway, pura.
[Trait("Category", "Component")]
public class Api_ApimTests
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
    public async Task Policy_Sin_Key_Es_401()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/apim/policy", new
        {
            contexto = new { ip = "10.0.0.1" },
            config = new { subscriptionRequired = true },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(401, (await Json(r)).GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Policy_Rate_Limit_Es_429()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/apim/policy", new
        {
            contexto = new { subscriptionKey = "k", ip = "1.1.1.1", llamadasEnVentana = 100 },
            config = new { subscriptionRequired = true, rateLimitCalls = 100 },
        });

        var j = await Json(r);
        Assert.Equal(429, j.GetProperty("status").GetInt32());
        Assert.Equal(60, j.GetProperty("retryAfter").GetInt32());
    }

    [Fact]
    public async Task Version_Segment_Resuelve()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/apim/version?esquema=Segment&apiPath=productos&entrada=/v2/productos/9&versiones=v1,v2");
        Assert.Equal("v2", (await Json(r)).GetProperty("version").GetString());
    }

    [Fact]
    public async Task Tier_Vnet_Es_Premium()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/apim/tier",
            new { produccion = true, requiereVNet = true });
        Assert.Equal("Premium", (await Json(r)).GetProperty("tier").GetString());
    }

    [Fact]
    public async Task Plan_Compone_Tier_Caso_Y_Policies()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/apim/plan", new
        {
            escenario = new { produccion = true, requiereVNet = true },
            uso = new { multiplesApis = true, necesitaRateLimitOCache = true, versionadoCentral = true },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("Premium", j.GetProperty("tier").GetString());
        Assert.True(j.GetProperty("apimRecomendado").GetBoolean());
        Assert.Equal("Segment", j.GetProperty("esquemaVersionado").GetString());
        Assert.True(j.GetProperty("checklist").GetArrayLength() > 5);
    }
}
