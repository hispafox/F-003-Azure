using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Trigger 2/4 — Timer. NCRONTAB cada minuto para que se vea en clase
// (en producción sería "0 0 3 * * *", 3 AM).
public sealed class LimpiezaProgramadaFunction
{
    private readonly ILimpiezaTracker _tracker;
    private readonly ILogger<LimpiezaProgramadaFunction> _logger;

    public LimpiezaProgramadaFunction(
        ILimpiezaTracker tracker,
        ILogger<LimpiezaProgramadaFunction> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    [Function(nameof(LimpiezaProgramada))]
    public void LimpiezaProgramada(
        [TimerTrigger("0 */1 * * * *")] TimerInfo timer)
    {
        Procesar(timer?.IsPastDue ?? false);
    }

    // Handler puro para tests: el TimerInfo del runtime es opaco, lo
    // separamos del método público para poder validar la lógica sin
    // crear un TimerInfo a mano.
    internal LimpiezaResultado Procesar(bool llegoTarde)
    {
        if (llegoTarde)
        {
            _logger.LogWarning("Limpieza llegó tarde — saltada la ejecución anterior");
        }

        // En producción esto sería un DELETE en BD. Aquí simulamos.
        var eliminados = Random.Shared.Next(10, 100);
        var resultado = _tracker.Registrar(eliminados, llegoTarde);

        _logger.LogInformation(
            "Limpieza ejecutada: {Eliminados} registros (total ejecuciones: {Total})",
            eliminados, _tracker.TotalEjecuciones);

        return resultado;
    }
}
