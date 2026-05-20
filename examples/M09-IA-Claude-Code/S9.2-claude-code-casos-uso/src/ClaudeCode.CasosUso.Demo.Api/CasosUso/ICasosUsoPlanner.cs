namespace ClaudeCode.CasosUso.Demo.Api.CasosUso;

public sealed record PlanCasoUso(
    ClasificacionCaso Clasificacion,
    PromptTemplate Template,
    EvaluacionPrompt? EvaluacionDelPromptDelAlumno,
    IReadOnlyList<string> Checklist);

// Compone CaseClassifier + PromptTemplateBuilder + PromptQualityEvaluator
// en el plan + checklist. Servicio inyectable (seam del test DI —
// lección M03-S3.4 / patrón M06-M09).
public interface ICasosUsoPlanner
{
    PlanCasoUso Planificar(string descripcionTarea, string? promptDelAlumno = null);
}

public sealed class CasosUsoPlanner : ICasosUsoPlanner
{
    public PlanCasoUso Planificar(string descripcionTarea, string? promptDelAlumno = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcionTarea);

        var clasif = CaseClassifier.Clasificar(descripcionTarea);
        var template = PromptTemplateBuilder.ParaCaso(clasif.Caso);
        var evaluacion = !string.IsNullOrWhiteSpace(promptDelAlumno)
            ? PromptQualityEvaluator.Evaluar(promptDelAlumno)
            : null;

        return new PlanCasoUso(
            Clasificacion: clasif,
            Template: template,
            EvaluacionDelPromptDelAlumno: evaluacion,
            // Slides 2-16 transversal — checklist del flujo "tarea → prompt".
            Checklist:
            [
                "Identifica el CASO de uso (migración, review, IaC, etc.) — slides 2-16.",
                "Empieza por el TEMPLATE canónico del caso (no escribas el prompt de cero).",
                "Añade los 4 ingredientes: contexto, constraints, formato salida, criterio éxito.",
                "Mete el archivo/path/ID concreto, no `el código` o `este servicio`.",
                "Si la tarea es larga, usa modo interactive y rompe en pasos (slide 9).",
                "Si la tarea es recurrente, define un skill (.claude/skills/) — lección S9.1.",
                "Si la tarea consume contexto, delega a un subagent (slide 16 / S9.1).",
                "Después del primer turno, verifica que la salida cumple el criterio de éxito.",
                "Itera con prompts cortos de refinamiento si hace falta.",
            ]);
    }
}
