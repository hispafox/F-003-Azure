using ClaudeCode.Infra.Demo.Api.Infra;

namespace ClaudeCode.Infra.Demo.Api.Endpoints;

public sealed record RequisitosRequest(string Descripcion);
public sealed record AuditRequest(IReadOnlyList<EstadoRecurso> Recursos);
public sealed record PlanRequest(string Descripcion, IReadOnlyList<EstadoRecurso>? Recursos = null);

public static class InfraEndpoints
{
    public static void MapInfra(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/infra");

        // Slides 2/3/17 — parsea requisitos en bruto.
        g.MapPost("/requisitos", (RequisitosRequest r) =>
            Results.Ok(InfraRequirementsParser.Parsear(r.Descripcion)));

        // Slides 2-17 — prompt canónico por escenario.
        g.MapGet("/prompt/{escenario}", (EscenarioInfra escenario) =>
            Results.Ok(InfraPromptBuilder.ParaEscenario(escenario)));

        // Slide 15 — audit de recursos contra reglas mínimas.
        g.MapPost("/audit", (AuditRequest r) =>
            Results.Ok(InfraAuditChecker.Auditar(r.Recursos)));

        // Plan + checklist del entregable.
        g.MapPost("/plan", (PlanRequest req, IInfraPlanner planner) =>
            Results.Ok(planner.Planificar(req.Descripcion, req.Recursos)));
    }
}
