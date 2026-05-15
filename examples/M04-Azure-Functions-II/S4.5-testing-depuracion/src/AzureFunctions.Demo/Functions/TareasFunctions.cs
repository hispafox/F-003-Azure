using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 10/11 — Timer y Blob triggers como "cable" fino sobre servicios.
// El test NUNCA toca el trigger: testea ILimpiezaService / ICsvResumenService
// directamente (rápido, sin esperar al CRON, sin Azurite).
public sealed class TareasFunctions
{
    private readonly ILimpiezaService _limpieza;
    private readonly ICsvResumenService _csv;
    private readonly ILogger<TareasFunctions> _logger;

    public TareasFunctions(
        ILimpiezaService limpieza,
        ICsvResumenService csv,
        ILogger<TareasFunctions> logger)
    {
        _limpieza = limpieza;
        _csv = csv;
        _logger = logger;
    }

    // Slide 10 — Timer cada 5 min (en prod sería "0 0 3 * * *").
    [Function(nameof(LimpiezaProgramada))]
    public void LimpiezaProgramada(
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer)
    {
        var eliminados = _limpieza.Limpiar(DateTimeOffset.UtcNow.AddDays(-7));
        _logger.LogInformation("Limpieza programada: {Count} eliminados", eliminados);
    }

    // Slide 11 — Blob trigger; la lógica de parseo está en el servicio.
    [Function(nameof(ProcesarCsv))]
    public void ProcesarCsv(
        [BlobTrigger("uploads/{nombre}.csv", Connection = "AzureWebJobsStorage")]
        string contenido,
        string nombre)
    {
        var resumen = _csv.Procesar(contenido, $"{nombre}.csv");
        _logger.LogInformation(
            "CSV {Archivo}: {Filas} filas, {Cols} columnas",
            resumen.Archivo, resumen.TotalFilas, resumen.Columnas.Count);
    }
}
