using System.Text.Json;
using Messaging.Demo.Api.Messaging;

namespace Messaging.Demo.Api.Endpoints;

public sealed record FiltroRequest(
    string Filtro, Dictionary<string, JsonElement> Propiedades);

public sealed record MensajeDto(string MessageId, double EncoladoSegundos);

public sealed record DedupRequest(
    int VentanaSegundos, List<MensajeDto> Mensajes);

public sealed record SuscripcionDto(string Nombre, string FiltroSql);

public sealed record PlanRequest(
    EscenarioMensajeria Escenario,
    string Topic,
    List<SuscripcionDto> Suscripciones,
    int VentanaDedupSegundos);

public static class MessagingEndpoints
{
    public static void MapMessaging(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var m = app.MapGroup("/messaging");

        // Slides 4-5 — evalúa un filtro SQL de suscripción contra las
        // ApplicationProperties de un mensaje (igual que el broker).
        m.MapPost("/filtro", (FiltroRequest req) =>
        {
            var props = req.Propiedades.ToDictionary(
                kv => kv.Key, kv => AJsonValor(kv.Value));
            bool entregado = SqlFilterEvaluator.Coincide(req.Filtro, props);
            return Results.Ok(new { req.Filtro, entregado });
        });

        // Slide 10 — deduplicación: qué mensajes entrega el broker y
        // cuáles descarta dentro de la ventana de detección.
        m.MapPost("/dedup", (DedupRequest req) =>
        {
            var ventana = TimeSpan.FromSeconds(req.VentanaSegundos);
            var mensajes = req.Mensajes.Select(x =>
                new MensajeEntrante(x.MessageId,
                    TimeSpan.FromSeconds(x.EncoladoSegundos)));
            return Results.Ok(MessageDeduplicator.Procesar(ventana, mensajes));
        });

        // Slides 16/17/32 — qué servicio de mensajería usar.
        m.MapGet("/recomendar", (
            TipoMensaje tipo,
            bool? fifo, int? tamanoKb, bool? push, bool? replay,
            bool? fanout, bool? vnet, long? opsMes) =>
            Results.Ok(MessagingServiceAdvisor.Recomendar(new EscenarioMensajeria(
                tipo,
                RequiereFifo: fifo ?? false,
                TamanoMensajeKb: tamanoKb ?? 64,
                PushAWebhook: push ?? false,
                RequiereReplay: replay ?? false,
                FanOutMultiplesSuscriptores: fanout ?? false,
                RequiereVNet: vnet ?? false,
                OperacionesMes: opsMes ?? 100_000))));

        // Slide 9/30/31 — por qué un mensaje cayó en la DLQ y qué hacer.
        m.MapGet("/dlq", (string motivo) =>
            Results.Ok(new
            {
                motivo,
                accion = MessagingServiceAdvisor.ClasificarDeadLetter(motivo),
            }));

        // Plan de despliegue + checklist del entregable (slide 19/31).
        m.MapPost("/plan", (PlanRequest req, IMessagingPlanner planner) =>
        {
            var subs = req.Suscripciones
                .Select(s => (s.Nombre, s.FiltroSql))
                .ToList()
                .AsReadOnly();
            var plan = planner.Planificar(
                req.Escenario, req.Topic, subs,
                TimeSpan.FromSeconds(req.VentanaDedupSegundos));
            return Results.Ok(plan);
        });
    }

    // Convierte el valor JSON crudo al tipo que espera el evaluador
    // (string / double / bool / null).
    private static object? AJsonValor(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => e.ToString(),
    };
}
