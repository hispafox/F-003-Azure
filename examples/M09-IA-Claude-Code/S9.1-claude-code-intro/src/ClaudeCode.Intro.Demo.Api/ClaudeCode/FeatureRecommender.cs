namespace ClaudeCode.Intro.Demo.Api.ClaudeCode;

public enum TipoTarea
{
    GenerarCodigo,    // crear servicios, endpoints, tests
    Refactorizar,
    DepurarError,
    GenerarIac,       // Bicep / Dockerfile / YAML pipelines
    AnalisisLogs,
    CodeReview,
    Arquitectura,     // diseño de sistemas complejos
    ChangelogODocs,
}

public enum ModoEjecucion
{
    Interactive,      // `claude` — conversación
    OneShot,          // `claude -p "..."` — un solo prompt
    Pipe,             // `cat x | claude -p "..."` — input por stdin
    Headless,         // `--no-interactive` para CI/CD
}

public sealed record CaracteristicaSugerida(string Nombre, string Slide, string Porque);

public sealed record RecomendacionFeature(
    ModoEjecucion Modo,
    bool UsarExtendedThinking,
    IReadOnlyList<CaracteristicaSugerida> Caracteristicas);

public sealed record EscenarioTarea(
    TipoTarea Tarea,
    bool EsRecurrente = false,
    bool EsCompleja = false,
    bool EnPipelineCiCd = false,
    bool RequiereContextoAislado = false);

// Slides 4, 7-10, 12, 15, 16, 18, 19, 20 — recomendador del modo de
// ejecución y de las features complementarias (extended thinking,
// subagent, skill, hook) según el tipo de tarea. Lógica pura.
public static class FeatureRecommender
{
    public static RecomendacionFeature Recomendar(EscenarioTarea e)
    {
        ArgumentNullException.ThrowIfNull(e);

        // Modo de ejecución (slide 12).
        ModoEjecucion modo;
        if (e.EnPipelineCiCd)
            modo = ModoEjecucion.Headless;            // slide 16
        else if (e.Tarea == TipoTarea.AnalisisLogs)
            modo = ModoEjecucion.Pipe;                // slide 12: cat log | claude
        else if (e.Tarea == TipoTarea.ChangelogODocs)
            modo = ModoEjecucion.OneShot;             // slide 17
        else if (e.EsCompleja || e.Tarea == TipoTarea.Arquitectura)
            modo = ModoEjecucion.Interactive;         // diálogo
        else
            modo = ModoEjecucion.Interactive;

        // Extended thinking (slide 15) para problemas de arquitectura
        // o tareas complejas con muchas restricciones cruzadas.
        bool extendedThinking =
            e.Tarea == TipoTarea.Arquitectura
            || (e.EsCompleja && e.Tarea is TipoTarea.Refactorizar or TipoTarea.DepurarError);

        // Features complementarias.
        var features = new List<CaracteristicaSugerida>();

        // Subagent (slide 18) si la tarea requiere contexto aislado
        // (code review, security audit, test running, exploración).
        if (e.RequiereContextoAislado
            || e.Tarea is TipoTarea.CodeReview or TipoTarea.AnalisisLogs)
            features.Add(new CaracteristicaSugerida(
                Nombre: SubagentSugerido(e.Tarea),
                Slide: "18",
                Porque: "Aísla el contexto del main thread: el subagent recoge logs/" +
                    "diffs y devuelve sólo el resumen."));

        // Skill (slide 20) si la tarea es recurrente.
        if (e.EsRecurrente)
            features.Add(new CaracteristicaSugerida(
                Nombre: SkillSugerido(e.Tarea),
                Slide: "20",
                Porque: "Workflow recurrente: define un skill en " +
                    "`.claude/skills/<nombre>/SKILL.md` para invocarlo con " +
                    "`/<nombre>`."));

        // Hook (slide 19) si se entra en pipeline o se ejecuta `bash`
        // que pueda ser destructivo.
        if (e.EnPipelineCiCd)
            features.Add(new CaracteristicaSugerida(
                Nombre: "PreToolUse hook (matcher Bash)",
                Slide: "19",
                Porque: "Quality gate determinístico: el hook decide bloquear " +
                    "(`exit 2`) o permitir (`exit 0`) antes de cualquier ejecución."));

        return new RecomendacionFeature(modo, extendedThinking, features);
    }

    private static string SubagentSugerido(TipoTarea t) => t switch
    {
        TipoTarea.CodeReview => "code-reviewer subagent",
        TipoTarea.AnalisisLogs => "log-analyst subagent",
        TipoTarea.DepurarError => "debugger subagent",
        _ => "Explore / Plan subagent",
    };

    private static string SkillSugerido(TipoTarea t) => t switch
    {
        TipoTarea.GenerarCodigo => "new-service / new-endpoint",
        TipoTarea.GenerarIac => "bicep-bootstrap",
        TipoTarea.ChangelogODocs => "changelog-from-commits",
        TipoTarea.CodeReview => "ai-code-review",
        _ => "team-template",
    };
}
