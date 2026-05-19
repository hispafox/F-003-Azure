using Datos.Demo.Api.Datos;

namespace Datos.Demo.Api.Tests;

// CAPA 1 — TLS mínimo + cifrado en tránsito (slides 3, 5, 14).
[Trait("Category", "Unit")]
public class Unit_TlsTransitValidatorTests
{
    [Theory]
    [InlineData("1.2", true)]
    [InlineData("1.3", true)]
    [InlineData("TLS1_2", true)]
    [InlineData("v1.2", true)]
    [InlineData("1.0", false)]      // deprecado (slide 3)
    [InlineData("TLS1_1", false)]   // deprecado
    public void VersionPermitida(string v, bool esperado)
        => Assert.Equal(esperado, TlsTransitValidator.VersionPermitida(v));

    [Theory]
    [InlineData("Server=x;Database=d;Encrypt=true;", true)]
    [InlineData("Server=x;Encrypt=Strict;", true)]
    [InlineData("Server=x;Database=d;", false)]
    public void SqlCifradoEnTransito(string cs, bool esperado)
        => Assert.Equal(esperado, TlsTransitValidator.SqlCifradoEnTransito(cs));

    [Theory]
    [InlineData("DefaultEndpointsProtocol=https;AccountName=x;", true)]
    [InlineData("https://stx.blob.core.windows.net", true)]
    [InlineData("DefaultEndpointsProtocol=http;AccountName=x;", false)]
    public void StorageCifradoEnTransito(string cs, bool esperado)
        => Assert.Equal(esperado, TlsTransitValidator.StorageCifradoEnTransito(cs));

    [Fact]
    public void Version_Vacia_Lanza()
        => Assert.Throws<ArgumentException>(
            () => TlsTransitValidator.VersionPermitida("  "));
}
