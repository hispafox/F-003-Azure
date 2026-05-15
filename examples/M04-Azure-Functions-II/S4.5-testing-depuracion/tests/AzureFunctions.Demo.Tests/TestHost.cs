using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

internal static class TestHost
{
    public static PedidosApi NewPedidosApi(IDescuentoCalculator calc)
        => new(calc);
}
