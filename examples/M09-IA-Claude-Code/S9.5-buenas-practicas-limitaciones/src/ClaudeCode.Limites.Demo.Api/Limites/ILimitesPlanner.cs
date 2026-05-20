namespace ClaudeCode.Limites.Demo.Api.Limites;

public sealed record PlanLimites(
    InformeAntiPatterns? AntiPatterns,
    ValidacionEstructura? Estructura,
    ClasificacionTarea? Clasificacion,
    IReadOnlyList<string> ReglasDeOro,
    IReadOnlyList<string> Checklist);

// Compone AntiPatternDetector + PromptStructureValidator +
// AceleraOFrenaClassifier en el plan + las 7 reglas de oro (slide 2)
// + checklist (slide 13). Servicio inyectable.
public interface ILimitesPlanner
{
    PlanLimites Planificar(
        string? descripcionUso = null,
        string? promptDelAlumno = null,
        TipoTareaIa? tipoTarea = null);
}

public sealed class LimitesPlanner : ILimitesPlanner
{
    // Slide 2 — las 7 reglas de oro del desarrollo asistido por IA.
    public static IReadOnlyList<string> ReglasDeOroSlide2 { get; } =
    [
        "1. Revisar siempre: IA genera, humano valida. Nunca mergear sin revisar.",
        "2. Dar contexto: prompts vagos producen código genérico. Sé específico.",
        "3. Iterar: el primer resultado rara vez es perfecto. Refina en 2-3 turnos.",
        "4. Tests primero: si los tests definen el comportamiento, el código generado es más fiable.",
        "5. No confiar ciegamente: la IA inventa APIs/métodos. Compila y ejecuta siempre.",
        "6. Seguridad: nunca pases secretos reales en el prompt. Variables de entorno.",
        "7. Documentar prompts útiles: si un prompt funciona, guárdalo en `.claude/prompts/`.",
    ];

    public PlanLimites Planificar(
        string? descripcionUso = null,
        string? promptDelAlumno = null,
        TipoTareaIa? tipoTarea = null)
    {
        var antiPatterns = !string.IsNullOrWhiteSpace(descripcionUso)
            ? AntiPatternDetector.Detectar(descripcionUso)
            : null;
        var estructura = !string.IsNullOrWhiteSpace(promptDelAlumno)
            ? PromptStructureValidator.Validar(promptDelAlumno)
            : null;
        var clasif = tipoTarea is not null
            ? AceleraOFrenaClassifier.Clasificar(tipoTarea.Value)
            : null;

        return new PlanLimites(
            AntiPatterns: antiPatterns,
            Estructura: estructura,
            Clasificacion: clasif,
            ReglasDeOro: ReglasDeOroSlide2,
            // Slide 13 — checklist defensiva.
            Checklist:
            [
                "¿Estás iterando en chunks pequeños? Sin `escríbeme todo` (slide 13 #1).",
                "¿Has revisado cada línea como si fuera un junior? (slide 13 #2).",
                "¿Tienes CLAUDE.md con stack + convenciones? (slide 13 #3).",
                "¿Los tests son parte del prompt? (slide 13 #4).",
                "¿Has ejecutado / compilado el output antes de mergear? (slide 13 #5).",
                "¿Aprovechas memory + subagents + skills? (slide 13 #6, S9.1).",
                "¿Tú decides la arquitectura, no Claude? (slide 13 #7).",
                "¿Has incluido contexto de negocio (user persona, KPI)? (slide 13 #8).",
                "¿Has sanitizado secrets / PII del prompt? (slide 13 #9, S9.4).",
                "¿El pipeline de IA crea PRs en vez de mergear directo? (slide 13 #10).",
            ]);
    }
}
