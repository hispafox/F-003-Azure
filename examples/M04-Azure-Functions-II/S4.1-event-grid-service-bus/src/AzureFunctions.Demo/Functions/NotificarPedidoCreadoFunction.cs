using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Trigger 2/3 de Service Bus — Topic + Subscription (slide 12).
//
// El mismo POST /api/pedidos publica al topic "pedidos-eventos". Esta
// función recibe los pedidos via la subscription "sub-notificaciones".
// Si añades otra subscription (slide 16: filtro SQL) reaccionará en paralelo
// sin que esta función lo note.
//
// Aceptamos el body como string (binding implícito) — sin acciones manuales,
// el runtime hace Complete automáticamente al terminar el método sin excepción.
public sealed class NotificarPedidoCreadoFunction
{
    private const string SuscripcionNombre = "sub-notificaciones";

    private readonly IEstadoTracker _tracker;
    private readonly ILogger<NotificarPedidoCreadoFunction> _logger;

    public NotificarPedidoCreadoFunction(
        IEstadoTracker tracker,
        ILogger<NotificarPedidoCreadoFunction> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    [Function(nameof(NotificarPedidoCreado))]
    public void NotificarPedidoCreado(
        [ServiceBusTrigger(
            "pedidos-eventos",
            SuscripcionNombre,
            Connection = "ServiceBusConnection")]
        string mensajeJson)
    {
        Procesar(mensajeJson);
    }

    // Handler puro para tests (slide 26 estrategia 1).
    internal Pedido? Procesar(string mensajeJson)
    {
        Pedido? pedido;
        try
        {
            pedido = JsonSerializer.Deserialize<Pedido>(mensajeJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (JsonException ex)
        {
            // En topic+subscription no tenemos las actions inyectadas
            // (binding simplificado a string), así que el runtime aplica
            // la política por defecto: reintenta y al fallar va al DLQ
            // de la subscription. Aquí solo logueamos para diagnóstico.
            _logger.LogError(ex, "Mensaje de topic malformado: {Raw}", mensajeJson);
            return null;
        }

        if (pedido is null || string.IsNullOrEmpty(pedido.Id)) return null;

        _logger.LogInformation(
            "Notificando pedido {Id} via topic.{Sub} cliente={Email}",
            pedido.Id, SuscripcionNombre, pedido.ClienteEmail);

        _tracker.NotificadoPorTopic(pedido.Id, SuscripcionNombre);
        return pedido;
    }
}
