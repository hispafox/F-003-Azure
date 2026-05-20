using System.Xml.Linq;
using PracticaMsix.Demo.Api.Practica;

namespace PracticaMsix.Demo.Api.Tests;

// CAPA 1 — builder de los artefactos canónicos (slides 6, 11).
[Trait("Category", "Unit")]
public class Unit_ArtefactosTests
{
    private static ParametrosPractica P() =>
        new("Empresa", "MsixDemo", "1.0.0.0", "https://stventasprod.blob.core.windows.net/msix");

    [Fact]
    public void Manifest_Tiene_Identity_Con_Empresa_App_Y_CN()
    {
        var xml = PracticaArtefactosBuilder.ConstruirManifest(P());
        var doc = XDocument.Parse(xml);
        var identity = doc.Root!.Elements()
            .First(e => e.Name.LocalName == "Identity");

        Assert.Equal("Empresa.MsixDemo", identity.Attribute("Name")!.Value);
        Assert.Equal("CN=Empresa", identity.Attribute("Publisher")!.Value);
        Assert.Equal("1.0.0.0", identity.Attribute("Version")!.Value);
        Assert.Equal("x64", identity.Attribute("ProcessorArchitecture")!.Value);
    }

    [Fact]
    public void Manifest_Declara_RunFullTrust_En_Rescap()
    {
        var xml = PracticaArtefactosBuilder.ConstruirManifest(P());
        // El namespace rescap debe estar declarado y aplicado al runFullTrust.
        Assert.Contains("rescap", xml);
        Assert.Contains("runFullTrust", xml);
    }

    [Fact]
    public void AppInstaller_Apunta_Al_Msix_Con_Empresa_App_Y_Version()
    {
        var xml = PracticaArtefactosBuilder.ConstruirAppInstaller(P());
        var doc = XDocument.Parse(xml);
        var main = doc.Root!.Elements()
            .First(e => e.Name.LocalName == "MainPackage");

        Assert.Equal("Empresa.MsixDemo", main.Attribute("Name")!.Value);
        Assert.Equal("1.0.0.0", main.Attribute("Version")!.Value);
        Assert.Contains("Empresa.MsixDemo_1.0.0.0_x64.msix",
            main.Attribute("Uri")!.Value);
    }

    [Fact]
    public void AppInstaller_UpdateSettings_Inmediato()
    {
        var xml = PracticaArtefactosBuilder.ConstruirAppInstaller(P());
        var doc = XDocument.Parse(xml);
        var onLaunch = doc.Descendants()
            .First(e => e.Name.LocalName == "OnLaunch");
        Assert.Equal("0", onLaunch.Attribute("HoursBetweenUpdateChecks")!.Value);
    }

    [Fact]
    public void Empresa_Vacia_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            PracticaArtefactosBuilder.ConstruirManifest(
                new ParametrosPractica("", "App", "1.0.0.0", "")));
}
