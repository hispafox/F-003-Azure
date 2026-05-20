using Plataforma.Demo.Api.Plataforma;

namespace Plataforma.Demo.Api.Tests;

// CAPA 1 — equivalencias YAML ADO ↔ GitHub Actions (slide 6).
[Trait("Category", "Unit")]
public class Unit_EquivalenciaTests
{
    [Fact]
    public void Hay_Al_Menos_15_Equivalencias_Slide_6()
        => Assert.True(SyntaxEquivalenceMapper.Todas.Count >= 15);

    [Theory]
    [InlineData("Pool / runner", "ubuntu-latest")]
    [InlineData("Setup .NET", "actions/setup-dotnet")]
    [InlineData("Secreto", "secrets.")]
    [InlineData("Job depende de otro", "needs:")]
    public void Conceptos_Clave_Tienen_Sintaxis_GitHub(
        string concepto, string fragmento)
    {
        var e = SyntaxEquivalenceMapper.Buscar(concepto);
        Assert.NotNull(e);
        Assert.Contains(fragmento, e!.GitHubYaml);
    }

    [Fact]
    public void Buscar_Por_Contencion_Funciona()
    {
        // "trigger" debe encontrar "Trigger en main".
        var e = SyntaxEquivalenceMapper.Buscar("trigger");
        Assert.NotNull(e);
        Assert.Contains("trigger", e!.AdoYaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Concepto_Inexistente_Devuelve_Null()
        => Assert.Null(SyntaxEquivalenceMapper.Buscar("ConceptoQueNoExiste"));

    [Fact]
    public void Variable_Inline_Cambia_De_Sintaxis_Slide_6()
    {
        var e = SyntaxEquivalenceMapper.Buscar("Variable inline");
        Assert.NotNull(e);
        Assert.Contains("$(", e!.AdoYaml);
        Assert.Contains("${{", e.GitHubYaml);
    }
}
