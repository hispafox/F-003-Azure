namespace Practica.CcMcp.Demo.Api.Practica;

public sealed record PlanPracticaCcMcp(
    ReportePreflight Preflight,
    IReadOnlyList<InformeEjercicio> Ejercicios,
    ComparativaPrompts? Comparativa,
    IReadOnlyList<string> Checklist);

public sealed record EvaluacionRequest(
    EscenarioPreflight Preflight,
    IReadOnlyList<EvidenciaEjercicio> Evidencias,
    string? PromptVago = null,
    string? PromptMedio = null,
    string? PromptDetallado = null);

// Compone PracticaPreflight + EjercicioEvaluator + PromptComparison
// en el plan + checklist (slide 8). Servicio inyectable.
public interface IPracticaCcMcpPlanner
{
    PlanPracticaCcMcp Planificar(EvaluacionRequest req);
}

public sealed class PracticaCcMcpPlanner : IPracticaCcMcpPlanner
{
    public PlanPracticaCcMcp Planificar(EvaluacionRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        var preflight = PracticaPreflight.Comprobar(req.Preflight);
        var ejercicios = req.Evidencias
            .Select(EjercicioEvaluator.Evaluar)
            .ToList();

        ComparativaPrompts? comparativa = null;
        if (!string.IsNullOrWhiteSpace(req.PromptVago)
            && !string.IsNullOrWhiteSpace(req.PromptMedio)
            && !string.IsNullOrWhiteSpace(req.PromptDetallado))
        {
            comparativa = PromptComparison.Comparar(
                req.PromptVago, req.PromptMedio, req.PromptDetallado);
        }

        return new PlanPracticaCcMcp(
            Preflight: preflight,
            Ejercicios: ejercicios,
            Comparativa: comparativa,
            // Slide 8 — checklist canónica de la práctica.
            Checklist:
            [
                "Claude Code instalado y autenticado (slide 2/8 ej. 1).",
                "Servicio generado + compila + tests pasan (slide 3/8 ej. 2).",
                "Bicep generado + `az bicep build` + `az deployment validate` OK (slide 4/8 ej. 3).",
                "MCP configurado con ADO (opcional, slide 5/8 ej. 4).",
                "Work item / bug creado vía MCP (opcional, slide 5).",
                "Error de producción analizado correctamente (slide 6/8 ej. 6).",
                "Refactoring sugerido es relevante y aplicable (slide 7).",
                "README generado refleja el código real, no inventa (slide 11).",
                "Comparativa de 3 prompts ejecutada y entendida (slide 12).",
                "MCP server custom arranca y `mcp-inspector` muestra los tools (slide 13).",
            ]);
    }
}
