using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

public class PedidoFactoryTests
{
    private readonly PedidoFactory _sut = new();

    private static CrearPedidoDto Dto(params (int cant, decimal precio)[] items) => new(
        "cli-1", "Pedro",
        items.Select((x, i) => new ItemPedidoDto($"p{i}", $"item{i}", x.cant, x.precio)).ToList());

    [Fact]
    public void Crear_Calcula_El_Total_Sumando_Items()
    {
        var (errores, pedido) = _sut.Crear(Dto((1, 999.99m), (2, 29.99m)));

        Assert.Empty(errores);
        Assert.NotNull(pedido);
        Assert.Equal(1059.97m, pedido!.Total);   // 999.99 + 2*29.99
        Assert.Equal("nuevo", pedido.Estado);
        Assert.Equal(2, pedido.Items.Count);
    }

    [Fact]
    public void Crear_Null_Devuelve_Error()
    {
        var (errores, pedido) = _sut.Crear(null);
        Assert.Null(pedido);
        Assert.NotEmpty(errores);
    }

    [Fact]
    public void Crear_Sin_Items_Devuelve_Error()
    {
        var (errores, pedido) = _sut.Crear(new CrearPedidoDto("c", "n", []));
        Assert.Null(pedido);
        Assert.Contains(errores, e => e.Contains("items"));
    }

    [Fact]
    public void Crear_Sin_ClienteId_Devuelve_Error()
    {
        var (errores, pedido) = _sut.Crear(new CrearPedidoDto("", "n",
            [new ItemPedidoDto("p", "x", 1, 1)]));
        Assert.Null(pedido);
        Assert.Contains(errores, e => e.Contains("ClienteId"));
    }

    [Fact]
    public void Crear_Item_Con_Cantidad_Cero_Devuelve_Error()
    {
        var (errores, pedido) = _sut.Crear(Dto((0, 10m)));
        Assert.Null(pedido);
        Assert.NotEmpty(errores);
    }

    [Fact]
    public void Crear_Genera_Id_Unico_Por_Pedido()
    {
        var (_, p1) = _sut.Crear(Dto((1, 10m)));
        var (_, p2) = _sut.Crear(Dto((1, 10m)));
        Assert.NotEqual(p1!.Id, p2!.Id);
    }
}
