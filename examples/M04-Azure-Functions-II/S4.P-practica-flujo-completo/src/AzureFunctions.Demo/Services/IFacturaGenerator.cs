using System.Text.Json;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Función 2 (Cosmos trigger) — generar la factura (IVA 21%) y el mensaje
// de cola. Lógica pura: el JSON que va a Blob y a Queue se construye aquí
// y se testea sin Storage.
public interface IFacturaGenerator
{
    Factura Generar(Pedido pedido);
    string SerializarFactura(Factura factura);
    string SerializarMensaje(Factura factura);
}

public sealed class FacturaGenerator : IFacturaGenerator
{
    private const decimal IvaRate = 0.21m;
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Factura Generar(Pedido pedido)
    {
        var iva = Math.Round(pedido.Total * IvaRate, 2);
        return new Factura(
            Numero: $"FAC-{pedido.CreadoEn:yyyyMMdd}-{Corto(pedido.Id)}",
            PedidoId: pedido.Id,
            ClienteId: pedido.ClienteId,
            ClienteNombre: pedido.ClienteNombre,
            Total: pedido.Total,
            Iva: iva,
            TotalConIva: pedido.Total + iva,
            FechaEmision: DateTimeOffset.UtcNow);
    }

    public string SerializarFactura(Factura f) => JsonSerializer.Serialize(f, Json);

    public string SerializarMensaje(Factura f) => JsonSerializer.Serialize(
        new MensajeFactura(f.PedidoId, f.Numero, f.TotalConIva), Json);

    private static string Corto(string id) =>
        id.Length >= 8 ? id[..8] : id;
}
