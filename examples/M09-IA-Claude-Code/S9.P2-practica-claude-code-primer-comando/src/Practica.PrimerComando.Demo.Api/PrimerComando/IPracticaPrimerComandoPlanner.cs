namespace Practica.PrimerComando.Demo.Api.PrimerComando;

public sealed record PlanPrimerComando(
    ReportePreflight Preflight,
    IReadOnlyList<InformePaso> Pasos,
    AnalisisPrompt? AnalisisDelPromptDelAlumno,
    IReadOnlyList<string> SlashCommandsEsenciales,
    IReadOnlyList<string> Checklist);

public sealed record PlanRequest(
    EscenarioPreflight Preflight,
    IReadOnlyList<EvidenciaPaso> Evidencias,
    string? PromptDelAlumno = null);

// Compone PrimerComandoPreflight + PasoEvaluator + PromptPatronDetector
// en el plan + checklist (slide 2) + referencia de slash commands
// (slide 9). Servicio inyectable.
public interface IPracticaPrimerComandoPlanner
{
    PlanPrimerComando Planificar(PlanRequest req);
}

public sealed class PracticaPrimerComandoPlanner : IPracticaPrimerComandoPlanner
{
    // Slide 9 — los slash commands que el alumno aprende.
    public static IReadOnlyList<string> SlashCommandsEsencialesSlide9 { get; } =
    [
        "/help — ver ayuda dentro de la sesión.",
        "/clear — limpiar pantalla.",
        "/compact — compactar el contexto cuando se acerca al límite.",
        "/permissions — cambiar entre default / acceptEdits / plan.",
        "/exit — salir de Claude Code limpiamente.",
        "/cost — ver tokens usados en la sesión.",
        "/model — cambiar entre Opus / Sonnet / Haiku.",
        "/init — generar `CLAUDE.md` inicial del proyecto.",
    ];

    public PlanPrimerComando Planificar(PlanRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        var preflight = PrimerComandoPreflight.Comprobar(req.Preflight);
        var pasos = req.Evidencias.Select(PasoEvaluator.Evaluar).ToList();
        var analisis = !string.IsNullOrWhiteSpace(req.PromptDelAlumno)
            ? PromptPatronDetector.Analizar(req.PromptDelAlumno)
            : null;

        return new PlanPrimerComando(
            Preflight: preflight,
            Pasos: pasos,
            AnalisisDelPromptDelAlumno: analisis,
            SlashCommandsEsenciales: SlashCommandsEsencialesSlide9,
            // Slide 2 — checklist canónica de los 8 pasos.
            Checklist:
            [
                "Claude Code instalado: `npm install -g @anthropic-ai/claude-code` (slide 4).",
                "Autenticado con `Login with claude.ai` o `ANTHROPIC_API_KEY` (slide 5).",
                "Primera sesión: `claude` en un proyecto y `> ¿Qué hay aquí?` (slide 5).",
                "Pedirle algo concreto: explicar `Program.cs` paso a paso (slide 6).",
                "Modificar un archivo guiado por Claude y aprobar el diff (slide 6).",
                "Ejecutar comandos con su aprobación: `dotnet build`, `dotnet run` (slide 7).",
                "Entender permission modes: `default`, `acceptEdits`, `plan` (slide 8).",
                "Slash commands esenciales: `/help`, `/cost`, `/model`, `/permissions` (slide 9).",
                "Generar `CLAUDE.md` con `/init` y pulirlo (slide 10).",
                "Pedirle generar y ejecutar un test xUnit trivial (slide 11).",
                "Cerrar sesión limpia con `/exit` o `Ctrl+C` (slide 2).",
            ]);
    }
}
