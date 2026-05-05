using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

public interface IImportSummaryService
{
    void Registrar(ImportResultado resultado);
    IReadOnlyList<ImportResultado> Listar();
    ImportResultado? GetByArchivo(string archivo);
}
