namespace Bonus.IntroIaAgentica.Demo.Api.Intro;

public sealed record PlanIntroIa(
    ClasificacionHerramienta? Clasificacion,
    RecomendacionHerramienta Recomendacion,
    EvaluacionNivel Nivel,
    IReadOnlyList<string> ObjetivosM11,
    IReadOnlyList<string> Checklist);

public sealed record PlanRequest(
    EscenarioUso Uso,
    EscenarioEquipo Equipo,
    string? DescripcionHerramientaActual = null);

// Compone GeneracionIaClassifier + CcVsCoworkRecommender +
// NivelUsoEvaluator en el plan + objetivos del módulo (slide 7) +
// checklist canónica.
public interface IIntroIaAgenticaPlanner
{
    PlanIntroIa Planificar(PlanRequest req);
}

public sealed class IntroIaAgenticaPlanner : IIntroIaAgenticaPlanner
{
    // Slide 7 — lo que vais a saber al terminar M11.
    public static IReadOnlyList<string> ObjetivosM11Slide7 { get; } =
    [
        "Configurar Claude Code para Azure (azure-skills plugin, Azure MCP, Bicep MCP, ADO MCP).",
        "Crear skills propios del equipo (convenciones, runbooks, checklists).",
        "Crear agentes especializados (code reviewer, bicep reviewer, security auditor).",
        "Orquestar workflows multi-agente.",
        "Usar Cowork para tareas no-code: reports, análisis de costes, exports.",
        "Volver sobre M1-M10 y ver cómo CC acelera cada uno (slide 6).",
        "Construir una solución Azure completa delegando ~70% a IA (slide 11).",
    ];

    public PlanIntroIa Planificar(PlanRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        var clasif = !string.IsNullOrWhiteSpace(req.DescripcionHerramientaActual)
            ? GeneracionIaClassifier.Clasificar(req.DescripcionHerramientaActual)
            : null;
        var rec = CcVsCoworkRecommender.Recomendar(req.Uso);
        var nivel = NivelUsoEvaluator.Evaluar(req.Equipo);

        return new PlanIntroIa(
            Clasificacion: clasif,
            Recomendacion: rec,
            Nivel: nivel,
            ObjetivosM11: ObjetivosM11Slide7,
            // Slide 8 + 18 — checklist de arranque.
            Checklist:
            [
                "Verifica `node --version` ≥ 18 y `npm` ≥ 9 (slide 8).",
                "Verifica `az --version` y opcional `azd --version` (slide 8).",
                "Instala Claude Code: `npm install -g @anthropic-ai/claude-code` (slide 8).",
                "Decide privacidad: plan personal vs Enterprise ZDR vs Claude en Foundry (slide 13/14).",
                "Empieza en Nivel 1 (ayudante) y sube según el equipo gana confianza (slide 10).",
                "Aplica los 4 principios desde el día 1: skills en Git + permisos mínimos + " +
                    "humano en loop + auditar uso (slide 18).",
                "Lee la sección 'Lo que NO es Claude Code' (slide 12) y comparte expectativas con el equipo.",
            ]);
    }
}
