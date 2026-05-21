using Practica.PrimerComando.Demo.Api.PrimerComando;

namespace Practica.PrimerComando.Demo.Api.Tests;

// CAPA 1 — evaluador de los 8 pasos (slides 4-11).
[Trait("Category", "Unit")]
public class Unit_PasoEvaluatorTests
{
    [Fact]
    public void Comando_Ejecutado_Y_Output_Visible_Es_Pasa()
    {
        var r = PasoEvaluator.Evaluar(new EvidenciaPaso(
            Paso.InstalarCli, ComandoEjecutado: true, OutputEsperadoVisible: true));
        Assert.Equal(ResultadoPaso.Pasa, r.Resultado);
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("completado", StringComparison.Ordinal));
    }

    [Fact]
    public void Ni_Comando_Ni_Output_Es_Falla()
    {
        var r = PasoEvaluator.Evaluar(new EvidenciaPaso(
            Paso.InstalarCli, ComandoEjecutado: false, OutputEsperadoVisible: false));
        Assert.Equal(ResultadoPaso.Falla, r.Resultado);
        Assert.True(r.AccionesSugeridas.Count >= 2);
    }

    [Fact]
    public void Comando_Ejecutado_Pero_Output_No_Esperado_Es_Pendiente()
    {
        var r = PasoEvaluator.Evaluar(new EvidenciaPaso(
            Paso.CrearClaudeMd, ComandoEjecutado: true, OutputEsperadoVisible: false));
        Assert.Equal(ResultadoPaso.Pendiente, r.Resultado);
    }

    [Theory]
    [InlineData(Paso.InstalarCli, "4")]
    [InlineData(Paso.LoginYPrimeraSesion, "5")]
    [InlineData(Paso.PedirAlgoMasConcreto, "6")]
    [InlineData(Paso.EjecutarComandos, "7")]
    [InlineData(Paso.EntenderPermissionModes, "8")]
    [InlineData(Paso.SlashCommands, "9")]
    [InlineData(Paso.CrearClaudeMd, "10")]
    [InlineData(Paso.PedirUnTest, "11")]
    public void Cada_Paso_Mapea_A_Su_Slide(Paso p, string slide)
    {
        var r = PasoEvaluator.Evaluar(new EvidenciaPaso(p, true, true));
        Assert.Equal(slide, r.Slide);
    }

    [Fact]
    public void Instalar_Cli_Sugiere_Npm_Install()
    {
        var r = PasoEvaluator.Evaluar(new EvidenciaPaso(
            Paso.InstalarCli, ComandoEjecutado: false, OutputEsperadoVisible: false));
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("npm install", StringComparison.Ordinal));
    }

    [Fact]
    public void Crear_Claude_Md_Sugiere_Init()
    {
        var r = PasoEvaluator.Evaluar(new EvidenciaPaso(
            Paso.CrearClaudeMd, ComandoEjecutado: false, OutputEsperadoVisible: false));
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("/init", StringComparison.Ordinal));
    }

    [Fact]
    public void Pedir_Test_Sugiere_Test_Trivial()
    {
        var r = PasoEvaluator.Evaluar(new EvidenciaPaso(
            Paso.PedirUnTest, ComandoEjecutado: false, OutputEsperadoVisible: false));
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("xUnit", StringComparison.Ordinal));
    }
}
