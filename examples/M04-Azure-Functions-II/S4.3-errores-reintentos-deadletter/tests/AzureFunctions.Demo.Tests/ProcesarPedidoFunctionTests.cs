using System.Text.Json;
using AzureFunctions.Demo.Services;
using Azure.Messaging.ServiceBus;

namespace AzureFunctions.Demo.Tests;

public class ProcesarPedidoFunctionTests
{
    private static ServiceBusReceivedMessage Msg(string body, string messageId = "m1")
        => ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(body), messageId: messageId);

    private static string PedidoJson(string id = "ped-1") => JsonSerializer.Serialize(new
    {
        id,
        clienteId = "c",
        clienteEmail = "a@b.c",
        total = 100m,
    });

    [Fact]
    public async Task Mensaje_Ok_Se_Completa_Y_Registra_Idempotencia()
    {
        var (fn, tracker, idem) = TestHost.NewProcesarPedido();
        var actions = new FakeServiceBusMessageActions();

        var r = await fn.ProcesarAsync(Msg(PedidoJson("ped-1")), actions);

        Assert.Equal("Complete", r.Accion);
        Assert.True(actions.CompleteCalled);
        Assert.Equal(1, tracker.Snapshot().Procesados);
        Assert.True(idem.YaProcesado("ped-1"));
    }

    [Fact]
    public async Task Json_Malformado_Va_A_DeadLetter_Inmediato()
    {
        var (fn, tracker, _) = TestHost.NewProcesarPedido();
        var actions = new FakeServiceBusMessageActions();

        var r = await fn.ProcesarAsync(Msg("{ broken json"), actions);

        Assert.Equal("DeadLetter", r.Accion);
        Assert.True(actions.DeadLetterCalled);
        Assert.Equal("MalformedJson", actions.DeadLetterReason);
        Assert.Equal(1, tracker.Snapshot().EnviadosADeadLetter);
    }

    [Fact]
    public async Task Mensaje_Duplicado_Se_Salta_Por_Idempotencia()
    {
        // Slide 10 — el mismo id procesado dos veces: la 2ª se completa
        // sin re-ejecutar el trabajo (duplicado at-least-once).
        var (fn, tracker, _) = TestHost.NewProcesarPedido();

        await fn.ProcesarAsync(Msg(PedidoJson("ped-dup")), new FakeServiceBusMessageActions());
        var segundo = new FakeServiceBusMessageActions();
        var r = await fn.ProcesarAsync(Msg(PedidoJson("ped-dup")), segundo);

        Assert.Equal("Complete", r.Accion);
        Assert.Equal("Duplicado", r.Detalle);
        Assert.True(segundo.CompleteCalled);
        Assert.Equal(1, tracker.Snapshot().Procesados);          // solo 1 real
        Assert.Equal(1, tracker.Snapshot().DuplicadosSaltados);  // 1 saltado
    }

    [Fact]
    public async Task Error_Transitorio_Hace_Abandon_Para_Reintento()
    {
        var (fn, _, _) = TestHost.NewProcesarPedido(
            new ThrowingApiClient(new ErrorTransitorioException("timeout BD")));
        var actions = new FakeServiceBusMessageActions();

        var r = await fn.ProcesarAsync(Msg(PedidoJson()), actions);

        Assert.Equal("Abandon", r.Accion);
        Assert.True(actions.AbandonCalled);
        Assert.False(actions.DeadLetterCalled);
    }

    [Fact]
    public async Task Error_Permanente_Va_A_DeadLetter()
    {
        var (fn, tracker, _) = TestHost.NewProcesarPedido(
            new ThrowingApiClient(new ErrorPermanenteException("regla negocio violada")));
        var actions = new FakeServiceBusMessageActions();

        var r = await fn.ProcesarAsync(Msg(PedidoJson()), actions);

        Assert.Equal("DeadLetter", r.Accion);
        Assert.True(actions.DeadLetterCalled);
        Assert.Equal(1, tracker.Snapshot().EnviadosADeadLetter);
    }

    [Fact]
    public async Task Error_Desconocido_Hace_Abandon_Para_Investigar()
    {
        var (fn, _, _) = TestHost.NewProcesarPedido(
            new ThrowingApiClient(new Exception("algo nunca visto")));
        var actions = new FakeServiceBusMessageActions();

        var r = await fn.ProcesarAsync(Msg(PedidoJson()), actions);

        Assert.Equal("Abandon", r.Accion);
        Assert.Equal("Desconocido", r.Detalle);
        Assert.True(actions.AbandonCalled);
    }

    [Fact]
    public async Task Pedido_Sin_Id_Va_A_DeadLetter()
    {
        var (fn, _, _) = TestHost.NewProcesarPedido();
        var sinId = JsonSerializer.Serialize(new { id = "", clienteId = "c", clienteEmail = "a@b.c", total = 1m });
        var actions = new FakeServiceBusMessageActions();

        var r = await fn.ProcesarAsync(Msg(sinId), actions);

        Assert.Equal("DeadLetter", r.Accion);
        Assert.Equal("BusinessRule", actions.DeadLetterReason);
    }
}
