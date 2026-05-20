namespace WizardMsix.Demo.Api.Wizard;

// Slide 15 — "lo que el wizard hace por debajo": qué binarios CLI
// invoca Visual Studio cuando el alumno pulsa Create App Packages.
public enum HerramientaCli { MakeAppx, SignTool, ImportCertificate, AddAppPackage }

public sealed record ComandoCli(
    HerramientaCli Herramienta, string Linea, string Razon);

public sealed record ParametrosWizard(
    string Empresa, string App, string Version,
    string BuildOutputDir, string CertPfx, string OutputMsix);

// Slide 15 — expande la intención del wizard (build + sign + install)
// a la secuencia de comandos CLI equivalentes. Es la "vista de
// pegamento" que conecta S7.P2 (wizard) con S7.P (CLI manual).
public static class WizardComandosExpander
{
    public static IReadOnlyList<ComandoCli> Expandir(ParametrosWizard p)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentException.ThrowIfNullOrWhiteSpace(p.Empresa);
        ArgumentException.ThrowIfNullOrWhiteSpace(p.App);
        ArgumentException.ThrowIfNullOrWhiteSpace(p.Version);
        ArgumentException.ThrowIfNullOrWhiteSpace(p.BuildOutputDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(p.CertPfx);
        ArgumentException.ThrowIfNullOrWhiteSpace(p.OutputMsix);

        string idCert = $"CN={p.Empresa}";

        return
        [
            new(HerramientaCli.MakeAppx,
                $"makeappx.exe pack /d \"{p.BuildOutputDir}\" /p \"{p.OutputMsix}\"",
                "Empaqueta los artefactos de Release/x64 en un .msix (slide 15)."),
            new(HerramientaCli.SignTool,
                $"signtool.exe sign /fd SHA256 /a /f \"{p.CertPfx}\" \"{p.OutputMsix}\"",
                "Firma el .msix con el cert self-signed; el Subject debe ser " +
                $"\"{idCert}\" (slide 15)."),
            new(HerramientaCli.ImportCertificate,
                $"Import-Certificate -FilePath \"{Path.ChangeExtension(p.CertPfx, ".cer")}\" " +
                "-CertStoreLocation Cert:\\LocalMachine\\TrustedPeople",
                "Marca el cert como trusted para que Windows acepte el .msix " +
                "(slide 8 / slide 15 — Install.ps1)."),
            new(HerramientaCli.AddAppPackage,
                $"Add-AppPackage -Path \"{p.OutputMsix}\"",
                "Instala el .msix en el PC del usuario (slide 9 / slide 15)."),
        ];
    }
}
