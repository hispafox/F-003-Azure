namespace Msix.Demo.Api.Msix;

public sealed record PlanMsix(
    bool ManifestValido,
    IReadOnlyList<string> ProblemasManifest,
    string NombreArchivo,
    IReadOnlyList<CanalDistribucion> Canales,
    IReadOnlyList<string> RazonesCanales,
    DistributionChannelAdvisor.PoliticaAutoUpdate PoliticaAutoUpdate,
    IReadOnlyList<string> Checklist);

// Compone AppxManifestValidator + PackageNamingResolver +
// DistributionChannelAdvisor en un plan + checklist del entregable.
// Servicio inyectable (seam del test DI — lección M03-S3.4).
public interface IMsixPackagingPlanner
{
    PlanMsix Planificar(AppxManifest manifest, EscenarioDistribucion distribucion);
}

public sealed class MsixPackagingPlanner : IMsixPackagingPlanner
{
    public PlanMsix Planificar(AppxManifest manifest, EscenarioDistribucion distribucion)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(distribucion);

        var validacion = AppxManifestValidator.Validar(manifest);
        var canal = DistributionChannelAdvisor.Recomendar(distribucion);

        return new PlanMsix(
            validacion.Valido,
            validacion.Problemas,
            PackageNamingResolver.NombreArchivo(manifest),
            canal.Canales,
            canal.Razones,
            DistributionChannelAdvisor.PoliticaPorDefecto(),
            // Slide 28 anti-patterns + slides 5/6/10/11/14 buenas prácticas.
            Checklist:
            [
                "Sin escrituras a HKLM ni a C:\\Program Files: usar ApplicationData (slide 28.1/28.2)",
                "Firmar también en desarrollo (cert self-signed), no solo en producción (slide 28.3)",
                "Multi-arch en .msixbundle: x64 + arm64 (slide 10/28.4)",
                "Cualquier cambio del manifest se reconstruye y se prueba en VM limpia (slide 28.5)",
                "Sin instaladores legacy dentro del MSIX: el MSIX ES el instalador (slide 28.7)",
                "AppInstaller/winget desde día 1, no después (slide 28.10)",
                "Telemetry de instalación (Application Insights) desde el primer día (slide 28.12)",
                "Clave privada del cert en Azure Key Vault; AzureSignTool en el pipeline (slide 6)",
                "Versionado incremental (Major.Minor.Build.Revision) en cada release (slide 3/11)",
                "Sideloading: cert trusted en los PCs vía Group Policy (Enterprise CA, slide 9)",
            ]);
    }
}
