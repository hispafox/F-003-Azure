using Apim.Demo.Api.Apim;

namespace Apim.Demo.Api.Endpoints;

public sealed record PolicyRequest(PolicyContext Contexto, PolicyConfig Config);

public sealed record PlanRequest(EscenarioApim Escenario, EscenarioUsoApim Uso);

public static class ApimEndpoints
{
    public static void MapApim(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var a = app.MapGroup("/apim");

        // Slides 5-6/9 — evalúa las policies inbound de una petición.
        a.MapPost("/policy", (PolicyRequest req) =>
            Results.Ok(ApimPolicyEvaluator.Evaluar(req.Contexto, req.Config)));

        // Slide 18 — circuit breaker: ¿reintentar el backend?
        a.MapGet("/retry", (int statusBackend, int intentos, int maxIntentos) =>
            Results.Ok(new
            {
                reintentar = ApimPolicyEvaluator.DebeReintentar(
                    statusBackend, intentos, maxIntentos),
            }));

        // Slide 7 — resolver la versión de API según el esquema.
        a.MapGet("/version", (EsquemaVersionado esquema, string apiPath,
                string entrada, string versiones) =>
        {
            var validas = versiones
                .Split(',', StringSplitOptions.RemoveEmptyEntries |
                            StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return Results.Ok(ApimVersioningResolver.Resolver(
                esquema, apiPath, entrada, validas));
        });

        // Slide 7 — esquema recomendado.
        a.MapGet("/version/recomendado", () =>
            Results.Ok(new { esquema = ApimVersioningResolver.Recomendado.ToString() }));

        // Slides 3/32 — tier recomendado + coste.
        a.MapPost("/tier", (EscenarioApim e) =>
            Results.Ok(ApimTierAdvisor.RecomendarTier(e)));

        // Slide 16 — ¿APIM aporta?
        a.MapPost("/caso", (EscenarioUsoApim u) =>
            Results.Ok(ApimTierAdvisor.EsBuenCaso(
                u.MultiplesApis, u.NecesitaRateLimitOCache, u.ExponeATerceros,
                u.VersionadoCentral, u.Analytics, u.UnaApiSimple,
                u.SoloTraficoInterno, u.PresupuestoLimitado)));

        // Plan de despliegue + checklist del entregable.
        a.MapPost("/plan", (PlanRequest req, IApimPlanner planner) =>
            Results.Ok(planner.Planificar(req.Escenario, req.Uso)));
    }
}
