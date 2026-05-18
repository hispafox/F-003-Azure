using Cosmos.Demo.Api.Cosmos;

namespace Cosmos.Demo.Api.Tests;

// CAPA 1 — "RU es la moneda" (slides 7-8): leer por id ≪ cross-partition.
[Trait("Category", "Unit")]
public class Unit_RuEstimatorTests
{
    [Fact]
    public void LeerPorId_Es_1RU()
        => Assert.Equal(1, RuEstimator.Estimar(TipoOperacion.LeerPorId));

    [Fact]
    public void Escritura_Escala_Por_Doc()
        => Assert.Equal(15, RuEstimator.Estimar(TipoOperacion.Escritura, 3)); // 5×3

    [Fact]
    public void CrossPartition_Es_10x_Single()
    {
        var single = RuEstimator.Estimar(TipoOperacion.QuerySinglePartition, 5);
        var cross = RuEstimator.Estimar(TipoOperacion.QueryCrossPartition, 5);
        Assert.Equal(single * RuEstimator.FactorCrossPartition, cross);
    }

    [Fact]
    public void LeerPorId_Mucho_Mas_Barato_Que_Query()
        => Assert.True(
            RuEstimator.Estimar(TipoOperacion.LeerPorId) <
            RuEstimator.Estimar(TipoOperacion.QuerySinglePartition));

    [Fact]
    public void Docs_Menor_Que_1_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => RuEstimator.Estimar(TipoOperacion.LeerPorId, 0));
}
