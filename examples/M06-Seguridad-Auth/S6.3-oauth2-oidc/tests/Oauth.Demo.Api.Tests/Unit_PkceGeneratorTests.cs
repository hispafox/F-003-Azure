using Oauth.Demo.Api.Oauth;

namespace Oauth.Demo.Api.Tests;

// CAPA 1 — PKCE S256 (slide 6). Verificado con el vector del RFC 7636 §4.
[Trait("Category", "Unit")]
public class Unit_PkceGeneratorTests
{
    [Fact]
    public void Challenge_Coincide_Con_El_Vector_RFC7636()
    {
        // RFC 7636, Appendix B.
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        const string esperado = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";
        Assert.Equal(esperado, PkceGenerator.Challenge(verifier));
    }

    [Fact]
    public void Generar_Produce_Par_Valido()
    {
        var p = PkceGenerator.Generar();
        Assert.Equal("S256", p.Method);
        Assert.InRange(p.CodeVerifier.Length, 43, 128);
        // base64url: sin +, /, = ni padding.
        Assert.DoesNotContain('+', p.CodeChallenge);
        Assert.DoesNotContain('/', p.CodeChallenge);
        Assert.DoesNotContain('=', p.CodeChallenge);
        Assert.Equal(p.CodeChallenge, PkceGenerator.Challenge(p.CodeVerifier));
    }

    [Fact]
    public void Verifiers_Son_Aleatorios()
        => Assert.NotEqual(
            PkceGenerator.GenerarVerifier(), PkceGenerator.GenerarVerifier());

    [Fact]
    public void Challenge_Verifier_Corto_Lanza()
        => Assert.Throws<ArgumentException>(() => PkceGenerator.Challenge("corto"));

    [Fact]
    public void GenerarVerifier_Bytes_Fuera_De_Rango_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => PkceGenerator.GenerarVerifier(8));
}
