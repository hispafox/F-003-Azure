using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

internal static class TestHost
{
    public static CrearPedidoFunction NewCrearPedido(IPedidosHandler? handler = null)
        => new(
            handler ?? new PedidosHandler(),
            NullLogger<CrearPedidoFunction>.Instance);

    public static ProcesarPedidoColaFunction NewProcesarCola()
        => new(NullLogger<ProcesarPedidoColaFunction>.Instance);
}
