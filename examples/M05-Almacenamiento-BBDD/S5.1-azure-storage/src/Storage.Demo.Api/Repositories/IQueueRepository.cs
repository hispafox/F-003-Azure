using Azure.Storage.Queues;

namespace Storage.Demo.Api.Repositories;

// Slides 18-19 — Queue Storage: el "hermano simple" de Service Bus.
// send / receive (peek-lock implícito con visibility timeout) / delete.
public interface IQueueRepository
{
    Task EncolarAsync(string cola, string cuerpo);
    Task<(string mensaje, string popReceipt, string messageId)?> RecibirAsync(string cola);
    Task BorrarAsync(string cola, string messageId, string popReceipt);
    Task<int> LongitudAproxAsync(string cola);
}

public sealed class QueueRepository(QueueServiceClient client) : IQueueRepository
{
    private async Task<QueueClient> ColaAsync(string cola)
    {
        var q = client.GetQueueClient(cola);
        await q.CreateIfNotExistsAsync();
        return q;
    }

    public async Task EncolarAsync(string cola, string cuerpo)
    {
        var q = await ColaAsync(cola);
        await q.SendMessageAsync(cuerpo);
    }

    public async Task<(string mensaje, string popReceipt, string messageId)?> RecibirAsync(
        string cola)
    {
        var q = await ColaAsync(cola);
        var msg = await q.ReceiveMessageAsync();
        if (msg.Value is null) return null;
        return (msg.Value.MessageText, msg.Value.PopReceipt, msg.Value.MessageId);
    }

    public async Task BorrarAsync(string cola, string messageId, string popReceipt)
    {
        var q = await ColaAsync(cola);
        await q.DeleteMessageAsync(messageId, popReceipt);
    }

    public async Task<int> LongitudAproxAsync(string cola)
    {
        var q = await ColaAsync(cola);
        var props = await q.GetPropertiesAsync();
        return props.Value.ApproximateMessagesCount;
    }
}
