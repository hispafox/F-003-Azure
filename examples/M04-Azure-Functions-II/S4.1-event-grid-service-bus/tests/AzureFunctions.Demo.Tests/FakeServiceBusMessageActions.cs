using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Tests;

// Fake derivado de ServiceBusMessageActions (clase abstracta del binding)
// para poder verificar en tests qué acciones se llamaron sobre el mensaje.
// Los tests aserrtan sobre las propiedades públicas en lugar de pelearse
// con Moq/NSubstitute sobre tipos sealed del SDK.
internal sealed class FakeServiceBusMessageActions : ServiceBusMessageActions
{
    public bool CompleteCalled { get; private set; }
    public bool AbandonCalled { get; private set; }
    public bool DeadLetterCalled { get; private set; }
    public string? DeadLetterReason { get; private set; }
    public string? DeadLetterDescription { get; private set; }

    public override Task CompleteMessageAsync(
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken = default)
    {
        CompleteCalled = true;
        return Task.CompletedTask;
    }

    public override Task AbandonMessageAsync(
        ServiceBusReceivedMessage message,
        IDictionary<string, object>? propertiesToModify = null,
        CancellationToken cancellationToken = default)
    {
        AbandonCalled = true;
        return Task.CompletedTask;
    }

    public override Task DeadLetterMessageAsync(
        ServiceBusReceivedMessage message,
        Dictionary<string, object>? propertiesToModify,
        string? deadLetterReason,
        string? deadLetterErrorDescription = null,
        CancellationToken cancellationToken = default)
    {
        DeadLetterCalled = true;
        DeadLetterReason = deadLetterReason;
        DeadLetterDescription = deadLetterErrorDescription;
        return Task.CompletedTask;
    }
}
