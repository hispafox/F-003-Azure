using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Messaging.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory. Sin broker:
// S7.1 son patrones de decisión puros (no hay round-trip de SDK).
[Trait("Category", "Component")]
public class Api_MessagingTests
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
    public async Task Filtro_Sql_Evalua_Contra_Propiedades()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/messaging/filtro", new
        {
            filtro = "total > 100 AND pais = 'ES'",
            propiedades = new Dictionary<string, object> { ["total"] = 299.99, ["pais"] = "ES" },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.True((await Json(r)).GetProperty("entregado").GetBoolean());
    }

    [Fact]
    public async Task Filtro_Sql_No_Entrega_Si_No_Cumple()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/messaging/filtro", new
        {
            filtro = "total > 100",
            propiedades = new Dictionary<string, object> { ["total"] = 50 },
        });

        Assert.False((await Json(r)).GetProperty("entregado").GetBoolean());
    }

    [Fact]
    public async Task Dedup_Descarta_Duplicado_En_Ventana()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/messaging/dedup", new
        {
            ventanaSegundos = 60,
            mensajes = new[]
            {
                new { messageId = "PED-1", encoladoSegundos = 0.0 },
                new { messageId = "PED-1", encoladoSegundos = 30.0 },
            },
        });

        var j = await Json(r);
        Assert.Equal(1, j.GetProperty("entregados").GetArrayLength());
        Assert.Equal(1, j.GetProperty("descartados").GetArrayLength());
    }

    [Fact]
    public async Task Recomendar_Streaming_Es_EventHubs()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/messaging/recomendar?tipo=Streaming");
        Assert.Equal("EventHubs", (await Json(r)).GetProperty("servicio").GetString());
    }

    [Fact]
    public async Task Dlq_Clasifica_Motivo()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/messaging/dlq?motivo=MaxDeliveryCountExceeded");
        Assert.Contains("reintentos",
            (await Json(r)).GetProperty("accion").GetString());
    }

    [Fact]
    public async Task Plan_Devuelve_Servicio_Y_Valida_Filtros()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/messaging/plan", new
        {
            escenario = new { tipo = "EventoNegocio", fanOutMultiplesSuscriptores = true },
            topic = "pedidos-eventos",
            suscripciones = new[]
            {
                new { nombre = "sub-ok", filtroSql = "total > 100" },
                new { nombre = "sub-rota", filtroSql = "total >>> 100" },
            },
            ventanaDedupSegundos = 86400,
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("ServiceBusTopic", j.GetProperty("servicioRecomendado").GetString());
        var subs = j.GetProperty("suscripciones");
        Assert.True(subs[0].GetProperty("filtroValido").GetBoolean());
        Assert.False(subs[1].GetProperty("filtroValido").GetBoolean());
    }
}
