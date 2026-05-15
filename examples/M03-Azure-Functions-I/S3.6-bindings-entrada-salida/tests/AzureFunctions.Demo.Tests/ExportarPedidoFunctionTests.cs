using System.Text.Json;
using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class ExportarPedidoFunctionTests
{
    private static HttpRequest EmptyReq() => new DefaultHttpContext().Request;

    [Fact]
    public void ExportarPedido_Con_Pedido_Existente_Devuelve_Ok_Y_BlobJson()
    {
        // Slide 7 — pipeline: HTTP → CosmosDBInput (input.pedido) → BlobOutput.
        // En el test pasamos el "pedido" como si Functions lo hubiera leído
        // de Cosmos por nosotros (binding ya resuelto).
        var fn = new ExportarPedidoFunction();
        var pedido = new Pedido
        {
            Id = "ped-001",
            ClienteId = "cliente-A",
            Estado = "confirmado",
            Total = 150m,
        };

        var result = fn.ExportarPedido(EmptyReq(), pedido, "ped-001", "cliente-A");

        var ok = Assert.IsType<OkObjectResult>(result.HttpResponse);
        Assert.Same(pedido, ok.Value);

        Assert.NotNull(result.BlobJson);
        using var doc = JsonDocument.Parse(result.BlobJson!);
        Assert.Equal("ped-001", doc.RootElement.GetProperty("id").GetString());
        Assert.Equal("cliente-A", doc.RootElement.GetProperty("clienteId").GetString());
    }

    [Fact]
    public void ExportarPedido_Cuando_CosmosInput_Es_Null_Devuelve_404_Sin_BlobJson()
    {
        // Si Cosmos no encuentra el documento, el binding nos pasa null.
        // En ese caso devolvemos 404 y dejamos BlobJson=null para que el
        // output binding NO escriba un blob vacío en exports/.
        var fn = new ExportarPedidoFunction();

        var result = fn.ExportarPedido(EmptyReq(), null, "no-existe", "cliente-A");

        var nf = Assert.IsType<NotFoundObjectResult>(result.HttpResponse);
        Assert.Equal(404, nf.StatusCode);
        Assert.Null(result.BlobJson);
    }
}
