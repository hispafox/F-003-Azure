using System.Xml.Linq;

namespace PracticaMsix.Demo.Api.Practica;

public sealed record ParametrosPractica(
    string Empresa, string App, string Version, string BaseUri);

// Slides 6, 11 — construye los dos artefactos canónicos de la práctica
// para que el alumno los compare con los suyos: Package.appxmanifest
// (slide 6) y .appinstaller (slide 11). Lógica pura, XLinq.
public static class PracticaArtefactosBuilder
{
    private static readonly XNamespace Foundation =
        "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
    private static readonly XNamespace Uap =
        "http://schemas.microsoft.com/appx/manifest/uap/windows10";
    private static readonly XNamespace Rescap =
        "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";
    private static readonly XNamespace AppInstaller =
        "http://schemas.microsoft.com/appx/appinstaller/2018";

    public static string ConstruirManifest(ParametrosPractica p)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentException.ThrowIfNullOrWhiteSpace(p.Empresa);
        ArgumentException.ThrowIfNullOrWhiteSpace(p.App);
        ArgumentException.ThrowIfNullOrWhiteSpace(p.Version);

        string identityName = $"{p.Empresa}.{p.App}";
        string publisher = $"CN={p.Empresa}";

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(Foundation + "Package",
                new XAttribute(XNamespace.Xmlns + "uap", Uap.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "rescap", Rescap.NamespaceName),
                new XElement(Foundation + "Identity",
                    new XAttribute("Name", identityName),
                    new XAttribute("Publisher", publisher),
                    new XAttribute("Version", p.Version),
                    new XAttribute("ProcessorArchitecture", "x64")),
                new XElement(Foundation + "Properties",
                    new XElement(Foundation + "DisplayName", $"{p.App} (Curso AZ-204)"),
                    new XElement(Foundation + "PublisherDisplayName", p.Empresa)),
                new XElement(Foundation + "Dependencies",
                    new XElement(Foundation + "TargetDeviceFamily",
                        new XAttribute("Name", "Windows.Desktop"),
                        new XAttribute("MinVersion", "10.0.17763.0"),
                        new XAttribute("MaxVersionTested", "10.0.22621.0"))),
                new XElement(Foundation + "Capabilities",
                    new XElement(Foundation + "Capability",
                        new XAttribute("Name", "internetClient")),
                    new XElement(Rescap + "Capability",
                        new XAttribute("Name", "runFullTrust")))));

        return doc.Declaration + Environment.NewLine + doc.Root!.ToString();
    }

    public static string ConstruirAppInstaller(ParametrosPractica p)
    {
        ArgumentNullException.ThrowIfNull(p);
        ArgumentException.ThrowIfNullOrWhiteSpace(p.BaseUri);

        string identityName = $"{p.Empresa}.{p.App}";
        string publisher = $"CN={p.Empresa}";
        string baseUri = p.BaseUri.TrimEnd('/');

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(AppInstaller + "AppInstaller",
                new XAttribute("Uri", $"{baseUri}/{p.App}.appinstaller"),
                new XAttribute("Version", p.Version),
                new XElement(AppInstaller + "MainPackage",
                    new XAttribute("Name", identityName),
                    new XAttribute("Version", p.Version),
                    new XAttribute("Publisher", publisher),
                    new XAttribute("ProcessorArchitecture", "x64"),
                    new XAttribute("Uri", $"{baseUri}/{identityName}_{p.Version}_x64.msix")),
                new XElement(AppInstaller + "UpdateSettings",
                    new XElement(AppInstaller + "OnLaunch",
                        new XAttribute("HoursBetweenUpdateChecks", 0)))));

        return doc.Declaration + Environment.NewLine + doc.Root!.ToString();
    }
}
