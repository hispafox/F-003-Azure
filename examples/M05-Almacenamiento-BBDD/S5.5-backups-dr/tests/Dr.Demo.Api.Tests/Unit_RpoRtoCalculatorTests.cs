using Dr.Demo.Api.Dr;

namespace Dr.Demo.Api.Tests;

// CAPA 1 — RPO/RTO y estrategia por criticidad (slides 8, 22, 24).
[Trait("Category", "Unit")]
public class Unit_RpoRtoCalculatorTests
{
    [Theory]
    [InlineData(Criticidad.MisionCritica, EstrategiaDr.ActiveActive)]
    [InlineData(Criticidad.Importante, EstrategiaDr.WarmStandby)]
    [InlineData(Criticidad.Interno, EstrategiaDr.ColdStandby)]
    public void Recomendar_Segun_Criticidad(Criticidad c, EstrategiaDr esperada)
        => Assert.Equal(esperada, RpoRtoCalculator.Recomendar(c));

    [Fact]
    public void ActiveActive_Es_El_Mas_Caro_Y_El_De_Menor_Rto()
    {
        var aa = RpoRtoCalculator.Perfil(EstrategiaDr.ActiveActive);
        var cold = RpoRtoCalculator.Perfil(EstrategiaDr.ColdStandby);
        Assert.True(aa.MultiplicadorCoste > cold.MultiplicadorCoste);
        Assert.True(aa.RtoMaxMinutos < cold.RtoMaxMinutos);
    }

    [Theory]
    // WarmStandby (RPO 15 / RTO 60) cumple objetivos holgados...
    [InlineData(EstrategiaDr.WarmStandby, 15, 60, true)]
    // ...pero no un RTO de 10 min.
    [InlineData(EstrategiaDr.WarmStandby, 15, 10, false)]
    // ActiveActive cumple casi cualquier objetivo razonable.
    [InlineData(EstrategiaDr.ActiveActive, 1, 1, true)]
    // ColdStandby no cumple objetivos exigentes.
    [InlineData(EstrategiaDr.ColdStandby, 5, 30, false)]
    public void CumpleObjetivos(EstrategiaDr e, int rpo, int rto, bool esperado)
        => Assert.Equal(esperado, RpoRtoCalculator.CumpleObjetivos(e, rpo, rto));

    [Fact]
    public void CumpleObjetivos_Negativo_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => RpoRtoCalculator.CumpleObjetivos(EstrategiaDr.WarmStandby, -1, 60));
}
