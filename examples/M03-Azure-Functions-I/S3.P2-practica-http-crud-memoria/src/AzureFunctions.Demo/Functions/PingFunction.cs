using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Slide 10 — Endpoint público (Anonymous): no requiere function key.
// Patrón típico para health checks externos y descubrimiento.
public sealed class PingFunction
{
    [Function(nameof(Ping))]
    public IActionResult Ping(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")] HttpRequest req)
    {
        return new OkObjectResult(new
        {
            status = "pong",
            timestamp = DateTimeOffset.UtcNow,
            functionsVersion = Environment.GetEnvironmentVariable("FUNCTIONS_EXTENSION_VERSION") ?? "local"
        });
    }
}
