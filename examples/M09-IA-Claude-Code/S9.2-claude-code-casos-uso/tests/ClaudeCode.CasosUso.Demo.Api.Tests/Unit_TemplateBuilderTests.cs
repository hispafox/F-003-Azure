using ClaudeCode.CasosUso.Demo.Api.CasosUso;

namespace ClaudeCode.CasosUso.Demo.Api.Tests;

// CAPA 1 — templates canónicos por caso (slides 2-16).
[Trait("Category", "Unit")]
public class Unit_TemplateBuilderTests
{
    [Fact]
    public void Migracion_Tiene_Placeholders_Archivo_Y_Versiones()
    {
        var t = PromptTemplateBuilder.ParaCaso(CasoUso.MigracionLegacyANet);
        Assert.Contains("archivo", t.Placeholders);
        Assert.Contains("versionLegacy", t.Placeholders);
        Assert.Contains("{{archivo}}", t.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Code_Review_Pide_Output_Json_Con_Severidad()
    {
        var t = PromptTemplateBuilder.ParaCaso(CasoUso.CodeReview);
        Assert.Contains("JSON", t.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("severidad", t.Texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Optimizacion_Pide_Metricas_Y_Objetivo_P99()
    {
        var t = PromptTemplateBuilder.ParaCaso(CasoUso.OptimizacionRendimiento);
        Assert.Contains("p50", t.Placeholders);
        Assert.Contains("p99", t.Placeholders);
        Assert.Contains("objetivoP99", t.Placeholders);
    }

    [Fact]
    public void Pair_Programming_Sugiere_Modo_Interactive_Y_Pasos()
    {
        var t = PromptTemplateBuilder.ParaCaso(CasoUso.PairProgramming);
        Assert.Contains("interactive", t.Texto, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("paso", t.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Caso_Otro_Devuelve_Template_Generico_Con_4_Ingredientes()
    {
        var t = PromptTemplateBuilder.ParaCaso(CasoUso.Otro);
        Assert.Contains("contexto", t.Placeholders);
        Assert.Contains("constraints", t.Placeholders);
        Assert.Contains("formatoSalida", t.Placeholders);
        Assert.Contains("criterioExito", t.Placeholders);
    }

    [Fact]
    public void Todos_Los_Templates_Mencionan_Su_Slide()
    {
        foreach (var caso in Enum.GetValues<CasoUso>())
        {
            var t = PromptTemplateBuilder.ParaCaso(caso);
            Assert.False(string.IsNullOrWhiteSpace(t.Slide),
                $"El caso {caso} no tiene slide.");
            Assert.False(string.IsNullOrWhiteSpace(t.Texto),
                $"El caso {caso} no tiene texto.");
        }
    }

    [Fact]
    public void Expand_Contract_Menciona_Las_Cuatro_Fases()
    {
        var t = PromptTemplateBuilder.ParaCaso(CasoUso.ExpandContractRefactor);
        Assert.Contains("Expand", t.Texto, StringComparison.Ordinal);
        Assert.Contains("Dual write", t.Texto, StringComparison.Ordinal);
        Assert.Contains("Switch reads", t.Texto, StringComparison.Ordinal);
        Assert.Contains("Contract", t.Texto, StringComparison.Ordinal);
    }
}
