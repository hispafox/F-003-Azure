using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class ProductosCrudTests
{
    [Fact]
    public void GetPorId_Returns_200_For_Existing_Product()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.GetPorId(HttpRequestFactory.Empty(), "p-001");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<Producto>(ok.Value);
    }

    [Fact]
    public void GetPorId_Returns_404_ProblemDetails_For_Missing()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.GetPorId(HttpRequestFactory.Empty(), "missing-id");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(obj.Value);
        Assert.Equal("Resource Not Found", problem.Title);
    }

    [Fact]
    public async Task Crear_With_Valid_Body_Returns_201()
    {
        var (fn, service) = TestHost.NewProductos();
        var dto = new CrearProductoDto
        {
            Nombre = "Nuevo producto",
            Categoria = "libros",
            Precio = 12.50m,
            Stock = 5
        };

        var result = await fn.Crear(HttpRequestFactory.WithJsonBody(dto));

        var created = Assert.IsType<CreatedResult>(result);
        var producto = Assert.IsType<Producto>(created.Value);
        Assert.Equal(dto.Nombre, producto.Nombre);
        Assert.NotNull(service.GetById(producto.Id));
    }

    [Fact]
    public async Task Crear_With_Invalid_Body_Returns_422_ProblemDetails()
    {
        var (fn, _) = TestHost.NewProductos();
        var invalido = new CrearProductoDto
        {
            Nombre = "X",        // < 3 chars
            Categoria = "",      // requerido
            Precio = -1,         // fuera de rango
            Stock = -1           // fuera de rango
        };

        var result = await fn.Crear(HttpRequestFactory.WithJsonBody(invalido));

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, obj.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(obj.Value);
        Assert.NotEmpty(problem.Errors);
    }

    [Fact]
    public async Task Crear_With_Malformed_Json_Returns_400()
    {
        var (fn, _) = TestHost.NewProductos();
        var req = HttpRequestFactory.WithRawBody("{ esto no es JSON valido");

        var result = await fn.Crear(req);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Actualizar_Existing_Product_Returns_200()
    {
        var (fn, service) = TestHost.NewProductos();
        var dto = new ActualizarProductoDto { Precio = 999.00m };

        var result = await fn.Actualizar(HttpRequestFactory.WithJsonBody(dto), "p-001");

        var ok = Assert.IsType<OkObjectResult>(result);
        var producto = Assert.IsType<Producto>(ok.Value);
        Assert.Equal(999.00m, producto.Precio);
        Assert.Equal(999.00m, service.GetById("p-001")!.Precio);
    }

    [Fact]
    public async Task Actualizar_Missing_Product_Returns_404()
    {
        var (fn, _) = TestHost.NewProductos();
        var dto = new ActualizarProductoDto { Precio = 1m };

        var result = await fn.Actualizar(HttpRequestFactory.WithJsonBody(dto), "missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
    }

    [Fact]
    public void Eliminar_Existing_Returns_204()
    {
        var (fn, service) = TestHost.NewProductos();

        var result = fn.Eliminar(HttpRequestFactory.Empty(), "p-001");

        Assert.IsType<NoContentResult>(result);
        Assert.Null(service.GetById("p-001"));
    }

    [Fact]
    public void Eliminar_Missing_Returns_404()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.Eliminar(HttpRequestFactory.Empty(), "missing");

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
    }
}
