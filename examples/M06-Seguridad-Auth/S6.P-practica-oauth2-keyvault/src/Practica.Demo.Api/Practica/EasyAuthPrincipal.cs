namespace Practica.Demo.Api.Practica;

public sealed record Principal(bool Autenticado, string? Nombre, string? IdentityProvider);

// Slide 9 — Easy Auth inyecta cabeceras X-MS-CLIENT-PRINCIPAL-* en cada
// request ya autenticada. Esta función pura las interpreta (sin token
// crudo: Easy Auth ya validó la firma en el front).
public static class EasyAuthPrincipal
{
    public const string HeaderNombre = "X-MS-CLIENT-PRINCIPAL-NAME";
    public const string HeaderIdp = "X-MS-CLIENT-PRINCIPAL-IDP";

    public static Principal Desde(IReadOnlyDictionary<string, string?> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        headers.TryGetValue(HeaderNombre, out var nombre);
        headers.TryGetValue(HeaderIdp, out var idp);

        var autenticado = !string.IsNullOrWhiteSpace(nombre);
        return new Principal(
            autenticado,
            autenticado ? nombre : null,
            autenticado ? (string.IsNullOrWhiteSpace(idp) ? "aad" : idp) : null);
    }
}
