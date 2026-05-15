namespace AzureFunctions.Demo.Models;

// Slide 8 — Resumen JSON que el Blob trigger genera en resultados/.
public sealed record ResumenCsv(
    string Archivo,
    IReadOnlyList<string> Columnas,
    int TotalFilas,
    DateTimeOffset ProcesadoEn,
    IReadOnlyList<string> Preview);
