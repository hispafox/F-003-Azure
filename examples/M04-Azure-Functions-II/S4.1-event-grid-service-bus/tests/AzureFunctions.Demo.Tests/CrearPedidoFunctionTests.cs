using System.Text;
using System.Text.Json;
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

    private static HttpRequest WithJsonBody<T>(T body) =>
        WithJsonBody(JsonSerializer.Serialize(body));

    [Fact]
    public async Task CrearPedido_Body_Valido_Devuelve_202_Y_Encola_A_Queue_Y_Topic()
    {
        // Slide 13 — el HTTP devuelve 202 Accepted (no espera al
        // procesamiento) y al mismo tiempo materializa los outputs a
        // la Queue Y al Topic. El mismo mensaje viaja a ambos destinos.
        var (fn, tracker) = TestHost.NewCrearPedido();
        var req = WithJsonBody(new CrearPedidoDto("cliente-A", "alice@example.com", 250m, "demo"));

        var result = await fn.CrearPedido(req);

        Assert.IsType<AcceptedResult>(result.HttpResponse);
        Assert.NotNull(result.MensajeCola);
        Assert.NotNull(result.MensajeTopic);
        Assert.Equal(result.MensajeCola, result.MensajeTopic);

        // El estado refleja que se encoló.
        Assert.Equal(1, tracker.Snapshot().Encolados);
    }

    [Fact]
    public async Task CrearPedido_Body_Invalido_Devuelve_400_Sin_Encolar()
    {
        // Slide 24 (S3.6) — validar ANTES del output. Si la validación falla,
        // los outputs son null y NO se materializan.
        var (fn, tracker) = TestHost.NewCrearPedido();
        var req = WithJsonBody(new CrearPedidoDto("", "no-email", -1m, null));

        var result = await fn.CrearPedido(req);

        Assert.IsType<BadRequestObjectResult>(result.HttpResponse);
        Assert.Null(result.MensajeCola);
        Assert.Null(result.MensajeTopic);
        Assert.Equal(0, tracker.Snapshot().Encolados);
    }

    [Fact]
    public async Task CrearPedido_Body_Malformado_Devuelve_400_Sin_Crash()
    {
        var (fn, _) = TestHost.NewCrearPedido();
        var req = WithJsonBody("{ broken json");

        var result = await fn.CrearPedido(req);

        Assert.IsType<BadRequestObjectResult>(result.HttpResponse);
        Assert.Null(result.MensajeCola);
        Assert.Null(result.MensajeTopic);
    }

    [Fact]
    public async Task CrearPedido_El_Mensaje_Lleva_El_Pedido_Completo()
    {
        // El mensaje SB lleva la entidad serializada; cualquier consumer
        // puede reconstruir el Pedido sin volver a la BD.
        var (fn, _) = TestHost.NewCrearPedido();
        var req = WithJsonBody(new CrearPedidoDto("c-A", "a@b.c", 99m, "n"));

        var result = await fn.CrearPedido(req);

        Assert.NotNull(result.MensajeCola);
        using var doc = JsonDocument.Parse(result.MensajeCola!);
        Assert.Equal("c-A", doc.RootElement.GetProperty("clienteId").GetString());
        Assert.Equal("a@b.c", doc.RootElement.GetProperty("clienteEmail").GetString());
        Assert.Equal(99m, doc.RootElement.GetProperty("total").GetDecimal());
    }
}
