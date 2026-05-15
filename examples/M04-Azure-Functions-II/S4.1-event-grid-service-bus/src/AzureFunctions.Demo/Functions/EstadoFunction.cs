using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Endpoint de inspección: devuelve el snapshot del IEstadoTracker para
// poder verificar end-to-end desde curl que cada uno de los 4 triggers
// (HTTP, SB queue, SB topic, EG) hace lo suyo.
public sealed class EstadoFunction
{
    private readonly IEstadoTracker _tracker;

    public EstadoFunction(IEstadoTracker tracker)
    {
        _tracker = tracker;
    }

    [Function(nameof(Estado))]
    public IActionResult Estado(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "estado")] HttpRequest req)
    {
        var snapshot = _tracker.Snapshot();
        return new OkObjectResult(snapshot);
    }
}
