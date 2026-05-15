using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Tests;

public class ReaccionarPedidosFunctionTests
{
    [Fact]
    public void Procesar_Anota_Cada_Pedido_Al_Log()
    {
        var (fn, log) = TestHost.NewReaccionar();
        var batch = new[]
        {
            new Pedido { Id = "ped-1", ClienteId = "cliente-A", Estado = "nuevo", Total = 100m },
            new Pedido { Id = "ped-2", ClienteId = "cliente-B", Estado = "confirmado", Total = 50m },
        };

        var procesados = fn.Procesar(batch);

        Assert.Equal(2, procesados);
        Assert.Equal(2, log.Total);
        var entries = log.Listar();
        Assert.Contains(entries, e => e.PedidoId == "ped-1");
        Assert.Contains(entries, e => e.PedidoId == "ped-2");
    }

    [Fact]
    public void Procesar_Batch_Vacio_O_Null_Es_Noop()
    {
        var (fn, log) = TestHost.NewReaccionar();

        Assert.Equal(0, fn.Procesar(null));
        Assert.Equal(0, fn.Procesar(Array.Empty<Pedido>()));
        Assert.Equal(0, log.Total);
    }

    [Fact]
    public void Procesar_Anota_Datos_Correctos()
    {
        var (fn, log) = TestHost.NewReaccionar();
        var pedido = new Pedido
        {
            Id = "ped-X",
            ClienteId = "cliente-X",
            Estado = "enviado",
            Total = 99.99m,
        };

        fn.Procesar(new[] { pedido });

        var entry = log.Listar().Single();
        Assert.Equal("ped-X", entry.PedidoId);
        Assert.Equal("cliente-X", entry.ClienteId);
        Assert.Equal("enviado", entry.Estado);
        Assert.Equal(99.99m, entry.Total);
    }
}
