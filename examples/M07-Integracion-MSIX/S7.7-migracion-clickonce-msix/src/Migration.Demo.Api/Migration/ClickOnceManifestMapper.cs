using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Migration.Demo.Api.Migration;

// Slide 6 — campos mínimos de un .application (ClickOnce) que se
// mapean a la Identity del MSIX.
public sealed record ClickOnceManifest(
    string AssemblyName,
    string Publisher,
    string Version);

// Salida: AppxManifest mínimo (slide 6 + S7.5).
public sealed record MappedAppxManifest(
    string IdentityName,
    string Publisher,
    string Version,
    string ProcessorArchitecture,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> CapabilitiesRescap);

// Slides 6, 8 — mapeo determinista de .application (ClickOnce) →
// AppxManifest (MSIX). Pura, sin IO.
public static partial class ClickOnceManifestMapper
{
    [GeneratedRegex(@"[^A-Za-z0-9]+")]
    private static partial Regex NoIdentRegex();

    public static MappedAppxManifest Mapear(ClickOnceManifest co)
    {
        ArgumentNullException.ThrowIfNull(co);
        ArgumentException.ThrowIfNullOrWhiteSpace(co.AssemblyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(co.Publisher);
        ArgumentException.ThrowIfNullOrWhiteSpace(co.Version);

        return new MappedAppxManifest(
            IdentityName: NormalizarIdentity(co.Publisher, co.AssemblyName),
            Publisher: NormalizarPublisher(co.Publisher),
            Version: NormalizarVersion(co.Version),
            // Slide 6 — WPF/WinForms necesitan full trust; runFullTrust
            // exige el namespace rescap:.
            ProcessorArchitecture: "x64",
            Capabilities: ["internetClient"],
            CapabilitiesRescap: ["runFullTrust"]);
    }

    // Slide 6 — el Identity.Name debe tener forma Empresa.AppName y
    // empezar por letra. Sanitizamos publisher + assemblyName.
    private static string NormalizarIdentity(string publisher, string app)
    {
        string emp = NoIdentRegex().Replace(publisher, "");
        string nom = NoIdentRegex().Replace(app, "");
        if (emp.Length == 0 || nom.Length == 0)
            throw new FormatException("Publisher/AssemblyName sin caracteres válidos.");
        if (!char.IsLetter(emp[0])) emp = "X" + emp;
        if (!char.IsLetter(nom[0])) nom = "X" + nom;
        return $"{emp}.{nom}";
    }

    // Slide 6 — Publisher DEBE empezar por `CN=` y coincidir con el
    // Subject del certificado de firma. Si ya es un DN se mantiene.
    private static string NormalizarPublisher(string publisher) =>
        publisher.StartsWith("CN=", StringComparison.Ordinal)
            ? publisher
            : $"CN={publisher}";

    // ClickOnce permite versiones de 4 partes. Si la versión llega con
    // 1-3 partes, completamos con ceros hasta Major.Minor.Build.Revision.
    private static string NormalizarVersion(string v)
    {
        var partes = v.Split('.');
        if (partes.Length == 0 || partes.Length > 4)
            throw new FormatException($"Version '{v}' fuera de rango (1-4 componentes).");
        foreach (var p in partes)
            if (!int.TryParse(p, out _))
                throw new FormatException($"Version '{v}' contiene un componente no numérico.");

        var lista = partes.ToList();
        while (lista.Count < 4) lista.Add("0");
        return string.Join('.', lista);
    }

    // Slide 8 (S7.7 contexto) — leer las partes que nos interesan del
    // .application (ClickOnce). El XML real tiene más cosas; aquí
    // tomamos solo assemblyIdentity.
    public static ClickOnceManifest Parsear(string applicationXml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationXml);

        XDocument doc;
        try { doc = XDocument.Parse(applicationXml); }
        catch (Exception ex) { throw new FormatException("XML inválido.", ex); }

        var root = doc.Root ?? throw new FormatException("Sin elemento raíz.");
        var asmId = root.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "assemblyIdentity")
            ?? throw new FormatException("Falta <assemblyIdentity>.");
        var publisher = root.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "description")
            ?.Attribute(XName.Get("publisher", "urn:schemas-microsoft-com:asm.v2"))
            ?.Value
            ?? root.Elements()
                .FirstOrDefault(e => e.Name.LocalName == "description")
                ?.Attributes().FirstOrDefault(a => a.Name.LocalName == "publisher")
                ?.Value
            ?? "";

        return new ClickOnceManifest(
            AssemblyName: asmId.Attribute("name")?.Value ?? "",
            Publisher: publisher,
            Version: asmId.Attribute("version")?.Value ?? "");
    }
}
