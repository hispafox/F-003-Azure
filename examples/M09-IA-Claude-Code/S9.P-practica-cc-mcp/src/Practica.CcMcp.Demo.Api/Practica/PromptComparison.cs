namespace Practica.CcMcp.Demo.Api.Practica;

public enum NivelDetalle { Vago, Medio, Detallado }

public sealed record PromptPuntuado(
    NivelDetalle Nivel, int Puntuacion, IReadOnlyList<string> IngredientesDetectados);

public sealed record ComparativaPrompts(
    PromptPuntuado Vago,
    PromptPuntuado Medio,
    PromptPuntuado Detallado,
    int DeltaVagoADetallado,
    IReadOnlyList<string> Lecciones);

// Slide 12 — comparador de los 3 niveles de detalle de prompt. Lógica
// pura. Reutiliza la idea del scoring del S9.2 con los 4 ingredientes
// canónicos (contexto, constraints, formato salida, criterio éxito);
// el delta vago→detallado demuestra el valor pedagógico del slide 12.
public static class PromptComparison
{
    // Marcadores ligeros por ingrediente; cada uno vale 25 puntos.
    private static readonly (string Ingrediente, string[] Marcadores)[] Ingredientes =
    [
        ("Contexto", ["proyecto", "stack", ".net", "cosmos", "framework", "azure"]),
        ("Constraints", ["no añadir", "no romper", "mantén", "preserva", "respeta",
            "no inventes"]),
        ("Formato salida", ["output:", "salida:", "devuelve", "formato:", "json",
            "markdown", "guarda en", "archivos"]),
        ("Criterio éxito", ["criterio éxito", "criterio de éxito", "tests verdes",
            "compila", "sin warnings", "criterio:"]),
    ];

    public static ComparativaPrompts Comparar(string vago, string medio, string detallado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vago);
        ArgumentException.ThrowIfNullOrWhiteSpace(medio);
        ArgumentException.ThrowIfNullOrWhiteSpace(detallado);

        var pV = Puntuar(vago, NivelDetalle.Vago);
        var pM = Puntuar(medio, NivelDetalle.Medio);
        var pD = Puntuar(detallado, NivelDetalle.Detallado);

        var lecciones = new List<string>
        {
            $"Prompt vago obtiene {pV.Puntuacion}/100; detallado {pD.Puntuacion}/100 " +
                "(slide 12).",
        };

        if (pD.Puntuacion > pV.Puntuacion + 40)
            lecciones.Add("El nivel de detalle reduce iteraciones de 5-6 a 1-2 " +
                "(slide 12 de S9.5).");
        else
            lecciones.Add("La diferencia entre vago y detallado es menor de lo esperado: " +
                "revisa que el prompt detallado incluya los 4 ingredientes.");

        if (!pD.IngredientesDetectados.Contains("Criterio éxito"))
            lecciones.Add("Incluso el prompt detallado se olvidó del criterio éxito. " +
                "Añade `tests verdes` o un umbral medible.");

        return new ComparativaPrompts(pV, pM, pD, pD.Puntuacion - pV.Puntuacion, lecciones);
    }

    private static PromptPuntuado Puntuar(string prompt, NivelDetalle nivel)
    {
        var lower = prompt.ToLowerInvariant();
        var detectados = new List<string>();
        int puntos = 0;

        foreach (var (ingrediente, marcadores) in Ingredientes)
        {
            if (marcadores.Any(m => lower.Contains(m, StringComparison.Ordinal)))
            {
                detectados.Add(ingrediente);
                puntos += 25;
            }
        }

        // Prompts < 40 chars son siempre vagos (cap a 25).
        if (prompt.Trim().Length < 40) puntos = Math.Min(puntos, 25);

        return new PromptPuntuado(nivel, puntos, detectados);
    }
}
