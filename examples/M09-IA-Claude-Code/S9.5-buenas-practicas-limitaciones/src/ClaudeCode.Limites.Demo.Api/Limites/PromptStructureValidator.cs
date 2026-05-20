namespace ClaudeCode.Limites.Demo.Api.Limites;

public enum SeccionPrompt
{
    Contexto,         // qué hace el sistema, stack
    Objetivo,         // qué quieres lograr
    Constraints,      // lo que NO puede hacer
    Input,            // datos/archivos relevantes
    Output,           // formato/estructura esperada
    Examples,         // patrones a seguir
    DefinitionOfDone, // cómo saber si está bien
}

public sealed record ValidacionEstructura(
    int Puntuacion,                                    // 0-100
    IReadOnlyList<SeccionPrompt> SeccionesDetectadas,
    IReadOnlyList<SeccionPrompt> SeccionesFaltantes,
    IReadOnlyList<string> Sugerencias);

// Slide 12 — validador del template de 7 secciones del prompt
// efectivo. Complementa a `PromptQualityEvaluator` del S9.2 (que mira
// 4 ingredientes) — aquí miramos los 7 bloques del slide 12 con
// puntuación ponderada (DoD y Output pesan más que Examples).
// Lógica pura.
public static class PromptStructureValidator
{
    // Marcadores para cada sección, en minúsculas. El validador acepta
    // cualquier mención clara — no exige un literal "CONTEXTO:".
    private static readonly Dictionary<SeccionPrompt, string[]> Marcadores = new()
    {
        [SeccionPrompt.Contexto] =
            ["contexto", "stack", "proyecto", "framework", ".net", "arquitectura"],
        [SeccionPrompt.Objetivo] =
            ["objetivo", "quiero lograr", "necesito", "crea", "genera", "refactoriza"],
        [SeccionPrompt.Constraints] =
            ["constraints", "no añadir", "no romper", "no modificar", "mantener",
             "respetar", "sin cambios en"],
        [SeccionPrompt.Input] =
            ["input:", "archivos:", "sample", "ejemplo de entrada",
             "src/", "datos:", "lee"],
        [SeccionPrompt.Output] =
            ["output:", "salida:", "devuelve", "formato:", "guarda en",
             "genera el archivo", "json", "markdown"],
        [SeccionPrompt.Examples] =
            ["ejemplo:", "como en", "siguiendo el patrón", "similar a",
             "mira el archivo", "examples:"],
        [SeccionPrompt.DefinitionOfDone] =
            ["criterio éxito", "criterio de éxito", "definition of done", "dod",
             "tests verdes", "compila", "sin warnings", "build limpio"],
    };

    // Peso por sección — Contexto/Objetivo/Constraints/Output/DoD son
    // los críticos; Input y Examples son nice-to-have.
    private static readonly Dictionary<SeccionPrompt, int> Pesos = new()
    {
        [SeccionPrompt.Contexto] = 18,
        [SeccionPrompt.Objetivo] = 18,
        [SeccionPrompt.Constraints] = 15,
        [SeccionPrompt.Input] = 8,
        [SeccionPrompt.Output] = 15,
        [SeccionPrompt.Examples] = 8,
        [SeccionPrompt.DefinitionOfDone] = 18,
    };

    public static ValidacionEstructura Validar(string prompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var lower = prompt.ToLowerInvariant();
        var detectadas = new List<SeccionPrompt>();
        var faltantes = new List<SeccionPrompt>();
        int puntos = 0;

        foreach (var seccion in Enum.GetValues<SeccionPrompt>())
        {
            bool presente = Marcadores[seccion].Any(m =>
                lower.Contains(m, StringComparison.Ordinal));
            if (presente)
            {
                detectadas.Add(seccion);
                puntos += Pesos[seccion];
            }
            else
            {
                faltantes.Add(seccion);
            }
        }

        var sugerencias = new List<string>();
        foreach (var f in faltantes)
            sugerencias.Add(SugerenciaPara(f));

        return new ValidacionEstructura(
            Puntuacion: Math.Min(puntos, 100),
            SeccionesDetectadas: detectadas,
            SeccionesFaltantes: faltantes,
            Sugerencias: sugerencias);
    }

    private static string SugerenciaPara(SeccionPrompt seccion) => seccion switch
    {
        SeccionPrompt.Contexto =>
            "Falta CONTEXTO: di qué hace el sistema y qué stack usa (slide 12).",
        SeccionPrompt.Objetivo =>
            "Falta OBJETIVO: di con un verbo qué quieres lograr (crea / refactoriza / valida).",
        SeccionPrompt.Constraints =>
            "Faltan CONSTRAINTS: qué NO puede hacer (no añadir deps, no romper API pública).",
        SeccionPrompt.Input =>
            "Falta INPUT: rutas de archivos o datos sample relevantes.",
        SeccionPrompt.Output =>
            "Falta OUTPUT: formato esperado (archivos a generar, JSON, Markdown, etc.).",
        SeccionPrompt.Examples =>
            "Faltan EXAMPLES: archivo / patrón existente que el resultado debe imitar.",
        SeccionPrompt.DefinitionOfDone =>
            "Falta DoD: cómo sabremos que el resultado es correcto (tests verdes, métrica, etc.).",
        _ => "Sección desconocida.",
    };
}
