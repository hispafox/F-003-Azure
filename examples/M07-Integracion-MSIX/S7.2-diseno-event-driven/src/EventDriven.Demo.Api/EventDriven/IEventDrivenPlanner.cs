namespace EventDriven.Demo.Api.EventDriven;

public sealed record EscenarioDiseno(
    bool MultiplesConsumidores = false,
    bool ProcesamientoPesado = false,
    bool EscaladoIndependiente = false,
    bool DisponibilidadSobreConsistencia = false,
    bool EquipoPuedeComplejidad = true,
    bool CrudSimple = false,
    bool ConsistenciaFuerteInmediata = false,
    bool VolumenBajo = false,
    bool ConsumidorAutonomo = false,
    bool EventosPequenos = false,
    bool AuditTrailCompleto = false,
    int PasosSaga = 3,
    bool LogicaCondicional = false);

public sealed record EventoInvalido(string Tipo, IReadOnlyList<string> Problemas);

public sealed record PlanEventDriven(
    bool EventDrivenRecomendado,
    IReadOnlyList<string> RazonesDecision,
    PatronEvento PatronEvento,
    EstiloSaga EstiloSaga,
    IReadOnlyList<EventoInvalido> EventosInvalidos,
    IReadOnlyList<string> Checklist);

// Compone EventDesignAdvisor + EventValidator en un plan de diseño +
// checklist del entregable. Servicio inyectable (seam del test DI —
// lección M03-S3.4).
public interface IEventDrivenPlanner
{
    PlanEventDriven Planificar(
        EscenarioDiseno escenario, IReadOnlyList<DefinicionEvento> catalogo);
}

public sealed class EventDrivenPlanner : IEventDrivenPlanner
{
    public PlanEventDriven Planificar(
        EscenarioDiseno e, IReadOnlyList<DefinicionEvento> catalogo)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(catalogo);

        var decision = EventDesignAdvisor.EsBuenCaso(
            e.MultiplesConsumidores, e.ProcesamientoPesado,
            e.EscaladoIndependiente, e.DisponibilidadSobreConsistencia,
            e.EquipoPuedeComplejidad, e.CrudSimple,
            e.ConsistenciaFuerteInmediata, e.VolumenBajo);

        var patron = EventDesignAdvisor.RecomendarPatron(
            e.ConsumidorAutonomo, e.EventosPequenos, e.AuditTrailCompleto);

        var saga = EventDesignAdvisor.RecomendarSaga(
            e.PasosSaga, e.LogicaCondicional);

        var invalidos = catalogo
            .Select(d => (d.Tipo, r: EventValidator.Validar(d)))
            .Where(x => !x.r.Valido)
            .Select(x => new EventoInvalido(x.Tipo, x.r.Problemas))
            .ToList();

        return new PlanEventDriven(
            decision.Recomendado,
            decision.Razones,
            patron,
            saga,
            invalidos,
            Checklist:
            [
                "Correlation ID generado y propagado en todos los mensajes (slide 9)",
                "Idempotencia en cada consumidor: idempotency key o upsert (slide 10)",
                "Outbox fiable vía Cosmos DB Change Feed (slide 11)",
                "UX para eventual consistency: optimistic UI / polling / SignalR (slide 16)",
                "Eventos versionados; cambios de schema retrocompatibles (slide 20.4)",
                $"Máx {EventValidator.MaxSaltosCadena} saltos por cadena; Orchestrator si hay más (slide 20.1)",
                "Contract tests publicador↔consumidor (slide 19)",
                "DLQ monitorizada por suscripción; compensación si el paso es crítico (slide 17)",
            ]);
    }
}
