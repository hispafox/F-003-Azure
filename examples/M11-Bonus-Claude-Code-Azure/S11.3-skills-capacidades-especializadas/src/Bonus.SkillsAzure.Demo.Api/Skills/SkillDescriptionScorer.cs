namespace Bonus.SkillsAzure.Demo.Api.Skills;

public sealed record EvaluacionDescription(
    int Puntuacion,                          // 0-100
    bool SeActivaraFiable,                   // ¿Claude lo cargará cuando aplique?
    IReadOnlyList<string> KeywordsDetectadas,
    IReadOnlyList<string> PalabrasVagas,
    IReadOnlyList<string> Sugerencias);

// Slide 16/24 — la `description` es el campo MÁS importante de un
// skill: Claude decide cargarlo (progressive disclosure, slide 5) por
// ella. Lógica pura. Premia keywords concretas (servicios Azure,
// acciones) y longitud; penaliza lenguaje vago ("help", "maybe",
// "puede") que Claude interpreta como "opcional".
public static class SkillDescriptionScorer
{
    // Señales de especificidad: si la description las menciona, Claude
    // sabe cuándo activar el skill.
    private static readonly string[] Keywords =
    [
        "deploy", "bicep", "what-if", "app service", "container apps",
        "cosmos", "functions", "service bus", "key vault", "rbac",
        "managed identity", "smoke test", "slot swap", "app insights",
        "storage", "msix", "clickonce", "azd", ".net", "review",
        "validate", "migrate", "cost", "security", "azure", "conventions",
    ];

    // Palabras vagas (slide 16): hacen que Claude no sepa cuándo cargar.
    private static readonly string[] Vagas =
    [
        "help", "helps", "helpful", "maybe", "perhaps", "things",
        "stuff", "various", "puede", "quizás", "quizas", "ayuda",
        "cosas", "general",
    ];

    public static EvaluacionDescription Evaluar(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var lower = description.ToLowerInvariant();

        var keywords = Keywords
            .Where(k => lower.Contains(k, StringComparison.Ordinal))
            .ToList();
        var vagas = Vagas
            .Where(v => lower.Contains(v, StringComparison.Ordinal))
            .ToList();

        int puntos = 0;

        // Keywords concretas: hasta 60 puntos (15 por cada una, 4 saturan).
        puntos += Math.Min(keywords.Count * 15, 60);

        // Longitud: una description específica suele pasar de 40 chars.
        if (description.Length >= 40) puntos += 20;
        else if (description.Length >= 20) puntos += 10;

        // Bonus por verbo de acción al inicio (Deploy, Review, Generate…).
        if (EmpiezaConVerboAccion(lower)) puntos += 20;

        // Penalización por lenguaje vago.
        puntos -= vagas.Count * 25;

        puntos = Math.Clamp(puntos, 0, 100);

        var sugerencias = new List<string>();
        if (vagas.Count > 0)
            sugerencias.Add($"Quita el lenguaje vago ({string.Join(", ", vagas)}): " +
                "Claude lo interpreta como \"opcional\" y no carga el skill (slide 16).");
        if (keywords.Count == 0)
            sugerencias.Add("Añade keywords concretas (servicio Azure + acción): " +
                "`Deploy a .NET app to Azure App Service with Bicep validation` (slide 16).");
        if (description.Length < 40)
            sugerencias.Add("La description es muy corta. Sé específico: si un humano " +
                "del equipo sabe cuándo usar el skill leyéndola, Claude también (slide 16).");

        // Slide 16: una description fiable tiene keywords, sin vaguedad.
        bool fiable = puntos >= 60 && vagas.Count == 0 && keywords.Count >= 1;

        return new EvaluacionDescription(
            Puntuacion: puntos,
            SeActivaraFiable: fiable,
            KeywordsDetectadas: keywords,
            PalabrasVagas: vagas,
            Sugerencias: sugerencias);
    }

    private static readonly string[] VerbosAccion =
        ["deploy", "review", "generate", "validate", "migrate", "create",
         "set up", "analyze", "audit", "prepare", "troubleshoot", "apply"];

    private static bool EmpiezaConVerboAccion(string lower) =>
        VerbosAccion.Any(v => lower.StartsWith(v, StringComparison.Ordinal));
}
