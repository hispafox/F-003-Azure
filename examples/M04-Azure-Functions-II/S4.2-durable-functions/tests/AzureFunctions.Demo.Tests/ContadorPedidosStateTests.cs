using AzureFunctions.Demo.Functions;

namespace AzureFunctions.Demo.Tests;

// Slide 17 — la lógica de la Entity vive en su State (POCO). Se testea
// directo; el dispatcher de Durable es solo el adaptador.
public class ContadorPedidosStateTests
{
    [Fact]
    public void Incrementos_Acumulan_Por_Categoria()
    {
        var s = new ContadorPedidosState();

        s.RegistrarCompletado();
        s.RegistrarCompletado();
        s.RegistrarCompensado();
        s.RegistrarRechazado();

        Assert.Equal(2, s.Completados);
        Assert.Equal(1, s.Compensados);
        Assert.Equal(1, s.Rechazados);
    }

    [Fact]
    public void Snapshot_Es_Una_Copia_Independiente()
    {
        var s = new ContadorPedidosState();
        s.RegistrarCompletado();

        var snap = s.Snapshot();
        s.RegistrarCompletado(); // muta el original tras el snapshot

        Assert.Equal(1, snap.Completados);  // el snapshot no cambió
        Assert.Equal(2, s.Completados);
    }
}
