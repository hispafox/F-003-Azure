using Distribution.Demo.Api.Distribution;

namespace Distribution.Demo.Api.Tests;

// CAPA 1 — recomendación de certificado de firma (slide 8).
[Trait("Category", "Unit")]
public class Unit_CertTests
{
    [Theory]
    [InlineData(EscenarioFirma.Desarrollo, TipoCertificado.SelfSigned)]
    [InlineData(EscenarioFirma.DistribucionInterna, TipoCertificado.EnterpriseCa)]
    [InlineData(EscenarioFirma.DistribucionExterna, TipoCertificado.PublicCa)]
    [InlineData(EscenarioFirma.PublicacionStore, TipoCertificado.MicrosoftStore)]
    public void Por_Escenario(EscenarioFirma e, TipoCertificado esperado)
        => Assert.Equal(esperado, SigningCertAdvisor.Recomendar(e).Tipo);

    [Fact]
    public void Self_Signed_Es_Gratis_Con_Warning()
    {
        var r = SigningCertAdvisor.Recomendar(EscenarioFirma.Desarrollo);
        Assert.Equal("Gratis", r.Coste);
        Assert.Equal("Warning", r.SmartScreen);
    }

    [Fact]
    public void Public_CA_Cuesta_Euros()
        => Assert.Contains("€",
            SigningCertAdvisor.Recomendar(EscenarioFirma.DistribucionExterna).Coste);
}
