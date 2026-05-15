using System.Text;
using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace AzureFunctions.Demo.Tests;

// CAPA 2 — Function test (slide 6). La función es "pegamento": mockeamos
// IDescuentoCalculator con NSubstitute y verificamos SOLO el wiring
// (status codes, deserialización, que delega en el servicio). La lógica
// del cálculo ya está cubierta en la capa 1.
[Trait("Category", "Function")]
public class Function_PedidosApiTests
{
    private static HttpRequest JsonReq(string body)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(body);
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        return ctx.Request;
    }

    [Fact]
    public async Task Body_Valido_Devuelve_200_Y_Delega_En_El_Servicio()
    {
        var calc = Substitute.For<IDescuentoCalculator>();
        calc.Aplicar(Arg.Any<Pedido>())
            .Returns(new PedidoConDescuento("p1", 500m, 50m, 450m));
        var fn = TestHost.NewPedidosApi(calc);

        var result = await fn.CalcularDescuento(
            JsonReq("""{"id":"p1","clienteId":"c1","total":500}"""));

        var ok = Assert.IsType<OkObjectResult>(result);
        var r = Assert.IsType<PedidoConDescuento>(ok.Value);
        Assert.Equal(450m, r.TotalFinal);
        // Verifica que la función DELEGÓ en el servicio (no recalculó).
        calc.Received(1).Aplicar(Arg.Is<Pedido>(p => p.Id == "p1"));
    }

    [Fact]
    public async Task Body_Json_Malformado_Devuelve_400_Sin_Tocar_El_Servicio()
    {
        var calc = Substitute.For<IDescuentoCalculator>();
        var fn = TestHost.NewPedidosApi(calc);

        var result = await fn.CalcularDescuento(JsonReq("{ roto"));

        Assert.IsType<BadRequestObjectResult>(result);
        calc.DidNotReceive().Aplicar(Arg.Any<Pedido>());
    }

    [Fact]
    public async Task Pedido_Sin_Id_Devuelve_400()
    {
        var fn = TestHost.NewPedidosApi(Substitute.For<IDescuentoCalculator>());

        var result = await fn.CalcularDescuento(
            JsonReq("""{"id":"","clienteId":"c1","total":100}"""));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Total_Negativo_Devuelve_400()
    {
        var fn = TestHost.NewPedidosApi(Substitute.For<IDescuentoCalculator>());

        var result = await fn.CalcularDescuento(
            JsonReq("""{"id":"p1","clienteId":"c1","total":-5}"""));

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
