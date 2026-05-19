using Datos.Demo.Api.Datos;

namespace Datos.Demo.Api.Tests;

// CAPA 1 — auditoría de política CORS (slide 13).
[Trait("Category", "Unit")]
public class Unit_CorsPolicyValidatorTests
{
    [Fact]
    public void Wildcard_Con_Credenciales_Es_Vulnerable()
    {
        var v = CorsPolicyValidator.Validar(["*"], allowCredentials: true);
        Assert.False(v.Segura);
        Assert.Contains(v.Problemas, p => p.Contains("AllowCredentials"));
    }

    [Fact]
    public void Origen_Explicito_Https_Es_Seguro()
    {
        var v = CorsPolicyValidator.Validar(
            ["https://app-ventas.azurewebsites.net"], allowCredentials: true);
        Assert.True(v.Segura);
        Assert.Empty(v.Problemas);
    }

    [Fact]
    public void Localhost_Http_No_Penaliza()
    {
        var v = CorsPolicyValidator.Validar(
            ["http://localhost:3000"], allowCredentials: false);
        Assert.True(v.Segura);
    }

    [Fact]
    public void Http_No_Localhost_En_Prod_Es_Problema()
    {
        var v = CorsPolicyValidator.Validar(
            ["http://app-insegura.com"], allowCredentials: false);
        Assert.False(v.Segura);
        Assert.Contains(v.Problemas, p => p.Contains("no-TLS"));
    }

    [Fact]
    public void Sin_Origenes_Es_Problema()
        => Assert.False(CorsPolicyValidator.Validar([], false).Segura);

    [Fact]
    public void Origenes_Null_Lanza()
        => Assert.Throws<ArgumentNullException>(
            () => CorsPolicyValidator.Validar(null!, false));
}
