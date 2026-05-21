namespace ProyectoIntegrador.Diseno.Demo.Api.Diseno;

public sealed record PlanProyecto(
    IReadOnlyList<EstadoArquitectura> Arquitectura,
    int PorcentajeDesplegado,
    RecomendacionBloque BloqueSiguiente,
    InformeEntrega? Entrega,
    IReadOnlyList<string> Retos);

public sealed record PlanRequest(
    EstadoSistema Sistema,
    EvidenciaEntrega? Entrega = null);

// Compone ArquitecturaChecklist + BloqueRecommender + EntregaEvaluator
// en el plan del proyecto integrador. Devuelve también los retos
// opcionales del slide 12. Servicio inyectable.
public interface IProyectoIntegradorPlanner
{
    PlanProyecto Planificar(PlanRequest req);
}

public sealed class ProyectoIntegradorPlanner : IProyectoIntegradorPlanner
{
    // Slide 12 — retos opcionales (bonus) del proyecto integrador.
    public static IReadOnlyList<string> RetosOpcionales { get; } =
    [
        "Reto 1: endpoint de búsqueda con filtros (fecha, importe, estado) (slide 12).",
        "Reto 2: auto-update MSIX para una app desktop que consume la API (M07-S7.6).",
        "Reto 3: timer trigger que genera un informe diario en Blob Storage (M03-S3.3 + M05-S5.1).",
        "Reto 4: usar Claude Code (M09) para generar uno de los componentes anteriores.",
        "Reto 5: canary deployment con feature flags (M08-S8.3).",
    ];

    public PlanProyecto Planificar(PlanRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        var arquitectura = ArquitecturaChecklist.Inventariar(req.Sistema);
        int pct = ArquitecturaChecklist.PorcentajeDesplegado(req.Sistema);
        var bloque = BloqueRecommender.Recomendar(req.Sistema);
        var entrega = req.Entrega is not null
            ? EntregaEvaluator.Evaluar(req.Entrega)
            : null;

        return new PlanProyecto(
            Arquitectura: arquitectura,
            PorcentajeDesplegado: pct,
            BloqueSiguiente: bloque,
            Entrega: entrega,
            Retos: RetosOpcionales);
    }
}
