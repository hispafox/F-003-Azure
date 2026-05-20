using Practica.CcMcp.Demo.Api.Practica;

namespace Practica.CcMcp.Demo.Api.Tests;

// CAPA 1 — evaluador de ejercicios (slides 3-7, 11-13).
[Trait("Category", "Unit")]
public class Unit_EjercicioEvaluatorTests
{
    [Fact]
    public void Compila_Tests_Y_Convenciones_Es_Pasa()
    {
        var r = EjercicioEvaluator.Evaluar(new EvidenciaEjercicio(
            Ejercicio.GenerarServicioCompleto,
            CompilaOLintOk: true,
            TestsOValidatePasa: true,
            OutputAplicaConvenciones: true));
        Assert.Equal(ResultadoEjercicio.Pasa, r.Resultado);
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("completado", StringComparison.Ordinal));
    }

    [Fact]
    public void Ni_Compila_Ni_Tests_Es_Falla()
    {
        var r = EjercicioEvaluator.Evaluar(new EvidenciaEjercicio(
            Ejercicio.GenerarServicioCompleto,
            CompilaOLintOk: false,
            TestsOValidatePasa: false,
            OutputAplicaConvenciones: true));
        Assert.Equal(ResultadoEjercicio.Falla, r.Resultado);
    }

    [Fact]
    public void Compila_Pero_Tests_Fallan_Es_Pendiente()
    {
        var r = EjercicioEvaluator.Evaluar(new EvidenciaEjercicio(
            Ejercicio.GenerarBicep,
            CompilaOLintOk: true,
            TestsOValidatePasa: false,
            OutputAplicaConvenciones: true));
        Assert.Equal(ResultadoEjercicio.Pendiente, r.Resultado);
    }

    [Fact]
    public void Sin_Convenciones_Aporta_Sugerencia_Especifica()
    {
        var r = EjercicioEvaluator.Evaluar(new EvidenciaEjercicio(
            Ejercicio.GenerarServicioCompleto,
            CompilaOLintOk: true,
            TestsOValidatePasa: true,
            OutputAplicaConvenciones: false));
        Assert.Equal(ResultadoEjercicio.Pendiente, r.Resultado);
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("convenciones", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(Ejercicio.GenerarServicioCompleto, "3")]
    [InlineData(Ejercicio.GenerarBicep, "4")]
    [InlineData(Ejercicio.McpConAzureDevOps, "5")]
    [InlineData(Ejercicio.AnalisisDeError, "6")]
    [InlineData(Ejercicio.RefactoringConIa, "7")]
    [InlineData(Ejercicio.GenerarDocumentacion, "11")]
    [InlineData(Ejercicio.ComparativaPrompts, "12")]
    [InlineData(Ejercicio.McpServerCustom, "13")]
    public void Cada_Ejercicio_Mapea_A_Su_Slide(Ejercicio ej, string slide)
    {
        var r = EjercicioEvaluator.Evaluar(new EvidenciaEjercicio(
            ej, true, true, true));
        Assert.Equal(slide, r.Slide);
    }

    [Fact]
    public void Bicep_Validate_Falla_Sugiere_Pasar_Output_A_Claude()
    {
        var r = EjercicioEvaluator.Evaluar(new EvidenciaEjercicio(
            Ejercicio.GenerarBicep,
            CompilaOLintOk: true,
            TestsOValidatePasa: false,
            OutputAplicaConvenciones: true));
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("validate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Mcp_Server_Custom_Sugiere_Mcp_Inspector()
    {
        var r = EjercicioEvaluator.Evaluar(new EvidenciaEjercicio(
            Ejercicio.McpServerCustom,
            CompilaOLintOk: true,
            TestsOValidatePasa: false,
            OutputAplicaConvenciones: true));
        Assert.Contains(r.AccionesSugeridas, s =>
            s.Contains("mcp-inspector", StringComparison.OrdinalIgnoreCase));
    }
}
