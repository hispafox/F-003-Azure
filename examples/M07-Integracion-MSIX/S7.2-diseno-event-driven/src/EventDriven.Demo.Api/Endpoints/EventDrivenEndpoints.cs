using EventDriven.Demo.Api.EventDriven;

namespace EventDriven.Demo.Api.Endpoints;

// DTO con discriminador para recibir el stream de eventos por JSON
// (System.Text.Json no deserializa la jerarquía polimórfica directa).
public sealed record EventoPedidoDto(
    string Tipo,
    string? ClienteId = null,
    string? Nombre = null,
    decimal? Precio = null,
    int? Cantidad = null,
    string? Codigo = null,
    decimal? Importe = null,
    string? Transaccion = null,
    string? Tracking = null);

public sealed record SourcingRequest(int SnapshotCada, List<EventoPedidoDto> Eventos);

public sealed record CompensacionRequest(List<string> Pasos, int FalloEnPaso);

public sealed record PlanRequest(
    EscenarioDiseno Escenario, List<DefinicionEvento> Catalogo);

public static class EventDrivenEndpoints
{
    public static void MapEventDriven(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        var e = app.MapGroup("/eventdriven");

        // Slide 6 — patrón de propagación de estado.
        e.MapGet("/patron", (bool? consumidorAutonomo, bool? eventosPequenos,
                bool? auditTrailCompleto) =>
            Results.Ok(new
            {
                patron = EventDesignAdvisor.RecomendarPatron(
                    consumidorAutonomo ?? false,
                    eventosPequenos ?? false,
                    auditTrailCompleto ?? false).ToString(),
            }));

        // Slide 13 — ¿buen caso para event-driven?
        e.MapPost("/caso", (EscenarioDiseno s) =>
            Results.Ok(EventDesignAdvisor.EsBuenCaso(
                s.MultiplesConsumidores, s.ProcesamientoPesado,
                s.EscaladoIndependiente, s.DisponibilidadSobreConsistencia,
                s.EquipoPuedeComplejidad, s.CrudSimple,
                s.ConsistenciaFuerteInmediata, s.VolumenBajo)));

        // Slide 8/22 — choreography vs orchestration.
        e.MapGet("/saga", (int pasos, bool? condicional) =>
            Results.Ok(new
            {
                estilo = EventDesignAdvisor.RecomendarSaga(
                    pasos, condicional ?? false).ToString(),
            }));

        // Slide 22 — secuencia de compensación (rollback inverso).
        e.MapPost("/compensacion", (CompensacionRequest r) =>
            Results.Ok(new
            {
                compensaciones = EventDesignAdvisor.SecuenciaCompensacion(
                    r.Pasos, r.FalloEnPaso),
            }));

        // Slide 20 — anti-patterns de un evento del catálogo.
        e.MapPost("/validar", (DefinicionEvento def) =>
            Results.Ok(EventValidator.Validar(def)));

        // Slide 20.1 — longitud de la cadena de eventos.
        e.MapGet("/cadena", (int saltos) =>
            Results.Ok(EventValidator.ValidarLongitudCadena(saltos)));

        // Slides 14-15/21 — Event Sourcing: replay + snapshot. El store
        // es por petición (stateful): se aplican los eventos y se carga
        // el estado (snapshot + replay de lo posterior).
        e.MapPost("/sourcing", (SourcingRequest req) =>
        {
            var store = new EventStore(req.SnapshotCada);
            const string stream = "PED-DEMO";
            foreach (var dto in req.Eventos)
                store.Append(stream, AEvento(dto));

            var estado = store.Cargar(stream);
            return Results.Ok(new
            {
                estado,
                snapshotsTomados = store.SnapshotsTomados,
                ultimoReplayCount = store.UltimoReplayCount,
            });
        });

        // Plan de diseño + checklist del entregable.
        e.MapPost("/plan", (PlanRequest req, IEventDrivenPlanner planner) =>
            Results.Ok(planner.Planificar(
                req.Escenario, req.Catalogo.AsReadOnly())));
    }

    private static EventoPedido AEvento(EventoPedidoDto d) => d.Tipo switch
    {
        "PedidoCreado" => new PedidoCreado(d.ClienteId ?? ""),
        "ItemAnadido" => new ItemAnadido(d.Nombre ?? "", d.Precio ?? 0m, d.Cantidad ?? 0),
        "DescuentoAplicado" => new DescuentoAplicado(d.Codigo ?? "", d.Importe ?? 0m),
        "PagoConfirmado" => new PagoConfirmado(d.Transaccion ?? ""),
        "PedidoEnviado" => new PedidoEnviado(d.Tracking ?? ""),
        _ => throw new ArgumentOutOfRangeException(nameof(d), d.Tipo),
    };
}
