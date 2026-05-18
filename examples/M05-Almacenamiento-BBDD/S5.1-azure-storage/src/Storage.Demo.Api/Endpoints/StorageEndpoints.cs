using Storage.Demo.Api.Models;
using Storage.Demo.Api.Repositories;
using Storage.Demo.Api.Storage;

namespace Storage.Demo.Api.Endpoints;

// Minimal API: endpoints finos que delegan en los repos. La lógica
// "pura" (rutas, tiers) está en Storage/* y se testea aparte.
public static class StorageEndpoints
{
    public static void MapStorage(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        // ── Blob (slides 6-10) ──
        var blob = app.MapGroup("/blob");

        blob.MapPost("/{container}/{*nombre}", async (
            string container, string nombre, HttpRequest req, IBlobRepository repo) =>
        {
            using var ms = new MemoryStream();
            await req.Body.CopyToAsync(ms);
            ms.Position = 0;
            await repo.SubirAsync(container, nombre, ms,
                req.ContentType ?? "application/octet-stream");
            return Results.Created($"/blob/{container}/{nombre}", new { container, nombre });
        });

        blob.MapGet("/{container}/{*nombre}", async (
            string container, string nombre, IBlobRepository repo) =>
        {
            var r = await repo.DescargarAsync(container, nombre);
            return r is null
                ? Results.NotFound(new { error = $"{container}/{nombre} no existe" })
                : Results.File(r.Value.datos, r.Value.contentType);
        });

        blob.MapGet("/{container}", async (
            string container, string? prefijo, IBlobRepository repo) =>
            Results.Ok(await repo.ListarAsync(container, prefijo)));

        blob.MapDelete("/{container}/{*nombre}", async (
            string container, string nombre, IBlobRepository repo) =>
            await repo.BorrarAsync(container, nombre)
                ? Results.NoContent()
                : Results.NotFound());

        // Slide 5 — sugerencia de access tier (lógica pura, sin tocar Azure).
        blob.MapGet("/tier-sugerido/{dias:int}", (int dias) =>
            Results.Ok(new
            {
                dias,
                tier = AccessTierPolicy.Sugerir(dias).ToString(),
                borrar = AccessTierPolicy.DebeBorrarse(dias),
            }));

        // ── Table (slides 11-12) ──
        var table = app.MapGroup("/table");

        table.MapPost("/", async (CrearAuditDto dto, ITableRepository repo) =>
            Results.Created("/table", await repo.InsertarAsync(dto)));

        table.MapGet("/{particion}/{rowKey}", async (
            string particion, string rowKey, ITableRepository repo) =>
        {
            var e = await repo.ObtenerAsync(particion, rowKey);
            return e is null ? Results.NotFound() : Results.Ok(e);
        });

        table.MapGet("/{particion}", async (string particion, ITableRepository repo) =>
            Results.Ok(await repo.PorParticionAsync(particion)));

        // ── Queue (slides 18-19) ──
        var queue = app.MapGroup("/queue");

        queue.MapPost("/{cola}", async (
            string cola, MensajeColaDto dto, IQueueRepository repo) =>
        {
            await repo.EncolarAsync(cola, dto.Cuerpo);
            return Results.Accepted();
        });

        queue.MapGet("/{cola}", async (string cola, IQueueRepository repo) =>
        {
            var m = await repo.RecibirAsync(cola);
            return m is null ? Results.NoContent() : Results.Ok(new { m.Value.mensaje, m.Value.messageId });
        });

        queue.MapGet("/{cola}/longitud", async (string cola, IQueueRepository repo) =>
            Results.Ok(new { cola, longitud = await repo.LongitudAproxAsync(cola) }));
    }
}
