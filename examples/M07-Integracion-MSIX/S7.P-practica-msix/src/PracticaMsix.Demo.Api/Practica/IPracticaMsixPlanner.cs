namespace PracticaMsix.Demo.Api.Practica;

public sealed record PlanPractica(
    IReadOnlyList<PasoInfo> Pasos,
    ResultadoCheck PublisherCertCheck,
    string ManifestEjemplo,
    string AppInstallerEjemplo,
    IReadOnlyList<string> Checklist);

// Compone PracticaSteps + PracticaCertCheck + PracticaArtefactosBuilder
// en el plan completo de la práctica (slide 15). Servicio inyectable
// (seam del test DI — lección M03-S3.4).
public interface IPracticaMsixPlanner
{
    PlanPractica Planificar(ParametrosPractica parametros, string subjectCertificado);
}

public sealed class PracticaMsixPlanner : IPracticaMsixPlanner
{
    public PlanPractica Planificar(
        ParametrosPractica parametros, string subjectCertificado)
    {
        ArgumentNullException.ThrowIfNull(parametros);

        string publisherEsperado = $"CN={parametros.Empresa}";
        var certCheck = PracticaCertCheck.PublisherCoincide(
            publisherEsperado, subjectCertificado);

        return new PlanPractica(
            Pasos: PracticaSteps.Pasos,
            PublisherCertCheck: certCheck,
            ManifestEjemplo: PracticaArtefactosBuilder.ConstruirManifest(parametros),
            AppInstallerEjemplo: PracticaArtefactosBuilder.ConstruirAppInstaller(parametros),
            // Slide 15 — checklist de los 11 items de la práctica.
            Checklist:
            [
                "Proyecto WPF + Packaging Project creados (slide 4)",
                "Package.appxmanifest configurado: nombre, publisher, capabilities (slide 6)",
                "Certificado self-signed creado con EKU Code Signing (slide 7)",
                "Publisher en manifest == Subject del certificado (slide 7 — error #1)",
                "Build MSIX exitoso en Release|x64 (slide 8)",
                "Certificado importado a Cert:\\LocalMachine\\TrustedPeople (slide 9)",
                "App instalada vía Add-AppxPackage (slide 9)",
                "App aparece en Start Menu y arranca (slide 9)",
                "Versión visible en la UI con Package.Current.Id.Version (slide 5/9)",
                "Actualización 1.0.0.0 → 1.0.1.0 in-place (slide 10)",
                "Desinstalación limpia desde Settings → Apps (sin residuos)",
            ]);
    }
}
