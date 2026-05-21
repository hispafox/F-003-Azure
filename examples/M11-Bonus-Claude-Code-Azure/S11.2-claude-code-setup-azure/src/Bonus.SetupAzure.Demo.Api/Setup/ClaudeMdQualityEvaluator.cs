namespace Bonus.SetupAzure.Demo.Api.Setup;

public enum SeccionClaudeMd
{
    Stack,            // qué tecnologías usa
    Convenciones,     // reglas no negociables
    Comandos,         // build / test / deploy local
    Arquitectura,     // 2-3 líneas o link a docs/
    Glosario,         // dominio
    ZonasFragiles,    // "no tocar sin preguntar"
}

public sealed record EvaluacionClaudeMd(
    int Puntuacion,                          // 0-100
    IReadOnlyList<SeccionClaudeMd> SeccionesPresentes,
    IReadOnlyList<SeccionClaudeMd> SeccionesFaltantes,
    IReadOnlyList<string> AvisosDeAntiPatrones,
    IReadOnlyList<string> Sugerencias);

// Slide 5/6 — evaluador de calidad del CLAUDE.md. Lógica pura.
// Cubre los `DO` del slide 6 (6 secciones) y avisa de los `DON'T`
// (manual completo, secretos, docs de features, cosas inferibles).
// Penaliza si el archivo > 1 pantalla (estimación por líneas).
public static class ClaudeMdQualityEvaluator
{
    // Marcadores por sección — comparten estilo con los headers
    // típicos del slide 5.
    private static readonly Dictionary<SeccionClaudeMd, string[]> Marcadores = new()
    {
        [SeccionClaudeMd.Stack] =
            ["## stack", "## tecnologías", "## tecnologias", ".net", "cosmos",
             "azure functions", "service bus", "key vault"],
        [SeccionClaudeMd.Convenciones] =
            ["## convenciones", "## conventions", "async/await", "logging estructurado",
             "ilogger", "records para dtos", "convenciones del equipo"],
        [SeccionClaudeMd.Comandos] =
            ["## comandos", "## tareas", "## build", "dotnet build", "dotnet test",
             "az deployment", "comandos del proyecto"],
        [SeccionClaudeMd.Arquitectura] =
            ["## arquitectura", "## architecture", "componentes:", "diagrama"],
        [SeccionClaudeMd.Glosario] =
            ["## glosario", "## glossary", "## dominio", "## domain"],
        [SeccionClaudeMd.ZonasFragiles] =
            ["## no tocar", "## frágil", "## fragil", "no tocar sin preguntar",
             "zonas frágiles", "zonas fragiles", "review del lead"],
    };

    // Pesos: stack/convenciones/zonasFragiles son críticos; el resto
    // son nice-to-have.
    private static readonly Dictionary<SeccionClaudeMd, int> Pesos = new()
    {
        [SeccionClaudeMd.Stack] = 25,
        [SeccionClaudeMd.Convenciones] = 25,
        [SeccionClaudeMd.ZonasFragiles] = 20,
        [SeccionClaudeMd.Comandos] = 15,
        [SeccionClaudeMd.Arquitectura] = 10,
        [SeccionClaudeMd.Glosario] = 5,
    };

    // Marcadores de los `DON'T` (slide 6).
    private static readonly (string Patron, string Aviso)[] AntiPatrones =
    [
        ("password=", "DON'T del slide 6: hay un `password=` en el CLAUDE.md. " +
            "Los secretos NUNCA van aquí."),
        ("connection string", "DON'T del slide 6: hay una `connection string` literal. " +
            "Usa referencias a Key Vault o variables de entorno."),
        ("sk-ant-", "DON'T del slide 6: parece haber una API key (`sk-ant-`) en el CLAUDE.md."),
        ("xxxxxxxx", "DON'T del slide 6: hay placeholders `xxxxxxxx`; sustituye o quítalo."),
    ];

    public static EvaluacionClaudeMd Evaluar(string claudeMd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(claudeMd);

        var lower = claudeMd.ToLowerInvariant();
        var presentes = new List<SeccionClaudeMd>();
        var faltantes = new List<SeccionClaudeMd>();
        int puntos = 0;

        foreach (var seccion in Enum.GetValues<SeccionClaudeMd>())
        {
            bool presente = Marcadores[seccion].Any(m =>
                lower.Contains(m, StringComparison.Ordinal));
            if (presente)
            {
                presentes.Add(seccion);
                puntos += Pesos[seccion];
            }
            else
            {
                faltantes.Add(seccion);
            }
        }

        // DON'T: detectar anti-patrones (slide 6).
        var avisos = new List<string>();
        foreach (var (patron, aviso) in AntiPatrones)
            if (lower.Contains(patron, StringComparison.Ordinal))
                avisos.Add(aviso);

        // Slide 6: "CLAUDE.md debería caber en 1 pantalla". Estimamos
        // 1 pantalla ≈ 50 líneas. Si pasa de 80, sugerimos partir.
        int lineas = claudeMd.Split('\n').Length;
        if (lineas > 80)
            avisos.Add($"El CLAUDE.md ocupa {lineas} líneas: probablemente esté " +
                "creciendo demasiado. Parte por temas y enlaza desde aquí (slide 6).");

        // Sugerencias por secciones faltantes.
        var sugerencias = new List<string>();
        foreach (var f in faltantes)
            sugerencias.Add(SugerenciaPara(f));

        return new EvaluacionClaudeMd(
            Puntuacion: Math.Min(puntos, 100),
            SeccionesPresentes: presentes,
            SeccionesFaltantes: faltantes,
            AvisosDeAntiPatrones: avisos,
            Sugerencias: sugerencias);
    }

    private static string SugerenciaPara(SeccionClaudeMd s) => s switch
    {
        SeccionClaudeMd.Stack =>
            "Añade `## Stack` con las tecnologías y versiones (slide 5).",
        SeccionClaudeMd.Convenciones =>
            "Añade `## Convenciones` con las reglas no negociables (async/await, " +
            "ILogger, records DTOs, tests).",
        SeccionClaudeMd.Comandos =>
            "Añade `## Comandos` con `dotnet build`, `dotnet test` y el deploy local.",
        SeccionClaudeMd.Arquitectura =>
            "Añade `## Arquitectura` (2-3 líneas) o enlaza a `docs/architecture.md`.",
        SeccionClaudeMd.Glosario =>
            "Si usáis términos de dominio específicos, añade `## Glosario`.",
        SeccionClaudeMd.ZonasFragiles =>
            "Añade `## No tocar sin preguntar` con los archivos/módulos críticos (slide 5).",
        _ => "Sección desconocida.",
    };
}
