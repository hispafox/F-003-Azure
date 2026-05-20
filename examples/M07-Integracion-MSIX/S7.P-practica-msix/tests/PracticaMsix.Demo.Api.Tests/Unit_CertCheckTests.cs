using PracticaMsix.Demo.Api.Practica;

namespace PracticaMsix.Demo.Api.Tests;

// CAPA 1 — check Publisher↔Cert (slide 7: el error #1 de la práctica).
[Trait("Category", "Unit")]
public class Unit_CertCheckTests
{
    [Fact]
    public void Coincide_Exactamente_Es_Ok()
        => Assert.True(PracticaCertCheck.PublisherCoincide(
            "CN=MsixDemoCurso", "CN=MsixDemoCurso").Ok);

    [Fact]
    public void Diferencia_En_Espacios_Falla()
    {
        // Windows no normaliza espacios — el match es ordinal y completo.
        var r = PracticaCertCheck.PublisherCoincide(
            "CN=MsixDemoCurso", "CN=MsixDemoCurso ");
        Assert.False(r.Ok);
        Assert.Contains("≠", r.Razon);
    }

    [Fact]
    public void Sin_Prefijo_CN_Falla()
    {
        var r = PracticaCertCheck.PublisherCoincide(
            "MsixDemoCurso", "CN=MsixDemoCurso");
        Assert.False(r.Ok);
        Assert.Contains("CN=", r.Razon);
    }

    [Fact]
    public void Eku_Code_Signing_Presente_Es_Ok()
        => Assert.True(PracticaCertCheck.UsoCorrecto(
            ["1.3.6.1.5.5.7.3.3", "1.3.6.1.5.5.7.3.2"]).Ok);

    [Fact]
    public void Eku_Sin_Code_Signing_Falla()
        => Assert.False(PracticaCertCheck.UsoCorrecto(
            ["1.3.6.1.5.5.7.3.1"]).Ok);

    [Fact]
    public void Publisher_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            PracticaCertCheck.PublisherCoincide("  ", "CN=X"));
}
