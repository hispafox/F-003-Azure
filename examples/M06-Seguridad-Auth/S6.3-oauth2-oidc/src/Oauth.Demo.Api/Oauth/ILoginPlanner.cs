namespace Oauth.Demo.Api.Oauth;

public sealed record PlanLogin(
    string TipoCliente,
    string Flujo,
    bool TieneUsuario,
    bool NecesitaSecreto,
    string? AuthorizeUrl,        // solo en flujos basados en /authorize
    string? CodeVerifier,        // a guardar para el intercambio /token
    string Nota);

// Compone los tres elementos puros (advisor + PKCE + authorize URL) en
// un "plan de login" listo para usar. Servicio inyectable (seam para el
// test de contenedor).
public interface ILoginPlanner
{
    PlanLogin Planificar(
        TipoCliente cliente, string tenantId, string clientId,
        string redirectUri, IReadOnlyList<string> scopes);
}

public sealed class LoginPlanner : ILoginPlanner
{
    public PlanLogin Planificar(
        TipoCliente cliente, string tenantId, string clientId,
        string redirectUri, IReadOnlyList<string> scopes)
    {
        var flujo = OAuthFlowAdvisor.Recomendar(cliente);
        var tieneUsuario = OAuthFlowAdvisor.TieneUsuario(flujo);
        var necesitaSecreto = OAuthFlowAdvisor.NecesitaSecreto(flujo);

        // Solo los flujos interactivos basados en código construyen
        // /authorize + PKCE (slide 6). Client Credentials / OBO no.
        if (flujo is OAuthFlow.AuthorizationCodePkce or OAuthFlow.AuthorizationCode)
        {
            var pkce = PkceGenerator.Generar();
            var url = AuthorizeUrlBuilder.Construir(new AuthorizeRequest(
                tenantId, clientId, redirectUri, scopes,
                State: Guid.NewGuid().ToString("N"),
                Nonce: Guid.NewGuid().ToString("N"),
                CodeChallenge: pkce.CodeChallenge));

            return new PlanLogin(
                cliente.ToString(), flujo.ToString(), tieneUsuario, necesitaSecreto,
                url, pkce.CodeVerifier,
                "Redirige al usuario a AuthorizeUrl; guarda CodeVerifier para /token.");
        }

        return new PlanLogin(
            cliente.ToString(), flujo.ToString(), tieneUsuario, necesitaSecreto,
            AuthorizeUrl: null, CodeVerifier: null,
            flujo == OAuthFlow.ClientCredentials
                ? "Sin usuario: la app pide token con client_id + client_secret."
                : flujo == OAuthFlow.DeviceCode
                    ? "Muestra el user_code y haz polling de /token."
                    : "On-Behalf-Of: usa el token entrante del usuario como assertion.");
    }
}
