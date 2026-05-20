namespace WizardMsix.Demo.Api.Wizard;

public sealed record DiagnosticoError(
    string Codigo, string Causa, string Diagnostico, string Fix);

// Slide 16 — catálogo de los 6 errores típicos al empaquetar/instalar
// MSIX, con causa + diagnóstico + fix. Lógica pura: lookup.
public static class MsixErrorTroubleshooter
{
    private static readonly Dictionary<string, DiagnosticoError> Catalogo =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["0x80073CFD"] = new("0x80073CFD",
                "El cert no está en TrustedPeople de LocalMachine (slide 16.1).",
                "Get-ChildItem Cert:\\LocalMachine\\TrustedPeople | " +
                "Where-Object Subject -like '*<publisher>*'  → vacío = no instalado.",
                "Import-Certificate -FilePath cert.cer -CertStoreLocation " +
                "Cert:\\LocalMachine\\TrustedPeople (PowerShell como admin)."),

            ["Add-AppPackage"] = new("Add-AppPackage",
                "Sideloading no habilitado (slide 16.2).",
                "Settings → Privacy & security → For developers → estado actual.",
                "Activar Developer Mode O cambiar 'Install apps' a Sideload."),

            ["MSB3325"] = new("MSB3325",
                "El .pfx no encuentra la password o la clave privada (slide 16.3).",
                "Error al compilar el packaging project con 'Cannot import the following key file'.",
                "Borrar el cert del proyecto, relanzar Create App Packages, re-crear con password vacía."),

            ["NotSigned"] = new("NotSigned",
                "Olvidaste firmar el .msix (slide 16.4).",
                "Get-AuthenticodeSignature MiApp.msix → Status = NotSigned.",
                "Volver al wizard y seleccionar el certificado en Signing."),

            ["CannotRegister"] = new("CannotRegister",
                "Ya existe una versión con publisher distinto (slide 16.5).",
                "Get-AppPackage -Name '<package-name>' → ves la instalación previa.",
                "Get-AppPackage -Name '<n>' | Remove-AppPackage, luego Add-AppPackage."),

            ["NoStartMenu"] = new("NoStartMenu",
                "Instalación con error silencioso o icono no renderizado (slide 16.6).",
                "Get-AppPackage -Name '<n>' | Format-List → revisar Status / IsResourcePackage.",
                "Remove-AppPackage + Add-AppPackage; revisar Event Viewer → AppXDeploymentClient."),
        };

    // Acepta tanto el HRESULT (0x80073CFD) como un alias del mensaje
    // (NotSigned, MSB3325, etc.).
    public static DiagnosticoError? Diagnosticar(string codigoOMensaje)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoOMensaje);
        string clave = codigoOMensaje.Trim();

        if (Catalogo.TryGetValue(clave, out var exacto))
            return exacto;

        // Match por contención: "0x80073CFD: The current user..." → 0x80073CFD.
        foreach (var (k, v) in Catalogo)
            if (codigoOMensaje.Contains(k, StringComparison.OrdinalIgnoreCase))
                return v;

        return null;
    }

    public static IReadOnlyCollection<DiagnosticoError> Todos() =>
        [.. Catalogo.Values];
}
