namespace WizardMsix.Demo.Api.Wizard;

public enum FlujoEmpaquetado { Wizard, Cli }

public sealed record ContextoEmpaquetado(
    bool AprendizajeInicial = false,
    bool AppSimpleSingleArch = true,
    bool PipelineCiCd = false,
    bool CertDesdeKeyVault = false,
    bool MultiArquitectura = false,
    bool EquipoGrande = false,
    bool DistribucionCorporativa = false);

public sealed record RecomendacionFlujo(
    FlujoEmpaquetado Flujo, IReadOnlyList<string> Razones);

// Slide 15/17 — ¿usar el wizard de Visual Studio o el CLI manual de
// S7.P? Lógica de votos pura: factores "CLI" pesan; sin ellos, wizard.
public static class WizardVsCliAdvisor
{
    public static RecomendacionFlujo Recomendar(ContextoEmpaquetado c)
    {
        ArgumentNullException.ThrowIfNull(c);
        var razones = new List<string>();

        if (c.PipelineCiCd) razones.Add("Pipeline CI/CD → todo CLI versionable (slide 15).");
        if (c.CertDesdeKeyVault) razones.Add("Cert desde Azure Key Vault → wizard no lo soporta (slide 17).");
        if (c.MultiArquitectura) razones.Add("Multi-arch (x86+x64+arm64) → wizard limitado (slide 15).");
        if (c.EquipoGrande) razones.Add("Equipo grande → CLI reproducible y revisable en PR (slide 15).");
        if (c.DistribucionCorporativa) razones.Add("Distribución corporativa con AppInstaller → CLI (slide 15/17).");

        if (razones.Count > 0)
            return new RecomendacionFlujo(FlujoEmpaquetado.Cli, razones);

        var pro = new List<string>();
        if (c.AprendizajeInicial) pro.Add("Aprendizaje inicial → wizard, 0 CLI (slide 15).");
        if (c.AppSimpleSingleArch) pro.Add("App simple single-arch → wizard es suficiente (slide 15).");
        if (pro.Count == 0)
            pro.Add("Sin señales fuertes hacia CLI → empezar con wizard y bajar al CLI si hace falta.");

        return new RecomendacionFlujo(FlujoEmpaquetado.Wizard, pro);
    }

    // Slide 17 — lo que el wizard NO permite, para que el alumno sepa
    // cuándo tendrá que migrar a CLI.
    public static IReadOnlyList<string> LimitacionesWizard { get; } =
    [
        "Cert: solo self-signed o de cert store; no Azure Key Vault ni HSM externo (slide 17).",
        "Multi-arch: un .msix por arquitectura, sin bundle .msixbundle (slide 15).",
        "Sin AppInstaller con auto-update integrado (slide 17).",
        "Sin firma con timestamping RFC 3161 personalizado.",
        "Sin modificación avanzada del manifest (capabilities restringidas, extensiones).",
    ];
}
