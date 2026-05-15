using System.Text.Json;
using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Slide 7 — Pipeline en una función: HTTP trigger → CosmosDBInput
// (lectura) → BlobOutput (escritura). Cero líneas de cliente SDK.
//
// Slide 10/16 — Binding expressions dinámicas:
//   {clienteId}, {id}  → de la route del HTTP trigger
//   {DateTime:yyyy-MM-dd} → fecha actual formateada
// El resultado: exports/2026-05-15/pedido-{id}.json
//
// Como queremos a la vez devolver HTTP y escribir blob, usamos el
// patrón multi-output (slide 6): un POCO con propiedades anotadas.
public sealed class ExportarPedidoFunction
{
    [Function(nameof(ExportarPedido))]
    public ExportarPedidoResult ExportarPedido(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "exportar/{clienteId}/{id}")]
        HttpRequest req,
        [CosmosDBInput(
            databaseName: "tienda",
            containerName: "pedidos",
            Connection = "CosmosDbConnection",
            Id = "{id}",
            PartitionKey = "{clienteId}")]
        Pedido? pedido,
        string id,
        string clienteId)
    {
        if (pedido is null)
        {
            // 404: BlobJson = null → el binding de blob NO se materializa.
            return new ExportarPedidoResult
            {
                HttpResponse = new NotFoundObjectResult(new
                {
                    error = $"No existe pedido '{id}' para cliente '{clienteId}'",
                }),
                BlobJson = null,
            };
        }

        var json = JsonSerializer.Serialize(pedido, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        return new ExportarPedidoResult
        {
            HttpResponse = new OkObjectResult(pedido),
            BlobJson = json,
        };
    }
}

public sealed class ExportarPedidoResult
{
    [HttpResult]
    public IActionResult HttpResponse { get; set; } = null!;

    // Slide 10/16 — La ruta usa placeholders del trigger ({clienteId},
    // {id}) y del sistema ({DateTime}). Functions los resuelve antes
    // de invocar la función.
    [BlobOutput(
        "exports/{DateTime:yyyy-MM-dd}/pedido-{clienteId}-{id}.json",
        Connection = "AzureWebJobsStorage")]
    public string? BlobJson { get; set; }
}
