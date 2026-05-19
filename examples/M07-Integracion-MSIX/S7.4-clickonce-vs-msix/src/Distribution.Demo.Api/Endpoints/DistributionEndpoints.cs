using Distribution.Demo.Api.Distribution;

namespace Distribution.Demo.Api.Endpoints;

public static class DistributionEndpoints
{
    public static void MapDistribution(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var d = app.MapGroup("/distribution");

        // Slide 4/26 — ¿soporta el formato esta característica?
        d.MapGet("/soporta", (FormatoDistribucion formato,
                CaracteristicaDistribucion caracteristica) =>
            Results.Ok(new
            {
                formato = formato.ToString(),
                caracteristica = caracteristica.ToString(),
                soporta = DistributionFormatComparator.Soporta(formato, caracteristica),
            }));

        // Slide 4 — comparativa entre dos formatos: características en
        // las que uno gana al otro.
        d.MapGet("/comparar", (FormatoDistribucion a, FormatoDistribucion b) =>
        {
            var ganaA = new List<string>();
            var ganaB = new List<string>();
            foreach (CaracteristicaDistribucion c in
                Enum.GetValues<CaracteristicaDistribucion>())
            {
                bool sa = DistributionFormatComparator.Soporta(a, c);
                bool sb = DistributionFormatComparator.Soporta(b, c);
                if (sa && !sb) ganaA.Add(c.ToString());
                if (sb && !sa) ganaB.Add(c.ToString());
            }
            return Results.Ok(new
            {
                a = a.ToString(),
                b = b.ToString(),
                ganaA,
                ganaB,
            });
        });

        // Slide 18 — ¿migrar ClickOnce → MSIX?
        d.MapPost("/migrar", (FactoresMigracion f) =>
            Results.Ok(MigrationDecisionAdvisor.DebeMigrar(
                f.IntunePlaneado, f.DotNet8Planeado, f.CertAuthenticodeExpira,
                f.ProblemasActualizacion, f.ClickOnceFuncionaBien,
                f.EquipoSinBandwidth)));

        // Slide 12 — qué escenario de migración (A/B/C).
        d.MapGet("/escenario", (bool? esAppNueva, bool? sobreDotNetFramework,
                bool? tieneTiempoEquipo) =>
            Results.Ok(new
            {
                escenario = MigrationDecisionAdvisor.RecomendarEscenario(
                    esAppNueva ?? false,
                    sobreDotNetFramework ?? false,
                    tieneTiempoEquipo ?? false).ToString(),
            }));

        // Slide 8 — qué certificado usar.
        d.MapGet("/cert", (EscenarioFirma escenario) =>
            Results.Ok(SigningCertAdvisor.Recomendar(escenario)));

        // Plan + checklist del entregable.
        d.MapPost("/plan", (FactoresMigracion f, IDistributionPlanner planner) =>
            Results.Ok(planner.Planificar(f)));
    }
}
