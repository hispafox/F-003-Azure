using Cosmos.Demo.Api.Cosmos;

namespace Cosmos.Demo.Api.Tests;

// CAPA 1 — los 5 niveles de consistencia (slide 11) como decisión pura.
[Trait("Category", "Unit")]
public class Unit_ConsistencyAdvisorTests
{
    [Theory]
    // financiero / inventario crítico → Strong (aunque cueste 2x RU).
    [InlineData(false, true, false, NivelConsistencia.Strong)]
    [InlineData(true, false, false, NivelConsistencia.Strong)]
    // solo importa latencia mínima → Eventual.
    [InlineData(false, false, true, NivelConsistencia.Eventual)]
    // caso normal (90%) → Session.
    [InlineData(false, false, false, NivelConsistencia.Session)]
    // Strong gana a "latencia mínima" si además es financiero.
    [InlineData(false, true, true, NivelConsistencia.Strong)]
    public void Recomendar(
        bool ultimaEscritura, bool financiero, bool latencia, NivelConsistencia esperado)
        => Assert.Equal(esperado,
            ConsistencyAdvisor.Recomendar(ultimaEscritura, financiero, latencia));

    [Theory]
    [InlineData(NivelConsistencia.Strong, 2)]
    [InlineData(NivelConsistencia.BoundedStaleness, 2)]
    [InlineData(NivelConsistencia.Session, 1)]
    [InlineData(NivelConsistencia.Eventual, 1)]
    public void MultiplicadorRu(NivelConsistencia nivel, int esperado)
        => Assert.Equal(esperado, ConsistencyAdvisor.MultiplicadorRu(nivel));
}
