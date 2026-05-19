using Oauth.Demo.Api.Oauth;

namespace Oauth.Demo.Api.Endpoints;

public sealed record PlanDto(
    TipoCliente Cliente, string TenantId, string ClientId,
    string RedirectUri, List<string> Scopes);

public static class OauthEndpoints
{
    public static void MapOauth(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        var oauth = app.MapGroup("/oauth");

        // Slide 5 — qué flujo OAuth2 toca según el tipo de cliente.
        oauth.MapGet("/flujo", (TipoCliente cliente) =>
        {
            var f = OAuthFlowAdvisor.Recomendar(cliente);
            return Results.Ok(new
            {
                cliente = cliente.ToString(),
                flujo = f.ToString(),
                tieneUsuario = OAuthFlowAdvisor.TieneUsuario(f),
                necesitaSecreto = OAuthFlowAdvisor.NecesitaSecreto(f),
            });
        });

        // Slide 5 — ¿es un flujo deprecado? (Implicit / ROPC)
        oauth.MapGet("/deprecado/{flujo}", (string flujo) =>
            Results.Ok(new { flujo, deprecado = OAuthFlowAdvisor.EstaDeprecado(flujo) }));

        // Slide 6 — generar un par PKCE (verifier + challenge S256).
        oauth.MapGet("/pkce", () => Results.Ok(PkceGenerator.Generar()));

        // Slides 5-6 — plan de login completo (flujo + authorize URL + PKCE).
        oauth.MapPost("/plan", (PlanDto dto, ILoginPlanner planner) =>
            Results.Ok(planner.Planificar(
                dto.Cliente, dto.TenantId, dto.ClientId,
                dto.RedirectUri, dto.Scopes)));
    }
}
