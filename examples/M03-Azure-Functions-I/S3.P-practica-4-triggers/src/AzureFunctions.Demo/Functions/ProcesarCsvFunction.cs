using System.Text.Json;
using AzureFunctions.Demo.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Trigger 3/4 — Blob. Procesa CSV subido a uploads/ y genera un JSON
// resumen en resultados/. Los dos contenedores deben ser DISTINTOS para
// evitar loops infinitos (lección de S3.4).
public sealed class ProcesarCsvFunction
{
    private readonly ILogger<ProcesarCsvFunction> _logger;

    public ProcesarCsvFunction(ILogger<ProcesarCsvFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcesarCsv))]
    [BlobOutput("resultados/{nombre}-resumen.json", Connection = "AzureWebJobsStorage")]
    public string ProcesarCsv(
        [BlobTrigger("uploads/{nombre}.csv", Connection = "AzureWebJobsStorage")]
        string contenido,
        string nombre)
    {
        _logger.LogInformation("Procesando CSV: {Nombre}", nombre);

        var resumen = Resumir(nombre, contenido);

        _logger.LogInformation(
            "CSV procesado: {Filas} filas, {Columnas} columnas",
            resumen.TotalFilas, resumen.Columnas.Count);

        return JsonSerializer.Serialize(resumen, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }

    // Handler puro para tests.
    internal static ResumenCsv Resumir(string nombre, string contenido)
    {
        var lineas = (contenido ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToList();

        var headers = lineas.FirstOrDefault()
            ?.Split(',')
            .Select(h => h.Trim())
            .ToArray() ?? Array.Empty<string>();

        var dataLines = lineas.Skip(1).ToList();

        return new ResumenCsv(
            Archivo: $"{nombre}.csv",
            Columnas: headers,
            TotalFilas: dataLines.Count,
            ProcesadoEn: DateTimeOffset.UtcNow,
            Preview: dataLines.Take(3).ToList());
    }
}
