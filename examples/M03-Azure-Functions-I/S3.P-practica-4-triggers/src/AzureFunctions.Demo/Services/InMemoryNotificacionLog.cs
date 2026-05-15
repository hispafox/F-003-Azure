using System.Collections.Concurrent;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public sealed class InMemoryNotificacionLog : INotificacionLog
{
    private readonly ConcurrentQueue<EntradaLog> _entries = new();

    public void Anotar(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        _entries.Enqueue(new EntradaLog(
            pedido.Id, pedido.ClienteId, pedido.Estado, pedido.Total,
            DateTimeOffset.UtcNow));
    }

    public IReadOnlyList<EntradaLog> Listar() =>
        _entries.OrderByDescending(e => e.RegistradoEn).ToList();

    public int Total => _entries.Count;
}
