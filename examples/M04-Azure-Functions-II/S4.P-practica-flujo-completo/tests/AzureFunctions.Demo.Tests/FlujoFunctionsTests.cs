using System.Text;
using System.Text.Json;
using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

public class FlujoFunctionsTests
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

    private static Pedido NuevoPedido(string id = "ped-1") => new()
    {
        Id = id, ClienteId = "c1", ClienteNombre = "Pedro",
        Total = 100m, Estado = "nuevo",
        Items = [new ItemPedido { ProductoId = "p", Nombre = "x", Cantidad = 1, PrecioUnitario = 100m }],
    };

    // ── PASO 1: CrearPedido (multi-output) ──

    [Fact]
    public async Task CrearPedido_Valido_Devuelve_201_Y_Documento_A_Cosmos()
    {
        var tracker = new InMemoryFlujoTracker();
        var fn = new CrearPedidoFunction(
            new PedidoFactory(), tracker, NullLogger<CrearPedidoFunction>.Instance);

        var r = await fn.CrearPedido(JsonReq(
            """{"clienteId":"c1","clienteNombre":"P","items":[{"productoId":"p","nombre":"x","cantidad":2,"precioUnitario":50}]}"""));

        Assert.IsType<CreatedResult>(r.Http);
        Assert.NotNull(r.PedidoCosmos);
        Assert.Equal(100m, r.PedidoCosmos!.Total);
        Assert.Equal(1, tracker.Snapshot().Creados);
    }

    [Fact]
    public async Task CrearPedido_Invalido_Devuelve_400_Sin_Documento()
    {
        var tracker = new InMemoryFlujoTracker();
        var fn = new CrearPedidoFunction(
            new PedidoFactory(), tracker, NullLogger<CrearPedidoFunction>.Instance);

        var r = await fn.CrearPedido(JsonReq("""{"clienteId":"","items":[]}"""));

        Assert.IsType<BadRequestObjectResult>(r.Http);
        Assert.Null(r.PedidoCosmos);
        Assert.Equal(0, tracker.Snapshot().Creados);
    }

    [Fact]
    public async Task CrearPedido_Json_Malformado_Devuelve_400()
    {
        var fn = new CrearPedidoFunction(
            new PedidoFactory(), new InMemoryFlujoTracker(),
            NullLogger<CrearPedidoFunction>.Instance);

        var r = await fn.CrearPedido(JsonReq("{ roto"));

        Assert.IsType<BadRequestObjectResult>(r.Http);
        Assert.Null(r.PedidoCosmos);
    }

    // ── PASO 2: ProcesarNuevosPedidos (idempotencia + estado) ──

    [Fact]
    public void Procesar_Pedido_Nuevo_Genera_Factura_Y_Mensaje()
    {
        var tracker = new InMemoryFlujoTracker();
        var fn = new ProcesarNuevosPedidosFunction(
            new FacturaGenerator(), tracker,
            NullLogger<ProcesarNuevosPedidosFunction>.Instance);

        var r = fn.Procesar([NuevoPedido()]);

        Assert.NotNull(r.FacturaJson);
        Assert.NotNull(r.MensajeCola);
        Assert.Equal(1, tracker.Snapshot().Facturados);
    }

    [Fact]
    public void Procesar_Mismo_Pedido_Dos_Veces_Solo_Factura_Una_Vez()
    {
        // Slide 11 — at-least-once: el Change Feed re-entrega el pedido.
        var tracker = new InMemoryFlujoTracker();
        var fn = new ProcesarNuevosPedidosFunction(
            new FacturaGenerator(), tracker,
            NullLogger<ProcesarNuevosPedidosFunction>.Instance);

        var r1 = fn.Procesar([NuevoPedido("ped-9")]);
        var r2 = fn.Procesar([NuevoPedido("ped-9")]);

        Assert.NotNull(r1.FacturaJson);
        Assert.Null(r2.FacturaJson);          // segundo intento: saltado
        Assert.Equal(1, tracker.Snapshot().Facturados);
    }

    [Fact]
    public void Procesar_Pedido_No_Nuevo_Se_Salta()
    {
        var tracker = new InMemoryFlujoTracker();
        var fn = new ProcesarNuevosPedidosFunction(
            new FacturaGenerator(), tracker,
            NullLogger<ProcesarNuevosPedidosFunction>.Instance);
        var p = NuevoPedido();
        p.Estado = "facturado";

        var r = fn.Procesar([p]);

        Assert.Null(r.FacturaJson);
        Assert.Equal(0, tracker.Snapshot().Facturados);
    }

    [Fact]
    public void Procesar_Batch_Null_O_Vacio_Es_Noop()
    {
        var fn = new ProcesarNuevosPedidosFunction(
            new FacturaGenerator(), new InMemoryFlujoTracker(),
            NullLogger<ProcesarNuevosPedidosFunction>.Instance);

        Assert.Null(fn.Procesar(null).FacturaJson);
        Assert.Null(fn.Procesar([]).FacturaJson);
    }

    // ── PASO 3: NotificarFactura ──

    [Fact]
    public void Notificar_Mensaje_Valido_Registra_Notificacion()
    {
        var tracker = new InMemoryFlujoTracker();
        var fn = new NotificarFacturaFunction(
            tracker, NullLogger<NotificarFacturaFunction>.Instance);
        var msg = JsonSerializer.Serialize(
            new MensajeFactura("ped-1", "FAC-1", 121m));

        var r = fn.Procesar(msg);

        Assert.NotNull(r);
        Assert.Equal("ped-1", r!.PedidoId);
        Assert.Equal(1, tracker.Snapshot().Notificados);
    }

    [Fact]
    public void Notificar_Mensaje_Malformado_Devuelve_Null()
    {
        var fn = new NotificarFacturaFunction(
            new InMemoryFlujoTracker(), NullLogger<NotificarFacturaFunction>.Instance);

        Assert.Null(fn.Procesar("{ roto"));
    }

    // ── Inspección end-to-end ──

    [Fact]
    public void Estado_Refleja_Los_3_Saltos()
    {
        var tracker = new InMemoryFlujoTracker();
        tracker.PedidoCreado("p1");
        tracker.TryMarcarFacturado("p1");
        tracker.Notificado("p1", "FAC-1");

        var fn = new EstadoFunction(tracker);
        var ok = fn.Estado(new DefaultHttpContext().Request) as OkObjectResult;

        var s = Assert.IsType<FlujoSnapshot>(ok!.Value);
        Assert.Equal(1, s.Creados);
        Assert.Equal(1, s.Facturados);
        Assert.Equal(1, s.Notificados);
    }
}
