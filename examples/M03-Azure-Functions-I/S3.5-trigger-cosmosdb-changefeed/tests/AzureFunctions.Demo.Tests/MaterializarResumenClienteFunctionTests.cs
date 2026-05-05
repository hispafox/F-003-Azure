using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Tests;

public class MaterializarResumenClienteFunctionTests
{
    private static Pedido P(string id, string clienteId, decimal total, long ts)
        => new() { Id = id, ClienteId = clienteId, Estado = "confirmado", Total = total, Timestamp = ts };

    [Fact]
    public void Procesar_Agrupa_Por_Cliente_Y_Calcula_Acumulado()
    {
        // Slide 9 — El batch trae cambios mezclados. La función agrupa
        // por clienteId y produce un resumen por cliente.
        var (fn, espejo) = TestHost.NewMaterializar();

        var resultado = fn.Procesar(new[]
        {
            P("ped-1", "cliente-A", 100m, 1700000010),
            P("ped-2", "cliente-B", 50m,  1700000020),
            P("ped-3", "cliente-A", 200m, 1700000030),
        });

        Assert.Equal(2, resultado.Count);

        var resumenA = resultado.First(r => r.ClienteId == "cliente-A");
        Assert.Equal("resumen-cliente-A", resumenA.Id);
        Assert.Equal(2, resumenA.TotalPedidos);
        Assert.Equal(300m, resumenA.ImporteAcumulado);
        Assert.Equal(1700000030, resumenA.UltimoPedidoTimestamp);

        var resumenB = resultado.First(r => r.ClienteId == "cliente-B");
        Assert.Equal(1, resumenB.TotalPedidos);
        Assert.Equal(50m, resumenB.ImporteAcumulado);
    }

    [Fact]
    public void Procesar_Refleja_El_Resultado_En_El_Espejo_InMemory()
    {
        // El return de la función va al CosmosDBOutput; el espejo es
        // lo que los endpoints HTTP exponen para inspeccionar.
        var (fn, espejo) = TestHost.NewMaterializar();

        fn.Procesar(new[] { P("ped-1", "cliente-A", 100m, 1700000010) });

        var enEspejo = espejo.Get("cliente-A");
        Assert.NotNull(enEspejo);
        Assert.Equal(100m, enEspejo!.ImporteAcumulado);
    }

    [Fact]
    public void Procesar_Con_Batches_Sucesivos_Sobrescribe_Resumen_En_Espejo()
    {
        // Slide 10 — el id del resumen es estable ("resumen-{clienteId}"),
        // así que el segundo batch hace upsert del primero.
        var (fn, espejo) = TestHost.NewMaterializar();

        fn.Procesar(new[] { P("ped-1", "cliente-A", 100m, 1700000010) });
        fn.Procesar(new[]
        {
            P("ped-2", "cliente-A", 50m,  1700000020),
            P("ped-3", "cliente-A", 30m,  1700000030),
        });

        var resumen = espejo.Get("cliente-A");
        Assert.NotNull(resumen);
        // El segundo batch reemplaza al primero (no acumula histórico):
        // esto es lo realista para una vista materializada por evento.
        Assert.Equal(2, resumen!.TotalPedidos);
        Assert.Equal(80m, resumen.ImporteAcumulado);
        Assert.Equal(1700000030, resumen.UltimoPedidoTimestamp);
    }

    [Fact]
    public void Procesar_Es_Idempotente_Sobre_El_Mismo_Batch()
    {
        // Si el Change Feed reenvía el mismo batch (slide 10), el espejo
        // termina con el mismo estado: el id del resumen no cambia.
        var (fn, espejo) = TestHost.NewMaterializar();
        var batch = new[]
        {
            P("ped-1", "cliente-A", 100m, 1700000010),
            P("ped-2", "cliente-A", 200m, 1700000020),
        };

        fn.Procesar(batch);
        fn.Procesar(batch);

        Assert.Equal(1, espejo.Total);
        var r = espejo.Get("cliente-A")!;
        Assert.Equal(2, r.TotalPedidos);
        Assert.Equal(300m, r.ImporteAcumulado);
    }

    [Fact]
    public void Procesar_Ignora_Pedidos_Sin_ClienteId()
    {
        // Documentos malformados: no podemos materializar un resumen
        // sin la clave de partición. Los descartamos en silencio.
        var (fn, espejo) = TestHost.NewMaterializar();

        var resultado = fn.Procesar(new[]
        {
            P("ped-1", "", 100m, 1700000010),
            P("ped-2", "cliente-A", 50m, 1700000020),
        });

        Assert.Single(resultado);
        Assert.Equal("cliente-A", resultado[0].ClienteId);
        Assert.Equal(1, espejo.Total);
    }

    [Fact]
    public void Procesar_Batch_Vacio_O_Null_Devuelve_Lista_Vacia()
    {
        var (fn, espejo) = TestHost.NewMaterializar();

        Assert.Empty(fn.Procesar(null));
        Assert.Empty(fn.Procesar(Array.Empty<Pedido>()));
        Assert.Equal(0, espejo.Total);
    }
}
