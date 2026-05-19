namespace Distribution.Demo.Api.Distribution;

public sealed record FactoresMigracion(
    bool IntunePlaneado = false,
    bool DotNet8Planeado = false,
    bool CertAuthenticodeExpira = false,
    bool ProblemasActualizacion = false,
    bool ClickOnceFuncionaBien = true,
    bool EquipoSinBandwidth = false,
    bool EsAppNueva = false,
    bool SobreDotNetFramework = true,
    bool TieneTiempoEquipo = false,
    EscenarioFirma EscenarioFirma = EscenarioFirma.DistribucionInterna);

public sealed record PlanDistribucion(
    bool MigrarRecomendado,
    IReadOnlyList<string> RazonesMigracion,
    EscenarioMigracion Escenario,
    RecomendacionCert Certificado,
    int VentajasMsixSobreClickOnce,
    IReadOnlyList<string> Checklist);

// Compone DistributionFormatComparator + MigrationDecisionAdvisor +
// SigningCertAdvisor en un plan + checklist del entregable. Servicio
// inyectable (seam del test DI — lección M03-S3.4).
public interface IDistributionPlanner
{
    PlanDistribucion Planificar(FactoresMigracion f);
}

public sealed class DistributionPlanner : IDistributionPlanner
{
    public PlanDistribucion Planificar(FactoresMigracion f)
    {
        ArgumentNullException.ThrowIfNull(f);

        var migrar = MigrationDecisionAdvisor.DebeMigrar(
            f.IntunePlaneado, f.DotNet8Planeado, f.CertAuthenticodeExpira,
            f.ProblemasActualizacion, f.ClickOnceFuncionaBien, f.EquipoSinBandwidth);

        var escenario = MigrationDecisionAdvisor.RecomendarEscenario(
            f.EsAppNueva, f.SobreDotNetFramework, f.TieneTiempoEquipo);

        var cert = SigningCertAdvisor.Recomendar(f.EscenarioFirma);

        return new PlanDistribucion(
            migrar.Recomendado,
            migrar.Razones,
            escenario,
            cert,
            DistributionFormatComparator.VentajasMsixSobreClickOnce(),
            // Slide 6/8/14/19/26 — checklist del entregable de migración.
            Checklist:
            [
                "Empezar por apps NUEVAS directamente en MSIX (slide 18/26)",
                "Distribución interna: sideloading con AppInstaller + Intune (slide 6/16)",
                "Certificado de firma elegido y desplegado (slide 8)",
                "Pipeline CI/CD: build → sign → upload → actualizar .appinstaller (slide 10/14)",
                "Single-file MSIX con .NET 8+ cuando se modernice (slide 22)",
                "winget para developer/power users + Microsoft Store para público (slide 15/26)",
                "AppInstaller en transición: previsto reemplazarlo (deprecated 2026, slide 19)",
                "Target x64 + arm64 en el msixbundle (slide 26)",
            ]);
    }
}
