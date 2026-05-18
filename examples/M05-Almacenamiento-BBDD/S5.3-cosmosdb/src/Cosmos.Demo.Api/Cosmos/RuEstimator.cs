namespace Cosmos.Demo.Api.Cosmos;

public enum TipoOperacion { LeerPorId, QuerySinglePartition, QueryCrossPartition, Escritura }

// Slides 7-8, 21 — "RU es la moneda de Cosmos". Estimación pura del coste
// de cada patrón de acceso, para enseñar por qué leer por id (1 RU) gana
// a una query cross-partition (50+ RU). Números base de la slide 7.
public static class RuEstimator
{
    public const double LeerPorId = 1;             // slide 7: 1 RU
    public const double QuerySingle = 3;           // slide 7: 2-3 RU
    public const double EscrituraPorDoc = 5;       // slide 7: 5-6 RU
    public const int FactorCrossPartition = 10;    // slide 7: 10-100x

    public static double Estimar(TipoOperacion op, int docs = 1)
    {
        if (docs < 1)
            throw new ArgumentOutOfRangeException(nameof(docs));

        return op switch
        {
            TipoOperacion.LeerPorId => LeerPorId,
            TipoOperacion.Escritura => EscrituraPorDoc * docs,
            TipoOperacion.QuerySinglePartition => QuerySingle + (docs - 1) * 0.5,
            TipoOperacion.QueryCrossPartition =>
                (QuerySingle + (docs - 1) * 0.5) * FactorCrossPartition,
            _ => throw new ArgumentOutOfRangeException(nameof(op)),
        };
    }
}
