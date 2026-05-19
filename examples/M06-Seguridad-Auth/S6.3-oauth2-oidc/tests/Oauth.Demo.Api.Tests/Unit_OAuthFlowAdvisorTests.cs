using Oauth.Demo.Api.Oauth;

namespace Oauth.Demo.Api.Tests;

// CAPA 1 — qué flujo OAuth2 por tipo de cliente (slide 5).
[Trait("Category", "Unit")]
public class Unit_OAuthFlowAdvisorTests
{
    [Theory]
    [InlineData(TipoCliente.Spa, OAuthFlow.AuthorizationCodePkce)]
    [InlineData(TipoCliente.Movil, OAuthFlow.AuthorizationCodePkce)]
    [InlineData(TipoCliente.WebAppServidor, OAuthFlow.AuthorizationCode)]
    [InlineData(TipoCliente.DaemonOServicio, OAuthFlow.ClientCredentials)]
    [InlineData(TipoCliente.Cli, OAuthFlow.DeviceCode)]
    [InlineData(TipoCliente.ApiLlamaApi, OAuthFlow.OnBehalfOf)]
    public void Recomendar(TipoCliente c, OAuthFlow esperado)
        => Assert.Equal(esperado, OAuthFlowAdvisor.Recomendar(c));

    [Fact]
    public void ClientCredentials_No_Tiene_Usuario()
        => Assert.False(OAuthFlowAdvisor.TieneUsuario(OAuthFlow.ClientCredentials));

    [Theory]
    [InlineData(OAuthFlow.AuthorizationCodePkce, false)]   // cliente público
    [InlineData(OAuthFlow.DeviceCode, false)]
    [InlineData(OAuthFlow.AuthorizationCode, true)]
    [InlineData(OAuthFlow.ClientCredentials, true)]
    [InlineData(OAuthFlow.OnBehalfOf, true)]
    public void NecesitaSecreto(OAuthFlow f, bool esperado)
        => Assert.Equal(esperado, OAuthFlowAdvisor.NecesitaSecreto(f));

    [Theory]
    [InlineData("Implicit", true)]
    [InlineData("ROPC", true)]
    [InlineData("Resource Owner Password Credentials", true)]
    [InlineData("AuthorizationCodePkce", false)]
    public void EstaDeprecado(string flujo, bool esperado)
        => Assert.Equal(esperado, OAuthFlowAdvisor.EstaDeprecado(flujo));
}
