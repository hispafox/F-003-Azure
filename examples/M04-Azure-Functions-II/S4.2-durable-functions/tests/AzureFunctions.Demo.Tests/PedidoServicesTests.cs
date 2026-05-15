using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

public class PedidoServicesTests
{
    private static Pedido Pedido(decimal total = 100m, string? clienteId = "c-A") => new(
        Id: "ped-1",
        ClienteId: clienteId ?? "",
        ClienteEmail: "a@b.c",
        Total: total,
        Items: [new LineaPedido("SKU-1", 1, total)]);

    // ── Validador ──

    [Fact]
    public void Validador_Pedido_Valido_No_Lanza()
    {
        var v = new PedidoValidador();
        v.Validar(Pedido()); // no exception
    }

    [Fact]
    public void Validador_Sin_ClienteId_Lanza()
    {
        var v = new PedidoValidador();
        Assert.Throws<InvalidOperationException>(() => v.Validar(Pedido(clienteId: "")));
    }

    [Fact]
    public void Validador_Sin_Items_Lanza()
    {
        var v = new PedidoValidador();
        var sinItems = Pedido() with { Items = [] };
        Assert.Throws<InvalidOperationException>(() => v.Validar(sinItems));
    }

    [Fact]
    public void Validador_Total_No_Positivo_Lanza()
    {
        var v = new PedidoValidador();
        Assert.Throws<InvalidOperationException>(() => v.Validar(Pedido(total: 0)));
    }

    // ── Inventario (reservar / liberar = compensación) ──

    [Fact]
    public void Inventario_Reservar_Devuelve_Reserva_Confirmada()
    {
        var inv = new InMemoryInventarioService(NullLogger<InMemoryInventarioService>.Instance);

        var r = inv.Reservar(Pedido());

        Assert.True(r.Confirmada);
        Assert.True(inv.ExisteReserva(r.ReservaId));
    }

    [Fact]
    public void Inventario_Liberar_Elimina_La_Reserva()
    {
        var inv = new InMemoryInventarioService(NullLogger<InMemoryInventarioService>.Instance);
        var r = inv.Reservar(Pedido());

        inv.Liberar(r.ReservaId);

        Assert.False(inv.ExisteReserva(r.ReservaId));
    }

    [Fact]
    public void Inventario_Liberar_Es_Idempotente()
    {
        // La compensación de la saga puede ejecutarse más de una vez
        // (reintentos). Liberar dos veces no debe romper.
        var inv = new InMemoryInventarioService(NullLogger<InMemoryInventarioService>.Instance);
        var r = inv.Reservar(Pedido());

        inv.Liberar(r.ReservaId);
        inv.Liberar(r.ReservaId); // no exception
    }

    // ── Pago (el fallo determinista alimenta la saga) ──

    [Fact]
    public void Pago_Total_Normal_Devuelve_Pago_Exitoso()
    {
        var pago = new InMemoryPagoService(NullLogger<InMemoryPagoService>.Instance);
        var reserva = new Reserva("r1", "ped-1", true);

        var p = pago.Cobrar(Pedido(total: 100m), reserva);

        Assert.True(p.Exito);
        Assert.False(string.IsNullOrEmpty(p.TransaccionId));
    }

    [Fact]
    public void Pago_Total_Terminado_En_99_Es_Rechazado()
    {
        // Regla determinista para forzar la compensación en demos/tests.
        var pago = new InMemoryPagoService(NullLogger<InMemoryPagoService>.Instance);
        var reserva = new Reserva("r1", "ped-1", true);

        Assert.Throws<PagoRechazadoException>(
            () => pago.Cobrar(Pedido(total: 99.99m), reserva));
    }

    [Fact]
    public void Pago_Reserva_No_Confirmada_Es_Rechazado()
    {
        var pago = new InMemoryPagoService(NullLogger<InMemoryPagoService>.Instance);
        var reserva = new Reserva("r1", "ped-1", Confirmada: false);

        Assert.Throws<PagoRechazadoException>(
            () => pago.Cobrar(Pedido(), reserva));
    }

    // ── Facturación (unidad del fan-out) ──

    [Fact]
    public void Facturacion_Importe_Positivo_Es_Exito()
    {
        var f = new InMemoryFacturacionService(NullLogger<InMemoryFacturacionService>.Instance);

        var r = f.Procesar(new Factura("f1", "c1", 100m));

        Assert.True(r.Exito);
        Assert.Equal(100m, r.Importe);
    }

    [Fact]
    public void Facturacion_Importe_No_Positivo_Es_Fallo()
    {
        var f = new InMemoryFacturacionService(NullLogger<InMemoryFacturacionService>.Instance);

        var r = f.Procesar(new Factura("f2", "c1", 0m));

        Assert.False(r.Exito);
        Assert.Equal(0m, r.Importe);
        Assert.NotNull(r.Error);
    }
}
