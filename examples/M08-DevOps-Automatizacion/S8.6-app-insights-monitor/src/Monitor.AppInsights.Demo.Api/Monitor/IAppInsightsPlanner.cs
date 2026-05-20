namespace Monitor.AppInsights.Demo.Api.Monitor;

public sealed record PlanObservabilidad(
    IReadOnlyList<KqlQuery> QueriesCanonicas,
    IReadOnlyList<ReglaAlerta> Alertas,
    IReadOnlyList<string> SmartDetection,
    IReadOnlyList<string> Runbook,
    IReadOnlyList<string> Checklist);

// Compone KqlQueryBuilder + AlertRecommender + Runbook en el plan +
// checklist del entregable. Servicio inyectable (seam del test DI —
// lección M03-S3.4 / patrón M06/M07/M08).
public interface IAppInsightsPlanner
{
    PlanObservabilidad Planificar(EscenarioAlertas escenario, VentanaTiempo? ventana = null);
}

public sealed class AppInsightsPlanner : IAppInsightsPlanner
{
    public PlanObservabilidad Planificar(
        EscenarioAlertas escenario,
        VentanaTiempo? ventana = null)
    {
        ArgumentNullException.ThrowIfNull(escenario);
        ventana ??= VentanaTiempo.Ultimas24h;

        var queries = new List<KqlQuery>
        {
            KqlQueryBuilder.P95PorEndpoint(ventana),
            KqlQueryBuilder.TasaErrorPorHora(VentanaTiempo.Ultimos7d),
            KqlQueryBuilder.ExcepcionesPorTipo(ventana),
            KqlQueryBuilder.DependenciasLentas(ventana),
        };

        var alertas = AlertRecommender.Recomendar(escenario);

        return new PlanObservabilidad(
            QueriesCanonicas: queries,
            Alertas: alertas,
            SmartDetection: AlertRecommender.SmartDetectionRecomendada,
            Runbook: AlertRecommender.Runbook,
            // Slides 2, 3, 8, 9, 12, 13, 15, 16, 20, 23 — checklist.
            Checklist:
            [
                "App Insights con `AddApplicationInsightsTelemetry()` (slide 3)",
                "Workspace-based (envía a Log Analytics) — Classic ya no se crea (slide 13/23)",
                "Custom events/metrics donde el auto-tracking no llega (slide 4)",
                "Sampling adaptativo por defecto; ajustar % si el coste sube (slide 12/20)",
                "Action Group con email + Teams/PagerDuty (slide 8)",
                "Reglas de alerta mínimas: 5xx, latencia, excepciones (slide 8)",
                "Smart Detection habilitado (zero-conf) (slide 9)",
                "Dashboard pinned con KPIs (req/min, P95, error rate, deploys) (slide 15)",
                "Daily cap o commitment tier para controlar coste (slide 16/20)",
                "Runbook de incidentes documentado y testeado (slide 21)",
            ]);
    }
}
