using ClaudeCode.Limites.Demo.Api.Limites;

namespace ClaudeCode.Limites.Demo.Api.Tests;

// CAPA 1 — clasificador acelera vs frena (slide 5).
[Trait("Category", "Unit")]
public class Unit_AceleraOFrenaTests
{
    [Theory]
    [InlineData(TipoTareaIa.Boilerplate, ImpactoIa.Acelera)]
    [InlineData(TipoTareaIa.TransformacionDatos, ImpactoIa.Acelera)]
    [InlineData(TipoTareaIa.InfrastructureAsCode, ImpactoIa.Acelera)]
    [InlineData(TipoTareaIa.DocumentacionDesdeCodigo, ImpactoIa.Acelera)]
    [InlineData(TipoTareaIa.AnalisisErroresConLogs, ImpactoIa.Acelera)]
    [InlineData(TipoTareaIa.RefactoringMecanico, ImpactoIa.Acelera)]
    public void Tareas_Aceleradas_Por_Ia(TipoTareaIa t, ImpactoIa esperado)
    {
        var r = AceleraOFrenaClassifier.Clasificar(t);
        Assert.Equal(esperado, r.Impacto);
    }

    [Theory]
    [InlineData(TipoTareaIa.LogicaNegocioCompleja)]
    [InlineData(TipoTareaIa.DecisionArquitectura)]
    [InlineData(TipoTareaIa.OptimizacionFinaRendimiento)]
    [InlineData(TipoTareaIa.SeguridadCritica)]
    [InlineData(TipoTareaIa.DebuggingRaceConditions)]
    public void Tareas_Donde_Ia_Frena(TipoTareaIa t)
    {
        var r = AceleraOFrenaClassifier.Clasificar(t);
        Assert.Equal(ImpactoIa.Frena, r.Impacto);
    }

    [Fact]
    public void Otro_Es_Neutro_Con_Sugerencia_De_Evaluacion()
    {
        var r = AceleraOFrenaClassifier.Clasificar(TipoTareaIa.Otro);
        Assert.Equal(ImpactoIa.Neutro, r.Impacto);
        Assert.Contains(r.Razones, s =>
            s.Contains("caso por caso", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Cada_Clasificacion_Lleva_Slide_Y_Razones_No_Vacias()
    {
        foreach (var t in Enum.GetValues<TipoTareaIa>())
        {
            var r = AceleraOFrenaClassifier.Clasificar(t);
            Assert.False(string.IsNullOrWhiteSpace(r.Slide), $"{t} sin slide");
            Assert.NotEmpty(r.Razones);
        }
    }

    [Fact]
    public void Optimizacion_Rendimiento_Recuerda_Medir_Antes()
    {
        var r = AceleraOFrenaClassifier.Clasificar(
            TipoTareaIa.OptimizacionFinaRendimiento);
        Assert.Contains(r.Razones, s =>
            s.Contains("medir", StringComparison.OrdinalIgnoreCase));
    }
}
