using Cosmos.Demo.Api.Cosmos;
using Cosmos.Demo.Api.Domain;
using Cosmos.Demo.Api.Repositories;

namespace Cosmos.Demo.Api.Endpoints;

// Minimal API: endpoints finos que delegan en el repo (Cosmos SDK) y en
// la lógica pura de Cosmos/* (partition key, consistencia, RU).
public static class PedidosEndpoints
{
    public static void MapPedidos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        // ── Pedidos (slides 8-12, 17) ──
        var ped = app.MapGroup("/pedidos");

        ped.MapPost("/", async (CrearPedidoDto dto, IPedidoRepository repo) =>
        {
            var creado = await repo.CrearAsync(dto);
            return Results.Created($"/pedidos/{creado.ClienteId}/{creado.Id}", creado);
        });

        // Slide 17 — pedido + movimiento atómico (TransactionalBatch).
        ped.MapPost("/con-movimiento", async (
            CrearPedidoConMovimientoDto dto, IPedidoRepository repo) =>
        {
            var (ok, ru) = await repo.CrearPedidoConMovimientoAsync(
                dto.Pedido,
                new Movimiento { Tipo = dto.Tipo, Importe = dto.Importe });
            return ok
                ? Results.Ok(new { ok, ru })
                : Results.Conflict(new { ok, ru });
        });

        // Slide 8 — leer por id dentro de su partición (1 RU).
        ped.MapGet("/{clienteId}/{id}", async (
            string clienteId, string id, IPedidoRepository repo) =>
        {
            var (pedido, ru) = await repo.GetAsync(clienteId, id);
            return pedido is null
                ? Results.NotFound(new { ru })
                : Results.Ok(new { pedido, ru });
        });

        // Slide 8 — query single-partition (clienteId = partition key).
        ped.MapGet("/{clienteId}", async (string clienteId, IPedidoRepository repo) =>
        {
            var (pedidos, ru) = await repo.PorClienteAsync(clienteId);
            return Results.Ok(new { pedidos, ru });
        });

        // Slide 12 — soft delete.
        ped.MapDelete("/{clienteId}/{id}", async (
            string clienteId, string id, IPedidoRepository repo) =>
            await repo.SoftDeleteAsync(clienteId, id)
                ? Results.NoContent()
                : Results.NotFound());

        // ── Cosmos: lógica pura (slides 4-8, 11) ──
        var cosmos = app.MapGroup("/cosmos");

        // Slides 4-6 — ¿es buena la partition key candidata?
        cosmos.MapGet("/partition-key", (
            int cardinalidad, bool uniforme, bool alineada) =>
            Results.Ok(new
            {
                cardinalidad,
                uniforme,
                alineada,
                veredicto = PartitionKeyAdvisor
                    .Evaluar(cardinalidad, uniforme, alineada).ToString(),
            }));

        // Slide 11 — nivel de consistencia recomendado + coste RU.
        cosmos.MapGet("/consistencia", (
            bool financiero, bool ultimaEscritura, bool latenciaMinima) =>
        {
            var nivel = ConsistencyAdvisor.Recomendar(
                ultimaEscritura, financiero, latenciaMinima);
            return Results.Ok(new
            {
                nivel = nivel.ToString(),
                multiplicadorRu = ConsistencyAdvisor.MultiplicadorRu(nivel),
            });
        });

        // Slides 7-8 — estimación de RU por patrón de acceso.
        cosmos.MapGet("/ru-estimado", (TipoOperacion op, int docs) =>
            Results.Ok(new { op = op.ToString(), docs, ru = RuEstimator.Estimar(op, docs) }));
    }
}

public sealed record CrearPedidoConMovimientoDto(
    CrearPedidoDto Pedido, string Tipo, decimal Importe);
