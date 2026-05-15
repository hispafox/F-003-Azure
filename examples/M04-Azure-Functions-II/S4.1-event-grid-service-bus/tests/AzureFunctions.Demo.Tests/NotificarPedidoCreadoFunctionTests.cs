using System.Text.Json;

namespace AzureFunctions.Demo.Tests;

public class NotificarPedidoCreadoFunctionTests
{
    [Fact]
    public void Procesar_Mensaje_Valido_Anota_Notificacion()
    {
        var (fn, tracker) = TestHost.NewNotificar();
        var json = JsonSerializer.Serialize(new
        {
            id = "ped-1",
            clienteId = "c",
            clienteEmail = "a@b.c",
            total = 50m,
        });

        var pedido = fn.Procesar(json);

        Assert.NotNull(pedido);
        Assert.Equal("ped-1", pedido!.Id);
        Assert.Equal(1, tracker.Snapshot().Notificaciones);
    }

    [Fact]
    public void Procesar_Mensaje_Malformado_Devuelve_Null_Sin_Anotar()
    {
        var (fn, tracker) = TestHost.NewNotificar();

        var pedido = fn.Procesar("{ totally broken");

        Assert.Null(pedido);
        Assert.Equal(0, tracker.Snapshot().Notificaciones);
    }

    [Fact]
    public void Procesar_Mensaje_Sin_Id_Devuelve_Null()
    {
        var (fn, tracker) = TestHost.NewNotificar();
        var json = JsonSerializer.Serialize(new { id = "", clienteId = "c" });

        var pedido = fn.Procesar(json);

        Assert.Null(pedido);
        Assert.Equal(0, tracker.Snapshot().Notificaciones);
    }
}
