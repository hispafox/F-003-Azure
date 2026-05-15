using System.Text.Json;
using AzureFunctions.Demo.Models;
using Azure.Messaging.ServiceBus;

namespace AzureFunctions.Demo.Tests;

public class ProcesarPedidoFunctionTests
{
    private static ServiceBusReceivedMessage FakeMessage(string body, string messageId = "msg-1")
    {
        // ServiceBusModelFactory permite crear mensajes "como si vinieran del
        // wire" sin necesidad de tocar SB real.
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(body),
            messageId: messageId);
    }

    private static string ValidPedidoJson(string id = "ped-1") => JsonSerializer.Serialize(new
    {
        id,
        clienteId = "cliente-A",
        clienteEmail = "alice@example.com",
        total = 100m,
        notas = (string?)null,
        creadoEn = DateTimeOffset.UtcNow,
    });

    [Fact]
    public async Task Procesar_Mensaje_Valido_Completa_Y_Actualiza_Tracker()
    {
        // Slide 18 — peek-lock: éxito → Complete → mensaje borrado de la cola.
        var (fn, tracker) = TestHost.NewProcesarPedido();
        var msg = FakeMessage(ValidPedidoJson("ped-1"));
        var actions = new FakeServiceBusMessageActions();

        var resultado = await fn.ProcesarAsync(msg, actions);

        Assert.Equal("Complete", resultado.Accion);
        Assert.True(actions.CompleteCalled);
        Assert.False(actions.AbandonCalled);
        Assert.False(actions.DeadLetterCalled);
        Assert.Equal(1, tracker.Snapshot().Procesados);
    }

    [Fact]
    public async Task Procesar_Mensaje_Malformado_Va_A_DeadLetter()
    {
        // Slide 18 — JSON inválido es un error PERMANENTE: si reintentamos,
        // el resultado no cambia. Lo mandamos al DLQ con motivo y descripción.
        var (fn, tracker) = TestHost.NewProcesarPedido();
        var msg = FakeMessage("{ broken");
        var actions = new FakeServiceBusMessageActions();

        var resultado = await fn.ProcesarAsync(msg, actions);

        Assert.Equal("DeadLetter", resultado.Accion);
        Assert.True(actions.DeadLetterCalled);
        Assert.Equal("MalformedJson", actions.DeadLetterReason);
        Assert.False(actions.CompleteCalled);
        Assert.Equal(0, tracker.Snapshot().Procesados);
        Assert.Equal(1, tracker.Snapshot().Abandonados);
    }

    [Fact]
    public async Task Procesar_Pedido_Sin_Id_Va_A_DeadLetter()
    {
        // Defensive: aunque deserialice, si falta el id no podemos procesar.
        var (fn, _) = TestHost.NewProcesarPedido();
        var sinId = JsonSerializer.Serialize(new
        {
            id = "",
            clienteId = "c",
            clienteEmail = "a@b.c",
            total = 1m,
        });
        var msg = FakeMessage(sinId);
        var actions = new FakeServiceBusMessageActions();

        var resultado = await fn.ProcesarAsync(msg, actions);

        Assert.Equal("DeadLetter", resultado.Accion);
        Assert.Equal("EmptyPedido", actions.DeadLetterReason);
    }
}
