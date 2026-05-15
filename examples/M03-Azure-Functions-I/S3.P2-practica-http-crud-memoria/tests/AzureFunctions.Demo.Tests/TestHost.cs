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
}
