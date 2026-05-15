using AzureFunctions.Demo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Demo.Functions;

// Slide 4 — Cosmos DB Input por SqlQuery. El binding ejecuta la query
// y nos entrega ya deserializado IEnumerable<Pedido>. Los placeholders
// {clienteId} se sustituyen desde la route del HTTP trigger (slide 10).
//
// Slide 8 — Cuándo usar binding vs SDK: este caso encaja con binding
// (lectura simple, sin paginación). Si quisiéramos paginar tendríamos
// que usar CosmosClient inyectado.
public sealed class GetPedidosPorClienteFunction
{
    [Function(nameof(GetPedidosPorCliente))]
    public IActionResult GetPedidosPorCliente(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "clientes/{clienteId}/pedidos")]
        HttpRequest req,
        [CosmosDBInput(
            databaseName: "tienda",
            containerName: "pedidos",
            Connection = "CosmosDbConnection",
            SqlQuery = "SELECT * FROM c WHERE c.clienteId = {clienteId} ORDER BY c._ts DESC",
            PartitionKey = "{clienteId}")]
        IEnumerable<Pedido> pedidos,
        string clienteId)
    {
        var lista = pedidos.ToList();
        return new OkObjectResult(new
        {
            clienteId,
            total = lista.Count,
            items = lista,
        });
    }
}
