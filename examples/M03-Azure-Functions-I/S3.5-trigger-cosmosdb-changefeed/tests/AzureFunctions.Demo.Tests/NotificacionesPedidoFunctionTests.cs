using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Tests;

public class NotificacionesPedidoFunctionTests
{
    private static Pedido P(string id, string clienteId, string estado, decimal total = 100m, long ts = 1700000000)
        => new() { Id = id, ClienteId = clienteId, Estado = estado, Total = total, Timestamp = ts };

    [Fact]
    public void Procesar_Envia_Una_Notificacion_Por_Pedido_Confirmado()
    {
        var (fn, notificaciones) = TestHost.NewNotificaciones();

        var enviadas = fn.Procesar(new[]
        {
            P("ped-1", "cliente-1", "confirmado", 250m),
            P("ped-2", "cliente-2", "enviado"),
            P("ped-3", "cliente-3", "entregado"),
        });

        Assert.Equal(3, enviadas);
        Assert.Equal(3, notificaciones.Total);

        var n1 = notificaciones.Buscar("ped-1", "confirmado");
        Assert.NotNull(n1);
        Assert.Contains("250", n1!.Mensaje);
    }

    [Fact]
    public void Procesar_Ignora_Estados_Sin_Notificacion()
    {
        var (fn, notificaciones) = TestHost.NewNotificaciones();

        var enviadas = fn.Procesar(new[]
        {
            P("ped-1", "cliente-1", "pendiente"),
            P("ped-2", "cliente-1", "en-preparacion"),
            P("ped-3", "cliente-1", "confirmado"),
        });

        Assert.Equal(1, enviadas);
        Assert.Equal(1, notificaciones.Total);
        Assert.NotNull(notificaciones.Buscar("ped-3", "confirmado"));
    }

    [Fact]
    public void Procesar_Es_Idempotente_Sobre_El_Mismo_Batch()
    {
        // Slide 10 — at-least-once: el Change Feed puede entregar el
        // mismo cambio dos veces. Procesar el mismo batch dos veces
        // produce el mismo número de notificaciones que una sola vez.
        var (fn, notificaciones) = TestHost.NewNotificaciones();
        var batch = new[]
        {
            P("ped-1", "cliente-1", "confirmado"),
            P("ped-2", "cliente-2", "enviado"),
        };

        var enviadasPrimera = fn.Procesar(batch);
        var enviadasSegunda = fn.Procesar(batch);

        Assert.Equal(2, enviadasPrimera);
        Assert.Equal(0, enviadasSegunda);
        Assert.Equal(2, notificaciones.Total);
    }

    [Fact]
    public void Procesar_Envia_Notificacion_Distinta_Para_Cada_Cambio_De_Estado()
    {
        // El mismo pedido pasando por confirmado → enviado → entregado
        // genera tres notificaciones (clave (PedidoId, Estado)).
        var (fn, notificaciones) = TestHost.NewNotificaciones();

        fn.Procesar(new[] { P("ped-1", "cliente-1", "confirmado") });
        fn.Procesar(new[] { P("ped-1", "cliente-1", "enviado") });
        fn.Procesar(new[] { P("ped-1", "cliente-1", "entregado") });

        Assert.Equal(3, notificaciones.Total);
        Assert.NotNull(notificaciones.Buscar("ped-1", "confirmado"));
        Assert.NotNull(notificaciones.Buscar("ped-1", "enviado"));
        Assert.NotNull(notificaciones.Buscar("ped-1", "entregado"));
    }

    [Fact]
    public void Procesar_Estado_Cancelado_Genera_Notificacion()
    {
        var (fn, notificaciones) = TestHost.NewNotificaciones();

        var enviadas = fn.Procesar(new[] { P("ped-9", "cliente-9", "cancelado") });

        Assert.Equal(1, enviadas);
        var n = notificaciones.Buscar("ped-9", "cancelado");
        Assert.NotNull(n);
        Assert.Contains("cancelado", n!.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Procesar_Estado_En_Mayusculas_Tambien_Notifica()
    {
        // Estados podrían venir variados desde el productor; el matching
        // por estado lo hacemos case-insensitive para no perder eventos.
        var (fn, notificaciones) = TestHost.NewNotificaciones();

        var enviadas = fn.Procesar(new[] { P("ped-1", "cliente-1", "CONFIRMADO") });

        Assert.Equal(1, enviadas);
        Assert.Equal(1, notificaciones.Total);
    }

    [Fact]
    public void Procesar_Batch_Vacio_O_Null_Es_Noop()
    {
        var (fn, notificaciones) = TestHost.NewNotificaciones();

        Assert.Equal(0, fn.Procesar(null));
        Assert.Equal(0, fn.Procesar(Array.Empty<Pedido>()));
        Assert.Equal(0, notificaciones.Total);
    }

    [Fact]
    public void Procesar_Maneja_Pedidos_Con_Datos_Invalidos_Sin_Abortar_Batch()
    {
        // Slide 12 — un pedido con ClienteId vacío hace que
        // EnviarSiNoEnviada lance, pero el batch debe continuar y
        // procesar el resto. La función NO relanza la excepción.
        var (fn, notificaciones) = TestHost.NewNotificaciones();

        var enviadas = fn.Procesar(new[]
        {
            P("ped-1", "", "confirmado"),       // clienteId vacío → lanza, se captura
            P("ped-2", "cliente-2", "enviado"), // este se procesa
        });

        Assert.Equal(1, enviadas);
        Assert.Equal(1, notificaciones.Total);
        Assert.NotNull(notificaciones.Buscar("ped-2", "enviado"));
    }
}
