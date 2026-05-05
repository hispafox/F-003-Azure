namespace AzureFunctions.Demo.Models;

// Resumen del procesamiento de un CSV. Se persiste como blob en
// procesados/{name}-resumen.json y se mantiene una copia en memoria via
// IImportSummaryService para que /api/imports lo exponga.
public sealed record ImportResultado(
    string Archivo,
    int LineasTotales,
    int LineasOk,
    int LineasError,
    IReadOnlyList<FilaImport> Errores,
    IReadOnlyList<string> ProductosCreados,
    DateTimeOffset ProcesadoEn);

public sealed record FilaImport(int NumeroLinea, string Detalle);
