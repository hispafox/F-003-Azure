using Cosmos.Demo.Api.Cosmos;

namespace Cosmos.Demo.Api.Tests;

// CAPA 1 — las 3 reglas de la partition key (slides 4-6) como tabla.
[Trait("Category", "Unit")]
public class Unit_PartitionKeyAdvisorTests
{
    [Theory]
    // alta cardinalidad + uniforme + alineada → buena (/clienteId).
    [InlineData(5000, true, true, PartitionKeyVeredicto.Buena)]
    // baja cardinalidad (/pais) → mala aunque lo demás esté bien.
    [InlineData(8, true, true, PartitionKeyVeredicto.Mala)]
    // hot partition (/fecha = todo hoy) → mala.
    [InlineData(5000, false, true, PartitionKeyVeredicto.Mala)]
    // no alineada con la query frecuente → mala (siempre cross-partition).
    [InlineData(5000, true, false, PartitionKeyVeredicto.Mala)]
    // justo en el umbral de baja cardinalidad.
    [InlineData(20, true, true, PartitionKeyVeredicto.Buena)]
    [InlineData(19, true, true, PartitionKeyVeredicto.Mala)]
    public void Evaluar_TablaDeDecision(
        int card, bool uniforme, bool alineada, PartitionKeyVeredicto esperado)
        => Assert.Equal(esperado,
            PartitionKeyAdvisor.Evaluar(card, uniforme, alineada));

    [Fact]
    public void Evaluar_CardinalidadNegativa_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => PartitionKeyAdvisor.Evaluar(-1, true, true));

    [Theory]
    // query filtra por la PK → single-partition (barata).
    [InlineData("/clienteId", "clienteId", false)]
    [InlineData("clienteId", "/clienteId", false)]
    // query filtra por otro campo → cross-partition (cara).
    [InlineData("/clienteId", "email", true)]
    public void EsCrossPartition(string pk, string filtro, bool esperado)
        => Assert.Equal(esperado, PartitionKeyAdvisor.EsCrossPartition(pk, filtro));
}
