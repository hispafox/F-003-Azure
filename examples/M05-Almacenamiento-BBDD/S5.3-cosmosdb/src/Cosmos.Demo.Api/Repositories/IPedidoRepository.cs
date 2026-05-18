using Microsoft.Azure.Cosmos;
using Cosmos.Demo.Api.Domain;

namespace Cosmos.Demo.Api.Repositories;

// Slides 8-10, 12, 17 — encapsula el Container de Cosmos. Cada método
// devuelve las RU consumidas (slide 8: monitorizar con RequestCharge).
public interface IPedidoRepository
{
    Task<PedidoCreadoDto> CrearAsync(CrearPedidoDto dto);
    Task<(Pedido? pedido, double ru)> GetAsync(string clienteId, string id);
    Task<(IReadOnlyList<Pedido> pedidos, double ru)> PorClienteAsync(string clienteId);
    Task<bool> SoftDeleteAsync(string clienteId, string id);
    Task<(bool ok, double ru)> CrearPedidoConMovimientoAsync(CrearPedidoDto dto, Movimiento mov);
}

public sealed class PedidoRepository(Container container) : IPedidoRepository
{
    public async Task<PedidoCreadoDto> CrearAsync(CrearPedidoDto dto)
    {
        var pedido = new Pedido
        {
            ClienteId = dto.ClienteId,
            ClienteNombre = dto.ClienteNombre,
            Items = [.. dto.Items.Select(i =>
                new PedidoItem(i.ProductoId, i.Nombre, i.Cantidad, i.Precio))],
            CreadoEn = DateTimeOffset.UtcNow,
        };
        pedido.Total = pedido.Items.Sum(i => i.Precio * i.Cantidad);

        // Slide 9 — CreateItem con la PartitionKey explícita.
        var resp = await container.CreateItemAsync(
            pedido, new PartitionKey(pedido.ClienteId));
        return new PedidoCreadoDto(
            pedido.Id, pedido.ClienteId, pedido.Total, resp.RequestCharge);
    }

    // Slide 8 — leer por id dentro de su partición: 1 RU (lo más barato).
    public async Task<(Pedido?, double)> GetAsync(string clienteId, string id)
    {
        try
        {
            var resp = await container.ReadItemAsync<Pedido>(
                id, new PartitionKey(clienteId));
            // Soft delete (slide 12): para los lectores, "no existe".
            return resp.Resource.Eliminado
                ? (null, resp.RequestCharge)
                : (resp.Resource, resp.RequestCharge);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (null, ex.RequestCharge);
        }
    }

    // Slide 8 — query SINGLE-partition (PartitionKey en las opciones):
    // ~3 RU, no el table scan cross-partition de "SELECT * FROM c".
    public async Task<(IReadOnlyList<Pedido>, double)> PorClienteAsync(string clienteId)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.clienteId = @cid AND c.eliminado = false")
            .WithParameter("@cid", clienteId);

        var opciones = new QueryRequestOptions { PartitionKey = new PartitionKey(clienteId) };
        using var it = container.GetItemQueryIterator<Pedido>(query, requestOptions: opciones);

        var pedidos = new List<Pedido>();
        double ru = 0;
        while (it.HasMoreResults)
        {
            var page = await it.ReadNextAsync();
            ru += page.RequestCharge;
            pedidos.AddRange(page);
        }
        return (pedidos, ru);
    }

    // Slide 12 — soft delete: upsert con eliminado=true (el Change Feed
    // lo ve como UPDATE; no registra DELETE reales).
    public async Task<bool> SoftDeleteAsync(string clienteId, string id)
    {
        try
        {
            var resp = await container.ReadItemAsync<Pedido>(
                id, new PartitionKey(clienteId));
            var pedido = resp.Resource;
            if (pedido.Eliminado) return false;

            pedido.Eliminado = true;
            pedido.EliminadoEn = DateTimeOffset.UtcNow;
            await container.UpsertItemAsync(pedido, new PartitionKey(clienteId));
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    // Slide 17 — TransactionalBatch: pedido + movimiento, atómico, en la
    // MISMA partición lógica (/clienteId). ACID dentro de la partición.
    public async Task<(bool, double)> CrearPedidoConMovimientoAsync(
        CrearPedidoDto dto, Movimiento mov)
    {
        var pedido = new Pedido
        {
            ClienteId = dto.ClienteId,
            ClienteNombre = dto.ClienteNombre,
            Items = [.. dto.Items.Select(i =>
                new PedidoItem(i.ProductoId, i.Nombre, i.Cantidad, i.Precio))],
            CreadoEn = DateTimeOffset.UtcNow,
        };
        pedido.Total = pedido.Items.Sum(i => i.Precio * i.Cantidad);
        mov.ClienteId = dto.ClienteId;

        var batch = container.CreateTransactionalBatch(new PartitionKey(dto.ClienteId))
            .CreateItem(pedido)
            .CreateItem(mov);

        using var resp = await batch.ExecuteAsync();
        return (resp.IsSuccessStatusCode, resp.RequestCharge);
    }
}
