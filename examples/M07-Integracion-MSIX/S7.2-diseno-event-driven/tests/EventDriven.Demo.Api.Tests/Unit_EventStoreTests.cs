using EventDriven.Demo.Api.EventDriven;

namespace EventDriven.Demo.Api.Tests;

// CAPA 1 — Event Sourcing: replay + snapshot (slides 14-15, 21).
[Trait("Category", "Unit")]
public class Unit_EventStoreTests
{
    [Fact]
    public void Replay_Reconstruye_El_Estado()
    {
        var s = PedidoProjection.Reconstruir(EstadoPedido.Inicial,
        [
            new PedidoCreado("CLI-001"),
            new ItemAnadido("Mouse", 29.99m, 2),
            new DescuentoAplicado("VERANO10", 10m),
            new PagoConfirmado("TXN-789"),
            new PedidoEnviado("ES123456"),
        ]);

        Assert.Equal("CLI-001", s.ClienteId);
        Assert.Equal(2, s.NumItems);
        Assert.Equal(29.99m * 2 - 10m, s.Total);
        Assert.Equal("Enviado", s.Estado);
        Assert.Equal(5, s.Version);
    }

    [Fact]
    public void Descuento_No_Deja_Total_Negativo()
    {
        var s = PedidoProjection.Reconstruir(EstadoPedido.Inicial,
        [
            new PedidoCreado("C"),
            new ItemAnadido("X", 5m, 1),
            new DescuentoAplicado("BIG", 999m),
        ]);
        Assert.Equal(0m, s.Total);
    }

    [Fact]
    public void Append_Devuelve_Version_Incremental()
    {
        var store = new EventStore(snapshotCada: 100);
        Assert.Equal(1, store.Append("PED-1", new PedidoCreado("C")));
        Assert.Equal(2, store.Append("PED-1", new ItemAnadido("A", 1m, 1)));
    }

    [Fact]
    public void Cargar_Sin_Snapshot_Reproduce_Todo()
    {
        var store = new EventStore(snapshotCada: 100);
        for (int i = 0; i < 5; i++)
            store.Append("PED-1", new ItemAnadido("A", 1m, 1));

        var estado = store.Cargar("PED-1");
        Assert.Equal(5, estado.NumItems);
        Assert.Equal(0, store.SnapshotsTomados);
        Assert.Equal(5, store.UltimoReplayCount);   // sin snapshot → 5
    }

    [Fact]
    public void Snapshot_Reduce_El_Replay()
    {
        var store = new EventStore(snapshotCada: 3);
        for (int i = 0; i < 7; i++)              // snapshots en v3 y v6
            store.Append("PED-1", new ItemAnadido("A", 2m, 1));

        var estado = store.Cargar("PED-1");
        Assert.Equal(14m, estado.Total);          // 7 × 2
        Assert.Equal(2, store.SnapshotsTomados);
        Assert.Equal(1, store.UltimoReplayCount);  // snapshot v6 + 1 evento
    }

    [Fact]
    public void Stream_Inexistente_Es_Estado_Inicial()
        => Assert.Equal(EstadoPedido.Inicial,
            new EventStore().Cargar("NO-EXISTE"));

    [Fact]
    public void SnapshotCada_No_Positivo_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new EventStore(0));
}
