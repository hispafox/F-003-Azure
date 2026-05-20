using PracticaMsix.Demo.Api.Practica;

namespace PracticaMsix.Demo.Api.Endpoints;

public sealed record AvanzarRequest(PasoPractica PasoActual, List<bool> CriteriosOk);
public sealed record CertCheckRequest(string PublisherManifest, string SubjectCertificado);
public sealed record EkuRequest(List<string> Ekus);
public sealed record PlanRequest(ParametrosPractica Parametros, string SubjectCertificado);

public static class PracticaEndpoints
{
    public static void MapPractica(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var p = app.MapGroup("/practica");

        // Slide 15 — los 8 pasos con sus criterios de validación.
        p.MapGet("/pasos", () => Results.Ok(PracticaSteps.Pasos));

        // Info de un paso concreto.
        p.MapGet("/paso", (PasoPractica paso) =>
            Results.Ok(PracticaSteps.Info(paso)));

        // Avanzar al siguiente paso si TODOS los criterios pasan.
        p.MapPost("/avanzar", (AvanzarRequest req) =>
        {
            var siguiente = PracticaSteps.SiguientePaso(req.PasoActual, req.CriteriosOk);
            return Results.Ok(new
            {
                actual = req.PasoActual.ToString(),
                siguiente = siguiente?.ToString(),
                avanza = siguiente.HasValue && siguiente != req.PasoActual,
                completada = siguiente is null,
            });
        });

        // Slide 7 — Publisher del manifest debe coincidir con Subject
        // del certificado. El error #1 de la práctica.
        p.MapPost("/cert-coincide", (CertCheckRequest r) =>
            Results.Ok(PracticaCertCheck.PublisherCoincide(
                r.PublisherManifest, r.SubjectCertificado)));

        // Slide 7 — el cert debe tener EKU Code Signing.
        p.MapPost("/cert-uso", (EkuRequest r) =>
            Results.Ok(PracticaCertCheck.UsoCorrecto(r.Ekus)));

        // Slide 6 — manifest canónico para comparar con el del alumno.
        p.MapGet("/artefactos/manifest",
            (string empresa, string app, string version) =>
                Results.Text(PracticaArtefactosBuilder.ConstruirManifest(
                    new ParametrosPractica(empresa, app, version, "")),
                    "application/xml"));

        // Slide 11 — .appinstaller canónico (reto).
        p.MapGet("/artefactos/appinstaller",
            (string empresa, string app, string version, string baseUri) =>
                Results.Text(PracticaArtefactosBuilder.ConstruirAppInstaller(
                    new ParametrosPractica(empresa, app, version, baseUri)),
                    "application/xml"));

        // Plan completo + checklist (slide 15).
        p.MapPost("/plan", (PlanRequest req, IPracticaMsixPlanner planner) =>
            Results.Ok(planner.Planificar(req.Parametros, req.SubjectCertificado)));
    }
}
