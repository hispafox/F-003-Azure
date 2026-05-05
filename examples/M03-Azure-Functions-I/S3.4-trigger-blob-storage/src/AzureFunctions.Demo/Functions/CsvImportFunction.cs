using System.Text.Json;
using AzureFunctions.Demo.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 6 — Blob trigger sobre uploads/{name}.csv que importa productos.
// Slide 10 — el output va a "procesados/" (contenedor distinto) para
// evitar loops infinitos: si escribiéramos en uploads/ la función se
// dispararía a si misma.
public sealed class CsvImportFunction(
    ICsvProductosImporter importer,
    IImportSummaryService summary,
    ILogger<CsvImportFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [Function(nameof(ImportarCsv))]
    [BlobOutput("procesados/{name}-resumen.json", Connection = "AzureWebJobsStorage")]
    public string ImportarCsv(
        [BlobTrigger("uploads/{name}.csv", Connection = "AzureWebJobsStorage")] string contenido,
        string name)
    {
        logger.LogInformation(
            "CSV detectado en uploads: {Name}.csv ({Bytes} chars)",
            name, contenido.Length);

        var resultado = importer.Import($"{name}.csv", contenido);
        summary.Registrar(resultado);

        logger.LogInformation(
            "CSV {Archivo}: {Ok}/{Total} ok, {Errores} errores",
            resultado.Archivo, resultado.LineasOk,
            resultado.LineasTotales, resultado.LineasError);

        // El return va al BlobOutput → procesados/{name}-resumen.json
        return JsonSerializer.Serialize(resultado, JsonOptions);
    }
}
