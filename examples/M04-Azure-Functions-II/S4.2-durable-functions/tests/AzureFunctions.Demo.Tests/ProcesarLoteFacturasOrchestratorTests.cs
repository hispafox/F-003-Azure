using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Models;
using Microsoft.DurableTask;
using NSubstitute;

namespace AzureFunctions.Demo.Tests;

// Slide 7 — fan-out/fan-in. Configuramos cada ProcesarFactura por el id
// de la factura y verificamos la consolidación.
public class ProcesarLoteFacturasOrchestratorTests
{
    private static TaskOrchestrationContext NewContext(List<Factura> facturas)
    {
        var ctx = Substitute.For<TaskOrchestrationContext>();
        ctx.GetInput<List<Factura>>().Returns(facturas);
        return ctx;
    }

    [Fact]
    public async Task Consolida_Exitosas_Y_Fallidas()
    {
        var facturas = new List<Factura>
        {
            new("f-1", "c1", 100m),
            new("f-2", "c1", 250m),
            new("f-3", "c2", 0m),     // fallará
            new("f-4", "c2", 75.5m),
        };
        var ctx = NewContext(facturas);

        // Cada llamada devuelve el resultado según el importe de la factura
        // de entrada (replicando la regla del servicio de facturación).
        ctx.CallActivityAsync<ResultadoFactura>(
                Arg.Is<TaskName>(n => n.Name == nameof(FacturaActivities.ProcesarFactura)),
                Arg.Any<object>(), Arg.Any<TaskOptions?>())
            .Returns(ci =>
            {
                var f = (Factura)ci.ArgAt<object>(1);
                return Task.FromResult(f.Importe > 0
                    ? new ResultadoFactura(f.Id, true, f.Importe, null)
                    : new ResultadoFactura(f.Id, false, 0, "Importe no positivo"));
            });

        var sut = new ProcesarLoteFacturasOrchestrator();
        var resumen = await sut.ProcesarLoteFacturas(ctx);

        Assert.Equal(4, resumen.Total);
        Assert.Equal(3, resumen.Exitosas);
        Assert.Equal(1, resumen.Fallidas);
        Assert.Equal(425.5m, resumen.ImporteTotal);
    }

    [Fact]
    public async Task Lote_Vacio_Devuelve_Resumen_En_Cero()
    {
        var ctx = NewContext([]);

        var sut = new ProcesarLoteFacturasOrchestrator();
        var resumen = await sut.ProcesarLoteFacturas(ctx);

        Assert.Equal(0, resumen.Total);
        Assert.Equal(0m, resumen.ImporteTotal);
    }

    [Fact]
    public async Task Procesa_En_Chunks_Pero_Devuelve_Todos_Los_Resultados()
    {
        // 120 facturas → 3 chunks de 50/50/20. Todas deben volver.
        var facturas = Enumerable.Range(1, 120)
            .Select(i => new Factura($"f-{i}", "c1", 10m))
            .ToList();
        var ctx = NewContext(facturas);

        ctx.CallActivityAsync<ResultadoFactura>(
                Arg.Any<TaskName>(), Arg.Any<object>(), Arg.Any<TaskOptions?>())
            .Returns(ci =>
            {
                var f = (Factura)ci.ArgAt<object>(1);
                return Task.FromResult(new ResultadoFactura(f.Id, true, f.Importe, null));
            });

        var sut = new ProcesarLoteFacturasOrchestrator();
        var resumen = await sut.ProcesarLoteFacturas(ctx);

        Assert.Equal(120, resumen.Total);
        Assert.Equal(120, resumen.Exitosas);
        Assert.Equal(1200m, resumen.ImporteTotal);
    }
}
