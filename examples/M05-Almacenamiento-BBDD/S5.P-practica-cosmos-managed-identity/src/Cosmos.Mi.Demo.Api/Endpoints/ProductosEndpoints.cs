using Cosmos.Mi.Demo.Api.Cosmos;
using Cosmos.Mi.Demo.Api.Domain;
using Cosmos.Mi.Demo.Api.Repositories;
using Cosmos.Mi.Demo.Api.Security;

namespace Cosmos.Mi.Demo.Api.Endpoints;

public static class ProductosEndpoints
{
    public static void MapPractica(this IEndpointRouteBuilder app)
    {
        // Slide 8 — la app declara que se conecta SIN passwords.
        app.MapGet("/", (IConfiguration cfg) => Results.Json(new
        {
            mensaje = "Cosmos DB con Managed Identity — sin passwords",
            endpoint = cfg["CosmosEndpoint"] ?? "(emulador local)",
        }));

        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        // ── Productos: CRUD keyless (slides 8, 12) ──
        var prod = app.MapGroup("/productos");

        prod.MapPost("/", async (CrearProductoDto dto, IProductoRepository repo) =>
        {
            var creado = await repo.CrearAsync(dto);
            return Results.Created($"/productos/{creado.Categoria}/{creado.Id}", creado);
        });

        prod.MapGet("/", async (string? categoria, IProductoRepository repo) =>
        {
            var (productos, ru) = await repo.ListarAsync(categoria);
            return Results.Ok(new { productos, ru, crossPartition = categoria is null });
        });

        prod.MapGet("/{categoria}/{id}", async (
            string categoria, string id, IProductoRepository repo) =>
        {
            var (p, ru) = await repo.GetAsync(categoria, id);
            return p is null ? Results.NotFound(new { ru }) : Results.Ok(new { producto = p, ru });
        });

        // Slide 11 — ¿buena partition key candidata?
        app.MapGet("/practica/partition-key", (
            int cardinalidad, bool uniforme, bool enQueries, bool estable) =>
            Results.Ok(new
            {
                veredicto = PartitionKeyAdvisor
                    .Evaluar(cardinalidad, uniforme, enQueries, estable).ToString(),
            }));

        // Slides 13, 19 — entregable: cero secretos en App Settings.
        // Solo debería verse CosmosEndpoint (una URL, no un secreto).
        app.MapGet("/practica/auditoria-secretos", (IConfiguration cfg) =>
        {
            var settings = cfg.AsEnumerable()
                .Where(kv => kv.Key.Contains("Cosmos", StringComparison.OrdinalIgnoreCase)
                          || kv.Key.Contains("Connection", StringComparison.OrdinalIgnoreCase)
                          || kv.Key.Contains("Key", StringComparison.OrdinalIgnoreCase));
            return Results.Ok(ZeroSecretsAuditor.Auditar(settings));
        });
    }
}
