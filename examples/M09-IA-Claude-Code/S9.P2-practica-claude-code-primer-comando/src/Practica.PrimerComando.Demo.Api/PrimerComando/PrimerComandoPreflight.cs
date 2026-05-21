namespace Practica.PrimerComando.Demo.Api.PrimerComando;

public enum NivelPreflight { Ok, Aviso, Bloqueante }

public sealed record HallazgoPreflight(NivelPreflight Nivel, string Comprobacion, string Mensaje);

public sealed record ReportePreflight(
    bool ListoParaArrancar,
    IReadOnlyList<HallazgoPreflight> Hallazgos);

public enum MetodoAuth { ClaudeAi, ApiKey, Ninguno }

public sealed record EscenarioPreflight(
    bool TieneNode18OSuperior = false,
    bool TieneCuentaAnthropic = false,
    MetodoAuth Auth = MetodoAuth.Ninguno,
    bool TieneTerminalModerna = true,
    bool TieneGit = true,
    bool TieneRepoPracticar = false);

// Slide 3 — preflight para la práctica simplificada. Más ligero que
// el de S9.P: aquí no necesitas az/gh CLI ni ADO, basta con tener
// Node, cuenta Anthropic, un repo donde practicar y un método de
// auth válido. Lógica pura.
public static class PrimerComandoPreflight
{
    public static ReportePreflight Comprobar(EscenarioPreflight e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var hallazgos = new List<HallazgoPreflight>
        {
            Check(e.TieneNode18OSuperior,
                "Node.js 18+ instalado",
                "Claude Code se distribuye por npm y requiere Node 18+ (slide 3). " +
                "Instala con `nvm install 18` o desde https://nodejs.org.",
                NivelPreflight.Bloqueante),

            Check(e.TieneCuentaAnthropic,
                "Cuenta Anthropic disponible",
                "Necesitas una cuenta en claude.ai (Free/Pro/Max/Team) o una API key. " +
                "Crea la cuenta en https://claude.ai/signup.",
                NivelPreflight.Bloqueante),

            Check(e.Auth != MetodoAuth.Ninguno,
                "Método de autenticación configurado",
                "Elige `claude.ai login` (recomendado) o `ANTHROPIC_API_KEY` " +
                "(necesario en CI/CD). Sin un método válido, `claude` no arranca.",
                NivelPreflight.Bloqueante),

            Check(e.TieneRepoPracticar,
                "Repositorio donde practicar",
                "Clona un sample (`dotnet/samples`) o reutiliza un proyecto previo " +
                "(M02-S2.P2, M03-S3.P2). Sin proyecto no hay nada que explorar (slide 3).",
                NivelPreflight.Bloqueante),

            Check(e.TieneTerminalModerna,
                "Terminal moderna disponible",
                "macOS Terminal / iTerm2 / Linux shell / Windows Terminal con PowerShell o WSL2. " +
                "La terminal `cmd.exe` clásica de Windows da problemas.",
                NivelPreflight.Aviso),

            Check(e.TieneGit,
                "Git instalado",
                "Probablemente ya lo tienes. Útil para versionar lo que Claude edite (slide 3).",
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
