using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// PASO 3 — Queue trigger. Recibe el mensaje que escribió el paso 2 y
// "notifica" (en prod: email). Cierra el flujo end-to-end.
public sealed class NotificarFacturaFunction
{
    private readonly IFlujoTracker _tracker;
    private readonly ILogger<NotificarFacturaFunction> _logger;

    public NotificarFacturaFunction(
        IFlujoTracker tracker,
        ILogger<NotificarFacturaFunction> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    [Function(nameof(NotificarFacturaGenerada))]
    public void NotificarFacturaGenerada(
        [QueueTrigger("facturas-generadas", Connection = "AzureWebJobsStorage")]
        string mensajeJson)
    {
        Procesar(mensajeJson);
    }

    // Handler puro para tests.
    internal MensajeFactura? Procesar(string mensajeJson)
    {
        MensajeFactura? msg;
        try
        {
            msg = JsonSerializer.Deserialize<MensajeFactura>(
                mensajeJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Mensaje de cola malformado: {Raw}", mensajeJson);
            return null;
        }

        if (msg is null || string.IsNullOrEmpty(msg.PedidoId)) return null;

        _logger.LogInformation(
            "=== NOTIFICACIÓN === pedido={PedidoId} factura={Num} total={Total}€ con IVA",
            msg.PedidoId, msg.FacturaNumero, msg.TotalConIva);

        _tracker.Notificado(msg.PedidoId, msg.FacturaNumero);
        return msg;
    }
}
