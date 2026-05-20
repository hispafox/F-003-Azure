using Migration.Demo.Api.Migration;

namespace Migration.Demo.Api.Tests;

// CAPA 1 — mapper ClickOnce → AppxManifest (slides 6, 8).
[Trait("Category", "Unit")]
public class Unit_MapperTests
{
    [Fact]
    public void Identity_Empresa_Punto_App_Sanitizado()
    {
        var m = ClickOnceManifestMapper.Mapear(
            new ClickOnceManifest("VentasDesktop", "Mi Empresa, S.L.", "2.4.1.0"));
        Assert.Equal("MiEmpresaSL.VentasDesktop", m.IdentityName);
    }

    [Fact]
    public void Publisher_Anade_CN_Si_Falta()
    {
        var m = ClickOnceManifestMapper.Mapear(
            new ClickOnceManifest("App", "MiEmpresa", "1.0.0.0"));
        Assert.StartsWith("CN=", m.Publisher);
    }

    [Fact]
    public void Publisher_Respeta_DN_Existente()
    {
        var m = ClickOnceManifestMapper.Mapear(
            new ClickOnceManifest("App", "CN=MiEmpresa, O=MiOrg", "1.0.0.0"));
        Assert.Equal("CN=MiEmpresa, O=MiOrg", m.Publisher);
    }

    [Theory]
    [InlineData("1", "1.0.0.0")]
    [InlineData("2.4", "2.4.0.0")]
    [InlineData("2.4.1", "2.4.1.0")]
    [InlineData("2.4.1.5", "2.4.1.5")]
    public void Version_Se_Normaliza_A_Cuatro_Componentes(string entrada, string esperado)
    {
        var m = ClickOnceManifestMapper.Mapear(
            new ClickOnceManifest("App", "MiEmpresa", entrada));
        Assert.Equal(esperado, m.Version);
    }

    [Fact]
    public void Version_Con_5_Componentes_Lanza()
        => Assert.Throws<FormatException>(() => ClickOnceManifestMapper.Mapear(
            new ClickOnceManifest("App", "MiEmpresa", "1.2.3.4.5")));

    [Fact]
    public void Version_Con_Texto_Lanza()
        => Assert.Throws<FormatException>(() => ClickOnceManifestMapper.Mapear(
            new ClickOnceManifest("App", "MiEmpresa", "v1.0")));

    [Fact]
    public void Run_Full_Trust_En_Rescap_Slide_6()
    {
        var m = ClickOnceManifestMapper.Mapear(
            new ClickOnceManifest("App", "MiEmpresa", "1.0.0.0"));
        Assert.Contains("runFullTrust", m.CapabilitiesRescap);
        Assert.Contains("internetClient", m.Capabilities);
    }

    [Fact]
    public void Parsear_Application_XML_Minimo()
    {
        const string xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <asmv1:assembly xmlns="urn:schemas-microsoft-com:asm.v2"
                        xmlns:asmv1="urn:schemas-microsoft-com:asm.v1">
          <asmv1:assemblyIdentity name="VentasDesktop" version="2.4.1.0"
            publicKeyToken="0000000000000000" language="neutral" />
          <description publisher="MiEmpresa" />
        </asmv1:assembly>
        """;
        var co = ClickOnceManifestMapper.Parsear(xml);
        Assert.Equal("VentasDesktop", co.AssemblyName);
        Assert.Equal("2.4.1.0", co.Version);
        Assert.Equal("MiEmpresa", co.Publisher);
    }
}
