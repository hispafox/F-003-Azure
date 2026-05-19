namespace Messaging.Demo.Api.Messaging;

// Slides 16, 17 y 32 — árbol de decisión: qué servicio de mensajería
// usar según el escenario. Más slide 9/30/31 — clasificación de DLQ.
// Tablas de decisión puras (sin broker), citando las slides.

public enum TipoMensaje { Comando, EventoNegocio, Streaming }

public enum ServicioMensajeria
{
    StorageQueue,
    ServiceBusQueue,
    ServiceBusTopic,
    ServiceBusPremium,
    EventGrid,
    EventHubs,
}

public sealed record EscenarioMensajeria(
    TipoMensaje Tipo,
    bool RequiereFifo = false,
    int TamanoMensajeKb = 64,
    bool PushAWebhook = false,
    bool RequiereReplay = false,
    bool FanOutMultiplesSuscriptores = false,
    bool RequiereVNet = false,
    long OperacionesMes = 100_000);

public sealed record RecomendacionServicio(
    ServicioMensajeria Servicio,
    string CosteAproximado,
    IReadOnlyList<string> Razones);

public static class MessagingServiceAdvisor
{
    // Coste aproximado 2026 (slides 17/23/32).
    private static string Coste(ServicioMensajeria s) => s switch
    {
        ServicioMensajeria.StorageQueue => "~0 € (0,36 €/millón de operaciones)",
        ServicioMensajeria.ServiceBusQueue or ServicioMensajeria.ServiceBusTopic
            => "~10 €/mes (Standard · 0,05 €/millón de operaciones)",
        ServicioMensajeria.ServiceBusPremium => "€600+/mes (1 messaging unit)",
        ServicioMensajeria.EventGrid => "0,60 €/millón de eventos",
        ServicioMensajeria.EventHubs => "~11 €/TU/mes",
        _ => "n/d",
    };

    // Árbol de decisión de la slide 32, evaluado por prioridad.
    public static RecomendacionServicio Recomendar(EscenarioMensajeria e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var razones = new List<string>();

        if (e.Tipo == TipoMensaje.Streaming || e.RequiereReplay)
        {
            razones.Add(e.Tipo == TipoMensaje.Streaming
                ? "Streaming de alto volumen → Event Hubs (slide 32)."
                : "Necesita reproducir eventos (replay) → Event Hubs retiene 7-90 días (slide 32).");
            return Crear(ServicioMensajeria.EventHubs, razones);
        }

        if (e.PushAWebhook)
        {
            razones.Add("Entrega push a webhook HTTP → Event Grid (slide 16/32).");
            return Crear(ServicioMensajeria.EventGrid, razones);
        }

        if (e.RequiereVNet || e.TamanoMensajeKb > 256)
        {
            if (e.RequiereVNet)
                razones.Add("VNet integration es obligatoria → solo Service Bus Premium la soporta de forma nativa (slide 23).");
            if (e.TamanoMensajeKb > 256)
                razones.Add($"Mensaje de {e.TamanoMensajeKb} KB > 256 KB → Service Bus Premium (hasta 100 MB) o patrón Claim Check (slide 25/32).");
            if (e.RequiereFifo)
                razones.Add("FIFO requerido → habilitar Sessions en la cola/suscripción (slide 26).");
            return Crear(ServicioMensajeria.ServiceBusPremium, razones);
        }

        if (e.FanOutMultiplesSuscriptores)
        {
            if (e.Tipo == TipoMensaje.EventoNegocio && (e.RequiereFifo || !e.PushAWebhook))
            {
                razones.Add("Fan-out de evento de negocio con garantías de entrega y DLQ por suscripción → Service Bus Topic (slide 3/16).");
                if (e.RequiereFifo)
                    razones.Add("FIFO requerido → Sessions en la suscripción (slide 26).");
                return Crear(ServicioMensajeria.ServiceBusTopic, razones);
            }
            razones.Add("Notificar a N suscriptores sin garantías estrictas → Event Grid (fan-out nativo, slide 16/32).");
            return Crear(ServicioMensajeria.EventGrid, razones);
        }

        if (e.RequiereFifo)
        {
            razones.Add("Orden FIFO requerido en una cola punto a punto → Service Bus Queue con Sessions (slide 26/32).");
            return Crear(ServicioMensajeria.ServiceBusQueue, razones);
        }

        if (e.Tipo == TipoMensaje.Comando && e.OperacionesMes < 1_000_000)
        {
            razones.Add("Comando simple, bajo volumen y sin necesidades avanzadas → Storage Queue (la opción más barata, slide 32).");
            return Crear(ServicioMensajeria.StorageQueue, razones);
        }

        razones.Add("Comunicación fiable entre servicios (comando/trabajo) → Service Bus Queue Standard (slide 16/17).");
        return Crear(ServicioMensajeria.ServiceBusQueue, razones);
    }

    private static RecomendacionServicio Crear(
        ServicioMensajeria s, List<string> razones) =>
        new(s, Coste(s), razones);

    // Slide 9/30 (ISSUE 2) / 31 — por qué un mensaje acabó en la DLQ y
    // qué hacer. La razón viene de ServiceBusReceivedMessage.DeadLetterReason.
    public static string ClasificarDeadLetter(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        var m = motivo.Trim();

        if (m.Contains("MaxDeliveryCount", StringComparison.OrdinalIgnoreCase))
            return "Se agotaron los reintentos: corregir la lógica de procesamiento (bug/dependencia caída) y reenviar el mensaje desde la DLQ.";
        if (m.Contains("TTL", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("Expired", StringComparison.OrdinalIgnoreCase))
            return "El consumidor no procesa a tiempo: escalar consumidores (Competing Consumers) o ampliar el TTL del mensaje.";
        if (m.Contains("HeaderSize", StringComparison.OrdinalIgnoreCase))
            return "Cabeceras demasiado grandes: reducir el tamaño de ApplicationProperties (mover datos al body o usar Claim Check).";
        if (m.Contains("Filter", StringComparison.OrdinalIgnoreCase) ||
            m.Contains("Rule", StringComparison.OrdinalIgnoreCase))
            return "Evaluación de filtro/regla de suscripción fallida: revisar la sintaxis del filtro SQL de la suscripción.";

        return "Inspeccionar DeadLetterReason y DeadLetterErrorDescription; corregir la causa raíz y reenviar a la cola principal (workflow de la slide 9).";
    }
}
