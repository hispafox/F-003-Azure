namespace EventDriven.Demo.Api.EventDriven;

// Slides 14-15, 21 — Event Sourcing: el estado no se guarda; se guardan
// los EVENTOS y el estado se reconstruye con replay. Snapshots cada N
// eventos para no reproducir miles. Todo en memoria, lógica pura
// (sin Cosmos): el patrón es lo que se enseña.

public abstract record EventoPedido;
public sealed record PedidoCreado(string ClienteId) : EventoPedido;
public sealed record ItemAnadido(string Nombre, decimal Precio, int Cantidad) : EventoPedido;
public sealed record DescuentoAplicado(string Codigo, decimal Importe) : EventoPedido;
public sealed record PagoConfirmado(string Transaccion) : EventoPedido;
public sealed record PedidoEnviado(string Tracking) : EventoPedido;

public sealed record EstadoPedido(
    string ClienteId, decimal Total, int NumItems, string Estado, long Version)
{
    public static readonly EstadoPedido Inicial =
        new("", 0m, 0, "Vacio", 0);
}

// Proyección pura: (estado, evento) → nuevo estado. Replay = fold.
public static class PedidoProjection
{
    public static EstadoPedido Aplicar(EstadoPedido s, EventoPedido e) => e switch
    {
        PedidoCreado c => s with { ClienteId = c.ClienteId, Estado = "Creado", Version = s.Version + 1 },
        ItemAnadido i => s with
        {
            Total = s.Total + i.Precio * i.Cantidad,
            NumItems = s.NumItems + i.Cantidad,
            Version = s.Version + 1,
        },
        DescuentoAplicado d => s with { Total = Math.Max(0m, s.Total - d.Importe), Version = s.Version + 1 },
        PagoConfirmado => s with { Estado = "Pagado", Version = s.Version + 1 },
        PedidoEnviado => s with { Estado = "Enviado", Version = s.Version + 1 },
        _ => throw new ArgumentOutOfRangeException(nameof(e), e.GetType().Name),
    };

    public static EstadoPedido Reconstruir(
        EstadoPedido desde, IEnumerable<EventoPedido> eventos) =>
        eventos.Aggregate(desde, Aplicar);
}

// Event store append-only en memoria con snapshots periódicos (slide 21).
public sealed class EventStore
{
    private readonly Dictionary<string, List<EventoPedido>> _streams = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EstadoPedido> _snapshots = new(StringComparer.Ordinal);
    private readonly int _snapshotCada;

    public EventStore(int snapshotCada = 100)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(snapshotCada);
        _snapshotCada = snapshotCada;
    }

    public int SnapshotsTomados { get; private set; }

    // Cuántos eventos reprodujo el último Cargar() (mide el ahorro del
    // snapshot: sin snapshot reproduce todo el stream).
    public int UltimoReplayCount { get; private set; }

    public long Append(string streamId, EventoPedido evento)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(evento);

        var stream = _streams.TryGetValue(streamId, out var s)
            ? s : _streams[streamId] = [];
        stream.Add(evento);
        long version = stream.Count;

        if (version % _snapshotCada == 0)
        {
            _snapshots[streamId] = PedidoProjection.Reconstruir(
                EstadoPedido.Inicial, stream);
            SnapshotsTomados++;
        }
        return version;
    }

    // Slide 21 — cargar = último snapshot + replay de lo posterior.
    public EstadoPedido Cargar(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        if (!_streams.TryGetValue(streamId, out var stream))
            return EstadoPedido.Inicial;

        EstadoPedido desde = EstadoPedido.Inicial;
        long baseVersion = 0;
        if (_snapshots.TryGetValue(streamId, out var snap))
        {
            desde = snap;
            baseVersion = snap.Version;
        }

        var posteriores = stream.Skip((int)baseVersion).ToList();
        UltimoReplayCount = posteriores.Count;
        return PedidoProjection.Reconstruir(desde, posteriores);
    }
}
