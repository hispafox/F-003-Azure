using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// HTTP endpoints para inspeccionar el resultado del Blob trigger sin
// tener que ir al portal a leer blobs.
public sealed class ImportsHttpFunctions(IImportSummaryService summary)
{
    [Function(nameof(ListarImports))]
    public IActionResult ListarImports(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "imports")] HttpRequest req)
    {
        var todos = summary.Listar();
        return new OkObjectResult(new { total = todos.Count, items = todos });
    }

    [Function(nameof(GetImportPorArchivo))]
    public IActionResult GetImportPorArchivo(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "imports/{archivo}")] HttpRequest req,
        string archivo)
    {
        var resultado = summary.GetByArchivo(archivo);
        return resultado is null
            ? new NotFoundObjectResult(new { error = $"Sin resumen para '{archivo}'" })
            : new OkObjectResult(resultado);
    }
}
