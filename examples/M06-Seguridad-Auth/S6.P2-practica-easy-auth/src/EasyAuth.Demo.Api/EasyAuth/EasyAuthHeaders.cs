namespace EasyAuth.Demo.Api.EasyAuth;

public sealed record PrincipalEasyAuth(
    bool Autenticado, string? Nombre, string? Id, string? IdentityProvider);

// Slide 4 — Easy Auth inyecta cabeceras X-MS-CLIENT-PRINCIPAL-* en cada
// request YA autenticada (validó el token antes que tu código). Función
// pura que las interpreta — reutiliza la idea del EasyAuthPrincipal de
// S6.P, aquí con el header -ID añadido (slide 4).
public static class EasyAuthHeaders
{
    public const string Nombre = "X-MS-CLIENT-PRINCIPAL-NAME";
    public const string Id = "X-MS-CLIENT-PRINCIPAL-ID";
    public const string Idp = "X-MS-CLIENT-PRINCIPAL-IDP";

    public static PrincipalEasyAuth Desde(IReadOnlyDictionary<string, string?> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        headers.TryGetValue(Nombre, out var nombre);
        headers.TryGetValue(Id, out var id);
        headers.TryGetValue(Idp, out var idp);

        var autenticado = !string.IsNullOrWhiteSpace(nombre);
        return new PrincipalEasyAuth(
            autenticado,
            autenticado ? nombre : null,
            autenticado ? id : null,
            autenticado ? (string.IsNullOrWhiteSpace(idp) ? "aad" : idp) : null);
    }
}
