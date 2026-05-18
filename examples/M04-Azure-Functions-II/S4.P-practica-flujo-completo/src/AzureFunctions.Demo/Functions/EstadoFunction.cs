using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Inspección end-to-end: GET /api/estado devuelve el snapshot del flujo
// (creados → facturados → notificados). Permite verificar los 3 saltos
// con un solo curl, sin entrar al Portal.
public sealed class EstadoFunction
{
    private readonly IFlujoTracker _tracker;

    public EstadoFunction(IFlujoTracker tracker)
    {
        _tracker = tracker;
    }

    [Function(nameof(Estado))]
    public IActionResult Estado(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "estado")] HttpRequest req)
        => new OkObjectResult(_tracker.Snapshot());
}
