using Sql.Demo.Api.Sql;

namespace Sql.Demo.Api.Tests;

// CAPA 1 — la lista de errores transitorios de Azure SQL (slide 13).
[Trait("Category", "Unit")]
public class Unit_AzureSqlRetryPolicyTests
{
    [Theory]
    [InlineData(4060)]
    [InlineData(40197)]
    [InlineData(40613)]
    [InlineData(49920)]
    public void EsTransitorio_True_Para_Codigos_Documentados(int code)
        => Assert.True(AzureSqlRetryPolicy.EsTransitorio(code));

    [Theory]
    [InlineData(0)]
    [InlineData(2627)]   // PK duplicada — NO transitorio (no reintentar)
    [InlineData(208)]    // tabla no existe — NO transitorio
    public void EsTransitorio_False_Para_Errores_Permanentes(int code)
        => Assert.False(AzureSqlRetryPolicy.EsTransitorio(code));

    [Fact]
    public void Config_Reintentos_Coherente()
    {
        Assert.Equal(7, AzureSqlRetryPolicy.ErroresTransitorios.Count);
        Assert.Equal(5, AzureSqlRetryPolicy.MaxReintentos);
        Assert.Equal(TimeSpan.FromSeconds(30), AzureSqlRetryPolicy.MaxRetraso);
    }
}
