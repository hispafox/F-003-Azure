namespace Cosmos.Demo.Api.Cosmos;

public enum PartitionKeyVeredicto { Buena, Mala }

// Slides 4-6 — la partition key es "la decisión más importante". Las
// tres reglas de la slide 5 (cardinalidad, distribución uniforme,
// alineada con la query frecuente) como función pura y testeable, igual
// que el SqlTierAdvisor de S5.2.
public static class PartitionKeyAdvisor
{
    // Slide 5 — "baja cardinalidad" satura una partición (ej. /pais).
    public const int UmbralBajaCardinalidad = 20;

    public static PartitionKeyVeredicto Evaluar(
        int cardinalidad, bool distribucionUniforme, bool alineadaConQueryFrecuente)
    {
        if (cardinalidad < 0)
            throw new ArgumentOutOfRangeException(nameof(cardinalidad));

        // Baja cardinalidad → pocas particiones, una se sobrecarga.
        if (cardinalidad < UmbralBajaCardinalidad)
            return PartitionKeyVeredicto.Mala;

        // Hot partition: distribución no uniorme (ej. /fecha = todo hoy).
        if (!distribucionUniforme)
            return PartitionKeyVeredicto.Mala;

        // No alineada con la query frecuente → siempre cross-partition.
        if (!alineadaConQueryFrecuente)
            return PartitionKeyVeredicto.Mala;

        return PartitionKeyVeredicto.Buena;
    }

    // Slides 4, 8 — una query es cross-partition (cara) si NO filtra por
    // la partition key del contenedor. Normaliza la barra inicial.
    public static bool EsCrossPartition(string pkContenedor, string campoFiltro)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pkContenedor);
        ArgumentException.ThrowIfNullOrWhiteSpace(campoFiltro);
        static string N(string s) => s.Trim().TrimStart('/');
        return !N(pkContenedor).Equals(N(campoFiltro), StringComparison.OrdinalIgnoreCase);
    }
}
