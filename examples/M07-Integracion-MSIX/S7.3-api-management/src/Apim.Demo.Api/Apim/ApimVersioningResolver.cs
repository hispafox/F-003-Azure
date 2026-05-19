namespace Apim.Demo.Api.Apim;

// Slide 7 — esquemas de versionado de APIs en APIM.
public enum EsquemaVersionado { Segment, Query, Header }

public sealed record VersionResuelta(string Version, string RutaGateway);

// Slide 7 — resuelve qué versión pide el cliente según el esquema del
// version set y construye la ruta del gateway. Lógica pura.
public static class ApimVersioningResolver
{
    // Recomendación de la slide 7: Segment es el más claro.
    public static EsquemaVersionado Recomendado => EsquemaVersionado.Segment;

    // - Segment: la versión es el primer segmento del path (/v1/productos).
    // - Query:   ?api-version=v1.
    // - Header:  Api-Version: v1.
    // `entrada` es el path (Segment), el valor del query param (Query) o
    // el valor del header (Header).
    public static VersionResuelta Resolver(
        EsquemaVersionado esquema, string apiPath, string entrada,
        IReadOnlySet<string> versionesValidas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(entrada);
        ArgumentNullException.ThrowIfNull(versionesValidas);

        string version = esquema switch
        {
            EsquemaVersionado.Segment =>
                entrada.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries)
                    is [var v, ..] ? v
                    : throw new FormatException("Path sin segmento de versión."),
            EsquemaVersionado.Query => entrada.Trim(),
            EsquemaVersionado.Header => entrada.Trim(),
            _ => throw new ArgumentOutOfRangeException(nameof(esquema)),
        };

        if (!versionesValidas.Contains(version))
            throw new ArgumentException(
                $"Versión '{version}' no existe en el version set.", nameof(entrada));

        string ruta = esquema switch
        {
            EsquemaVersionado.Segment => $"/{version}/{apiPath}",
            EsquemaVersionado.Query => $"/{apiPath}?api-version={version}",
            EsquemaVersionado.Header => $"/{apiPath} (Api-Version: {version})",
            _ => throw new ArgumentOutOfRangeException(nameof(esquema)),
        };

        return new VersionResuelta(version, ruta);
    }
}
