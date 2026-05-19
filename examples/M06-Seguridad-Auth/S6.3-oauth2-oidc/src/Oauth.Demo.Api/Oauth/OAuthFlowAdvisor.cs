namespace Oauth.Demo.Api.Oauth;

public enum TipoCliente
{
    WebAppServidor, Spa, Movil, DaemonOServicio, Cli, ApiLlamaApi,
}

public enum OAuthFlow
{
    AuthorizationCodePkce, AuthorizationCode,
    ClientCredentials, DeviceCode, OnBehalfOf,
}

// Slide 5 — qué flujo OAuth2 usar según el tipo de cliente. Tabla de
// decisión pura (como los *Advisor previos).
public static class OAuthFlowAdvisor
{
    public static OAuthFlow Recomendar(TipoCliente cliente) => cliente switch
    {
        TipoCliente.Spa or TipoCliente.Movil => OAuthFlow.AuthorizationCodePkce,
        TipoCliente.WebAppServidor => OAuthFlow.AuthorizationCode,
        TipoCliente.DaemonOServicio => OAuthFlow.ClientCredentials,
        TipoCliente.Cli => OAuthFlow.DeviceCode,
        TipoCliente.ApiLlamaApi => OAuthFlow.OnBehalfOf,
        _ => throw new ArgumentOutOfRangeException(nameof(cliente)),
    };

    // Slide 5 — ¿el flujo implica un usuario interactivo?
    public static bool TieneUsuario(OAuthFlow f) => f != OAuthFlow.ClientCredentials;

    // Slide 5 — ¿el cliente necesita guardar un secreto? PKCE y Device
    // Code no (clientes públicos).
    public static bool NecesitaSecreto(OAuthFlow f) => f is
        OAuthFlow.AuthorizationCode or
        OAuthFlow.ClientCredentials or
        OAuthFlow.OnBehalfOf;

    // Slide 5 — flujos DEPRECADOS: nunca usar en apps nuevas.
    public static bool EstaDeprecado(string flujo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flujo);
        var f = flujo.Trim();
        return f.Equals("Implicit", StringComparison.OrdinalIgnoreCase)
            || f.Equals("ROPC", StringComparison.OrdinalIgnoreCase)
            || f.Equals("Resource Owner Password Credentials",
                StringComparison.OrdinalIgnoreCase);
    }
}
