using Devops.Repos.Demo.Api.Repos;

namespace Devops.Repos.Demo.Api.Tests;

// CAPA 1 — Conventional Commits (slide 7) + work items (slide 12).
[Trait("Category", "Unit")]
public class Unit_CommitParserTests
{
    [Fact]
    public void Commit_Feat_Con_Scope_Y_WorkItem()
    {
        var c = ConventionalCommitParser.Parsear(
            "feat(pedidos): buscar por fecha #1234");

        Assert.True(c.Valido);
        Assert.Equal("feat", c.Tipo);
        Assert.Equal("pedidos", c.Scope);
        Assert.False(c.BreakingChange);
        Assert.Equal("buscar por fecha #1234", c.Descripcion);
        Assert.Equal(new[] { 1234 }, c.WorkItems);
    }

    [Fact]
    public void Breaking_Change_Marcado_Con_Exclamacion()
    {
        var c = ConventionalCommitParser.Parsear(
            "feat!: nueva firma de PedidoController.Buscar");
        Assert.True(c.BreakingChange);
        Assert.True(c.Valido);
    }

    [Fact]
    public void Tipo_Invalido_Detectado()
    {
        var c = ConventionalCommitParser.Parsear("wip: jugando");
        Assert.False(c.Valido);
        Assert.Contains(c.Problemas, p => p.Contains("wip"));
    }

    [Fact]
    public void Formato_Sin_Tipo_Es_Invalido()
    {
        var c = ConventionalCommitParser.Parsear("implemented some stuff");
        Assert.False(c.Valido);
        Assert.Contains(c.Problemas, p => p.Contains("Formato"));
    }

    [Fact]
    public void Work_Items_Multiples_Y_Deduplicados()
    {
        var c = ConventionalCommitParser.Parsear(
            "fix: corregir null #100 #200\n\nCloses #100, related to #300.");
        Assert.Equal(new[] { 100, 200, 300 }, c.WorkItems);
    }

    [Theory]
    [InlineData("feat")]
    [InlineData("fix")]
    [InlineData("docs")]
    [InlineData("refactor")]
    [InlineData("test")]
    [InlineData("chore")]
    [InlineData("perf")]
    [InlineData("ci")]
    [InlineData("build")]
    [InlineData("style")]
    public void Tipos_Canonicos_Slide_7(string tipo)
    {
        var c = ConventionalCommitParser.Parsear($"{tipo}: descripción");
        Assert.True(c.Valido, $"Tipo '{tipo}' marcado como inválido");
    }

    [Fact]
    public void Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            ConventionalCommitParser.Parsear("   "));

    [Fact]
    public void Solo_Se_Analiza_La_Primera_Linea_Como_Header()
    {
        // El cuerpo no afecta a la validación del encabezado.
        var c = ConventionalCommitParser.Parsear(
            "fix: corregir cosa\n\nDetalle extenso del fix.\nWIP: nada que ver.");
        Assert.True(c.Valido);
        Assert.Equal("fix", c.Tipo);
    }
}
