using System.Text;
using System.Text.Json;
using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class CrearPedidoFunctionTests
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

    private static HttpRequest WithJsonBody<T>(T body)
        => WithJsonBody(JsonSerializer.Serialize(body));

    [Fact]
    public async Task CrearPedido_Body_Valido_Devuelve_Created_Y_Materializa_Cosmos_Y_Cola()
    {
        // Slide 6 — el shape MultiResponse contiene los 3 efectos:
        //   - HttpResponse (201 Created)
        //   - PedidoCosmos (output binding a Cosmos)
        //   - MensajeCola (output binding a Queue)
        // Cuando los 3 son no-null, Functions materializa los outputs.
        var fn = TestHost.NewCrearPedido();
        var req = WithJsonBody(new CrearPedidoDto("cliente-A", 250m, "test"));

        var result = await fn.CrearPedido(req);

        var http = Assert.IsType<CreatedResult>(result.HttpResponse);
        Assert.Equal(201, http.StatusCode);

        Assert.NotNull(result.PedidoCosmos);
        Assert.Equal("cliente-A", result.PedidoCosmos!.ClienteId);
        Assert.Equal(250m, result.PedidoCosmos.Total);
        Assert.False(string.IsNullOrEmpty(result.PedidoCosmos.Id));

        Assert.NotNull(result.MensajeCola);
        // El mensaje de cola lleva el id del pedido para que el consumer
        // pueda correlacionarlo.
        Assert.Contains(result.PedidoCosmos.Id, result.MensajeCola);
        Assert.Contains("cliente-A", result.MensajeCola);
    }

    [Fact]
    public async Task CrearPedido_Body_Invalido_Devuelve_400_Y_NO_Materializa_Cosmos_Ni_Cola()
    {
        // Slide 24 — validar ANTES del output. Si Cosmos/Cola son null,
        // los bindings NO se materializan (no escribimos basura).
        var fn = TestHost.NewCrearPedido();
        var req = WithJsonBody(new CrearPedidoDto("", -1m, null));

        var result = await fn.CrearPedido(req);

        var http = Assert.IsType<BadRequestObjectResult>(result.HttpResponse);
        Assert.Equal(400, http.StatusCode);

        Assert.Null(result.PedidoCosmos);
        Assert.Null(result.MensajeCola);
    }

    [Fact]
    public async Task CrearPedido_Body_Json_Malformado_Devuelve_400_Sin_Crash()
    {
        // Slide 21 anti-pattern — no dejamos que Functions deserialice
        // directo al parámetro; capturamos JsonException y devolvemos 400
        // con el detalle. Sin esto, un body malformado tiraría una excepción
        // genérica que se transformaría en 500.
        var fn = TestHost.NewCrearPedido();
        var req = WithJsonBody("{ totally not valid json");

        var result = await fn.CrearPedido(req);

        var http = Assert.IsType<BadRequestObjectResult>(result.HttpResponse);
        Assert.Equal(400, http.StatusCode);
        Assert.Null(result.PedidoCosmos);
        Assert.Null(result.MensajeCola);
    }

    [Fact]
    public async Task CrearPedido_Pedido_Cosmos_Y_Mensaje_Cola_Correlacionan_Por_Id()
    {
        // El id es el mismo en el documento de Cosmos y en el mensaje de
        // la cola. Esto es lo que permite que el consumer de la cola
        // pueda hacer follow-up sobre el documento.
        var fn = TestHost.NewCrearPedido();
        var req = WithJsonBody(new CrearPedidoDto("cliente-A", 99m, null));

        var result = await fn.CrearPedido(req);

        Assert.NotNull(result.PedidoCosmos);
        Assert.NotNull(result.MensajeCola);

        using var doc = JsonDocument.Parse(result.MensajeCola!);
        var pedidoIdEnCola = doc.RootElement.GetProperty("pedidoId").GetString();
        Assert.Equal(result.PedidoCosmos!.Id, pedidoIdEnCola);
    }
}
