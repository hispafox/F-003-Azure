namespace ClaudeCode.Limites.Demo.Api.Limites;

public enum AntiPattern
{
    EscribemeTodoElSistema,         // slide 13 #1
    AceptarSinEntender,              // slide 13 #2
    SinContextoDeProyecto,           // slide 13 #3
    SkipTestsPorVelocidad,           // slide 13 #4
    ConfianzaEnPrimerOutput,         // slide 13 #5
    SinMemoryNiContext,              // slide 13 #6
    ClaudeLoArreglaTodo,             // slide 13 #7
    SinContextoDeNegocio,            // slide 13 #8
    SecretosOPiiEnPrompt,            // slide 13 #9
    IaEnCiSinGuardrails,             // slide 13 #10
}

public sealed record AntiPatternDetectado(
    AntiPattern Pattern,
    string Causa,
    string Fix);

public sealed record InformeAntiPatterns(
    bool Limpio,
    IReadOnlyList<AntiPatternDetectado> Hallazgos);

// Slide 13 — detector de los 10 anti-patterns. Lógica pura. Busca
// frases canónicas en la descripción de cómo se está usando Claude
// Code en el equipo. Cada match propone un fix concreto del slide.
public static class AntiPatternDetector
{
    // Patrón → (anti-pattern, causa, fix). Primer match acumula
    // (varios pueden coexistir).
    private static readonly (string[] Patrones, AntiPattern Pattern, string Causa, string Fix)[]
        Reglas =
        [
            (["todo el sistema", "todo el código", "todo el proyecto", "scaffold all"],
                AntiPattern.EscribemeTodoElSistema,
                "Pedirle a Claude que genere todo el sistema de una vez.",
                "Iterar en chunks pequeños (1 endpoint a la vez) y commits frecuentes."),

            (["funciona, no toco", "no entiendo pero compila", "sin revisar", "sin entender"],
                AntiPattern.AceptarSinEntender,
                "Mergear código sin revisar línea a línea.",
                "Code review como si fuera un junior: lee cada línea y pregunta el porqué."),

            (["sin claude.md", "sin agents.md", "sin contexto del proyecto", "cada vez de cero"],
                AntiPattern.SinContextoDeProyecto,
                "Cada conversación arranca de cero sin convenciones del proyecto.",
                "CLAUDE.md robusto con stack, convenciones, naming y ejemplos."),

            (["sin tests", "skip tests", "tests luego", "tests después"],
                AntiPattern.SkipTestsPorVelocidad,
                "Generar código sin generar también los tests.",
                "Los tests son parte del prompt: TDD-style o `generate code AND tests`."),

            (["el primer output", "primer resultado", "sin verificar", "sin ejecutar"],
                AntiPattern.ConfianzaEnPrimerOutput,
                "Confiar en lo primero que devuelve Claude sin ejecutar ni verificar.",
                "Run + test edge cases + pídele a Claude que critique su propio output."),

            (["sin memory", "sin subagent", "sin skill", "repito el contexto"],
                AntiPattern.SinMemoryNiContext,
                "No aprovechar memory, subagents ni skills del 2026.",
                "Memory para preferencias persistentes, subagents para paralelo, skills para flujos repetidos."),

            (["claude lo arregla todo", "deja que claude piense", "no pienso", "que decida claude"],
                AntiPattern.ClaudeLoArreglaTodo,
                "Delegar el thinking en Claude; senior devs se vuelven juniors.",
                "Claude como pair: tú decides y revisas, Claude implementa e itera."),

            (["sin contexto de negocio", "sin kpi", "sin user persona", "ignorando el dominio"],
                AntiPattern.SinContextoDeNegocio,
                "Generar código técnicamente correcto que no resuelve el problema real.",
                "Incluir user persona + KPI a impactar + compliance requirement en el prompt."),

            (["connection string", "password real", "datos de producción", "pii de clientes",
                "secret en el prompt", "secreto en el prompt"],
                AntiPattern.SecretosOPiiEnPrompt,
                "Compartir secretos o PII reales en el prompt — salen de tu red.",
                "Sanitiza antes de compartir: usa placeholders y MCP con tokens scope-limited (Enterprise = zero retention)."),

            (["claude commitea", "auto-merge", "claude mergea", "sin review humano", "pipeline sin gates"],
                AntiPattern.IaEnCiSinGuardrails,
                "Pipeline de IA en CI sin human-in-the-loop.",
                "Claude crea PR (no merge) + tests automáticos + review humano obligatorio."),
        ];

    public static InformeAntiPatterns Detectar(string descripcionUso)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcionUso);

        var lower = descripcionUso.ToLowerInvariant();
        var hallazgos = new List<AntiPatternDetectado>();
        var vistos = new HashSet<AntiPattern>();

        foreach (var (patrones, pattern, causa, fix) in Reglas)
        {
            if (vistos.Contains(pattern)) continue;
            foreach (var p in patrones)
            {
                if (lower.Contains(p, StringComparison.Ordinal))
                {
                    hallazgos.Add(new AntiPatternDetectado(pattern, causa, fix));
                    vistos.Add(pattern);
                    break;
                }
            }
        }

        return new InformeAntiPatterns(hallazgos.Count == 0, hallazgos);
    }
}
