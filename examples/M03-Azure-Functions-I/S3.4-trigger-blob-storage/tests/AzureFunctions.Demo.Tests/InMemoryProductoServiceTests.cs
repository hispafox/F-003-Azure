using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

// Tests unitarios puros del servicio (sin tocar HttpRequest). Útil para
// ejercitar los filtros y la paginación con menos boilerplate.
public class InMemoryProductoServiceTests
{
    [Fact]
    public void Crear_Returns_Product_With_Generated_Id()
    {
        var svc = new InMemoryProductoService();
        var dto = new CrearProductoDto
        {
            Nombre = "Test",
            Categoria = "libros",
            Precio = 10m,
            Stock = 1
        };

        var creado = svc.Crear(dto);

        Assert.StartsWith("p-", creado.Id);
        Assert.Equal(dto.Nombre, creado.Nombre);
    }

    [Fact]
    public void Buscar_Filters_By_Categoria_Case_Insensitive()
    {
        var svc = new InMemoryProductoService();
        var query = new BuscarProductosQuery(null, "ELECTRONICA", null, null, 1, 50);

        var resultados = svc.Buscar(query, out var total);

        Assert.Equal(2, total);
        Assert.All(resultados, p =>
            Assert.Equal("electronica", p.Categoria, ignoreCase: true));
    }

    [Fact]
    public void Buscar_Pagination_Works()
    {
        var svc = new InMemoryProductoService();
        var query = new BuscarProductosQuery(null, null, null, null, Pagina: 2, PorPagina: 2);

        var resultados = svc.Buscar(query, out var total);

        Assert.Equal(3, total);
        Assert.Single(resultados);
    }

    [Fact]
    public void Eliminar_Removes_From_Store()
    {
        var svc = new InMemoryProductoService();

        Assert.True(svc.Eliminar("p-001"));
        Assert.Null(svc.GetById("p-001"));
        Assert.False(svc.Eliminar("p-001")); // segunda vez devuelve false
    }
}
