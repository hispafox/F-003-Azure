using Practica.PrimerComando.Demo.Api.PrimerComando;

namespace Practica.PrimerComando.Demo.Api.Tests;

// CAPA 1 — preflight ligero (slide 3).
[Trait("Category", "Unit")]
public class Unit_PreflightTests
{
    private static EscenarioPreflight TodoOk() => new(
        TieneNode18OSuperior: true,
        TieneCuentaAnthropic: true,
        Auth: MetodoAuth.ClaudeAi,
        TieneTerminalModerna: true,
        TieneGit: true,
        TieneRepoPracticar: true);

    [Fact]
    public void Todo_Ok_Esta_Listo()
    {
        var r = PrimerComandoPreflight.Comprobar(TodoOk());
        Assert.True(r.ListoParaArrancar);
        Assert.DoesNotContain(r.Hallazgos, h => h.Nivel == NivelPreflight.Bloqueante);
    }

    [Fact]
    public void Sin_Node_Es_Bloqueante()
    {
        var r = PrimerComandoPreflight.Comprobar(
            TodoOk() with { TieneNode18OSuperior = false });
        Assert.False(r.ListoParaArrancar);
        Assert.Contains(r.Hallazgos, h =>
            h.Nivel == NivelPreflight.Bloqueante
            && h.Comprobacion.Contains("Node", StringComparison.Ordinal));
    }

    [Fact]
    public void Sin_Auth_Es_Bloqueante()
    {
        var r = PrimerComandoPreflight.Comprobar(
            TodoOk() with { Auth = MetodoAuth.Ninguno });
        Assert.False(r.ListoParaArrancar);
    }

    [Fact]
    public void Sin_Cuenta_Anthropic_Es_Bloqueante()
    {
        var r = PrimerComandoPreflight.Comprobar(
            TodoOk() with { TieneCuentaAnthropic = false });
        Assert.False(r.ListoParaArrancar);
    }

    [Fact]
    public void Sin_Repo_Para_Practicar_Es_Bloqueante()
    {
        var r = PrimerComandoPreflight.Comprobar(
            TodoOk() with { TieneRepoPracticar = false });
        Assert.False(r.ListoParaArrancar);
    }

    [Fact]
    public void Sin_Terminal_Moderna_Es_Aviso()
    {
        var r = PrimerComandoPreflight.Comprobar(
            TodoOk() with { TieneTerminalModerna = false });
        Assert.True(r.ListoParaArrancar);
        Assert.Contains(r.Hallazgos, h =>
            h.Nivel == NivelPreflight.Aviso
            && h.Comprobacion.Contains("Terminal", StringComparison.Ordinal));
    }

    [Fact]
    public void Auth_Api_Key_Sirve_Igual_Que_Claude_Ai()
    {
        var r = PrimerComandoPreflight.Comprobar(
            TodoOk() with { Auth = MetodoAuth.ApiKey });
        Assert.True(r.ListoParaArrancar);
    }
}
