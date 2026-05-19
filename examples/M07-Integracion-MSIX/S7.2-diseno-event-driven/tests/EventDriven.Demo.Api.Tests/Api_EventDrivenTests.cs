using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventDriven.Demo.Api.Tests;

// CAPA E2E — la app completa vía WebApplicationFactory. Sin Azure:
// S7.2 son patrones de diseño puros.
[Trait("Category", "Component")]
public class Api_EventDrivenTests
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
    public async Task Patron_Audit_Trail_Es_Event_Sourcing()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync(
            "/eventdriven/patron?auditTrailCompleto=true");
        Assert.Equal("EventSourcing",
            (await Json(r)).GetProperty("patron").GetString());
    }

    [Fact]
    public async Task Saga_6_Pasos_Es_Orchestration()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().GetAsync("/eventdriven/saga?pasos=6");
        Assert.Equal("Orchestration",
            (await Json(r)).GetProperty("estilo").GetString());
    }

    [Fact]
    public async Task Validar_Detecta_Anti_Patterns()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/eventdriven/validar",
            new { tipo = "EnviarEmail", campos = new[] { "password" } });

        var j = await Json(r);
        Assert.False(j.GetProperty("valido").GetBoolean());
        Assert.True(j.GetProperty("problemas").GetArrayLength() >= 2);
    }

    [Fact]
    public async Task Sourcing_Replay_Con_Snapshot()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/eventdriven/sourcing", new
        {
            snapshotCada = 3,
            eventos = new object[]
            {
                new { tipo = "PedidoCreado", clienteId = "CLI-001" },
                new { tipo = "ItemAnadido", nombre = "Mouse", precio = 29.99, cantidad = 2 },
                new { tipo = "DescuentoAplicado", codigo = "V10", importe = 10 },
                new { tipo = "PagoConfirmado", transaccion = "TXN-1" },
                new { tipo = "PedidoEnviado", tracking = "ES1" },
            },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.Equal("Enviado", j.GetProperty("estado").GetProperty("estado").GetString());
        Assert.Equal(1, j.GetProperty("snapshotsTomados").GetInt32());     // snapshot en v3
        Assert.Equal(2, j.GetProperty("ultimoReplayCount").GetInt32());    // v3 + 2 eventos
    }

    [Fact]
    public async Task Plan_Compone_Decision_Patron_Y_Validacion()
    {
        await using var f = new WebApplicationFactory<Program>();
        var r = await f.CreateClient().PostAsJsonAsync("/eventdriven/plan", new
        {
            escenario = new { multiplesConsumidores = true, procesamientoPesado = true, auditTrailCompleto = true, pasosSaga = 6 },
            catalogo = new[]
            {
                new { tipo = "PedidoCreado", campos = new[] { "pedidoId", "version" } },
                new { tipo = "CobrarTarjeta", campos = new[] { "tarjeta", "cvv" } },
            },
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await Json(r);
        Assert.True(j.GetProperty("eventDrivenRecomendado").GetBoolean());
        Assert.Equal("EventSourcing", j.GetProperty("patronEvento").GetString());
        Assert.Equal("Orchestration", j.GetProperty("estiloSaga").GetString());
        Assert.Equal(1, j.GetProperty("eventosInvalidos").GetArrayLength());
    }
}
