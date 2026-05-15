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

    private static HttpRequest WithJsonBody<T>(T body) =>
        WithJsonBody(JsonSerializer.Serialize(body));

    private static HttpRequest EmptyReq() => new DefaultHttpContext().Request;

    // ── LISTAR ──

    [Fact]
    public void ListarProductos_Devuelve_Los_3_Seed()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.ListarProductos(EmptyReq()) as OkObjectResult;

        Assert.NotNull(result);
        var total = (int)result!.Value!.GetType().GetProperty("total")!.GetValue(result.Value)!;
        Assert.Equal(3, total);
    }

    // ── OBTENER ──

    [Fact]
    public void ObtenerProducto_Existente_Devuelve_Ok()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.ObtenerProducto(EmptyReq(), "p001") as OkObjectResult;

        Assert.NotNull(result);
        var producto = (Producto)result!.Value!;
        Assert.Equal("Laptop Dell", producto.Nombre);
        Assert.Equal(1299.00m, producto.Precio);
        Assert.Equal(5, producto.Stock);
    }

    [Fact]
    public void ObtenerProducto_Inexistente_Devuelve_404()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.ObtenerProducto(EmptyReq(), "no-existe");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── CREAR ──

    [Fact]
    public async Task CrearProducto_Body_Valido_Devuelve_201_Y_Anade()
    {
        var (fn, svc) = TestHost.NewProductos();
        var req = WithJsonBody(new CrearProductoDto("Mouse", 29.99m, 50));

        var result = await fn.CrearProducto(req);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
        var producto = (Producto)created.Value!;
        Assert.Equal("Mouse", producto.Nombre);
        Assert.StartsWith("p", producto.Id); // id auto-generado con prefijo "p"
        Assert.Equal(4, svc.Total);
    }

    [Fact]
    public async Task CrearProducto_Sin_Nombre_Devuelve_400()
    {
        var (fn, svc) = TestHost.NewProductos();
        var req = WithJsonBody(new CrearProductoDto("", 10m, 5));

        var result = await fn.CrearProducto(req);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(3, svc.Total);
    }

    [Fact]
    public async Task CrearProducto_Con_Precio_Cero_Devuelve_400()
    {
        var (fn, _) = TestHost.NewProductos();
        var req = WithJsonBody(new CrearProductoDto("Algo", 0m, 5));

        var result = await fn.CrearProducto(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CrearProducto_Con_Stock_Negativo_Devuelve_400()
    {
        var (fn, _) = TestHost.NewProductos();
        var req = WithJsonBody(new CrearProductoDto("Algo", 10m, -1));

        var result = await fn.CrearProducto(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CrearProducto_Body_Json_Malformado_Devuelve_400()
    {
        var (fn, _) = TestHost.NewProductos();
        var req = WithJsonBody("{ broken json");

        var result = await fn.CrearProducto(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── ACTUALIZAR ──

    [Fact]
    public async Task ActualizarProducto_Existente_Devuelve_Ok_Y_Modifica()
    {
        var (fn, svc) = TestHost.NewProductos();
        var req = WithJsonBody(new CrearProductoDto("Laptop XPS", 1499.00m, 2));

        var result = await fn.ActualizarProducto(req, "p001");

        var ok = Assert.IsType<OkObjectResult>(result);
        var producto = (Producto)ok.Value!;
        Assert.Equal("p001", producto.Id);
        Assert.Equal("Laptop XPS", producto.Nombre);
        Assert.Equal(1499.00m, producto.Precio);
        Assert.Equal(2, producto.Stock);

        // Y el store refleja el cambio
        Assert.Equal("Laptop XPS", svc.GetById("p001")!.Nombre);
        Assert.Equal(3, svc.Total); // sigue siendo 3
    }

    [Fact]
    public async Task ActualizarProducto_Inexistente_Devuelve_404_Sin_Crear()
    {
        var (fn, svc) = TestHost.NewProductos();
        var req = WithJsonBody(new CrearProductoDto("X", 10m, 1));

        var result = await fn.ActualizarProducto(req, "no-existe");

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(3, svc.Total); // NO crea (PUT no es upsert)
    }

    [Fact]
    public async Task ActualizarProducto_Body_Invalido_Devuelve_400()
    {
        var (fn, _) = TestHost.NewProductos();
        var req = WithJsonBody(new CrearProductoDto("", -1m, 0));

        var result = await fn.ActualizarProducto(req, "p001");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── BORRAR ──

    [Fact]
    public void BorrarProducto_Existente_Devuelve_204()
    {
        var (fn, svc) = TestHost.NewProductos();

        var result = fn.BorrarProducto(EmptyReq(), "p001");

        Assert.IsType<NoContentResult>(result);
        Assert.Null(svc.GetById("p001"));
        Assert.Equal(2, svc.Total);
    }

    [Fact]
    public void BorrarProducto_Inexistente_Devuelve_404()
    {
        var (fn, svc) = TestHost.NewProductos();

        var result = fn.BorrarProducto(EmptyReq(), "no-existe");

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(3, svc.Total);
    }
}
