using Storage.Demo.Api.Storage;

namespace Storage.Demo.Api.Tests;

// CAPA 1 — la curva de lifecycle (slide 5) como tabla de decisión.
[Trait("Category", "Unit")]
public class Unit_AccessTierPolicyTests
{
    [Theory]
    [InlineData(0, AccessTier.Hot)]
    [InlineData(29, AccessTier.Hot)]
    [InlineData(30, AccessTier.Cool)]     // umbral a Cool
    [InlineData(179, AccessTier.Cool)]
    [InlineData(180, AccessTier.Archive)] // umbral a Archive
    [InlineData(400, AccessTier.Archive)]
    public void Sugerir_Escalonado(int dias, AccessTier esperado)
        => Assert.Equal(esperado, AccessTierPolicy.Sugerir(dias));

    [Theory]
    [InlineData(364, false)]
    [InlineData(365, true)]   // umbral de borrado
    [InlineData(500, true)]
    public void DebeBorrarse_Al_Ano(int dias, bool esperado)
        => Assert.Equal(esperado, AccessTierPolicy.DebeBorrarse(dias));

    [Fact]
    public void Sugerir_Dias_Negativos_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() => AccessTierPolicy.Sugerir(-1));
}
