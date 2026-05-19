namespace Msix.Demo.Api.Msix;

// Slide 4 — el archivo final tiene la forma {Name}_{Version}_{Arch}.msix
// (y .msixbundle si combina arquitecturas, slide 10). Lógica pura.
public static class PackageNamingResolver
{
    public static string NombreArchivo(AppxManifest m)
    {
        ArgumentNullException.ThrowIfNull(m);
        ArgumentException.ThrowIfNullOrWhiteSpace(m.IdentityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(m.Version);
        return $"{m.IdentityName}_{m.Version}_{m.ProcessorArchitecture}.msix";
    }

    public static string NombreBundle(string identityName, string version) =>
        $"{Validar(identityName)}_{Validar(version)}.msixbundle";

    // Slide 11 — sustituye la versión del manifest por una versión de
    // build: 2.4.{buildId}.0 (mantiene Major.Minor y resetea Revision).
    public static string SiguienteVersion(string actual, int buildId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actual);
        ArgumentOutOfRangeException.ThrowIfNegative(buildId);

        var partes = actual.Split('.');
        if (partes.Length != 4 || !partes.All(p => int.TryParse(p, out _)))
            throw new FormatException($"Version '{actual}' no es Major.Minor.Build.Revision.");

        return $"{partes[0]}.{partes[1]}.{buildId}.0";
    }

    // Slide 3 — "siempre incremental": la nueva version debe ser mayor.
    public static bool EsIncremental(string anterior, string nueva)
    {
        if (!Version.TryParse(anterior, out var a)) return false;
        if (!Version.TryParse(nueva, out var n)) return false;
        return n > a;
    }

    private static string Validar(string s)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(s);
        return s;
    }
}
