using AzureFunctions.Demo.Models;
using Azure.Messaging.ServiceBus;

namespace AzureFunctions.Demo.Tests;

public class ProcesarDeadLetterFunctionTests
{
    // DeadLetterReason / DeadLetterErrorDescription se exponen como
    // propiedades que leen de ApplicationProperties con claves bien
    // conocidas; el ModelFactory no tiene params dedicados, así que las
    // pasamos por el diccionario `properties`.
    private static ServiceBusReceivedMessage DlqMsg(string reason, string desc)
        => ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "dlq-1",
            properties: new Dictionary<string, object>
            {
                ["DeadLetterReason"] = reason,
                ["DeadLetterErrorDescription"] = desc,
            });

    [Fact]
    public async Task Json_Malo_Se_Descarta_Y_Completa()
    {
        var (fn, tracker) = TestHost.NewDeadLetter();
        var actions = new FakeServiceBusMessageActions();

        var accion = await fn.ProcesarAsync(
            DlqMsg("MalformedJson", "JsonException"), actions);

        Assert.Equal(PoisonAction.Discard, accion);
        Assert.True(actions.CompleteCalled); // siempre sale de la DLQ
        Assert.Equal(1, tracker.Snapshot().PoisonProcesados);
    }

    [Fact]
    public async Task MaxDeliveryCount_Va_A_Cuarentena()
    {
        var (fn, _) = TestHost.NewDeadLetter();
        var actions = new FakeServiceBusMessageActions();

        var accion = await fn.ProcesarAsync(
            DlqMsg("MaxDeliveryCountExceeded", ""), actions);

        Assert.Equal(PoisonAction.Quarantine, accion);
        Assert.True(actions.CompleteCalled);
    }

    [Fact]
    public async Task Timeout_Reintenta_Con_Aviso()
    {
        var (fn, _) = TestHost.NewDeadLetter();
        var actions = new FakeServiceBusMessageActions();

        var accion = await fn.ProcesarAsync(
            DlqMsg("ProcessingError", "timeout after 30s"), actions);

        Assert.Equal(PoisonAction.NotifyAndRetry, accion);
        Assert.True(actions.CompleteCalled);
    }

    [Fact]
    public async Task Siempre_Completa_El_Mensaje_De_La_DLQ()
    {
        // Invariante: pase lo que pase, el mensaje sale de la DLQ para
        // que no crezca sin fin.
        var (fn, _) = TestHost.NewDeadLetter();
        var actions = new FakeServiceBusMessageActions();

        await fn.ProcesarAsync(DlqMsg("Cualquiera", "lo que sea"), actions);

        Assert.True(actions.CompleteCalled);
    }
}
