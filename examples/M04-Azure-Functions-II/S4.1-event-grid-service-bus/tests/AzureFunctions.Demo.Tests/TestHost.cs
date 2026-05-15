using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

internal static class TestHost
{
    public static (CrearPedidoFunction fn, IEstadoTracker tracker) NewCrearPedido()
    {
        var tracker = new InMemoryEstadoTracker();
        var fn = new CrearPedidoFunction(
            new PedidosOrquestador(),
            tracker,
            NullLogger<CrearPedidoFunction>.Instance);
        return (fn, tracker);
    }

    public static (ProcesarPedidoFunction fn, IEstadoTracker tracker) NewProcesarPedido()
    {
        var tracker = new InMemoryEstadoTracker();
        var fn = new ProcesarPedidoFunction(
            tracker,
            NullLogger<ProcesarPedidoFunction>.Instance);
        return (fn, tracker);
    }

    public static (NotificarPedidoCreadoFunction fn, IEstadoTracker tracker) NewNotificar()
    {
        var tracker = new InMemoryEstadoTracker();
        var fn = new NotificarPedidoCreadoFunction(
            tracker,
            NullLogger<NotificarPedidoCreadoFunction>.Instance);
        return (fn, tracker);
    }

    public static (ClasificarArchivoFunction fn, IEstadoTracker tracker) NewClasificar()
    {
        var tracker = new InMemoryEstadoTracker();
        var fn = new ClasificarArchivoFunction(
            tracker,
            NullLogger<ClasificarArchivoFunction>.Instance);
        return (fn, tracker);
    }
}
