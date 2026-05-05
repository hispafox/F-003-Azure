namespace AzureFunctions.Demo.Models;

// Resultado del timer InformeDiario. La clave es la fecha del día sobre el
// que se reporta — se usa como id para conseguir idempotencia (slide 12).
public sealed record Informe(
    string Id,
    DateOnly Fecha,
    int TotalProductos,
    int ProductosSinStock,
    decimal ValorTotalStock,
    DateTimeOffset GeneradoEn);

// Resultado del timer CleanupCadaMinuto.
public sealed record CatalogoStats(int Total, int SinStock, decimal ValorTotalStock);
