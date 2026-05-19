using Apim.Demo.Api.Apim;

namespace Apim.Demo.Api.Tests;

// CAPA 1 — selección de tier (slides 3/32) + ¿buen caso? (slide 16).
[Trait("Category", "Unit")]
public class Unit_TierTests
{
    [Fact]
    public void Vnet_Es_Premium()
    {
        var r = ApimTierAdvisor.RecomendarTier(
            new EscenarioApim(Produccion: true, RequiereVNet: true));
        Assert.Equal(ApimTier.Premium, r.Tier);
        Assert.Contains("2200", r.CosteAproximado);
    }

    [Fact]
    public void Multi_Region_Es_Premium()
        => Assert.Equal(ApimTier.Premium,
            ApimTierAdvisor.RecomendarTier(
                new EscenarioApim(MultiRegion: true)).Tier);

    [Fact]
    public void Prod_Mas_De_1000_Rps_Es_Premium()
        => Assert.Equal(ApimTier.Premium,
            ApimTierAdvisor.RecomendarTier(
                new EscenarioApim(Produccion: true, LlamadasPorSegundo: 5000)).Tier);

    [Fact]
    public void Dev_Test_Es_Developer()
        => Assert.Equal(ApimTier.Developer,
            ApimTierAdvisor.RecomendarTier(
                new EscenarioApim(DevTest: true)).Tier);

    [Fact]
    public void Produccion_Media_Es_Standard()
        => Assert.Equal(ApimTier.Standard,
            ApimTierAdvisor.RecomendarTier(
                new EscenarioApim(Produccion: true)).Tier);

    [Fact]
    public void Bajo_Volumen_Sin_Requisitos_Es_Consumption()
    {
        var r = ApimTierAdvisor.RecomendarTier(
            new EscenarioApim(LlamadasMes: 50_000));
        Assert.Equal(ApimTier.Consumption, r.Tier);
        Assert.Contains("gratis", r.CosteAproximado);
    }

    [Fact]
    public void Buen_Caso_Cuando_Pesan_Las_Senales_A_Favor()
    {
        var d = ApimTierAdvisor.EsBuenCaso(
            multiplesApis: true, necesitaRateLimitOCache: true,
            exponeATerceros: true, versionadoCentral: true, analytics: true,
            unaApiSimple: false, soloTraficoInterno: false,
            presupuestoLimitado: false);
        Assert.True(d.Recomendado);
    }

    [Fact]
    public void Mal_Caso_Una_Api_Interna_Sin_Presupuesto()
    {
        var d = ApimTierAdvisor.EsBuenCaso(
            false, false, false, false, false,
            unaApiSimple: true, soloTraficoInterno: true,
            presupuestoLimitado: true);
        Assert.False(d.Recomendado);
        Assert.Contains(d.Razones, x => x.Contains("overhead"));
    }
}
