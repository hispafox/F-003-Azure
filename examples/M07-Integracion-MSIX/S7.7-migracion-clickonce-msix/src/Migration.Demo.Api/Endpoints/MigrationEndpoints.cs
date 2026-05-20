using Migration.Demo.Api.Migration;

namespace Migration.Demo.Api.Endpoints;

public sealed record ApplicationXml(string Xml);
public sealed record CompatRequest(List<ComportamientoApp> Comportamientos);
public sealed record SiguienteFaseRequest(
    FaseMigracion FaseActual, List<bool> CriteriosOk);

public static class MigrationEndpoints
{
    public static void MapMigration(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var m = app.MapGroup("/migracion");

        // Slide 6/8 — mapear .application (ClickOnce) → AppxManifest.
        m.MapPost("/mapear", (ClickOnceManifest co) =>
            Results.Ok(ClickOnceManifestMapper.Mapear(co)));

        // Parsea un .application XML existente.
        m.MapPost("/parsear", (ApplicationXml req) =>
            Results.Ok(ClickOnceManifestMapper.Parsear(req.Xml)));

        // Slides 3, 12 — evaluar compatibilidad con MSIX.
        m.MapPost("/compatibilidad", (CompatRequest r) =>
            Results.Ok(MigrationCompatibilityCheck.Evaluar(r.Comportamientos)));

        // Slide 2/11 — info de una fase del roadmap.
        m.MapGet("/fase", (FaseMigracion fase) =>
            Results.Ok(MigrationRoadmap.Info(fase)));

        // Slide 11 — avanzar a la siguiente fase si los criterios pasan.
        m.MapPost("/siguiente-fase", (SiguienteFaseRequest r) =>
        {
            var siguiente = MigrationRoadmap.SiguienteFase(r.FaseActual, r.CriteriosOk);
            return Results.Ok(new
            {
                actual = r.FaseActual.ToString(),
                siguiente = siguiente?.ToString(),
                avanza = siguiente is not null && siguiente != r.FaseActual,
            });
        });

        // Plan + checklist del entregable.
        m.MapPost("/plan", (EscenarioMigracion e, IMigrationPlanner planner) =>
            Results.Ok(planner.Planificar(e)));
    }
}
