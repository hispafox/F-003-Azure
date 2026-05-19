using Entra.Demo.Api.Entra;

namespace Entra.Demo.Api.Tests;

// CAPA 1 — App Roles: autorización por el claim `roles` (slide 19).
[Trait("Category", "Unit")]
public class Unit_AppRolesAuthorizerTests
{
    private readonly IAppRolesAuthorizer _auth = new AppRolesAuthorizer();

    [Fact]
    public void Autoriza_Cuando_El_Token_Tiene_El_Rol()
    {
        var d = _auth.Autorizar(["Admin", "Developer"], "Admin");
        Assert.True(d.Autorizado);
    }

    [Fact]
    public void Case_Insensitive()
        => Assert.True(_auth.Autorizar(["admin"], "Admin").Autorizado);

    [Fact]
    public void Deniega_Y_Explica_Cuando_Falta_El_Rol()
    {
        var d = _auth.Autorizar(["Viewer"], "Admin");
        Assert.False(d.Autorizado);
        Assert.Contains("403", d.Motivo);
    }

    [Theory]
    [InlineData(new[] { "Viewer", "Admin" }, true)]
    [InlineData(new[] { "Viewer" }, false)]
    public void AutorizaAlguno(string[] roles, bool esperado)
        => Assert.Equal(esperado, _auth.AutorizaAlguno(roles, "Admin", "Owner"));

    [Fact]
    public void Roles_Null_Lanza()
        => Assert.Throws<ArgumentNullException>(() => _auth.Autorizar(null!, "Admin"));
}
