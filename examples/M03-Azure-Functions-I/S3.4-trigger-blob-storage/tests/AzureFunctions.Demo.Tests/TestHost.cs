using AzureFunctions.Demo.Configuration;
using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AzureFunctions.Demo.Tests;

// Helper para construir un ProductosFunctions "vivo" con un servicio en memoria
// independiente para cada test. Evita estado compartido entre tests.
internal static class TestHost
{
    public static (ProductosFunctions function, IProductoService service) NewProductos(
        ProductosOptions? options = null)
    {
        var service = new InMemoryProductoService();
        var fn = new ProductosFunctions(
            service,
            Options.Create(options ?? new ProductosOptions()),
            NullLogger<ProductosFunctions>.Instance);
        return (fn, service);
    }
}
