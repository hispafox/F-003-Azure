using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Slide 4 — Cosmos DB Input por id. El binding hace el ReadItemAsync
// por nosotros: cero líneas de cliente, conexión gestionada por el host.
//
// Slide 10/16 — Binding expressions: {id} se resuelve desde la route
// del HTTP trigger. PartitionKey debe ser un valor del documento real;
// como diseñamos pedidos con PK = /clienteId, no podemos hacer un
// read-by-id sin saber el cliente. Aquí ilustramos el caso "PK = id"
// que funciona cuando el id es único globalmente.
public sealed class GetPedidoByIdFunction
{
    [Function(nameof(GetPedidoById))]
    public IActionResult GetPedidoById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pedidos/{clienteId}/{id}")]
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
        // El binding nos entrega null si el documento no existe.
        // No hay try/catch ni RequestFailedException: Functions absorbe
        // el 404 de Cosmos y nos pasa null limpio.
        return pedido is null
            ? new NotFoundObjectResult(new
            {
                error = $"No existe pedido '{id}' para cliente '{clienteId}'",
            })
            : new OkObjectResult(pedido);
    }
}
