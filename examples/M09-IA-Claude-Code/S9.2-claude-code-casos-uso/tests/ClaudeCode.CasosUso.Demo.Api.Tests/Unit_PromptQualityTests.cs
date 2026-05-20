using ClaudeCode.CasosUso.Demo.Api.CasosUso;

namespace ClaudeCode.CasosUso.Demo.Api.Tests;

// CAPA 1 — evaluador de calidad del prompt (slides 18-23 transversal).
[Trait("Category", "Unit")]
public class Unit_PromptQualityTests
{
    [Fact]
    public void Prompt_Vago_Y_Corto_Es_Pobre()
    {
        var e = PromptQualityEvaluator.Evaluar("refactor esto");
        Assert.Equal(NivelCalidad.Pobre, e.Nivel);
        Assert.True(e.Puntuacion <= 25);
    }

    [Fact]
    public void Prompt_Completo_Con_Cuatro_Ingredientes_Es_Excelente()
    {
        const string promptBueno =
            "Refactoriza PedidoService.cs en este proyecto .NET 10. " +
            "Mantén la funcionalidad pública (no rompas los tests existentes). " +
            "Output: archivo modificado en su sitio. " +
            "Criterio éxito: `dotnet test` sigue verde y sin warnings.";

        var e = PromptQualityEvaluator.Evaluar(promptBueno);
        Assert.Equal(NivelCalidad.Excelente, e.Nivel);
        Assert.True(e.TieneContexto);
        Assert.True(e.TieneConstraints);
        Assert.True(e.TieneFormatoSalida);
        Assert.True(e.TieneCriterioExito);
    }

    [Fact]
    public void Solo_Contexto_Y_Formato_Es_Aceptable()
    {
        const string p = "En este proyecto .NET 10, devuelve un JSON con los endpoints.";
        var e = PromptQualityEvaluator.Evaluar(p);
        Assert.Equal(NivelCalidad.Aceptable, e.Nivel);
        Assert.True(e.TieneContexto);
        Assert.True(e.TieneFormatoSalida);
        Assert.False(e.TieneConstraints);
        Assert.False(e.TieneCriterioExito);
    }

    [Fact]
    public void Sugerencias_Cubren_Los_Ingredientes_Faltantes()
    {
        var e = PromptQualityEvaluator.Evaluar("refactoriza esto un poquito");
        Assert.Equal(4, e.Sugerencias.Count(s =>
            s.Contains("Falta", StringComparison.Ordinal)));
    }

    [Fact]
    public void Prompt_Muy_Corto_Se_Penaliza_Aunque_Tenga_Marcadores()
    {
        // < 40 chars: aunque tenga palabras clave, su puntuación queda capada en 25.
        var e = PromptQualityEvaluator.Evaluar("usamos json output");
        Assert.True(e.Puntuacion <= 25);
        Assert.Contains(e.Sugerencias, s =>
            s.Contains("demasiado corto", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Prompt_Vacio_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PromptQualityEvaluator.Evaluar(" "));
    }
}
