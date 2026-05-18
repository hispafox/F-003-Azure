using Sql.Demo.Api.Domain;
using Sql.Demo.Api.Repositories;
using Sql.Demo.Api.Sql;

namespace Sql.Demo.Api.Endpoints;

// Minimal API: endpoints finos que delegan en los repos (EF Core) y en
// la lógica pura de Sql/* (afinado de conexión, tier advisor).
public static class VentasEndpoints
{
    public static void MapVentas(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        // ── Productos: CRUD (slide 7) ──
        var prod = app.MapGroup("/productos");

        prod.MapGet("/", async (IProductoRepository repo) =>
            Results.Ok(await repo.ListarAsync()));

        prod.MapGet("/{id:int}", async (int id, IProductoRepository repo) =>
        {
            var p = await repo.GetAsync(id);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        prod.MapPost("/", async (CrearProductoDto dto, IProductoRepository repo) =>
        {
            var p = await repo.CrearAsync(dto);
            return Results.Created($"/productos/{p.Id}", p);
        });

        prod.MapPut("/{id:int}", async (
            int id, ActualizarProductoDto dto, IProductoRepository repo) =>
        {
            var p = await repo.ActualizarAsync(id, dto);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        prod.MapDelete("/{id:int}", async (int id, IProductoRepository repo) =>
            await repo.BorrarAsync(id) ? Results.NoContent() : Results.NotFound());

        // ── Pedidos: crear (descuenta stock) + listar con Include ──
        var ped = app.MapGroup("/pedidos");

        ped.MapPost("/", async (CrearPedidoDto dto, IPedidoRepository repo) =>
        {
            var (resultado, pedido) = await repo.CrearAsync(dto);
            return resultado switch
            {
                CrearPedidoResultado.Ok => Results.Created($"/pedidos/{pedido!.Id}", pedido),
                CrearPedidoResultado.ProductoNoExiste =>
                    Results.NotFound(new { error = $"Producto {dto.ProductoId} no existe" }),
                _ => Results.Conflict(new { error = "Stock insuficiente" }),
            };
        });

        ped.MapGet("/", async (IPedidoRepository repo) =>
            Results.Ok(await repo.ListarAsync()));

        ped.MapGet("/{id:int}", async (int id, IPedidoRepository repo) =>
        {
            var p = await repo.GetAsync(id);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });

        // ── SQL: lógica pura (slides 4-5, 6, 10, 20) ──
        var sql = app.MapGroup("/sql");

        // Slides 4-5, 21 — recomendación de tier por carga.
        sql.MapGet("/tier-sugerido", (bool intermitente, int maxConexiones, int datosGb) =>
            Results.Ok(new
            {
                intermitente,
                maxConexiones,
                datosGb,
                tier = SqlTierAdvisor.Sugerir(intermitente, maxConexiones, datosGb).ToString(),
            }));

        // Slides 6, 10, 20 — info de la conexión efectiva, SIN exponer
        // secretos: solo si usa Managed Identity y los límites de pool.
        sql.MapGet("/conn-info", (IConfiguration cfg) =>
        {
            var cs = cfg["SqlConnection"];
            if (string.IsNullOrWhiteSpace(cs))
                return Results.Ok(new { configurada = false });
            return Results.Ok(new
            {
                configurada = true,
                usaManagedIdentity = SqlConnectionTuning.UsaManagedIdentity(cs),
                maxPoolSize = SqlConnectionTuning.MaxPoolSize,
                minPoolSize = SqlConnectionTuning.MinPoolSize,
            });
        });
    }
}
