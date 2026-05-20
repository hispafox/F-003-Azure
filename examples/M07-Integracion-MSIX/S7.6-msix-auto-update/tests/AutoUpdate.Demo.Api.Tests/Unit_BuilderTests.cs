using AutoUpdate.Demo.Api.AutoUpdate;

namespace AutoUpdate.Demo.Api.Tests;

// CAPA 1 — builder/parser del .appinstaller (slides 2-3, 13).
[Trait("Category", "Unit")]
public class Unit_BuilderTests
{
    private static AppInstallerConfig Cfg(bool critica = false) => new(
        AppInstallerUri: "https://stventasprod.blob.core.windows.net/msix/MiApp.appinstaller",
        Version: "2.4.1.0",
        MainPackage: new MainPackageConfig(
            "MiEmpresa.VentasDesktop", "2.4.1.0",
            "CN=MiEmpresa, O=MiOrg, C=ES", "x64",
            "https://stventasprod.blob.core.windows.net/msix/MiApp_2.4.1.0_x64.msix"),
        UpdateSettings: new UpdateSettingsConfig(
            UpdateBlocksActivation: critica));

    [Fact]
    public void Construir_Incluye_Identidad_Y_UpdateSettings()
    {
        var xml = AppInstallerBuilder.Construir(Cfg());
        Assert.Contains("MiEmpresa.VentasDesktop", xml);
        Assert.Contains("Version=\"2.4.1.0\"", xml);
        Assert.Contains("HoursBetweenUpdateChecks=\"1\"", xml);
        Assert.Contains("AutomaticBackgroundTask", xml);
        Assert.Contains("ForceUpdateFromAnyVersion", xml);
    }

    [Fact]
    public void Construir_Critica_Bloquea_Activacion_Slide_13()
    {
        var xml = AppInstallerBuilder.Construir(Cfg(critica: true));
        Assert.Contains("UpdateBlocksActivation=\"true\"", xml);
    }

    [Fact]
    public void RoundTrip_Construir_Y_Parsear_Es_Equivalente()
    {
        var original = Cfg();
        var xml = AppInstallerBuilder.Construir(original);
        var parsed = AppInstallerBuilder.Parsear(xml);

        Assert.Equal(original.AppInstallerUri, parsed.AppInstallerUri);
        Assert.Equal(original.Version, parsed.Version);
        Assert.Equal(original.MainPackage, parsed.MainPackage);
        Assert.Equal(original.UpdateSettings.HoursBetweenUpdateChecks,
            parsed.UpdateSettings.HoursBetweenUpdateChecks);
        Assert.Equal(original.UpdateSettings.AutomaticBackgroundTask,
            parsed.UpdateSettings.AutomaticBackgroundTask);
        Assert.Equal(original.UpdateSettings.ForceUpdateFromAnyVersion,
            parsed.UpdateSettings.ForceUpdateFromAnyVersion);
    }

    [Fact]
    public void Parsear_Xml_Invalido_Lanza()
        => Assert.Throws<FormatException>(() =>
            AppInstallerBuilder.Parsear("<no-xml"));
}
