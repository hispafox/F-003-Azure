using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Permite ver el resultado de InformeDiario sin tener que pinchar logs.
// AuthorizationLevel.Function porque expone datos del catálogo.
public sealed class InformesHttpFunctions(IInformeService informes)
{
    [Function(nameof(Listar))]
    public IActionResult Listar(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "informes")] HttpRequest req)
    {
        var todos = informes.Listar();
        return new OkObjectResult(new { total = todos.Count, items = todos });
    }

    [Function(nameof(GetPorFecha))]
    public IActionResult GetPorFecha(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "informes/{fecha}")] HttpRequest req,
        string fecha)
    {
        if (!DateOnly.TryParse(fecha, out var parsed))
        {
            return new BadRequestObjectResult(new
            {
                error = "Fecha invalida",
                esperado = "yyyy-MM-dd"
            });
        }

        var informe = informes.GetByFecha(parsed);
        return informe is null
            ? new NotFoundObjectResult(new { error = $"Sin informe para {fecha}" })
            : new OkObjectResult(informe);
    }
}
