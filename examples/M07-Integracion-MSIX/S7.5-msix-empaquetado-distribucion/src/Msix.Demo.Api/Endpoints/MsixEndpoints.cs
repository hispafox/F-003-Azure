using Msix.Demo.Api.Msix;

namespace Msix.Demo.Api.Endpoints;

public sealed record ManifestXml(string Xml);
public sealed record VersionRequest(string Actual, int BuildId);
public sealed record PlanRequest(AppxManifest Manifest, EscenarioDistribucion Distribucion);

public static class MsixEndpoints
{
    public static void MapMsix(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var m = app.MapGroup("/msix");

        // Slide 3 — parsea un Package.appxmanifest minimal.
        m.MapPost("/parsear", (ManifestXml req) =>
            Results.Ok(AppxManifestValidator.Parsear(req.Xml)));

        // Slides 3, 15, 28 — valida el manifest.
        m.MapPost("/validar", (AppxManifest manifest) =>
            Results.Ok(AppxManifestValidator.Validar(manifest)));

        // Slide 4 — nombre del archivo {Name}_{Version}_{Arch}.msix.
        m.MapGet("/nombre", (string identityName, string version, string arch) =>
            Results.Ok(new
            {
                archivo = PackageNamingResolver.NombreArchivo(
                    new AppxManifest(identityName, "CN=", version, arch, "10.0.17763.0", [])),
            }));

        // Slide 11 — calcula la siguiente version desde el build id.
        m.MapPost("/version-siguiente", (VersionRequest r) =>
            Results.Ok(new { siguiente = PackageNamingResolver.SiguienteVersion(r.Actual, r.BuildId) }));

        // Slide 3 — comprueba que la nueva version es incremental.
        m.MapGet("/incremental", (string anterior, string nueva) =>
            Results.Ok(new { incremental = PackageNamingResolver.EsIncremental(anterior, nueva) }));

        // Slides 7/8/9/26/27 — qué canal(es) usar.
        m.MapPost("/distribucion", (EscenarioDistribucion e) =>
            Results.Ok(DistributionChannelAdvisor.Recomendar(e)));

        // Slide 7 — política UpdateSettings del .appinstaller.
        m.MapGet("/politica-auto-update", () =>
            Results.Ok(DistributionChannelAdvisor.PoliticaPorDefecto()));

        // Plan + checklist del entregable.
        m.MapPost("/plan", (PlanRequest req, IMsixPackagingPlanner planner) =>
            Results.Ok(planner.Planificar(req.Manifest, req.Distribucion)));
    }
}
