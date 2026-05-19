using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Msix.Demo.Api.Msix;

// Slide 3 — los campos mínimos del Package.appxmanifest. Modelo plano
// y testeable (no dependemos del manifest XML directamente en la API).
public sealed record AppxManifest(
    string IdentityName,
    string Publisher,
    string Version,
    string ProcessorArchitecture,
    string TargetMinVersion,
    IReadOnlyList<string> Capabilities);

public sealed record ResultadoValidacion(
    bool Valido, IReadOnlyList<string> Problemas);

// Slides 3, 15, 28 — validador del manifest. Lógica pura: parsea el XML
// y comprueba las reglas que rompen el empaquetado o el sideloading.
public static partial class AppxManifestValidator
{
    // Slide 7 (S7.4) — Windows 10 1809+ (10.0.17763.0).
    public const string MinTargetVersionSoportado = "10.0.17763.0";

    // Slide 3 — Identity.Name: Empresa.NombreApp (alfanumérico + puntos).
    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)+$")]
    private static partial Regex IdentityNameRegex();

    // Slide 3 — Version: cuatro componentes numéricos.
    [GeneratedRegex(@"^\d+\.\d+\.\d+\.\d+$")]
    private static partial Regex VersionRegex();

    // Slide 3/28 — restricted capabilities que requieren declaración
    // específica del namespace rescap:.
    private static readonly HashSet<string> CapacidadesRestringidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "runFullTrust",
        "broadFileSystemAccess",
        "allAppMods",
        "enterpriseDataPolicy",
    };

    public static ResultadoValidacion Validar(AppxManifest m)
    {
        ArgumentNullException.ThrowIfNull(m);
        var p = new List<string>();

        // Identity.Name — formato Empresa.NombreApp.
        if (string.IsNullOrWhiteSpace(m.IdentityName) ||
            !IdentityNameRegex().IsMatch(m.IdentityName))
            p.Add($"Identity.Name '{m.IdentityName}' no cumple el formato 'Empresa.NombreApp' (slide 3).");

        // Publisher — debe empezar por CN= (Subject del certificado).
        if (string.IsNullOrWhiteSpace(m.Publisher) ||
            !m.Publisher.StartsWith("CN=", StringComparison.Ordinal))
            p.Add($"Publisher '{m.Publisher}' debe empezar por 'CN=' y coincidir con el Subject del certificado (slide 3/5).");

        // Version — Major.Minor.Build.Revision.
        if (string.IsNullOrWhiteSpace(m.Version) ||
            !VersionRegex().IsMatch(m.Version))
            p.Add($"Version '{m.Version}' no es Major.Minor.Build.Revision (slide 3).");

        // ProcessorArchitecture — slide 10: x64 / arm64 / neutral.
        if (m.ProcessorArchitecture is not ("x64" or "arm64" or "neutral" or "x86"))
            p.Add($"ProcessorArchitecture '{m.ProcessorArchitecture}' no soportada (usar x64/arm64/neutral, slide 10).");

        // TargetDeviceFamily MinVersion ≥ Windows 10 1809 (slide 7 S7.4).
        if (!Version.TryParse(m.TargetMinVersion, out var v) ||
            v < Version.Parse(MinTargetVersionSoportado))
            p.Add($"TargetDeviceFamily MinVersion '{m.TargetMinVersion}' < {MinTargetVersionSoportado} (Windows 10 1809 mínimo).");

        // Capabilities restringidas → avisar (no es un error en sí pero
        // exige el namespace `rescap:`, slide 3 — `<rescap:Capability …>`).
        foreach (var cap in m.Capabilities ?? [])
            if (CapacidadesRestringidas.Contains(cap))
                p.Add($"Capability '{cap}' es restringida: declárala con el namespace 'rescap:' (slide 3/15).");

        return new ResultadoValidacion(p.Count == 0, p);
    }

    // Parsea un Package.appxmanifest minimalmente (slide 3). No
    // dependencias externas: System.Xml.Linq.
    public static AppxManifest Parsear(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch (Exception ex) { throw new FormatException("XML inválido.", ex); }

        // Aceptamos cualquier namespace de manifest (foundation/uap/...).
        var root = doc.Root ?? throw new FormatException("Sin elemento raíz.");
        var identity = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Identity")
            ?? throw new FormatException("Falta <Identity>.");
        var dependencies = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Dependencies");
        var capabilities = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Capabilities");

        return new AppxManifest(
            IdentityName: identity.Attribute("Name")?.Value ?? "",
            Publisher: identity.Attribute("Publisher")?.Value ?? "",
            Version: identity.Attribute("Version")?.Value ?? "",
            ProcessorArchitecture: identity.Attribute("ProcessorArchitecture")?.Value ?? "neutral",
            TargetMinVersion:
                dependencies?.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "TargetDeviceFamily")
                    ?.Attribute("MinVersion")?.Value ?? "",
            Capabilities:
                capabilities?.Elements()
                    .Where(e => e.Name.LocalName == "Capability")
                    .Select(e => e.Attribute("Name")?.Value ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList() ?? []);
    }
}
