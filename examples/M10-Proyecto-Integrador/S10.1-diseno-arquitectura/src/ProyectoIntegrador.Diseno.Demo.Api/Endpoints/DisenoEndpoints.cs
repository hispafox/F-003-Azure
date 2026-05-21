using ProyectoIntegrador.Diseno.Demo.Api.Diseno;

namespace ProyectoIntegrador.Diseno.Demo.Api.Endpoints;

public static class DisenoEndpoints
{
    public static void MapDiseno(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var g = app.MapGroup("/diseno");

        // Slide 3/4 — inventario de componentes con su estado.
        g.MapPost("/arquitectura", (EstadoSistema s) =>
            Results.Ok(ArquitecturaChecklist.Inventariar(s)));

        // Slide 4 — porcentaje de componentes desplegados.
        g.MapPost("/arquitectura/porcentaje", (EstadoSistema s) =>
            Results.Ok(new { porcentaje = ArquitecturaChecklist.PorcentajeDesplegado(s) }));

        // Slide 5 — bloque recomendado por progreso (A/B/C/D).
        g.MapPost("/bloque-siguiente", (EstadoSistema s) =>
            Results.Ok(BloqueRecommender.Recomendar(s)));

        // Slide 11 — evaluador de la entrega final.
        g.MapPost("/entrega", (EvidenciaEntrega e) =>
            Results.Ok(EntregaEvaluator.Evaluar(e)));

        // Slide 12 — retos opcionales (bonus).
        g.MapGet("/retos",
            () => Results.Ok(ProyectoIntegradorPlanner.RetosOpcionales));

        // Plan completo + checklist + entrega + retos.
        g.MapPost("/plan", (PlanRequest req, IProyectoIntegradorPlanner planner) =>
            Results.Ok(planner.Planificar(req)));
    }
}
