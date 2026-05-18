using Dr.Demo.Api.Dr;

namespace Dr.Demo.Api.Endpoints;

public sealed record PlanDto(
    Criticidad Criticidad,
    List<ServicioAzure> Servicios,
    int RpoObjetivoMin,
    int RtoObjetivoMin);

public static class DrEndpoints
{
    public static void MapDr(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        var dr = app.MapGroup("/dr");

        // Slides 3, 6, 19 — backup "de fábrica" de un servicio.
        dr.MapGet("/backup/{servicio}", (ServicioAzure servicio) =>
            Results.Ok(BackupPolicyAdvisor.Describir(servicio)));

        // Slides 8, 14, 22, 24 — RPO/RTO por estrategia.
        dr.MapGet("/rpo-rto/{estrategia}", (EstrategiaDr estrategia) =>
            Results.Ok(RpoRtoCalculator.Perfil(estrategia)));

        // Slide 20 — requisito de retención por regulación.
        dr.MapGet("/retencion/{regimen}", (Regimen regimen) =>
            Results.Ok(new
            {
                requisito = RetentionPolicyAdvisor.Requisito(regimen),
                diasInmutabilidad = RetentionPolicyAdvisor.DiasInmutabilidad(regimen),
            }));

        // Slides 8, 12, 16 — plan de DR completo (compone los advisors).
        dr.MapPost("/plan", (PlanDto dto, IDrPlanner planner) =>
            Results.Ok(planner.Generar(
                dto.Criticidad, dto.Servicios,
                dto.RpoObjetivoMin, dto.RtoObjetivoMin)));
    }
}
