using Entra.Demo.Api.Entra;

namespace Entra.Demo.Api.Tests;

// CAPA 1 — decodificación de claims del JWT (slide 18). NO valida firma.
[Trait("Category", "Unit")]
public class Unit_JwtInspectorTests
{
    private const string Payload =
        """
        {"aud":"client-id","iss":"https://login.microsoftonline.com/t/v2.0",
         "sub":"user-123","name":"Pedro Garcia","preferred_username":"pedro@empresa.com",
         "email":"pedro@empresa.com","roles":["Admin","Developer"],
         "groups":["grp-dev"],"exp":4102444800}
        """;

    [Fact]
    public void Inspeccionar_Extrae_Los_Claims_Slide18()
    {
        var c = JwtInspector.Inspeccionar(Jwt.Crear(Payload));

        Assert.Equal("user-123", c.Sub);
        Assert.Equal("Pedro Garcia", c.Name);
        Assert.Equal("pedro@empresa.com", c.PreferredUsername);
        Assert.Equal(["Admin", "Developer"], c.Roles);
        Assert.Equal(["grp-dev"], c.Groups);
        Assert.Equal("client-id", c.Aud);
        Assert.False(c.Expirado);                       // exp = año 2100
    }

    [Fact]
    public void Inspeccionar_Detecta_Token_Expirado()
    {
        // exp = 2020-01-01; "ahora" inyectado = 2026.
        var jwt = Jwt.Crear("""{"sub":"x","exp":1577836800}""");
        var c = JwtInspector.Inspeccionar(
            jwt, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Assert.True(c.Expirado);
        Assert.NotNull(c.Exp);
    }

    [Fact]
    public void Inspeccionar_Roles_Como_String_Simple()
    {
        var c = JwtInspector.Inspeccionar(Jwt.Crear("""{"roles":"Admin"}"""));
        Assert.Equal(["Admin"], c.Roles);
    }

    [Fact]
    public void Inspeccionar_Sin_Claims_Opcionales_No_Rompe()
    {
        var c = JwtInspector.Inspeccionar(Jwt.Crear("""{"sub":"x"}"""));
        Assert.Equal("x", c.Sub);
        Assert.Empty(c.Roles);
        Assert.Null(c.Exp);
        Assert.False(c.Expirado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("solo-una-parte")]
    public void Inspeccionar_Invalido_Lanza(string jwt)
        => Assert.ThrowsAny<Exception>(() => JwtInspector.Inspeccionar(jwt));
}
