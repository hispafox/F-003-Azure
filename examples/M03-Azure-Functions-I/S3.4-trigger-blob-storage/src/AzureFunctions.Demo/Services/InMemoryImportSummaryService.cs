using System.Collections.Concurrent;
using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public sealed class InMemoryImportSummaryService : IImportSummaryService
{
    private readonly ConcurrentDictionary<string, ImportResultado> _store = new();

    public void Registrar(ImportResultado resultado) =>
        _store[resultado.Archivo] = resultado;

    public IReadOnlyList<ImportResultado> Listar() =>
        _store.Values.OrderByDescending(r => r.ProcesadoEn).ToList();

    public ImportResultado? GetByArchivo(string archivo) =>
        _store.TryGetValue(archivo, out var r) ? r : null;
}
