using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

internal static class TestHost
{
    public static (ProductosApi fn, IProductoService svc) NewProductos()
    {
        var svc = new InMemoryProductoService();
        var fn = new ProductosApi(svc, NullLogger<ProductosApi>.Instance);
        return (fn, svc);
    }

    public static (LimpiezaProgramadaFunction fn, ILimpiezaTracker tracker) NewLimpieza()
    {
        var tracker = new InMemoryLimpiezaTracker();
        var fn = new LimpiezaProgramadaFunction(
            tracker, NullLogger<LimpiezaProgramadaFunction>.Instance);
        return (fn, tracker);
    }

    public static ProcesarCsvFunction NewCsv()
        => new(NullLogger<ProcesarCsvFunction>.Instance);

    public static (ReaccionarPedidosFunction fn, INotificacionLog log) NewReaccionar()
    {
        var log = new InMemoryNotificacionLog();
        var fn = new ReaccionarPedidosFunction(
            log, NullLogger<ReaccionarPedidosFunction>.Instance);
        return (fn, log);
    }

    public static EstadoFunction NewEstado(
        IProductoService productos,
        ILimpiezaTracker tracker,
        INotificacionLog log)
        => new(productos, tracker, log);
}
