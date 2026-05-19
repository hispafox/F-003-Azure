namespace Oauth.Demo.Api.Oauth;

public sealed record AuthorizeRequest(
    string TenantId,
    string ClientId,
    string RedirectUri,
    IReadOnlyList<string> Scopes,
    string State,
    string Nonce,
    string CodeChallenge);

// Slide 6 — construye la URL de /authorize de Entra ID para el flujo
// Authorization Code + PKCE. Pura: encodea bien los parámetros.
public static class AuthorizeUrlBuilder
{
    public static string Construir(AuthorizeRequest r)
    {
        ArgumentNullException.ThrowIfNull(r);
        ArgumentException.ThrowIfNullOrWhiteSpace(r.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(r.ClientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(r.RedirectUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(r.CodeChallenge);
        if (r.Scopes is null || r.Scopes.Count == 0)
            throw new ArgumentException("scope no puede estar vacío", nameof(r));

        // openid es obligatorio para OIDC (id_token) — slide 3/6.
        var scopes = new List<string>();
        if (!r.Scopes.Contains("openid")) scopes.Add("openid");
        scopes.AddRange(r.Scopes);

        var qs = new[]
        {
            ("client_id", r.ClientId),
            ("response_type", "code"),
            ("redirect_uri", r.RedirectUri),
            ("response_mode", "query"),
            ("scope", string.Join(' ', scopes)),
            ("state", r.State),
            ("nonce", r.Nonce),
            ("code_challenge", r.CodeChallenge),
            ("code_challenge_method", PkceGenerator.Method),
        };

        var query = string.Join('&', qs.Select(p =>
            $"{Uri.EscapeDataString(p.Item1)}={Uri.EscapeDataString(p.Item2)}"));

        return $"https://login.microsoftonline.com/{Uri.EscapeDataString(r.TenantId)}" +
               $"/oauth2/v2.0/authorize?{query}";
    }
}
