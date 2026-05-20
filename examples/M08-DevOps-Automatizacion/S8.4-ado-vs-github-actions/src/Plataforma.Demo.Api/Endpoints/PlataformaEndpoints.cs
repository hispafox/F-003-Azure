using Plataforma.Demo.Api.Plataforma;

namespace Plataforma.Demo.Api.Endpoints;

public sealed record PlanRequest(
    EscenarioPlataforma Escenario, EscenarioCoste Coste);

public static class PlataformaEndpoints
{
    public static void MapPlataforma(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var p = app.MapGroup("/plataforma");

        // Slides 4, 5, 8, 11, 19 — ¿ADO, GitHub o híbrido?
        p.MapPost("/elegir", (EscenarioPlataforma e) =>
            Results.Ok(PlatformAdvisor.Recomendar(e)));

        // Slide 6 — tabla completa de equivalencias YAML.
        p.MapGet("/equivalencias", () =>
            Results.Ok(SyntaxEquivalenceMapper.Todas));

        // Slide 6 — buscar por concepto (exacto o contención).
        p.MapGet("/equivalencia", (string concepto) =>
        {
            var e = SyntaxEquivalenceMapper.Buscar(concepto);
            return e is null
                ? Results.NotFound(new { mensaje = $"Sin equivalencia para '{concepto}' (slide 6)." })
                : Results.Ok(e);
        });

        // Slides 12, 17 — coste comparado.
        p.MapPost("/coste", (EscenarioCoste c) =>
            Results.Ok(MigrationCostEstimator.Comparar(c)));

        // Plan + checklist + equivalencias clave + coste.
        p.MapPost("/plan", (PlanRequest req, IPlatformPlanner planner) =>
            Results.Ok(planner.Planificar(req.Escenario, req.Coste)));
    }
}
