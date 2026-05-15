using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class ProductosVersionadasTests
{
    private static HttpRequest Req() => new DefaultHttpContext().Request;

    [Fact]
    public void V1_Devuelve_Contrato_Sin_Moneda_Ni_Stock()
    {
        var fn = TestHost.NewVersionadas();

        var ok = fn.ListarV1(Req()) as OkObjectResult;

        Assert.NotNull(ok);
        var items = (IEnumerable<ProductoV1>)ok!.Value!.GetType()
            .GetProperty("items")!.GetValue(ok.Value)!;
        var primero = items.First();
        // ProductoV1 NO tiene Moneda/Stock — el tipo lo garantiza en
        // compilación; aquí confirmamos que el mapeo es correcto.
        Assert.Equal("p001", primero.Id);
        Assert.Equal(1299.00m, primero.Precio);
        Assert.Equal(3, items.Count());
    }

    [Fact]
    public void V2_Devuelve_Contrato_Ampliado_Con_Moneda_Y_Stock()
    {
        var fn = TestHost.NewVersionadas();

        var ok = fn.ListarV2(Req()) as OkObjectResult;

        Assert.NotNull(ok);
        var items = (IEnumerable<ProductoV2>)ok!.Value!.GetType()
            .GetProperty("items")!.GetValue(ok.Value)!;
        var primero = items.First();
        Assert.Equal("EUR", primero.Moneda);   // campo nuevo en v2
        Assert.Equal(5, primero.Stock);        // campo nuevo en v2
    }

    [Fact]
    public void V1_Y_V2_Proyectan_El_Mismo_Dominio()
    {
        // El precio (campo compartido) es idéntico en ambas versiones:
        // versionar el contrato NO cambia la lógica de negocio (slide 7).
        var fn = TestHost.NewVersionadas();

        var v1 = ((fn.GetV1(Req(), "p002") as OkObjectResult)!.Value as ProductoV1)!;
        var v2 = ((fn.GetV2(Req(), "p002") as OkObjectResult)!.Value as ProductoV2)!;

        Assert.Equal(v1.Id, v2.Id);
        Assert.Equal(v1.Nombre, v2.Nombre);
        Assert.Equal(v1.Precio, v2.Precio);
    }

    [Fact]
    public void Id_Inexistente_Devuelve_404_En_Ambas_Versiones()
    {
        var fn = TestHost.NewVersionadas();

        Assert.IsType<NotFoundObjectResult>(fn.GetV1(Req(), "zzz"));
        Assert.IsType<NotFoundObjectResult>(fn.GetV2(Req(), "zzz"));
    }
}
