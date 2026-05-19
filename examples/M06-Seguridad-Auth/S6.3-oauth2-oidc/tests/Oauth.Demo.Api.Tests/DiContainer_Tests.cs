using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Oauth.Demo.Api.Oauth;

namespace Oauth.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Sin CAPA de integración (OAuth2 con
// IdP real no es emulable de forma fiable), este test es el único que
// ejercita el grafo DI. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void LoginPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<ILoginPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<ILoginPlanner>());

        // SPA → Authorization Code + PKCE → con authorize URL + verifier.
        var spa = planner.Planificar(
            TipoCliente.Spa, "t", "c", "http://localhost/cb", ["openid"]);
        Assert.Equal(nameof(OAuthFlow.AuthorizationCodePkce), spa.Flujo);
        Assert.NotNull(spa.AuthorizeUrl);
        Assert.NotNull(spa.CodeVerifier);

        // Daemon → Client Credentials → sin authorize URL ni verifier.
        var daemon = planner.Planificar(
            TipoCliente.DaemonOServicio, "t", "c", "", ["api://x/.default"]);
        Assert.Equal(nameof(OAuthFlow.ClientCredentials), daemon.Flujo);
        Assert.Null(daemon.AuthorizeUrl);
        Assert.False(daemon.TieneUsuario);
    }
}
