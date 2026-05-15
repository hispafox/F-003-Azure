using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 9 — Anatomia minima: un trigger (HTTP) + nada de input/output bindings.
// Devuelve campos diagnosticos como en el Hello World del M01: util para
// confirmar tras un deploy que la Function App arrancó y el aislamiento .NET
// está cargado correctamente.
public sealed class HelloFunction(ILogger<HelloFunction> logger)
{
    [Function(nameof(Hello))]
    public IActionResult Hello(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hello")] HttpRequest req)
    {
        var name = req.Query["name"].ToString();
        if (string.IsNullOrWhiteSpace(name)) name = "Azure";

        logger.LogInformation("Hello function called with name={Name}", name);

        return new OkObjectResult(new
        {
            mensaje = $"Hello {name} desde Azure Functions",
            entorno = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Local",
            servidor = Environment.MachineName,
            hora_utc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            runtime = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription,
            functionsVersion = Environment.GetEnvironmentVariable("FUNCTIONS_EXTENSION_VERSION") ?? "local",
            workerRuntime = Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME") ?? "dotnet-isolated"
        });
    }
}
