namespace Messaging.Demo.Api.Messaging;

public sealed record SuscripcionPlan(
    string Nombre, string FiltroSql, bool FiltroValido);

public sealed record PlanMensajeria(
    ServicioMensajeria ServicioRecomendado,
    string CosteAproximado,
    IReadOnlyList<string> Razones,
    string Topic,
    IReadOnlyList<SuscripcionPlan> Suscripciones,
    int VentanaDedupSegundos,
    int UmbralAlertaDlq,
    IReadOnlyList<string> Checklist);

// Compone SqlFilterEvaluator + MessageDeduplicator +
// MessagingServiceAdvisor en un plan de despliegue + checklist del
// entregable. Servicio inyectable (seam del test DI — lección M03-S3.4).
public interface IMessagingPlanner
{
    PlanMensajeria Planificar(
        EscenarioMensajeria escenario,
        string topic,
        IReadOnlyList<(string Nombre, string FiltroSql)> suscripciones,
        TimeSpan ventanaDedup);
}

public sealed class MessagingPlanner : IMessagingPlanner
{
    public PlanMensajeria Planificar(
        EscenarioMensajeria escenario,
        string topic,
        IReadOnlyList<(string Nombre, string FiltroSql)> suscripciones,
        TimeSpan ventanaDedup)
    {
        ArgumentNullException.ThrowIfNull(escenario);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(suscripciones);
        if (!MessageDeduplicator.VentanaValida(ventanaDedup))
            throw new ArgumentOutOfRangeException(nameof(ventanaDedup),
                "Fuera del rango de detección de duplicados de Service Bus (20 s – 7 días).");

        var rec = MessagingServiceAdvisor.Recomendar(escenario);

        var subs = suscripciones
            .Select(s => new SuscripcionPlan(
                s.Nombre, s.FiltroSql, FiltroCompila(s.FiltroSql)))
            .ToList();

        return new PlanMensajeria(
            rec.Servicio,
            rec.CosteAproximado,
            rec.Razones,
            topic,
            subs,
            (int)ventanaDedup.TotalSeconds,
            UmbralAlertaDlq: 10,                       // slide 19/31
            Checklist:
            [
                "ServiceBusClient registrado como singleton en DI (anti-pattern 1, slide 18/31)",
                "Managed Identity + RBAC; cero connection strings en config (anti-pattern 5, slide 31)",
                "Lock duration ≥ tiempo máx de procesamiento + margen, o AutoLockRenewal (anti-pattern 6, slide 20)",
                "Idempotencia: dedup por MessageId + store de estado (anti-pattern 8, slide 10)",
                "Alerta configurada si DLQ count > 10 (anti-pattern 3, slide 19)",
                "Filtros SQL evaluados en el broker, no en el código (slide 4)",
                "Cada suscripción tiene su propia DLQ; reenvío revisado (workflow slide 9)",
            ]);
    }

    // Un filtro es válido si el evaluador lo parsea sin lanzar
    // (slide 4 — la sintaxis SQL la valida Service Bus al crear la regla).
    private static bool FiltroCompila(string filtroSql)
    {
        try
        {
            SqlFilterEvaluator.Coincide(
                filtroSql, new Dictionary<string, object?>());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
