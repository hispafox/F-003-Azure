using Practica.Pipeline.Demo.Api.Pipeline;

namespace Practica.Pipeline.Demo.Api.Tests;

// CAPA 1 — evaluador del smoke test post-deploy (slide 5/6/10).
[Trait("Category", "Unit")]
public class Unit_SmokeTests
{
    [Fact]
    public void Http_200_Latencia_Baja_Sin_Errores_Continua()
    {
        var r = SmokeTestEvaluator.Evaluar(
            new MedidasSmoke(HttpCode: 200, LatenciaMediaSegundos: 0.3,
                ErrorRatePorcentaje: 0.1));
        Assert.Equal(DecisionSmoke.Continuar, r.Decision);
    }

    [Fact]
    public void Http_503_Dispara_Rollback()
    {
        var r = SmokeTestEvaluator.Evaluar(
            new MedidasSmoke(HttpCode: 503, LatenciaMediaSegundos: 0.3,
                ErrorRatePorcentaje: 0.1));
        Assert.Equal(DecisionSmoke.RollbackNecesario, r.Decision);
        Assert.Contains(r.Razones, m => m.Contains("503", StringComparison.Ordinal));
    }

    [Fact]
    public void Latencia_Por_Encima_Del_Umbral_Dispara_Rollback()
    {
        var r = SmokeTestEvaluator.Evaluar(
            new MedidasSmoke(HttpCode: 200, LatenciaMediaSegundos: 5.0,
                ErrorRatePorcentaje: 0.1));
        Assert.Equal(DecisionSmoke.RollbackNecesario, r.Decision);
        Assert.Contains(r.Razones, m =>
            m.Contains("Latencia", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Error_Rate_Alto_Dispara_Rollback()
    {
        var r = SmokeTestEvaluator.Evaluar(
            new MedidasSmoke(HttpCode: 200, LatenciaMediaSegundos: 0.3,
                ErrorRatePorcentaje: 12.4));
        Assert.Equal(DecisionSmoke.RollbackNecesario, r.Decision);
        Assert.Contains(r.Razones, m =>
            m.Contains("Error rate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Umbrales_Custom_Permiten_Latencia_Mas_Alta()
    {
        var r = SmokeTestEvaluator.Evaluar(
            new MedidasSmoke(HttpCode: 200, LatenciaMediaSegundos: 4.0,
                ErrorRatePorcentaje: 0.0),
            new UmbralesSmoke(LatenciaMaxSegundos: 10.0));
        Assert.Equal(DecisionSmoke.Continuar, r.Decision);
    }

    [Fact]
    public void Resultado_Continuar_Incluye_Detalles_Con_Medidas()
    {
        var r = SmokeTestEvaluator.Evaluar(
            new MedidasSmoke(HttpCode: 200, LatenciaMediaSegundos: 0.5,
                ErrorRatePorcentaje: 0.0));
        Assert.Equal(3, r.Detalles.Count);
        Assert.Contains(r.Detalles, d => d.StartsWith("HTTP 200", StringComparison.Ordinal));
    }

    [Fact]
    public void Multiples_Fallos_Reportan_Multiples_Razones()
    {
        var r = SmokeTestEvaluator.Evaluar(
            new MedidasSmoke(HttpCode: 500, LatenciaMediaSegundos: 5.0,
                ErrorRatePorcentaje: 20.0));
        Assert.Equal(DecisionSmoke.RollbackNecesario, r.Decision);
        Assert.True(r.Razones.Count >= 3);
    }
}
