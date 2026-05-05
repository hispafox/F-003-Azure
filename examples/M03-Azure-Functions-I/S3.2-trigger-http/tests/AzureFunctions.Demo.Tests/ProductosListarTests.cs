using System.Reflection;
using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class ProductosListarTests
{
    [Fact]
    public void Listar_Without_Filters_Returns_All_Seeded_Products()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.Listar(HttpRequestFactory.Empty());

        var ok = Assert.IsType<OkObjectResult>(result);
        var total = (int)ok.Value!.GetType().GetProperty("total")!.GetValue(ok.Value)!;
        Assert.Equal(3, total);
    }

    [Fact]
    public void Listar_With_Categoria_Filter_Returns_Subset()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.Listar(HttpRequestFactory.WithQuery("categoria=ropa"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var total = (int)ok.Value!.GetType().GetProperty("total")!.GetValue(ok.Value)!;
        Assert.Equal(1, total);
    }

    [Fact]
    public void Listar_Sets_X_Total_Count_Header()
    {
        var (fn, _) = TestHost.NewProductos();
        var req = HttpRequestFactory.Empty();

        var _ = fn.Listar(req);

        var header = req.HttpContext.Response.Headers["X-Total-Count"].ToString();
        Assert.Equal("3", header);
    }

    [Fact]
    public void Listar_Clamps_PorPagina_To_Max_Allowed()
    {
        var (fn, _) = TestHost.NewProductos(new() { MaxPorPagina = 2, PorPaginaPorDefecto = 20 });

        var result = fn.Listar(HttpRequestFactory.WithQuery("porPagina=999"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var porPagina = (int)ok.Value!.GetType().GetProperty("porPagina")!.GetValue(ok.Value)!;
        Assert.Equal(2, porPagina);
    }

    [Fact]
    public void Listar_With_MinPrecio_Filter()
    {
        var (fn, _) = TestHost.NewProductos();

        var result = fn.Listar(HttpRequestFactory.WithQuery("minPrecio=100"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = (System.Collections.IEnumerable)ok.Value!.GetType().GetProperty("items")!.GetValue(ok.Value)!;
        var precios = items.Cast<Producto>().Select(p => p.Precio).ToList();
        Assert.All(precios, p => Assert.True(p >= 100));
    }
}
