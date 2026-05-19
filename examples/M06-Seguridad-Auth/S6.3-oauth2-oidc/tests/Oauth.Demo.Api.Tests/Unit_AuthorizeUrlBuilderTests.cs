using Oauth.Demo.Api.Oauth;

namespace Oauth.Demo.Api.Tests;

// CAPA 1 — construcción de la URL /authorize (slide 6).
[Trait("Category", "Unit")]
public class Unit_AuthorizeUrlBuilderTests
{
    private static AuthorizeRequest Req(params string[] scopes) => new(
        "tenant-1", "client-1", "http://localhost:5173/cb",
        scopes, State: "st", Nonce: "nc", CodeChallenge: "chal");

    [Fact]
    public void Construir_Incluye_Los_Parametros_PKCE()
    {
        var url = AuthorizeUrlBuilder.Construir(Req("openid", "profile"));

        Assert.StartsWith(
            "https://login.microsoftonline.com/tenant-1/oauth2/v2.0/authorize?", url);
        Assert.Contains("response_type=code", url);
        Assert.Contains("code_challenge=chal", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("client_id=client-1", url);
        Assert.Contains("state=st", url);
        Assert.Contains("nonce=nc", url);
    }

    [Fact]
    public void Construir_Fuerza_Scope_Openid()
    {
        var url = AuthorizeUrlBuilder.Construir(Req("profile"));
        // scope = "openid profile" → URL-encoded el espacio.
        Assert.Contains("scope=openid%20profile", url);
    }

    [Fact]
    public void Construir_Encodea_Redirect_Uri()
    {
        var url = AuthorizeUrlBuilder.Construir(Req("openid"));
        Assert.Contains("redirect_uri=http%3A%2F%2Flocalhost%3A5173%2Fcb", url);
    }

    [Fact]
    public void Construir_Sin_Scopes_Lanza()
        => Assert.Throws<ArgumentException>(
            () => AuthorizeUrlBuilder.Construir(Req()));

    [Fact]
    public void Construir_Sin_ClientId_Lanza()
        => Assert.Throws<ArgumentException>(() => AuthorizeUrlBuilder.Construir(
            new AuthorizeRequest("t", "", "http://x", ["openid"], "s", "n", "c")));
}
