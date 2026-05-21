namespace Bonus.IntroIaAgentica.Demo.Api.Intro;

public enum GeneracionIa
{
    Gen1Autocompletado,    // 2020-2023
    Gen2Chat,              // 2023-2025
    Gen3Agente,            // 2025-2026+
    Desconocida,
}

public sealed record ClasificacionHerramienta(
    GeneracionIa Generacion,
    string Anios,
    string Contexto,
    string Accion);

// Slide 3 — clasifica una herramienta o uso de IA por generación.
// Lógica pura: busca palabras clave en la descripción y devuelve la
// generación canónica con su contexto/acción típica.
public static class GeneracionIaClassifier
{
    private static readonly (string[] Patrones, GeneracionIa Generacion)[] Reglas =
    [
        // Gen 3: agentes. Más específicos primero para que ganen.
        (["claude code", "claude-code", "cursor", "aider", "cowork",
            "ejecuta comandos", "edita 15 archivos", "agente",
            "multi-paso", "lee el repo"],
            GeneracionIa.Gen3Agente),

        // Gen 2: chat conversacional.
        (["chatgpt", "claude.ai", "copilot chat", "chat conversacional",
            "copy paste", "copia y pega", "generar bloque",
            "conversación con la ia"],
            GeneracionIa.Gen2Chat),

        // Gen 1: autocompletado.
        (["autocompletado", "github copilot", "intellisense",
            "sugerencia inline", "copilot inline", "línea a línea",
            "autocomplete", "tab completion"],
            GeneracionIa.Gen1Autocompletado),
    ];

    public static ClasificacionHerramienta Clasificar(string descripcion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);
        var lower = descripcion.ToLowerInvariant();

        foreach (var (patrones, gen) in Reglas)
        {
            if (patrones.Any(p => lower.Contains(p, StringComparison.Ordinal)))
                return Detalles(gen);
        }

        return Detalles(GeneracionIa.Desconocida);
    }

    private static ClasificacionHerramienta Detalles(GeneracionIa g) => g switch
    {
        GeneracionIa.Gen1Autocompletado => new(g, "2020-2023",
            "Archivo actual.",
            "Completar código mientras escribes (slide 3)."),

        GeneracionIa.Gen2Chat => new(g, "2023-2025",
            "Lo que copies y pegues en el prompt.",
            "Generar bloques de código bajo demanda (slide 3)."),

        GeneracionIa.Gen3Agente => new(g, "2025-2026+",
            "Proyecto completo + herramientas reales.",
            "Ejecuta tareas multipaso (leer, escribir, ejecutar, verificar) (slide 3)."),

        _ => new(g, "—",
            "Contexto desconocido.",
            "Descripción no encaja con ninguna generación; usa palabras clave del slide 3."),
    };
}
