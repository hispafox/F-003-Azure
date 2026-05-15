using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

public class PedidosHandlerTests
{
    private readonly PedidosHandler _sut = new();

    [Fact]
    public void ValidarYConstruir_Con_Dto_Valido_Devuelve_Pedido()
    {
        var dto = new CrearPedidoDto("cliente-A", 150m, "demo");

        var (errores, pedido) = _sut.ValidarYConstruir(dto);

        Assert.Empty(errores);
        Assert.NotNull(pedido);
        Assert.Equal("cliente-A", pedido!.ClienteId);
        Assert.Equal(150m, pedido.Total);
        Assert.Equal("nuevo", pedido.Estado);
        Assert.False(string.IsNullOrEmpty(pedido.Id));
        Assert.NotEqual(default, pedido.CreadoEn);
    }

    [Fact]
    public void ValidarYConstruir_Con_Null_Devuelve_Error_De_Body()
    {
        var (errores, pedido) = _sut.ValidarYConstruir(null);

        Assert.Null(pedido);
        Assert.Single(errores);
        Assert.Equal("body", errores[0].Campo);
    }

    [Fact]
    public void ValidarYConstruir_Con_ClienteId_Vacio_Devuelve_Error()
    {
        var (errores, pedido) = _sut.ValidarYConstruir(new CrearPedidoDto("", 10m, null));

        Assert.Null(pedido);
        Assert.Contains(errores, e => e.Campo == "ClienteId");
    }

    [Fact]
    public void ValidarYConstruir_Con_ClienteId_Demasiado_Corto_Devuelve_Error()
    {
        var (errores, pedido) = _sut.ValidarYConstruir(new CrearPedidoDto("X", 10m, null));

        Assert.Null(pedido);
        Assert.Contains(errores, e => e.Campo == "ClienteId");
    }

    [Fact]
    public void ValidarYConstruir_Con_Total_Cero_O_Negativo_Devuelve_Error()
    {
        var (errores0, _) = _sut.ValidarYConstruir(new CrearPedidoDto("cliente-A", 0m, null));
        var (erroresNeg, _) = _sut.ValidarYConstruir(new CrearPedidoDto("cliente-A", -5m, null));

        Assert.Contains(errores0, e => e.Campo == "Total");
        Assert.Contains(erroresNeg, e => e.Campo == "Total");
    }

    [Fact]
    public void ValidarYConstruir_Con_Notas_Muy_Largas_Devuelve_Error()
    {
        var notasLargas = new string('a', 501);
        var (errores, pedido) = _sut.ValidarYConstruir(new CrearPedidoDto("cliente-A", 10m, notasLargas));

        Assert.Null(pedido);
        Assert.Contains(errores, e => e.Campo == "Notas");
    }

    [Fact]
    public void ValidarYConstruir_Acumula_Multiples_Errores()
    {
        var (errores, pedido) = _sut.ValidarYConstruir(new CrearPedidoDto("", -1m, null));

        Assert.Null(pedido);
        Assert.True(errores.Count >= 2);
        Assert.Contains(errores, e => e.Campo == "ClienteId");
        Assert.Contains(errores, e => e.Campo == "Total");
    }

    [Fact]
    public void ValidarYConstruir_Genera_Id_Unico_Por_Llamada()
    {
        var dto = new CrearPedidoDto("cliente-A", 10m, null);
        var (_, p1) = _sut.ValidarYConstruir(dto);
        var (_, p2) = _sut.ValidarYConstruir(dto);

        Assert.NotEqual(p1!.Id, p2!.Id);
    }
}
