using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

// Helper para construir las funciones del ejemplo con servicios en memoria
// independientes por test. Cada test arranca con estado limpio.
internal static class TestHost
{
    public static (NotificacionesPedidoFunction fn, INotificacionService notificaciones) NewNotificaciones()
    {
        var notificaciones = new InMemoryNotificacionService();
        var fn = new NotificacionesPedidoFunction(
            notificaciones,
            NullLogger<NotificacionesPedidoFunction>.Instance);
        return (fn, notificaciones);
    }

    public static (MaterializarResumenClienteFunction fn, IResumenClienteService resumenes) NewMaterializar()
    {
        var resumenes = new InMemoryResumenClienteService();
        var fn = new MaterializarResumenClienteFunction(
            resumenes,
            NullLogger<MaterializarResumenClienteFunction>.Instance);
        return (fn, resumenes);
    }

    public static InspeccionHttpFunctions NewInspeccion(
        INotificacionService notificaciones,
        IResumenClienteService resumenes)
        => new(notificaciones, resumenes);
}
