using AutoUpdate.Demo.Api.AutoUpdate;

namespace AutoUpdate.Demo.Api.Endpoints;

public sealed record AppInstallerXml(string Xml);
public sealed record RollbackRequest(string VersionMala, List<string> Historial);

public static class AutoUpdateEndpoints
{
    public static void MapAutoUpdate(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var u = app.MapGroup("/update");

        // Slides 2-3, 13 — construir el .appinstaller.
        u.MapPost("/appinstaller", (AppInstallerConfig cfg) =>
            Results.Ok(new { xml = AppInstallerBuilder.Construir(cfg) }));

        // Lee un .appinstaller existente.
        u.MapPost("/parsear", (AppInstallerXml req) =>
            Results.Ok(AppInstallerBuilder.Parsear(req.Xml)));

        // Slide 10 — URL del .appinstaller por canal.
        u.MapGet("/canal", (CanalDistribucion canal, string baseUrl) =>
            Results.Ok(new
            {
                canal = canal.ToString(),
                appInstallerUri = CanaryRolloutPolicy.AppInstallerUri(canal, baseUrl),
            }));

        // Slide 20 — ¿este userId entra en la cohorte de la etapa?
        u.MapGet("/canary", (string userId, int porcentaje) =>
            Results.Ok(CanaryRolloutPolicy.RecibeActualizacion(userId, porcentaje)));

        // Slide 20/21/24 — siguiente etapa si la salud es OK.
        u.MapGet("/siguiente-etapa", (int etapaActual, bool saludOk) =>
            Results.Ok(new
            {
                siguiente = CanaryRolloutPolicy.SiguienteEtapa(etapaActual, saludOk),
            }));

        // Slide 7 — ¿se aplica la actualización?
        u.MapGet("/comparar", (string actual, string disponible,
                bool? forceFromAny) =>
            Results.Ok(UpdateVersionAdvisor.Comparar(actual, disponible,
                forceFromAny ?? false)));

        // Slide 13 — ¿debe ser obligatoria?
        u.MapGet("/obligatoria", (string actual, string minimoSoportado) =>
            Results.Ok(new
            {
                esObligatoria = UpdateVersionAdvisor.EsObligatoria(actual, minimoSoportado),
                updateBlocksActivation = UpdateVersionAdvisor.EsObligatoria(actual, minimoSoportado),
            }));

        // Slide 8 — plan de rollback (republicar la previa con build+1).
        u.MapPost("/rollback", (RollbackRequest r) =>
        {
            var plan = UpdateVersionAdvisor.PlanificarRollback(r.VersionMala, r.Historial);
            return plan is null
                ? Results.NotFound(new { mensaje = "Sin versión previa en el historial." })
                : Results.Ok(plan);
        });

        // Plan + checklist del entregable.
        u.MapPost("/plan", (EscenarioAutoUpdate e, IAutoUpdatePlanner planner) =>
            Results.Ok(planner.Planificar(e)));
    }
}
