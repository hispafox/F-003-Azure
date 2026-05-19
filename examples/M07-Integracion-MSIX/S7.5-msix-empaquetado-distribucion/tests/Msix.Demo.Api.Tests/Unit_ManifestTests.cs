using Msix.Demo.Api.Msix;

namespace Msix.Demo.Api.Tests;

// CAPA 1 — validación del Package.appxmanifest (slides 3, 15, 28).
[Trait("Category", "Unit")]
public class Unit_ManifestTests
{
    private static AppxManifest Bueno() => new(
        IdentityName: "MiEmpresa.VentasDesktop",
        Publisher: "CN=MiEmpresa, O=MiOrg, C=ES",
        Version: "2.4.1.0",
        ProcessorArchitecture: "x64",
        TargetMinVersion: "10.0.17763.0",
        Capabilities: ["internetClient"]);

    [Fact]
    public void Manifest_Correcto_Es_Valido()
    {
        var r = AppxManifestValidator.Validar(Bueno());
        Assert.True(r.Valido);
        Assert.Empty(r.Problemas);
    }

    [Theory]
    [InlineData("sin-formato")]
    [InlineData("Empresa")]
    [InlineData("123.App")]
    public void Identity_Name_Incorrecto(string nombre)
    {
        var r = AppxManifestValidator.Validar(Bueno() with { IdentityName = nombre });
        Assert.False(r.Valido);
        Assert.Contains(r.Problemas, p => p.Contains("Identity.Name"));
    }

    [Fact]
    public void Publisher_Sin_CN_Detectado()
    {
        var r = AppxManifestValidator.Validar(Bueno() with { Publisher = "MiEmpresa" });
        Assert.Contains(r.Problemas, p => p.Contains("CN="));
    }

    [Theory]
    [InlineData("2.4")]
    [InlineData("2.4.1")]
    [InlineData("v2.4.1.0")]
    public void Version_No_Es_Major_Minor_Build_Revision(string version)
    {
        var r = AppxManifestValidator.Validar(Bueno() with { Version = version });
        Assert.Contains(r.Problemas, p => p.Contains("Version"));
    }

    [Fact]
    public void Architecture_No_Soportada_Detectada()
        => Assert.Contains(
            AppxManifestValidator.Validar(Bueno() with { ProcessorArchitecture = "mips" }).Problemas,
            p => p.Contains("ProcessorArchitecture"));

    [Fact]
    public void Target_Min_Version_Anterior_A_1809_Detectada()
    {
        var r = AppxManifestValidator.Validar(
            Bueno() with { TargetMinVersion = "10.0.17134.0" });   // 1803
        Assert.Contains(r.Problemas, p => p.Contains("MinVersion"));
    }

    [Fact]
    public void Capability_Restringida_Requiere_Rescap()
    {
        var r = AppxManifestValidator.Validar(
            Bueno() with { Capabilities = ["runFullTrust", "internetClient"] });
        Assert.Contains(r.Problemas, p => p.Contains("rescap"));
    }

    [Fact]
    public void Parsear_Manifest_Mininal()
    {
        const string xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">
          <Identity Name="MiEmpresa.App" Publisher="CN=MiEmpresa" Version="1.2.3.4" ProcessorArchitecture="x64"/>
          <Dependencies>
            <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0"/>
          </Dependencies>
          <Capabilities>
            <Capability Name="internetClient"/>
            <Capability Name="runFullTrust"/>
          </Capabilities>
        </Package>
        """;
        var m = AppxManifestValidator.Parsear(xml);

        Assert.Equal("MiEmpresa.App", m.IdentityName);
        Assert.Equal("CN=MiEmpresa", m.Publisher);
        Assert.Equal("1.2.3.4", m.Version);
        Assert.Equal("x64", m.ProcessorArchitecture);
        Assert.Equal("10.0.17763.0", m.TargetMinVersion);
        Assert.Equal(2, m.Capabilities.Count);
    }

    [Fact]
    public void Parsear_Xml_Invalido_Lanza()
        => Assert.Throws<FormatException>(() =>
            AppxManifestValidator.Parsear("<not-xml"));
}
