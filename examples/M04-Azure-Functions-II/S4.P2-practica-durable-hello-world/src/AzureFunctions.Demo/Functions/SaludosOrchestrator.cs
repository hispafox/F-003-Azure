using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 6 — Fan-out / fan-in en su forma MÍNIMA: recibe N nombres,
// lanza N activities en paralelo, espera a todas, consolida.
//
// REGLA (S4.2 slide 5): el orquestador es DETERMINISTA. Solo context.*,
// nada de DateTime.Now/Random/I/O.
public sealed class SaludosOrchestrator
{
    [Function(nameof(SaludarATodos))]
    public async Task<List<string>> SaludarATodos(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger<SaludosOrchestrator>();
        var nombres = context.GetInput<List<string>>() ?? [];

        if (nombres.Count == 0)
        {
            logger.LogWarning("Lista de nombres vacía");
            return [];
        }

        logger.LogInformation("Orquestando {Count} saludos", nombres.Count);

        // Fan-out: una tarea por nombre, todas a la vez.
        var tareas = nombres
            .Select(n => context.CallActivityAsync<string>(
                nameof(SaludarActivity.Saludar), n))
            .ToList();

        // Fan-in: esperar a que TODAS terminen y consolidar.
        var saludos = await Task.WhenAll(tareas);

        logger.LogInformation("Completados {Count} saludos", saludos.Length);
        return [.. saludos];
    }
}
