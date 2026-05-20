using ClaudeCode.Intro.Demo.Api.ClaudeCode;

namespace ClaudeCode.Intro.Demo.Api.Tests;

// CAPA 1 — recomendador de modo + features (slides 4/7-10/12/15/16/18/19/20).
[Trait("Category", "Unit")]
public class Unit_FeatureRecommenderTests
{
    [Fact]
    public void Pipeline_CiCd_Usa_Modo_Headless_Y_Anade_Hook()
    {
        var r = FeatureRecommender.Recomendar(new EscenarioTarea(
            TipoTarea.CodeReview, EnPipelineCiCd: true));
        Assert.Equal(ModoEjecucion.Headless, r.Modo);
        Assert.Contains(r.Caracteristicas, c =>
            c.Nombre.Contains("PreToolUse hook", StringComparison.Ordinal));
    }

    [Fact]
    public void Analisis_Logs_Usa_Modo_Pipe()
    {
        var r = FeatureRecommender.Recomendar(new EscenarioTarea(
            TipoTarea.AnalisisLogs));
        Assert.Equal(ModoEjecucion.Pipe, r.Modo);
    }

    [Fact]
    public void Changelog_Usa_Modo_OneShot()
    {
        var r = FeatureRecommender.Recomendar(new EscenarioTarea(
            TipoTarea.ChangelogODocs));
        Assert.Equal(ModoEjecucion.OneShot, r.Modo);
    }

    [Fact]
    public void Arquitectura_Activa_Extended_Thinking()
    {
        var r = FeatureRecommender.Recomendar(new EscenarioTarea(
            TipoTarea.Arquitectura));
        Assert.True(r.UsarExtendedThinking);
    }

    [Fact]
    public void Refactor_Simple_No_Activa_Extended_Thinking()
    {
        var r = FeatureRecommender.Recomendar(new EscenarioTarea(
            TipoTarea.Refactorizar));
        Assert.False(r.UsarExtendedThinking);
    }

    [Fact]
    public void Refactor_Complejo_Activa_Extended_Thinking()
    {
        var r = FeatureRecommender.Recomendar(new EscenarioTarea(
            TipoTarea.Refactorizar, EsCompleja: true));
        Assert.True(r.UsarExtendedThinking);
    }

    [Fact]
    public void Code_Review_Sugiere_Subagent_Code_Reviewer()
    {
        var r = FeatureRecommender.Recomendar(new EscenarioTarea(
            TipoTarea.CodeReview));
        Assert.Contains(r.Caracteristicas, c =>
            c.Nombre.Contains("code-reviewer", StringComparison.Ordinal));
    }

    [Fact]
    public void Tarea_Recurrente_Sugiere_Skill()
    {
        var r = FeatureRecommender.Recomendar(new EscenarioTarea(
            TipoTarea.GenerarIac, EsRecurrente: true));
        Assert.Contains(r.Caracteristicas, c =>
            c.Slide == "20" && c.Nombre.Contains("bicep", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Tarea_No_Recurrente_No_Sugiere_Skill()
    {
        var r = FeatureRecommender.Recomendar(new EscenarioTarea(
            TipoTarea.GenerarCodigo, EsRecurrente: false));
        Assert.DoesNotContain(r.Caracteristicas, c => c.Slide == "20");
    }

    [Fact]
    public void Contexto_Aislado_Sugiere_Subagent_Aunque_No_Sea_CodeReview()
    {
        var r = FeatureRecommender.Recomendar(new EscenarioTarea(
            TipoTarea.GenerarCodigo, RequiereContextoAislado: true));
        Assert.Contains(r.Caracteristicas, c => c.Slide == "18");
    }
}
