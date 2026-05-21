namespace Practica.PrimerComando.Demo.Api.PrimerComando;

public enum PatronPrompt
{
    AntiMuyGenerico,         // slide 12 — "mejora el código"
    AntiPedirleAdivinar,     // slide 12 — "arregla los bugs"
    AntiTodoDeGolpe,         // slide 12 — "crea una API + auth + tests + CI/CD"
    BuenoConfirmacionPrevia, // slide 12 — "antes de implementar, dime cómo lo harías"
    BuenoRubberDuck,         // slide 12 — "mi enfoque es X, ¿me explico mal?"
}

public sealed record PatronDetectado(
    PatronPrompt Patron, string Causa, string SugerenciaFix);

public sealed record AnalisisPrompt(
    bool TieneAntiPatterns,
    IReadOnlyList<PatronDetectado> Hallazgos,
    int PuntuacionEstimada);  // 0-100

// Slide 12 — detector de los 5 patterns canónicos del prompt en la
// práctica simplificada. Anti-patterns restan, patterns positivos
// suman. Lógica pura.
public static class PromptPatronDetector
{
    // Frase → (patrón, causa, fix). Primer match dentro de cada
    // categoría cuenta una sola vez.
    private static readonly (string[] Patrones, PatronPrompt Patron, string Causa, string Fix)[]
        Reglas =
        [
            (["mejora el código", "mejora el codigo", "mejora esto", "haz que sea mejor",
                "limpia el código"],
                PatronPrompt.AntiMuyGenerico,
                "Prompt demasiado genérico: Claude no sabe qué priorizar (slide 12).",
                "Sé concreto: `Refactoriza X para extraer Y a un método separado llamado Z`."),

            (["arregla los bugs", "arregla todo", "fix the bugs", "arregla esto",
                "arréglalo"],
                PatronPrompt.AntiPedirleAdivinar,
                "Pides a Claude que adivine qué bug arreglar (slide 12).",
                "Da el síntoma: `Cuando ejecuto X sale el error Y en la línea Z. Arréglalo`."),

            (["crea una api", "monta el sistema entero", "todo a la vez",
                "todo el sistema", "haz el proyecto completo"],
                PatronPrompt.AntiTodoDeGolpe,
                "Demasiado scope en un solo prompt: saldrá una mezcla mediocre (slide 12).",
                "Itera por chunks: 1) esqueleto, 2) un endpoint, 3) tests, 4) CI/CD."),

            (["antes de implementar", "antes de hacer nada", "dime cómo lo harías",
                "dime como lo harias", "explícame tu plan", "explicame tu plan"],
                PatronPrompt.BuenoConfirmacionPrevia,
                "Pattern de confirmación previa: pides el plan antes de ejecutar (slide 12).",
                "Mantén este pattern para refactors grandes."),

            (["mi enfoque es", "mi enfoque actual", "estoy intentando hacer",
                "me explico mal", "me estoy explicando mal"],
                PatronPrompt.BuenoRubberDuck,
                "Pattern rubber duck: usas a Claude para pensar mejor, no solo ejecutar (slide 12).",
                "Mantén este pattern cuando estés atascado."),
        ];

    public static AnalisisPrompt Analizar(string prompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var lower = prompt.ToLowerInvariant();
        var hallazgos = new List<PatronDetectado>();
        var vistos = new HashSet<PatronPrompt>();

        foreach (var (patrones, patron, causa, fix) in Reglas)
        {
            if (vistos.Contains(patron)) continue;
            foreach (var p in patrones)
            {
                if (lower.Contains(p, StringComparison.Ordinal))
                {
                    hallazgos.Add(new PatronDetectado(patron, causa, fix));
                    vistos.Add(patron);
                    break;
                }
            }
        }

        bool tieneAntis = hallazgos.Any(h => EsAntiPattern(h.Patron));
        int puntuacion = CalcularPuntuacion(hallazgos);

        return new AnalisisPrompt(tieneAntis, hallazgos, puntuacion);
    }

    private static bool EsAntiPattern(PatronPrompt p) =>
        p is PatronPrompt.AntiMuyGenerico
            or PatronPrompt.AntiPedirleAdivinar
            or PatronPrompt.AntiTodoDeGolpe;

    private static int CalcularPuntuacion(IReadOnlyList<PatronDetectado> hallazgos)
    {
        // Base 50: prompt neutro. Cada anti-pattern resta 25 (cap 0).
        // Cada pattern positivo suma 25 (cap 100).
        int p = 50;
        foreach (var h in hallazgos)
        {
            if (EsAntiPattern(h.Patron)) p -= 25;
            else p += 25;
        }
        return Math.Clamp(p, 0, 100);
    }
}
