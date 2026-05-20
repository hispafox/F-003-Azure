using System.Xml.Linq;

namespace AutoUpdate.Demo.Api.AutoUpdate;

// Slides 2-3, 13 — el `.appinstaller` es un XML que dice a Windows
// dónde está el .msix y cómo auto-actualizar. Modelo plano y testeable.
public sealed record MainPackageConfig(
    string Name, string Version, string Publisher,
    string ProcessorArchitecture, string PackageUri);

public sealed record UpdateSettingsConfig(
    int HoursBetweenUpdateChecks = 1,
    bool ShowPrompt = true,
    bool UpdateBlocksActivation = false,   // slide 13 — true = obligatoria
    bool AutomaticBackgroundTask = true,
    bool ForceUpdateFromAnyVersion = true);

public sealed record AppInstallerConfig(
    string AppInstallerUri,                 // URL pública del .appinstaller
    string Version,                          // versión del propio .appinstaller
    MainPackageConfig MainPackage,
    UpdateSettingsConfig UpdateSettings);

// Slides 2-3, 13 — builder + parser del .appinstaller. Lógica pura
// (System.Xml.Linq), sin Azure.
public static class AppInstallerBuilder
{
    private static readonly XNamespace Ns = "http://schemas.microsoft.com/appx/appinstaller/2018";

    public static string Construir(AppInstallerConfig cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        ArgumentException.ThrowIfNullOrWhiteSpace(cfg.AppInstallerUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(cfg.Version);
        ArgumentNullException.ThrowIfNull(cfg.MainPackage);
        ArgumentNullException.ThrowIfNull(cfg.UpdateSettings);

        var us = cfg.UpdateSettings;
        var updateSettings = new XElement(Ns + "UpdateSettings",
            new XElement(Ns + "OnLaunch",
                new XAttribute("HoursBetweenUpdateChecks", us.HoursBetweenUpdateChecks),
                new XAttribute("ShowPrompt", us.ShowPrompt),
                new XAttribute("UpdateBlocksActivation", us.UpdateBlocksActivation)));

        if (us.AutomaticBackgroundTask)
            updateSettings.Add(new XElement(Ns + "AutomaticBackgroundTask"));

        if (us.ForceUpdateFromAnyVersion)
            updateSettings.Add(new XElement(Ns + "ForceUpdateFromAnyVersion", true));

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(Ns + "AppInstaller",
                new XAttribute("Uri", cfg.AppInstallerUri),
                new XAttribute("Version", cfg.Version),
                new XElement(Ns + "MainPackage",
                    new XAttribute("Name", cfg.MainPackage.Name),
                    new XAttribute("Version", cfg.MainPackage.Version),
                    new XAttribute("Publisher", cfg.MainPackage.Publisher),
                    new XAttribute("ProcessorArchitecture", cfg.MainPackage.ProcessorArchitecture),
                    new XAttribute("Uri", cfg.MainPackage.PackageUri)),
                updateSettings));

        return doc.Declaration + Environment.NewLine + doc.Root!.ToString();
    }

    public static AppInstallerConfig Parsear(string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch (Exception ex) { throw new FormatException("XML inválido.", ex); }

        var root = doc.Root ?? throw new FormatException("Sin raíz.");
        var main = root.Elements().FirstOrDefault(e => e.Name.LocalName == "MainPackage")
            ?? throw new FormatException("Falta <MainPackage>.");
        var us = root.Elements().FirstOrDefault(e => e.Name.LocalName == "UpdateSettings");
        var onLaunch = us?.Elements().FirstOrDefault(e => e.Name.LocalName == "OnLaunch");

        return new AppInstallerConfig(
            AppInstallerUri: root.Attribute("Uri")?.Value ?? "",
            Version: root.Attribute("Version")?.Value ?? "",
            MainPackage: new MainPackageConfig(
                Name: main.Attribute("Name")?.Value ?? "",
                Version: main.Attribute("Version")?.Value ?? "",
                Publisher: main.Attribute("Publisher")?.Value ?? "",
                ProcessorArchitecture: main.Attribute("ProcessorArchitecture")?.Value ?? "",
                PackageUri: main.Attribute("Uri")?.Value ?? ""),
            UpdateSettings: new UpdateSettingsConfig(
                HoursBetweenUpdateChecks:
                    int.TryParse(onLaunch?.Attribute("HoursBetweenUpdateChecks")?.Value, out var h) ? h : 1,
                ShowPrompt:
                    bool.TryParse(onLaunch?.Attribute("ShowPrompt")?.Value, out var sp) && sp,
                UpdateBlocksActivation:
                    bool.TryParse(onLaunch?.Attribute("UpdateBlocksActivation")?.Value, out var uba) && uba,
                AutomaticBackgroundTask:
                    us?.Elements().Any(e => e.Name.LocalName == "AutomaticBackgroundTask") ?? false,
                ForceUpdateFromAnyVersion:
                    bool.TryParse(us?.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == "ForceUpdateFromAnyVersion")?.Value,
                        out var fu) && fu));
    }
}
