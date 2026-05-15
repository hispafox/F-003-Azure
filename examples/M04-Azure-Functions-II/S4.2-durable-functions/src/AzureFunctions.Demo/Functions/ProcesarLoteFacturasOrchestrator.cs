using AzureFunctions.Demo.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace AzureFunctions.Demo.Functions;

// Slide 7 — Fan-out / Fan-in con control de paralelismo.
//
// Fan-out: lanzar todas las activities a la vez.
// Fan-in:  Task.WhenAll espera a que TODAS terminen.
// Control: procesamos en chunks de TamanoLote para no saturar el backend
//          (slide 7 — "si 500 es demasiado, limitad").
public sealed class ProcesarLoteFacturasOrchestrator
{
    public const int TamanoLote = 50;

    [Function(nameof(ProcesarLoteFacturas))]
    public async Task<ResumenLote> ProcesarLoteFacturas(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var facturas = context.GetInput<List<Factura>>() ?? [];

        var resultados = new List<ResultadoFactura>(facturas.Count);

        // Procesar en lotes para acotar el paralelismo. Cada chunk hace
        // fan-out completo y fan-in antes de pasar al siguiente.
        foreach (var lote in facturas.Chunk(TamanoLote))
        {
            var tareas = lote
                .Select(f => context.CallActivityAsync<ResultadoFactura>(
                    nameof(FacturaActivities.ProcesarFactura), f))
                .ToList();

            var resultadosLote = await Task.WhenAll(tareas);
            resultados.AddRange(resultadosLote);
        }

        // Consolidación (fan-in): determinista, solo agrega lo recibido.
        return new ResumenLote(
            Total: resultados.Count,
            Exitosas: resultados.Count(r => r.Exito),
            Fallidas: resultados.Count(r => !r.Exito),
            ImporteTotal: resultados.Where(r => r.Exito).Sum(r => r.Importe));
    }
}
