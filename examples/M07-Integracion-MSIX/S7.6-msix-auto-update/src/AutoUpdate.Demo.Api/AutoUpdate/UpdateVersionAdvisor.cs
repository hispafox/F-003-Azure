namespace AutoUpdate.Demo.Api.AutoUpdate;

public sealed record DecisionActualizar(
    bool DebeActualizar, string Comparacion, string Razon);

// Slide 8 opción 1 — la "etiqueta de rollback" es `versionMala` con
// build+1; el código que se publica es el de la versión previa buena.
public sealed record PlanRollback(
    string VersionPreviaBuena, string EtiquetaRollback);

// Slides 7, 8, 13 — comparación de versiones, obligatoriedad y plan
// de rollback. Lógica pura.
public static class UpdateVersionAdvisor
{
    // Slide 7 — la nueva DEBE ser mayor; ForceUpdateFromAnyVersion
    // permite "downgrade" (saltar a una versión inferior, p.ej. rollback).
    public static DecisionActualizar Comparar(
        string actual, string disponible, bool forceFromAnyVersion = false)
    {
        var a = ParseVersion(actual, nameof(actual));
        var d = ParseVersion(disponible, nameof(disponible));

        int cmp = d.CompareTo(a);
        string comp = cmp == 0 ? "igual" : cmp > 0 ? "mayor" : "menor";

        return cmp switch
        {
            > 0 => new(true, comp, "Versión disponible es mayor (slide 7)."),
            0 => new(false, comp, "Misma versión: no actualizar (slide 7)."),
            _ => forceFromAnyVersion
                ? new(true, comp, "ForceUpdateFromAnyVersion permite el downgrade (slide 7/8).")
                : new(false, comp, "Versión disponible es menor: bloqueada sin ForceUpdateFromAnyVersion."),
        };
    }

    // Slide 13 — si la versión instalada está por debajo del mínimo
    // soportado, la actualización debe ser obligatoria
    // (UpdateBlocksActivation=true).
    public static bool EsObligatoria(string actual, string minimoSoportado)
    {
        var a = ParseVersion(actual, nameof(actual));
        var m = ParseVersion(minimoSoportado, nameof(minimoSoportado));
        return a < m;
    }

    // Slide 8 opción 1 — rollback más limpio: re-publicar la versión
    // previa buena con la etiqueta de versión actual+1 en el build.
    public static PlanRollback? PlanificarRollback(
        string versionMala, IReadOnlyList<string> historial)
    {
        ArgumentNullException.ThrowIfNull(historial);
        var mala = ParseVersion(versionMala, nameof(versionMala));

        // El historial puede venir desordenado; ordenamos ascendente.
        var ordenado = historial
            .Select(v => (raw: v, parsed: Version.TryParse(v, out var p) ? p : null))
            .Where(x => x.parsed is not null)
            .OrderBy(x => x.parsed)
            .ToList();

        int idx = ordenado.FindIndex(x => x.parsed!.Equals(mala));
        if (idx <= 0) return null;                  // no hay previa

        var previa = ordenado[idx - 1].raw;
        return new PlanRollback(
            VersionPreviaBuena: previa,
            EtiquetaRollback: IncrementarBuild(versionMala));
    }

    private static string IncrementarBuild(string v)
    {
        var partes = v.Split('.');
        if (partes.Length != 4 || !int.TryParse(partes[2], out var build))
            throw new FormatException($"Version '{v}' no es Major.Minor.Build.Revision.");
        return $"{partes[0]}.{partes[1]}.{build + 1}.0";
    }

    private static Version ParseVersion(string v, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(v, paramName);
        // Slide 3/7 — exigimos los 4 componentes (Version.TryParse
        // acepta "2.4", que no es válido en este dominio).
        if (v.Split('.').Length != 4 || !Version.TryParse(v, out var parsed))
            throw new ArgumentException(
                $"Version '{v}' no es Major.Minor.Build.Revision.", paramName);
        return parsed;
    }
}
