using Dr.Demo.Api.Dr;

namespace Dr.Demo.Api.Tests;

// CAPA 1 — retención por regulación (slide 20).
[Trait("Category", "Unit")]
public class Unit_RetentionPolicyAdvisorTests
{
    [Theory]
    [InlineData(Regimen.SecFinra, 6, true)]        // 6 años WORM
    [InlineData(Regimen.SarbanesOxley, 7, false)]  // 7 años
    [InlineData(Regimen.LegalEspana, 30, false)]   // 30 años
    [InlineData(Regimen.FdaCfr11, -1, true)]       // permanente
    public void Requisito_AniosYWorm(Regimen r, int anios, bool worm)
    {
        var req = RetentionPolicyAdvisor.Requisito(r);
        Assert.Equal(anios, req.Anios);
        Assert.Equal(worm, req.Worm);
    }

    [Fact]
    public void Rgpd_Es_Derecho_Al_Olvido()
    {
        var req = RetentionPolicyAdvisor.Requisito(Regimen.Rgpd);
        Assert.True(req.DerechoAlOlvido);
        Assert.Equal(0, req.Anios);
        Assert.Equal(0, RetentionPolicyAdvisor.DiasInmutabilidad(Regimen.Rgpd));
    }

    [Fact]
    public void DiasInmutabilidad_Sec_Son_2190_Dias()   // 6 años × 365
        => Assert.Equal(2190, RetentionPolicyAdvisor.DiasInmutabilidad(Regimen.SecFinra));

    [Fact]
    public void DiasInmutabilidad_Permanente_Es_Menos_Uno()
        => Assert.Equal(-1, RetentionPolicyAdvisor.DiasInmutabilidad(Regimen.FdaCfr11));
}
