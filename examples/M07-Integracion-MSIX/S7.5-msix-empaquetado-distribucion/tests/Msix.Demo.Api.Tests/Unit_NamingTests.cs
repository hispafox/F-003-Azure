using Msix.Demo.Api.Msix;

namespace Msix.Demo.Api.Tests;

// CAPA 1 — nombre del paquete + versionado (slides 3, 4, 10, 11).
[Trait("Category", "Unit")]
public class Unit_NamingTests
{
    [Fact]
    public void Nombre_Archivo_Formato_Slide_4()
    {
        var m = new AppxManifest("MiEmpresa.VentasDesktop", "CN=MiEmpresa",
            "2.4.1.0", "x64", "10.0.17763.0", []);
        Assert.Equal("MiEmpresa.VentasDesktop_2.4.1.0_x64.msix",
            PackageNamingResolver.NombreArchivo(m));
    }

    [Fact]
    public void Nombre_Bundle_Slide_10()
        => Assert.Equal("MiEmpresa.App_2.4.1.0.msixbundle",
            PackageNamingResolver.NombreBundle("MiEmpresa.App", "2.4.1.0"));

    [Fact]
    public void Siguiente_Version_Usa_BuildId_Slide_11()
        => Assert.Equal("2.4.1234.0",
            PackageNamingResolver.SiguienteVersion("2.4.1.0", 1234));

    [Fact]
    public void Siguiente_Version_Lanza_Si_Actual_Mal_Formada()
        => Assert.Throws<FormatException>(() =>
            PackageNamingResolver.SiguienteVersion("2.4", 1));

    [Theory]
    [InlineData("2.4.1.0", "2.4.2.0", true)]
    [InlineData("2.4.1.0", "2.4.1.0", false)]    // igual no es incremental
    [InlineData("2.4.2.0", "2.4.1.0", false)]    // hacia atrás
    [InlineData("2.4.1.0", "v2.4.2.0", false)]   // formato inválido
    public void Es_Incremental(string anterior, string nueva, bool esperado)
        => Assert.Equal(esperado,
            PackageNamingResolver.EsIncremental(anterior, nueva));
}
