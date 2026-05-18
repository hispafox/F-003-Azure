using Sql.Demo.Api.Sql;

namespace Sql.Demo.Api.Tests;

// CAPA 1 — el modelo de compra (slides 4, 5, 21) como tabla de decisión.
[Trait("Category", "Unit")]
public class Unit_SqlTierAdvisorTests
{
    [Theory]
    // > 1 TB → Hyperscale gana sobre todo lo demás (slide 21).
    [InlineData(true, 10, 2000, SqlTier.Hyperscale)]
    [InlineData(false, 10, 1025, SqlTier.Hyperscale)]
    // Intermitente → Serverless (slide 5), aunque sea pequeña.
    [InlineData(true, 5, 1, SqlTier.GeneralPurposeServerless)]
    [InlineData(true, 200, 50, SqlTier.GeneralPurposeServerless)]
    // Sostenido + muchas conexiones → vCore General Purpose (slide 10).
    [InlineData(false, 61, 10, SqlTier.GeneralPurpose)]
    // Diminuta dev/test → Basic (slide 4).
    [InlineData(false, 5, 2, SqlTier.Basic)]
    // Caso por defecto del curso → S0 (slide 4).
    [InlineData(false, 30, 20, SqlTier.S0)]
    [InlineData(false, 6, 2, SqlTier.S0)]   // 6 conexiones ya no es "diminuta"
    public void Sugerir_TablaDeDecision(
        bool intermitente, int maxCon, int gb, SqlTier esperado)
        => Assert.Equal(esperado, SqlTierAdvisor.Sugerir(intermitente, maxCon, gb));

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(10, -1)]
    public void Sugerir_ValoresNegativos_Lanza(int maxCon, int gb)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => SqlTierAdvisor.Sugerir(false, maxCon, gb));
}
