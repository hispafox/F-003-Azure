using Azure;
using Azure.Data.Tables;
using Tables.Demo.Api.Tables;

namespace Tables.Demo.Api.Domain;

// Slide 7/8 — CRUD sobre la tabla "productos". El TableClient se
// construye lazy (NO toca red en el ctor → el test de DI corre offline);
// la tabla la crea el script/test, no el arranque (igual criterio que
// S5.2/S5.3: nada de bootstrap en Program.cs).
public interface IProductosService
{
    Task<List<Producto>> GetAllAsync();
    Task<List<Producto>> GetByCategoriaAsync(string categoria);
    Task<Producto?> GetByIdAsync(string categoria, string id);
    Task<Producto> CreateAsync(Producto producto);
    Task<bool> UpdateAsync(Producto producto);
    Task<bool> DeleteAsync(string categoria, string id);
}

public sealed class ProductosService : IProductosService
{
    public const string TablaNombre = "productos";
    private readonly TableClient _table;

    public ProductosService(IConfiguration config)
    {
        var cs = config["Storage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(cs))
            cs = "UseDevelopmentStorage=true";        // Azurite local (slide 11)
        _table = new TableClient(cs, TablaNombre);
    }

    public async Task<List<Producto>> GetAllAsync()
    {
        var r = new List<Producto>();
        await foreach (var e in _table.QueryAsync<Producto>())
            r.Add(e);
        return r;
    }

    public async Task<List<Producto>> GetByCategoriaAsync(string categoria)
    {
        var r = new List<Producto>();
        // Slide 10 — filtro OData seguro (escapa comillas).
        await foreach (var e in _table.QueryAsync<Producto>(
            filter: ODataFilter.PorParticion(categoria)))
            r.Add(e);
        return r;
    }

    public async Task<Producto?> GetByIdAsync(string categoria, string id)
    {
        try
        {
            var resp = await _table.GetEntityAsync<Producto>(categoria, id);
            return resp.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<Producto> CreateAsync(Producto producto)
    {
        await _table.AddEntityAsync(producto);
        return producto;
    }

    public async Task<bool> UpdateAsync(Producto producto)
    {
        try
        {
            await _table.UpdateEntityAsync(producto, ETag.All);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string categoria, string id)
    {
        try
        {
            await _table.DeleteEntityAsync(categoria, id);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }
}
