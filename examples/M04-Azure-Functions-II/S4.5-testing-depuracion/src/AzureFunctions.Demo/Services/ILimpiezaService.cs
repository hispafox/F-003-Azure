using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Services;

// Slide 10 — no se puede esperar a que el CRON dispare. La solución:
// extraer la lógica a un servicio y testear el SERVICIO directamente.
// La función Timer es solo el "cable".
public interface ILimpiezaService
{
    // Devuelve cuántos registros se eliminarían dado un cutoff.
    int Limpiar(DateTimeOffset anterioresA);
}

public sealed class InMemoryLimpiezaService(ILogger<InMemoryLimpiezaService> logger)
    : ILimpiezaService
{
    // Simula un store de registros temporales con su fecha.
    private readonly List<DateTimeOffset> _registros =
    [
        DateTimeOffset.UtcNow.AddDays(-10),
        DateTimeOffset.UtcNow.AddDays(-5),
        DateTimeOffset.UtcNow.AddDays(-1),
        DateTimeOffset.UtcNow.AddHours(-1),
    ];

    public int Limpiar(DateTimeOffset anterioresA)
    {
        var aBorrar = _registros.Count(r => r < anterioresA);
        _registros.RemoveAll(r => r < anterioresA);
        logger.LogInformation("Limpieza: {Count} registros anteriores a {Fecha}",
            aBorrar, anterioresA);
        return aBorrar;
    }
}
