namespace Tables.Demo.Api.Tables;

// Slides 5, 14 — PartitionKey + RowKey son la clave única total. Table
// Storage prohíbe ciertos caracteres en las claves y tiene patrones
// clásicos (RowKey con timestamp invertido). Lógica pura y testeable.
public static class TableKeys
{
    // Caracteres prohibidos en PartitionKey/RowKey (doc oficial).
    private static readonly char[] Prohibidos = ['/', '\\', '#', '?', '\t', '\n', '\r'];

    private static bool EsCaracterIlegal(char c)
    {
        if (Array.IndexOf(Prohibidos, c) >= 0) return true;
        int u = c;
        // Control chars C0 (0x00-0x1F) y DEL + C1 (0x7F-0x9F).
        return u <= 0x1F || (u >= 0x7F && u <= 0x9F);
    }

    public static bool EsValida(string? key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        foreach (var c in key)
            if (EsCaracterIlegal(c)) return false;
        return true;
    }

    // Sustituye los caracteres no permitidos por '-' (slide 5).
    public static string Sanitizar(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);
        return new string([.. raw.Select(c => EsCaracterIlegal(c) ? '-' : c)]);
    }

    // Slide 14 (patrón 3) — RowKey con timestamp invertido: los más
    // recientes ordenan primero (Table devuelve por orden de RowKey).
    public static string RowKeyTimestampInvertido(DateTimeOffset cuando)
        => (DateTimeOffset.MaxValue.Ticks - cuando.UtcTicks).ToString("D19");
}
