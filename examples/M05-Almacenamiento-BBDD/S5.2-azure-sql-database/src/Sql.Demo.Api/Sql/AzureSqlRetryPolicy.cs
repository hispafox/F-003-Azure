namespace Sql.Demo.Api.Sql;

// Slide 13 — resiliencia de conexión. Azure SQL devuelve errores
// "transitorios" (la DB serverless despertando, throttling, failover,
// blips de red). EF Core los reintenta con backoff si le pasamos la
// lista de error numbers. Aquí esa lista vive como dato puro y testeable
// en vez de incrustada en Program.cs.
public static class AzureSqlRetryPolicy
{
    // Códigos de error transitorios documentados de Azure SQL (slide 13).
    public static readonly IReadOnlyList<int> ErroresTransitorios =
    [
        4060,   // no se pudo abrir la base de datos
        40197,  // el servicio encontró un error procesando la petición
        40501,  // servicio ocupado (throttling)
        40613,  // base de datos no disponible (despertando / failover)
        49918,  // no hay capacidad para procesar la petición
        49919,  // demasiadas operaciones create/update
        49920,  // servicio ocupado, demasiadas peticiones
    ];

    public const int MaxReintentos = 5;
    public static readonly TimeSpan MaxRetraso = TimeSpan.FromSeconds(30);

    public static bool EsTransitorio(int errorNumber)
        => ErroresTransitorios.Contains(errorNumber);
}
