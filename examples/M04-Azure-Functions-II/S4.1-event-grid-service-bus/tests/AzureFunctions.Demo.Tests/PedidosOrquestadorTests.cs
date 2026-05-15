using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

public class PedidosOrquestadorTests
{
    private readonly PedidosOrquestador _sut = new();

    [Fact]
    public void ValidarYPreparar_Con_Dto_Valido_Devuelve_Pedido_Y_Mensaje_Serializado()
    {
        var dto = new CrearPedidoDto("cliente-A", "alice@example.com", 100m, "demo");

        var (errores, pedido, mensaje) = _sut.ValidarYPreparar(dto);

        Assert.Empty(errores);
        Assert.NotNull(pedido);
        Assert.NotNull(mensaje);
        Assert.False(string.IsNullOrEmpty(pedido!.Id));
        Assert.Equal("alice@example.com", pedido.ClienteEmail);

        // El mensaje se serializa con camelCase para que cualquier consumer
        // (otro Function App, Logic App, app externa) lo entienda.
        using var doc = JsonDocument.Parse(mensaje!);
        Assert.Equal(pedido.Id, doc.RootElement.GetProperty("id").GetString());
        Assert.Equal("alice@example.com", doc.RootElement.GetProperty("clienteEmail").GetString());
    }

    [Fact]
    public void ValidarYPreparar_Con_Null_Acumula_Error_Body()
    {
        var (errores, pedido, mensaje) = _sut.ValidarYPreparar(null);

        Assert.Null(pedido);
        Assert.Null(mensaje);
        Assert.Contains("Body", errores[0]);
    }

    [Fact]
    public void ValidarYPreparar_Sin_ClienteId_Devuelve_Error()
    {
        var (errores, pedido, _) = _sut.ValidarYPreparar(
            new CrearPedidoDto("", "a@b.c", 1m, null));

        Assert.Null(pedido);
        Assert.Contains(errores, e => e.Contains("ClienteId"));
    }

    [Fact]
    public void ValidarYPreparar_Con_Email_Sin_Arroba_Devuelve_Error()
    {
        var (errores, pedido, _) = _sut.ValidarYPreparar(
            new CrearPedidoDto("c1", "no-email", 1m, null));

        Assert.Null(pedido);
        Assert.Contains(errores, e => e.Contains("Email"));
    }

    [Fact]
    public void ValidarYPreparar_Con_Total_No_Positivo_Devuelve_Error()
    {
        var (errores, _, _) = _sut.ValidarYPreparar(
            new CrearPedidoDto("c1", "a@b.c", 0m, null));

        Assert.Contains(errores, e => e.Contains("Total"));
    }

    [Fact]
    public void ValidarYPreparar_Acumula_Multiples_Errores()
    {
        var (errores, pedido, _) = _sut.ValidarYPreparar(
            new CrearPedidoDto("", "", -1m, null));

        Assert.Null(pedido);
        Assert.True(errores.Count >= 3); // ClienteId + Email + Total
    }

    [Fact]
    public void ValidarYPreparar_Genera_Id_Unico_Por_Llamada()
    {
        var dto = new CrearPedidoDto("c1", "a@b.c", 10m, null);
        var (_, p1, _) = _sut.ValidarYPreparar(dto);
        var (_, p2, _) = _sut.ValidarYPreparar(dto);

        Assert.NotEqual(p1!.Id, p2!.Id);
    }
}
