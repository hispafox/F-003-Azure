namespace EventDriven.Demo.Api.EventDriven;

// Slide 6 — patrones de propagación de estado en event-driven.
public enum PatronEvento { EventNotification, EventCarriedStateTransfer, EventSourcing }

// Slide 8/22 — coordinación de una transacción distribuida (Saga).
public enum EstiloSaga { Choreography, Orchestration }

public sealed record DecisionEventDriven(bool Recomendado, IReadOnlyList<string> Razones);

// Slides 6, 8, 13, 22 — tablas de decisión de diseño event-driven.
// Lógica pura (sin Azure): el "criterio" es lo que se enseña.
public static class EventDesignAdvisor
{
    // Slide 6 — qué patrón de evento usar.
    //  - auditTrailCompleto  → Event Sourcing (replay, time-travel).
    //  - consumidorAutonomo  → Event-Carried State Transfer (sin N+1).
    //  - resto               → Event Notification (evento mínimo, ID).
    public static PatronEvento RecomendarPatron(
        bool consumidorAutonomo, bool eventosPequenos, bool auditTrailCompleto)
    {
        if (auditTrailCompleto) return PatronEvento.EventSourcing;
        if (consumidorAutonomo && !eventosPequenos)
            return PatronEvento.EventCarriedStateTransfer;
        return PatronEvento.EventNotification;
    }

    // Slide 13 — ¿es buen caso para event-driven? Cuenta señales a favor
    // y en contra; recomienda si pesan más las primeras.
    public static DecisionEventDriven EsBuenCaso(
        bool multiplesConsumidores,
        bool procesamientoPesado,
        bool escaladoIndependiente,
        bool disponibilidadSobreConsistencia,
        bool equipoPuedeComplejidad,
        bool crudSimple,
        bool consistenciaFuerteInmediata,
        bool volumenBajo)
    {
        var aFavor = new List<string>();
        if (multiplesConsumidores) aFavor.Add("Varios servicios reaccionan al mismo evento (slide 13).");
        if (procesamientoPesado) aFavor.Add("Procesamiento pesado que no debe bloquear al usuario (slide 3).");
        if (escaladoIndependiente) aFavor.Add("Necesita escalar servicios de forma independiente (slide 4).");
        if (disponibilidadSobreConsistencia) aFavor.Add("Disponibilidad > consistencia inmediata (slide 13).");
        if (equipoPuedeComplejidad) aFavor.Add("El equipo puede manejar la complejidad (slide 13).");

        var enContra = new List<string>();
        if (crudSimple) enContra.Add("Es un CRUD simple de un solo servicio (slide 13 — NO).");
        if (consistenciaFuerteInmediata) enContra.Add("Exige consistencia fuerte inmediata (slide 13 — NO).");
        if (volumenBajo) enContra.Add("Volumen bajo: un monolito basta (slide 13 — NO).");
        if (!equipoPuedeComplejidad) enContra.Add("La complejidad no se justifica para el equipo (slide 5/13).");

        bool recomendado = aFavor.Count > enContra.Count;
        var razones = recomendado ? aFavor : enContra;
        return new DecisionEventDriven(recomendado,
            razones.Count > 0 ? razones
                : ["Señales equilibradas: empezar simple (monolito) y evolucionar (slide 13)."]);
    }

    // Slide 8/22 — choreography para flujos simples (≤4 pasos, sin
    // lógica condicional); orchestration (Durable) para 5+ o condicional.
    public static EstiloSaga RecomendarSaga(int pasos, bool logicaCondicional)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pasos);
        return pasos >= 5 || logicaCondicional
            ? EstiloSaga.Orchestration
            : EstiloSaga.Choreography;
    }

    // Slide 8/22 — al fallar el paso `falloEnPaso` (1-based), compensar
    // los pasos YA completados en orden inverso (rollback).
    public static IReadOnlyList<string> SecuenciaCompensacion(
        IReadOnlyList<string> pasosCompletados, int falloEnPaso)
    {
        ArgumentNullException.ThrowIfNull(pasosCompletados);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(falloEnPaso);

        // Se compensan los pasos [1 .. falloEnPaso-1], en orden inverso.
        int completados = Math.Min(falloEnPaso - 1, pasosCompletados.Count);
        return [.. pasosCompletados.Take(completados)
            .Reverse()
            .Select(p => $"Compensar: {p}")];
    }
}
