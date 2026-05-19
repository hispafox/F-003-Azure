using Practica.Demo.Api.Practica;

namespace Practica.Demo.Api.Tests;

// CAPA 1 — lógica pura de la práctica (slides 7, 8, 9, 11).
[Trait("Category", "Unit")]
public class Unit_PracticaLogicTests
{
    [Theory]
    [InlineData(TipoApp.Api, "Return401")]
    [InlineData(TipoApp.WebApp, "LoginWithAzureActiveDirectory")]
    public void EasyAuth_Accion(TipoApp t, string esperado)
        => Assert.Equal(esperado, EasyAuthAdvisor.AccionNoAutenticado(t));

    [Fact]
    public void EasyAuth_Issuer_V2()
        => Assert.Equal("https://login.microsoftonline.com/t-1/v2.0",
            EasyAuthAdvisor.Issuer("t-1"));

    [Fact]
    public void AppSettings_Secretos_Son_Referencias_KV()
    {
        var s = KeyVaultRefAppSettings.Construir("t-1", "c-1", "kv-curso");
        Assert.Equal("c-1", s["AzureAd__ClientId"]);                 // público
        Assert.StartsWith("@Microsoft.KeyVault(", s["AzureAd__ClientSecret"]);
        Assert.StartsWith("@Microsoft.KeyVault(", s["ExternalApiKey"]);
        Assert.True(KeyVaultRefAppSettings.SoloReferencias(s));
    }

    [Fact]
    public void SoloReferencias_Detecta_Secreto_En_Claro()
    {
        var malo = new Dictionary<string, string>
        {
            ["AzureAd__ClientSecret"] = "valor-en-claro-MAL",
        };
        Assert.False(KeyVaultRefAppSettings.SoloReferencias(malo));
    }

    [Fact]
    public void Principal_Autenticado_Con_Cabeceras()
    {
        var p = EasyAuthPrincipal.Desde(new Dictionary<string, string?>
        {
            [EasyAuthPrincipal.HeaderNombre] = "pedro@empresa.com",
            [EasyAuthPrincipal.HeaderIdp] = "aad",
        });
        Assert.True(p.Autenticado);
        Assert.Equal("pedro@empresa.com", p.Nombre);
        Assert.Equal("aad", p.IdentityProvider);
    }

    [Fact]
    public void Principal_No_Autenticado_Sin_Cabeceras()
    {
        var p = EasyAuthPrincipal.Desde(new Dictionary<string, string?>());
        Assert.False(p.Autenticado);
        Assert.Null(p.Nombre);
    }

    [Fact]
    public void EasyAuth_Issuer_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() => EasyAuthAdvisor.Issuer("  "));
}
