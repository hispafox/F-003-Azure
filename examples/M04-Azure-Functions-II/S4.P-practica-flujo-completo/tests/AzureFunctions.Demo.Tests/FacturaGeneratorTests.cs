using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

public class FacturaGeneratorTests
{
    private readonly FacturaGenerator _sut = new();

    private static Pedido Pedido(decimal total = 1000m) => new()
    {
        Id = "12345678-aaaa-bbbb-cccc-ddddeeeeffff",
        ClienteId = "cli-1",
        ClienteNombre = "Pedro",
        Total = total,
        CreadoEn = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero),
    };

    [Fact]
    public void Generar_Calcula_IVA_21_Y_Total_Con_IVA()
    {
        var f = _sut.Generar(Pedido(1000m));

        Assert.Equal(210m, f.Iva);            // 21% de 1000
        Assert.Equal(1210m, f.TotalConIva);
        Assert.Equal("cli-1", f.ClienteId);
    }

    [Fact]
    public void Generar_Numero_Lleva_Fecha_Y_Prefijo_Del_Id()
    {
        var f = _sut.Generar(Pedido());
        Assert.StartsWith("FAC-20260515-12345678", f.Numero);
    }

    [Fact]
    public void Generar_Redondea_IVA_A_2_Decimales()
    {
        var f = _sut.Generar(Pedido(33.33m));
        Assert.Equal(7.00m, f.Iva);           // 33.33*0.21 = 6.9993 → 7.00
    }

    [Fact]
    public void SerializarFactura_Produce_JSON_CamelCase_Con_Campos_Clave()
    {
        var f = _sut.Generar(Pedido(500m));
        var json = _sut.SerializarFactura(f);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(f.Numero, doc.RootElement.GetProperty("numero").GetString());
        Assert.Equal(105m, doc.RootElement.GetProperty("iva").GetDecimal());
        Assert.Equal(605m, doc.RootElement.GetProperty("totalConIva").GetDecimal());
    }

    [Fact]
    public void SerializarMensaje_Lleva_PedidoId_Y_Numero()
    {
        var f = _sut.Generar(Pedido());
        var json = _sut.SerializarMensaje(f);

        var msg = JsonSerializer.Deserialize<MensajeFactura>(
            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(f.PedidoId, msg!.PedidoId);
        Assert.Equal(f.Numero, msg.FacturaNumero);
    }
}
