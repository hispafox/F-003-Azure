namespace Datos.Demo.Api.Datos;

public sealed record VeredictoCors(
    bool Segura, IReadOnlyList<string> Problemas);

// Slide 13 — CORS mal configurado es una vulnerabilidad real. La regla
// de oro: NUNCA AllowAnyOrigin() junto con AllowCredentials(). Lógica
// pura que audita una política CORS.
public static class CorsPolicyValidator
{
    public static VeredictoCors Validar(
        IReadOnlyList<string> origenes, bool allowCredentials)
    {
        ArgumentNullException.ThrowIfNull(origenes);
        var problemas = new List<string>();

        var tieneWildcard = origenes.Any(o => o?.Trim() == "*");

        // Slide 13 — combinación prohibida: wildcard + credenciales.
        if (tieneWildcard && allowCredentials)
            problemas.Add("AllowAnyOrigin + AllowCredentials: cualquier sitio "
                + "puede hacer peticiones autenticadas (slide 13).");

        if (tieneWildcard)
            problemas.Add("Origen '*': usa orígenes explícitos en producción.");

        if (origenes.Count == 0)
            problemas.Add("Sin orígenes definidos.");

        // Buen indicio: orígenes https explícitos.
        foreach (var o in origenes.Where(o => !string.IsNullOrWhiteSpace(o) && o != "*"))
        {
            if (o.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !o.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                problemas.Add($"Origen no-TLS en producción: {o}");
        }

        return new VeredictoCors(problemas.Count == 0, problemas);
    }
}
