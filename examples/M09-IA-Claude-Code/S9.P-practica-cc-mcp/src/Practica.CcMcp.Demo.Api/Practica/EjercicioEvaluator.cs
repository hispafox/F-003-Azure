namespace Practica.CcMcp.Demo.Api.Practica;

public enum Ejercicio
{
    GenerarServicioCompleto,      // slide 3
    GenerarBicep,                  // slide 4
    McpConAzureDevOps,             // slide 5
    AnalisisDeError,               // slide 6
    RefactoringConIa,              // slide 7
    GenerarDocumentacion,          // slide 11
    ComparativaPrompts,            // slide 12
    McpServerCustom,               // slide 13
}

public enum ResultadoEjercicio { Pasa, Falla, Pendiente }

public sealed record EvidenciaEjercicio(
    Ejercicio Ejercicio,
    bool CompilaOLintOk,
    bool TestsOValidatePasa,
    bool OutputAplicaConvenciones,
    string? Comentario = null);

public sealed record InformeEjercicio(
    Ejercicio Ejercicio,
    string Slide,
    ResultadoEjercicio Resultado,
    IReadOnlyList<string> AccionesSugeridas);

// Slides 3-7, 11-13 — evaluador del resultado de cada ejercicio.
// Lógica pura: recibe evidencias booleanas que el alumno marca y
// devuelve el veredicto + acciones concretas para arreglar lo que
// falle.
public static class EjercicioEvaluator
{
    public static InformeEjercicio Evaluar(EvidenciaEjercicio e)
    {
        ArgumentNullException.ThrowIfNull(e);

        string slide = SlideDe(e.Ejercicio);
        var acciones = new List<string>();

        if (!e.CompilaOLintOk)
            acciones.Add(SugerenciaCompila(e.Ejercicio));
        if (!e.TestsOValidatePasa)
            acciones.Add(SugerenciaTests(e.Ejercicio));
        if (!e.OutputAplicaConvenciones)
            acciones.Add("El output no respeta las convenciones del proyecto. " +
                "Revisa `.claude/CLAUDE.md` y pídele a Claude que itere (slide 13 #3 de S9.5).");

        ResultadoEjercicio resultado;
        if (acciones.Count == 0)
            resultado = ResultadoEjercicio.Pasa;
        else if (!e.CompilaOLintOk && !e.TestsOValidatePasa)
            resultado = ResultadoEjercicio.Falla;
        else
            resultado = ResultadoEjercicio.Pendiente;

        if (resultado == ResultadoEjercicio.Pasa)
            acciones.Add($"Ejercicio {e.Ejercicio} completado (slide {slide}).");

        return new InformeEjercicio(e.Ejercicio, slide, resultado, acciones);
    }

    public static string SlideDe(Ejercicio ej) => ej switch
    {
        Ejercicio.GenerarServicioCompleto => "3",
        Ejercicio.GenerarBicep => "4",
        Ejercicio.McpConAzureDevOps => "5",
        Ejercicio.AnalisisDeError => "6",
        Ejercicio.RefactoringConIa => "7",
        Ejercicio.GenerarDocumentacion => "11",
        Ejercicio.ComparativaPrompts => "12",
        Ejercicio.McpServerCustom => "13",
        _ => "0",
    };

    private static string SugerenciaCompila(Ejercicio ej) => ej switch
    {
        Ejercicio.GenerarServicioCompleto =>
            "El servicio no compila. Pídele a Claude el stack trace exacto y que lo corrija " +
            "antes de seguir (slide 3 + anti-pattern #5 del S9.5).",
        Ejercicio.GenerarBicep =>
            "`az bicep build` falla. Pega el error a Claude y pídele el fix con " +
            "`--no-restore` si aplica (slide 4).",
        Ejercicio.GenerarDocumentacion =>
            "El README generado contiene comandos que no compilan. " +
            "Pídele a Claude que lea el código real, no que invente.",
        _ => "Algo no compila / lint falla. Devuelve el error a Claude para que itere.",
    };

    private static string SugerenciaTests(Ejercicio ej) => ej switch
    {
        Ejercicio.GenerarServicioCompleto =>
            "Los tests no pasan. Pídele a Claude que genere primero los tests y luego " +
            "ajuste el servicio (TDD-style, slide 9).",
        Ejercicio.GenerarBicep =>
            "`az deployment group validate` falla. Pásale a Claude el output y pide el fix.",
        Ejercicio.AnalisisDeError =>
            "El fix sugerido no es correcto. Pídele que ejecute el caso concreto del " +
            "stack trace y verifique.",
        Ejercicio.McpServerCustom =>
            "El MCP server no arranca o los tools no validan. Usa `mcp-inspector` para " +
            "ver el schema y arreglar el error (slide 13).",
        _ => "La verificación falla. Aporta evidencia (logs, output) a Claude y vuelve a iterar.",
    };
}
