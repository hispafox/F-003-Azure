using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class GetPedidoFunctionsTests
{
    private static HttpRequest EmptyReq() => new DefaultHttpContext().Request;

    [Fact]
    public void GetPedidoById_Existente_Devuelve_Ok_Con_Pedido()
    {
        var fn = new GetPedidoByIdFunction();
        var pedido = new Pedido { Id = "ped-001", ClienteId = "cliente-A", Total = 50m };

        var result = fn.GetPedidoById(EmptyReq(), pedido, "ped-001", "cliente-A");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(pedido, ok.Value);
    }

    [Fact]
    public void GetPedidoById_Cuando_Binding_Devuelve_Null_Devuelve_404()
    {
        // Slide 4 — CosmosDBInput por id: si el documento no existe,
        // Functions absorbe el 404 de Cosmos y nos pasa null.
        var fn = new GetPedidoByIdFunction();

        var result = fn.GetPedidoById(EmptyReq(), null, "no-existe", "cliente-A");

        var nf = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, nf.StatusCode);
    }

    [Fact]
    public void GetPedidosPorCliente_Devuelve_Lista_Con_Total()
    {
        var fn = new GetPedidosPorClienteFunction();
        var pedidos = new[]
        {
            new Pedido { Id = "ped-1", ClienteId = "cliente-A", Total = 10m },
            new Pedido { Id = "ped-2", ClienteId = "cliente-A", Total = 20m },
        };

        var result = fn.GetPedidosPorCliente(EmptyReq(), pedidos, "cliente-A");

        var ok = Assert.IsType<OkObjectResult>(result);
        var total = (int)ok.Value!.GetType().GetProperty("total")!.GetValue(ok.Value)!;
        Assert.Equal(2, total);
    }

    [Fact]
    public void GetPedidosPorCliente_Sin_Resultados_Devuelve_Total_Cero()
    {
        var fn = new GetPedidosPorClienteFunction();

        var result = fn.GetPedidosPorCliente(EmptyReq(), Array.Empty<Pedido>(), "cliente-A");

        var ok = Assert.IsType<OkObjectResult>(result);
        var total = (int)ok.Value!.GetType().GetProperty("total")!.GetValue(ok.Value)!;
        Assert.Equal(0, total);
    }
}
