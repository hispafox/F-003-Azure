using Monitor.AppInsights.Demo.Api.Monitor;

namespace Monitor.AppInsights.Demo.Api.Endpoints;

public sealed record KqlRequest(string Ventana = "24h", int Minimo = 100);
public sealed record DependenciasRequest(string Ventana = "24h", int UmbralMs = 1000);
public sealed record CorrelacionRequest(string OperationId);
public sealed record RespuestaRequest(string Json);
public sealed record UsoIngestaRequest(string Ventana = "30d", double EurosPorGb = 2.5);
public sealed record PlanRequest(EscenarioAlertas Escenario, string Ventana = "24h");

public static class MonitorEndpoints
{
    public static void MapMonitor(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/monitor");

        // Slide 5/26 — KQL canónico: P95 por endpoint.
        g.MapPost("/kql/p95", (KqlRequest r) =>
            Results.Ok(KqlQueryBuilder.P95PorEndpoint(new VentanaTiempo(r.Ventana), r.Minimo)));

        // Slide 5/26 — tasa de errores por hora.
        g.MapGet("/kql/tasa-error", (string ventana) =>
            Results.Ok(KqlQueryBuilder.TasaErrorPorHora(new VentanaTiempo(ventana ?? "7d"))));

        // Slide 5 — excepciones por tipo.
        g.MapGet("/kql/excepciones", (string ventana) =>
            Results.Ok(KqlQueryBuilder.ExcepcionesPorTipo(new VentanaTiempo(ventana ?? "24h"))));

        // Slide 5 — dependencias lentas.
        g.MapPost("/kql/dependencias-lentas", (DependenciasRequest r) =>
            Results.Ok(KqlQueryBuilder.DependenciasLentas(
                new VentanaTiempo(r.Ventana), r.UmbralMs)));

        // Slide 5/19 — traza end-to-end por operation_Id.
        g.MapPost("/kql/correlacion", (CorrelacionRequest r) =>
            Results.Ok(KqlQueryBuilder.TrazaPorOperationId(r.OperationId)));

        // Slide 20 — coste estimado por solución/tipo.
        g.MapPost("/kql/uso-ingesta", (UsoIngestaRequest r) =>
            Results.Ok(KqlQueryBuilder.UsoEingestaPorTipo(
                new VentanaTiempo(r.Ventana), r.EurosPorGb)));

        // Slide 8/18/21 — recomendador de alertas.
        g.MapPost("/alertas/recomendar", (EscenarioAlertas e) =>
            Results.Ok(AlertRecommender.Recomendar(e)));

        // Slide 9 — Smart Detection a habilitar.
        g.MapGet("/alertas/smart-detection",
            () => Results.Ok(AlertRecommender.SmartDetectionRecomendada));

        // Slide 21 — runbook de respuesta a incidentes.
        g.MapGet("/alertas/runbook",
            () => Results.Ok(AlertRecommender.Runbook));

        // Slide 5/13 — parser del shape de `az monitor app-insights query`.
        g.MapPost("/respuesta/parsear", (RespuestaRequest r) =>
            Results.Ok(MonitorResponseParser.Parsear(r.Json)));

        // Plan + checklist del entregable.
        g.MapPost("/plan", (PlanRequest req, IAppInsightsPlanner planner) =>
            Results.Ok(planner.Planificar(req.Escenario, new VentanaTiempo(req.Ventana))));
    }
}
