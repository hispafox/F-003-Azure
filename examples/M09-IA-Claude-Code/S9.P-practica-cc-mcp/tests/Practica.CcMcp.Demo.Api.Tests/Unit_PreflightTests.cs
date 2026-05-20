using Practica.CcMcp.Demo.Api.Practica;

namespace Practica.CcMcp.Demo.Api.Tests;

// CAPA 1 — preflight de la práctica (slide 2/8).
[Trait("Category", "Unit")]
public class Unit_PreflightTests
{
    private static EscenarioPreflight TodoOk() => new(
        TieneNode18OSuperior: true,
        ClaudeInstaladoYAutenticado: true,
        TieneApiKey: true,
        TieneAzCli: true,
        TieneGhCli: true,
        TieneAccesoAdo: true,
        TieneRepoLocal: true,
        ClaudeMdConfigurado: true);

    [Fact]
    public void Todo_Ok_Esta_Listo_Sin_Bloqueantes()
    {
        var r = PracticaPreflight.Comprobar(TodoOk());
        Assert.True(r.ListoParaArrancar);
        Assert.DoesNotContain(r.Hallazgos, h => h.Nivel == NivelPreflight.Bloqueante);
    }

    [Fact]
    public void Sin_Node18_Es_Bloqueante()
    {
        var e = TodoOk() with { TieneNode18OSuperior = false };
        var r = PracticaPreflight.Comprobar(e);
        Assert.False(r.ListoParaArrancar);
        Assert.Contains(r.Hallazgos, h =>
            h.Comprobacion.Contains("Node", StringComparison.Ordinal)
            && h.Nivel == NivelPreflight.Bloqueante);
    }

    [Fact]
    public void Sin_Claude_Autenticado_Es_Bloqueante()
    {
        var e = TodoOk() with { ClaudeInstaladoYAutenticado = false };
        var r = PracticaPreflight.Comprobar(e);
        Assert.False(r.ListoParaArrancar);
    }

    [Fact]
    public void Sin_Api_Key_Es_Bloqueante()
    {
        var e = TodoOk() with { TieneApiKey = false };
        var r = PracticaPreflight.Comprobar(e);
        Assert.False(r.ListoParaArrancar);
    }

    [Fact]
    public void Sin_Repo_Local_Es_Bloqueante()
    {
        var e = TodoOk() with { TieneRepoLocal = false };
        var r = PracticaPreflight.Comprobar(e);
        Assert.False(r.ListoParaArrancar);
    }

    [Fact]
    public void Sin_Az_O_Gh_Cli_Es_Aviso_No_Bloqueante()
    {
        var e = TodoOk() with { TieneAzCli = false, TieneGhCli = false };
        var r = PracticaPreflight.Comprobar(e);
        Assert.True(r.ListoParaArrancar);
        Assert.Contains(r.Hallazgos, h =>
            h.Nivel == NivelPreflight.Aviso
            && h.Comprobacion.Contains("Azure CLI", StringComparison.Ordinal));
    }

    [Fact]
    public void Sin_Claude_Md_Es_Aviso()
    {
        var e = TodoOk() with { ClaudeMdConfigurado = false };
        var r = PracticaPreflight.Comprobar(e);
        Assert.Contains(r.Hallazgos, h =>
            h.Nivel == NivelPreflight.Aviso
            && h.Comprobacion.Contains("CLAUDE.md", StringComparison.Ordinal));
    }
}
