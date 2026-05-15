using System.Text;
using System.Text.Json;
using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class ProductosApiTests
{
    private static HttpRequest WithJsonBody(string json)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(json);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        return ctx.Request;
    }

    private static HttpRequest EmptyReq() => new DefaultHttpContext().Request;

    [Fact]
    public void ListarProductos_Devuelve_Los_3_Seed()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.ListarProductos(EmptyReq()) as OkObjectResult;

        Assert.NotNull(result);
        var total = (int)result!.Value!.GetType().GetProperty("total")!.GetValue(result.Value)!;
        Assert.Equal(3, total);
    }

    [Fact]
    public void GetProducto_Existente_Devuelve_Ok()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.GetProducto(EmptyReq(), "1") as OkObjectResult;

        Assert.NotNull(result);
        var producto = (Producto)result!.Value!;
        Assert.Equal("Laptop", producto.Nombre);
    }

    [Fact]
    public void GetProducto_Inexistente_Devuelve_404()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.GetProducto(EmptyReq(), "no-existe");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CrearProducto_Body_Valido_Devuelve_201()
    {
        var (fn, svc) = TestHost.NewProductos();
        var req = WithJsonBody(JsonSerializer.Serialize(new CrearProductoDto("Mouse", 29.99m)));

        var result = await fn.CrearProducto(req);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Equal(4, svc.Total);
    }

    [Fact]
    public async Task CrearProducto_Body_Invalido_Devuelve_400()
    {
        var (fn, svc) = TestHost.NewProductos();
        var req = WithJsonBody(JsonSerializer.Serialize(new CrearProductoDto("", -1m)));

        var result = await fn.CrearProducto(req);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(3, svc.Total); // No se añadió
    }

    [Fact]
    public async Task CrearProducto_Body_Json_Malformado_Devuelve_400()
    {
        var (fn, _) = TestHost.NewProductos();
        var req = WithJsonBody("{ broken");

        var result = await fn.CrearProducto(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
