namespace Practica.CcMcp.Demo.Api.Practica;

public enum NivelPreflight { Ok, Aviso, Bloqueante }

public sealed record HallazgoPreflight(NivelPreflight Nivel, string Comprobacion, string Mensaje);

public sealed record ReportePreflight(
    bool ListoParaArrancar,
    IReadOnlyList<HallazgoPreflight> Hallazgos);

public sealed record EscenarioPreflight(
    bool TieneNode18OSuperior = false,
    bool ClaudeInstaladoYAutenticado = false,
    bool TieneApiKey = false,
    bool TieneAzCli = false,
    bool TieneGhCli = false,
    bool TieneAccesoAdo = false,
    bool TieneRepoLocal = false,
    bool ClaudeMdConfigurado = false);

// Slide 2/8 — preflight para los 8 ejercicios de la práctica.
// `Node 18+` + `claude --version` + API key + repo local son
// bloqueantes; az/gh CLI y ADO son avisos (algunos ejercicios son
// opcionales). Lógica pura.
public static class PracticaPreflight
{
    public static ReportePreflight Comprobar(EscenarioPreflight e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var hallazgos = new List<HallazgoPreflight>
        {
            Check(e.TieneNode18OSuperior,
                "Node.js 18+ instalado",
                "Claude Code requiere Node 18+. Instala con `nvm install 18` o equivalente.",
                NivelPreflight.Bloqueante),

            Check(e.ClaudeInstaladoYAutenticado,
                "`claude --version` responde y está autenticado",
                "Instala con `npm install -g @anthropic-ai/claude-code` y autentica con " +
                "`claude auth login`.",
                NivelPreflight.Bloqueante),

            Check(e.TieneApiKey,
                "API key de Anthropic disponible",
                "Crea la API key en console.anthropic.com y expórtala como " +
                "`ANTHROPIC_API_KEY` o usa `claude auth login`.",
                NivelPreflight.Bloqueante),

            Check(e.TieneRepoLocal,
                "Repositorio local con código abierto",
                "Necesitas un proyecto .NET local para los ejercicios 1, 2 y 4.",
                NivelPreflight.Bloqueante),

            Check(e.ClaudeMdConfigurado,
                "`.claude/CLAUDE.md` con convenciones del proyecto",
                "Sin CLAUDE.md cada conversación arranca de cero (anti-pattern #3 del S9.5).",
                NivelPreflight.Aviso),

            Check(e.TieneAzCli,
                "Azure CLI (`az`) disponible",
                "Necesario para los ejercicios 2 (`az bicep build`) y 4 (validate).",
                NivelPreflight.Aviso),

            Check(e.TieneGhCli,
                "GitHub CLI (`gh`) disponible",
                "Útil para crear PRs si el MCP de ADO no aplica.",
                NivelPreflight.Aviso),

            Check(e.TieneAccesoAdo,
                "Acceso a Azure DevOps con PAT",
                "Necesario para el ejercicio 3 (MCP + ADO). Es opcional.",
                NivelPreflight.Aviso),
        };

        bool listo = !hallazgos.Any(h => h.Nivel == NivelPreflight.Bloqueante);
        return new ReportePreflight(listo, hallazgos);
    }

    private static HallazgoPreflight Check(bool ok, string nombre, string mensaje, NivelPreflight nivelFallo)
        => ok
            ? new HallazgoPreflight(NivelPreflight.Ok, nombre, "OK.")
            : new HallazgoPreflight(nivelFallo, nombre, mensaje);
}
