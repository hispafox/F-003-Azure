using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 7/8 — Starter (client function): arranca el orchestrator y
// devuelve la URL de estado. Más un GET para consultar el estado.
public sealed class SaludosStarterFunctions
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly ILogger<SaludosStarterFunctions> _logger;

    public SaludosStarterFunctions(ILogger<SaludosStarterFunctions> logger)
    {
        _logger = logger;
    }

    // POST /api/saludos  body: ["Ana","Luis","Marta"]
    [Function(nameof(IniciarSaludos))]
    public async Task<IActionResult> IniciarSaludos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "saludos")]
        HttpRequest req,
        [DurableClient] DurableTaskClient client)
    {
        List<string>? nombres;
        try
        {
            nombres = await JsonSerializer.DeserializeAsync<List<string>>(req.Body, JsonOpts);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult(new { error = "Body inválido (se espera un array de nombres)" });
        }

        if (nombres is null || nombres.Count == 0)
            return new BadRequestObjectResult(new { error = "La lista de nombres no puede estar vacía" });

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(SaludosOrchestrator.SaludarATodos), nombres);

        _logger.LogInformation("Orquestación {Id} iniciada para {Count} nombres",
            instanceId, nombres.Count);

        return new AcceptedResult(
            $"/api/saludos/{instanceId}",
            new { instanceId, estadoUrl = $"/api/saludos/{instanceId}" });
    }

    // GET /api/saludos/{instanceId}
    [Function(nameof(EstadoSaludos))]
    public async Task<IActionResult> EstadoSaludos(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "saludos/{instanceId}")]
        HttpRequest req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        var estado = await client.GetInstanceAsync(instanceId, getInputsAndOutputs: true);
        if (estado is null)
            return new NotFoundObjectResult(new { error = $"Instancia {instanceId} no encontrada" });

        return new OkObjectResult(new
        {
            instanceId,
            runtimeStatus = estado.RuntimeStatus.ToString(),
            createdAt = estado.CreatedAt,
            lastUpdatedAt = estado.LastUpdatedAt,
            output = estado.SerializedOutput,
        });
    }
}
