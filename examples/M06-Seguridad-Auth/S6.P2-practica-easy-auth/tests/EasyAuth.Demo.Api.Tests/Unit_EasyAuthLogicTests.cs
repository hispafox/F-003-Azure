using EasyAuth.Demo.Api.EasyAuth;

namespace EasyAuth.Demo.Api.Tests;

// CAPA 1 — lógica pura de Easy Auth (slides 2-6).
[Trait("Category", "Unit")]
public class Unit_EasyAuthLogicTests
{
    [Theory]
    [InlineData(TipoApp.SitioWeb, "RedirectToLoginPage", 302)]
    [InlineData(TipoApp.Api, "Return401", 401)]
    public void Config_Por_Tipo(TipoApp t, string accion, int codigo)
    {
        Assert.Equal(accion, EasyAuthConfigAdvisor.AccionNoAutenticado(t));
        Assert.Equal(codigo, EasyAuthConfigAdvisor.CodigoHttp(t));
    }

    [Fact]
    public void Soporta_Los_Proveedores_De_La_Slide3()
    {
        Assert.True(EasyAuthConfigAdvisor.SoportaProveedor(Proveedor.MicrosoftEntra));
        Assert.True(EasyAuthConfigAdvisor.SoportaProveedor(Proveedor.GitHub));
        Assert.Equal(6, EasyAuthConfigAdvisor.Proveedores.Count);
    }

    [Fact]
    public void Login_Con_Redirect_Encodea()
        => Assert.Equal(
            "/.auth/login/aad?post_login_redirect_url=%2Fpanel",
            AuthEndpoints.Login("aad", "/panel"));

    [Fact]
    public void Logout_Y_Callback()
    {
        Assert.Equal("/.auth/logout", AuthEndpoints.Logout());
        Assert.Equal("/.auth/login/aad/callback", AuthEndpoints.LoginCallback());
    }

    [Theory]
    [InlineData("/.auth/me", true)]
    [InlineData("/.auth/login/aad", true)]
    [InlineData("/api/datos", false)]
    public void EsRutaEasyAuth(string path, bool esperado)
        => Assert.Equal(esperado, AuthEndpoints.EsRutaEasyAuth(path));

    [Fact]
    public void Headers_Autenticado()
    {
        var p = EasyAuthHeaders.Desde(new Dictionary<string, string?>
        {
            [EasyAuthHeaders.Nombre] = "pedro@empresa.com",
            [EasyAuthHeaders.Id] = "id-1",
            [EasyAuthHeaders.Idp] = "aad",
        });
        Assert.True(p.Autenticado);
        Assert.Equal("pedro@empresa.com", p.Nombre);
        Assert.Equal("id-1", p.Id);
    }

    [Fact]
    public void Headers_Sin_Nombre_No_Autenticado()
        => Assert.False(EasyAuthHeaders.Desde(
            new Dictionary<string, string?>()).Autenticado);

    [Fact]
    public void Login_Proveedor_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() => AuthEndpoints.Login("  "));
}
