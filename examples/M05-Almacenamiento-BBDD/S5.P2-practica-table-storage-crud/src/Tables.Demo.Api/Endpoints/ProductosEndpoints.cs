using Azure;
using Azure.Data.Tables;
using Tables.Demo.Api.Domain;
using Tables.Demo.Api.Tables;

namespace Tables.Demo.Api.Endpoints;

public static class ProductosEndpoints
{
    public static void MapProductos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        // GET /productos — listar todos (scan, slide 8/15)
        app.MapGet("/productos", async (IProductosService svc) =>
        {
            var productos = await svc.GetAllAsync();
            return Results.Ok(new { total = productos.Count, productos });
        });

        // GET /productos/categoria/{categoria} — por PartitionKey (rápido)
        app.MapGet("/productos/categoria/{categoria}", async (
            string categoria, IProductosService svc) =>
        {
            var productos = await svc.GetByCategoriaAsync(categoria);
            return Results.Ok(new { categoria, total = productos.Count, productos });
        });

        // GET /productos/{categoria}/{id} — uno
        app.MapGet("/productos/{categoria}/{id}", async (
            string categoria, string id, IProductosService svc) =>
        {
            var p = await svc.GetByIdAsync(categoria, id);
            return p is null
                ? Results.NotFound(new { error = $"Producto {id} no encontrado en {categoria}" })
                : Results.Ok(p);
        });

        // POST /productos — crear
        app.MapPost("/productos", async (Producto producto, IProductosService svc) =>
        {
            if (!TableKeys.EsValida(producto.PartitionKey) ||
                !TableKeys.EsValida(producto.RowKey))
                return Results.BadRequest(new
                {
                    error = "PartitionKey/RowKey obligatorios y sin / \\ # ? ni control chars",
                });
            try
            {
                var creado = await svc.CreateAsync(producto);
                return Results.Created(
                    $"/productos/{creado.PartitionKey}/{creado.RowKey}", creado);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return Results.Conflict(new { error = "Ya existe (PartitionKey + RowKey)" });
            }
        });

        // PUT /productos/{categoria}/{id} — actualizar
        app.MapPut("/productos/{categoria}/{id}", async (
            string categoria, string id, Producto producto, IProductosService svc) =>
        {
            producto.PartitionKey = categoria;
            producto.RowKey = id;
            return await svc.UpdateAsync(producto)
                ? Results.Ok(producto)
                : Results.NotFound(new { error = $"Producto {id} no encontrado" });
        });

        // DELETE /productos/{categoria}/{id} — borrar
        app.MapDelete("/productos/{categoria}/{id}", async (
            string categoria, string id, IProductosService svc) =>
            await svc.DeleteAsync(categoria, id)
                ? Results.NoContent()
                : Results.NotFound(new { error = $"Producto {id} no encontrado" }));

        // Reto 1 (slide 20) — filtro por rango de precio (OData seguro).
        app.MapGet("/productos/precio", (double min, double max) =>
            Results.Ok(new { filtro = ODataFilter.RangoPrecio(min, max) }));

        // Slide 5/14 — validar/normalizar una clave (lógica pura).
        app.MapGet("/tools/clave", (string valor) => Results.Ok(new
        {
            valor,
            valida = TableKeys.EsValida(valor),
            sanitizada = TableKeys.Sanitizar(valor),
        }));
    }
}
