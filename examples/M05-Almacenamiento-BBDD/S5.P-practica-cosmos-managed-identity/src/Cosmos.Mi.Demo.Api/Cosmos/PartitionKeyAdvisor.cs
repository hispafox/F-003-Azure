namespace Cosmos.Mi.Demo.Api.Cosmos;

public enum PartitionKeyVeredicto { Buena, Mala }

// Slide 11 — "la decisión que define el rendimiento". Las 4 reglas de la
// slide (cardinalidad, distribución, aparece en queries, estable) como
// función pura. /categoria (la de la práctica) es aceptable pero con
// riesgo de hot partition si una categoría es muy popular.
public static class PartitionKeyAdvisor
{
    public const int UmbralBajaCardinalidad = 20;

    public static PartitionKeyVeredicto Evaluar(
        int cardinalidad, bool distribucionUniforme,
        bool apareceEnQueries, bool estable)
    {
        if (cardinalidad < 0)
            throw new ArgumentOutOfRangeException(nameof(cardinalidad));

        // Baja cardinalidad (/tipo con 2 valores) → hot partitions.
        if (cardinalidad < UmbralBajaCardinalidad) return PartitionKeyVeredicto.Mala;
        // Distribución sesgada (90% a 1 cliente) → hot partition.
        if (!distribucionUniforme) return PartitionKeyVeredicto.Mala;
        // No aparece en las queries → cross-partition siempre (caro).
        if (!apareceEnQueries) return PartitionKeyVeredicto.Mala;
        // Cambia tras crear el documento → no se puede (inmutable).
        if (!estable) return PartitionKeyVeredicto.Mala;
        return PartitionKeyVeredicto.Buena;
    }

    // Slide 11 — una query es cara (cross-partition) si NO filtra por la
    // partition key del contenedor.
    public static bool EsCrossPartition(string pkContenedor, string campoFiltro)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pkContenedor);
        ArgumentException.ThrowIfNullOrWhiteSpace(campoFiltro);
        static string N(string s) => s.Trim().TrimStart('/');
        return !N(pkContenedor).Equals(N(campoFiltro), StringComparison.OrdinalIgnoreCase);
    }
}
