using Microsoft.Azure.Cosmos;
using Cosmos.Mi.Demo.Api.Domain;

namespace Cosmos.Mi.Demo.Api.Repositories;

// Slides 8, 12 — CRUD sobre el Container. Cada operación devuelve las RU
// (slide 12: monitorizar consumo con RequestCharge).
public interface IProductoRepository
{
    Task<ProductoCreadoDto> CrearAsync(CrearProductoDto dto);
    Task<(IReadOnlyList<Producto> productos, double ru)> ListarAsync(string? categoria);
    Task<(Producto? producto, double ru)> GetAsync(string categoria, string id);
}

public sealed class ProductoRepository(Container container) : IProductoRepository
{
    public async Task<ProductoCreadoDto> CrearAsync(CrearProductoDto dto)
    {
        var p = new Producto
        {
            Categoria = dto.Categoria,
            Nombre = dto.Nombre,
            Precio = dto.Precio,
        };
        var resp = await container.CreateItemAsync(p, new PartitionKey(p.Categoria));
        return new ProductoCreadoDto(p.Id, p.Categoria, resp.RequestCharge);
    }

    // Slide 12 — con categoría: single-partition (~3 RU). Sin categoría:
    // cross-partition (caro) — se expone para enseñar la diferencia.
    public async Task<(IReadOnlyList<Producto>, double)> ListarAsync(string? categoria)
    {
        QueryRequestOptions? opciones = categoria is null
            ? null
            : new QueryRequestOptions { PartitionKey = new PartitionKey(categoria) };

        var query = categoria is null
            ? new QueryDefinition("SELECT * FROM c")
            : new QueryDefinition("SELECT * FROM c WHERE c.categoria = @cat")
                .WithParameter("@cat", categoria);

        using var it = container.GetItemQueryIterator<Producto>(query, requestOptions: opciones);
        var productos = new List<Producto>();
        double ru = 0;
        while (it.HasMoreResults)
        {
            var page = await it.ReadNextAsync();
            ru += page.RequestCharge;
            productos.AddRange(page);
        }
        return (productos, ru);
    }

    // Slide 12 — leer por id dentro de su partición: 1 RU (lo más barato).
    public async Task<(Producto?, double)> GetAsync(string categoria, string id)
    {
        try
        {
            var resp = await container.ReadItemAsync<Producto>(
                id, new PartitionKey(categoria));
            return (resp.Resource, resp.RequestCharge);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (null, ex.RequestCharge);
        }
    }
}
