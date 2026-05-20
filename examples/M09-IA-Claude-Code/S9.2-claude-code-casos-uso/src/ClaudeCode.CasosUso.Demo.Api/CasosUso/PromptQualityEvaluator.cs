namespace ClaudeCode.CasosUso.Demo.Api.CasosUso;

public enum NivelCalidad { Pobre, Aceptable, Bueno, Excelente }

public sealed record EvaluacionPrompt(
    int Puntuacion,                // 0-100
    NivelCalidad Nivel,
    bool TieneContexto,
    bool TieneConstraints,
    bool TieneFormatoSalida,
    bool TieneCriterioExito,
    IReadOnlyList<string> Sugerencias);

// Slides 18-23 (transversal) — evaluador de calidad del prompt que
// escribe el alumno. Heurística simple basada en la presencia de los 4
// ingredientes canónicos. Lógica pura.
public static class PromptQualityEvaluator
{
    // Marcadores que indican que el prompt incluye cada ingrediente.
    private static readonly string[] MarcadoresContexto =
    [
        "este proyecto", "el sistema", "la app", "uso ", "usamos",
        "framework", ".net", "azure", "cosmos", "service bus",
        "infraestructura", "stack",
    ];

    private static readonly string[] MarcadoresConstraints =
    [
        "no debe", "mantén", "mantener", "preserva", "preservar",
        "sin romper", "sin cambios en", "respetando", "respeta",
        "no inventes", "no rompas", "compatible con",
    ];

    private static readonly string[] MarcadoresFormato =
    [
        "output:", "salida:", "formato:", "devuelve", "responde",
        "json", "markdown", "tabla", "yaml", "csv", "guarda en",
        "guarda como", "genera el archivo",
    ];

    private static readonly string[] MarcadoresCriterio =
    [
        "criterio éxito", "criterio de éxito", "objetivo:",
        "tests verdes", "compila", "build limpio", "sin warnings",
        "p99 <", "p95 <", "latencia <", "ru <",
    ];

    public static EvaluacionPrompt Evaluar(string prompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var lower = prompt.ToLowerInvariant();

        bool contexto = MarcadoresContexto.Any(m => lower.Contains(m, StringComparison.Ordinal));
        bool constraints = MarcadoresConstraints.Any(m => lower.Contains(m, StringComparison.Ordinal));
        bool formato = MarcadoresFormato.Any(m => lower.Contains(m, StringComparison.Ordinal));
        bool criterio = MarcadoresCriterio.Any(m => lower.Contains(m, StringComparison.Ordinal));

        // Penalizar prompts demasiado cortos (< 40 caracteres es siempre vago).
        bool muyCorto = prompt.Trim().Length < 40;

        int puntos = 0;
        if (contexto) puntos += 25;
        if (constraints) puntos += 25;
        if (formato) puntos += 25;
        if (criterio) puntos += 25;
        if (muyCorto) puntos = Math.Min(puntos, 25);

        var sugerencias = new List<string>();
        if (!contexto)
            sugerencias.Add("Falta contexto: di qué proyecto/sistema/stack es " +
                "(framework, persistencia, módulo).");
        if (!constraints)
            sugerencias.Add("Faltan constraints: explica qué NO debe romper " +
                "(funcionalidad existente, naming público, contratos).");
        if (!formato)
            sugerencias.Add("Falta formato de salida: especifica qué tipo de respuesta " +
                "esperas (JSON, Markdown, archivos a generar, etc.).");
        if (!criterio)
            sugerencias.Add("Falta criterio de éxito: cómo sabremos que el resultado " +
                "es correcto (tests verdes, build limpio, métrica objetivo, etc.).");
        if (muyCorto)
            sugerencias.Add("Prompt demasiado corto: amplía el contexto, los detalles " +
                "ahorran iteraciones (cada turno extra cuesta tokens).");

        var nivel = puntos switch
        {
            >= 90 => NivelCalidad.Excelente,
            >= 70 => NivelCalidad.Bueno,
            >= 40 => NivelCalidad.Aceptable,
            _ => NivelCalidad.Pobre,
        };

        return new EvaluacionPrompt(
            Puntuacion: puntos,
            Nivel: nivel,
            TieneContexto: contexto,
            TieneConstraints: constraints,
            TieneFormatoSalida: formato,
            TieneCriterioExito: criterio,
            Sugerencias: sugerencias);
    }
}
