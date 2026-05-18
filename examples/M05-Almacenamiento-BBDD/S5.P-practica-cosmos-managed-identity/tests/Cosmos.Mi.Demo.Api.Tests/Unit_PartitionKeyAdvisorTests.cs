using Cosmos.Mi.Demo.Api.Cosmos;

namespace Cosmos.Mi.Demo.Api.Tests;

// CAPA 1 — las 4 reglas de la partition key (slide 11).
[Trait("Category", "Unit")]
public class Unit_PartitionKeyAdvisorTests
{
    [Theory]
    [InlineData(5000, true, true, true, PartitionKeyVeredicto.Buena)]
    [InlineData(2, true, true, true, PartitionKeyVeredicto.Mala)]      // baja cardinalidad
    [InlineData(5000, false, true, true, PartitionKeyVeredicto.Mala)]  // distribución sesgada
    [InlineData(5000, true, false, true, PartitionKeyVeredicto.Mala)]  // no en queries
    [InlineData(5000, true, true, false, PartitionKeyVeredicto.Mala)]  // no estable
    public void Evaluar(int card, bool uni, bool q, bool est, PartitionKeyVeredicto esperado)
        => Assert.Equal(esperado, PartitionKeyAdvisor.Evaluar(card, uni, q, est));

    [Fact]
    public void Evaluar_CardinalidadNegativa_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => PartitionKeyAdvisor.Evaluar(-1, true, true, true));

    [Theory]
    [InlineData("/categoria", "categoria", false)]   // filtra por la PK → barato
    [InlineData("/categoria", "precio", true)]        // otro campo → cross-partition
    public void EsCrossPartition(string pk, string filtro, bool esperado)
        => Assert.Equal(esperado, PartitionKeyAdvisor.EsCrossPartition(pk, filtro));
}
