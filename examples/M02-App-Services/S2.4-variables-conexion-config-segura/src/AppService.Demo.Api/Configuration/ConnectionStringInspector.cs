namespace AppService.Demo.Api.Configuration;

// Slide 7 — Helper para mostrar a qué servidor/base de datos apunta una
// connection string sin filtrar la password. Útil en /connection y en logs.
public static class ConnectionStringInspector
{
    private static readonly HashSet<string> SafeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "server",
        "data source",
        "database",
        "initial catalog",
        "encrypt",
        "trustservercertificate",
        "multipleactiveresultsets"
    };

    public static IDictionary<string, string> ExtractSafeFields(string? connectionString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(connectionString)) return result;

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;

            var key = part[..idx].Trim();
            var value = part[(idx + 1)..].Trim();

            if (SafeKeys.Contains(key))
            {
                result[key] = value;
            }
        }

        return result;
    }
}
