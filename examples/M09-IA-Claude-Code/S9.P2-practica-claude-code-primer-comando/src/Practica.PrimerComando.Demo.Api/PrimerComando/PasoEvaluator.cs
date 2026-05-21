namespace Practica.PrimerComando.Demo.Api.PrimerComando;

public enum Paso
{
    InstalarCli,             // slide 4
    LoginYPrimeraSesion,     // slide 5
    PedirAlgoMasConcreto,    // slide 6
    EjecutarComandos,        // slide 7
    EntenderPermissionModes, // slide 8
    SlashCommands,           // slide 9
    CrearClaudeMd,           // slide 10
    PedirUnTest,             // slide 11
}

public enum ResultadoPaso { Pasa, Falla, Pendiente }

public sealed record EvidenciaPaso(
    Paso Paso,
    bool ComandoEjecutado,
    bool OutputEsperadoVisible,
    string? Comentario = null);

public sealed record InformePaso(
    Paso Paso,
    string Slide,
    ResultadoPaso Resultado,
    IReadOnlyList<string> AccionesSugeridas);

// Slides 4-11 — evaluador de los 8 pasos de la práctica simplificada.
// Cada paso se mide con dos flags: el alumno ejecutó el comando y
// vio el output esperado. Si ambos true → Pasa. Si ninguno → Falla.
// Si solo uno → Pendiente con sugerencia específica.
public static class PasoEvaluator
{
    public static InformePaso Evaluar(EvidenciaPaso e)
    {
        ArgumentNullException.ThrowIfNull(e);

        string slide = SlideDe(e.Paso);
        var acciones = new List<string>();

        if (!e.ComandoEjecutado)
            acciones.Add(SugerenciaComando(e.Paso));
        if (!e.OutputEsperadoVisible)
            acciones.Add(SugerenciaOutput(e.Paso));

        ResultadoPaso resultado;
        if (acciones.Count == 0)
        {
            resultado = ResultadoPaso.Pasa;
            acciones.Add($"Paso {e.Paso} completado (slide {slide}).");
        }
        else if (!e.ComandoEjecutado && !e.OutputEsperadoVisible)
        {
            resultado = ResultadoPaso.Falla;
        }
        else
        {
            resultado = ResultadoPaso.Pendiente;
        }

        return new InformePaso(e.Paso, slide, resultado, acciones);
    }

    public static string SlideDe(Paso p) => p switch
    {
        Paso.InstalarCli => "4",
        Paso.LoginYPrimeraSesion => "5",
        Paso.PedirAlgoMasConcreto => "6",
        Paso.EjecutarComandos => "7",
        Paso.EntenderPermissionModes => "8",
        Paso.SlashCommands => "9",
        Paso.CrearClaudeMd => "10",
        Paso.PedirUnTest => "11",
        _ => "0",
    };

    private static string SugerenciaComando(Paso p) => p switch
    {
        Paso.InstalarCli =>
            "Ejecuta `npm install -g @anthropic-ai/claude-code` y luego `claude --version` " +
            "(slide 4).",
        Paso.LoginYPrimeraSesion =>
            "Arranca `claude` en un proyecto y haz `Login with claude.ai` (slide 5).",
        Paso.PedirAlgoMasConcreto =>
            "Pídele algo específico: `> Mira Program.cs y explícame qué hace` (slide 6).",
        Paso.EjecutarComandos =>
            "Pídele `> Compila el proyecto con dotnet build` y aprueba la ejecución (slide 7).",
        Paso.EntenderPermissionModes =>
            "Ejecuta `/permissions` dentro de Claude y prueba `acceptEdits` o `plan` (slide 8).",
        Paso.SlashCommands =>
            "Prueba `/help`, `/cost`, `/model` para ver los slash commands esenciales (slide 9).",
        Paso.CrearClaudeMd =>
            "Ejecuta `/init` para generar `CLAUDE.md` automáticamente (slide 10).",
        Paso.PedirUnTest =>
            "Pídele a Claude: `> Crea un proyecto xUnit con un test trivial y ejecútalo` (slide 11).",
        _ => "Ejecuta el comando indicado en el slide correspondiente.",
    };

    private static string SugerenciaOutput(Paso p) => p switch
    {
        Paso.InstalarCli =>
            "`claude --version` debe devolver `1.x.x`. Si falla con permisos, usa npm con `--prefix` o " +
            "PowerShell como admin (slide 4).",
        Paso.LoginYPrimeraSesion =>
            "Debes ver el banner `Welcome to Claude Code!` con el `Working directory` correcto (slide 5).",
        Paso.PedirAlgoMasConcreto =>
            "Claude debe leer el archivo y devolver una explicación específica, no genérica. " +
            "Si suena genérica, da más contexto en el prompt (slide 12).",
        Paso.EjecutarComandos =>
            "Claude debe pedir tu confirmación con `[y/N]` antes de ejecutar el comando shell (slide 7).",
        Paso.EntenderPermissionModes =>
            "Tras `/permissions` debes ver el selector de modos. `acceptEdits` deja de pedirte permiso " +
            "para edits pero sigue pidiéndolo para shell (slide 8).",
        Paso.SlashCommands =>
            "`/cost` debe mostrar los tokens usados; `/model` el modelo activo (slide 9).",
        Paso.CrearClaudeMd =>
            "`CLAUDE.md` queda en la raíz del proyecto con secciones Overview / Tech Stack / Key Files / " +
            "Common Tasks / Conventions (slide 10).",
        Paso.PedirUnTest =>
            "Debes ver `Test passed (1/1)` y el archivo `BasicTest.cs` en disco (slide 11).",
        _ => "Verifica el output esperado del slide correspondiente.",
    };
}
