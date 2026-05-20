using ClaudeCode.Intro.Demo.Api.ClaudeCode;

namespace ClaudeCode.Intro.Demo.Api.Tests;

// CAPA 1 — settings.json recomendado (slides 6, 11, 13, 19).
[Trait("Category", "Unit")]
public class Unit_ConfigBuilderTests
{
    [Fact]
    public void Settings_Por_Defecto_Tiene_Tools_Minimas()
    {
        var s = ProjectConfigBuilder.Construir(new EscenarioEquipo());
        Assert.Contains("Read", s.AllowedTools);
        Assert.Contains("Write", s.AllowedTools);
        Assert.Contains("Edit", s.AllowedTools);
        Assert.Contains("Grep", s.AllowedTools);
    }

    [Fact]
    public void Toca_Infraestructura_Anade_Bash()
    {
        var s = ProjectConfigBuilder.Construir(new EscenarioEquipo(
            TocaInfraestructura: true));
        Assert.Contains("Bash", s.AllowedTools);
    }

    [Fact]
    public void Sin_Infraestructura_No_Anade_Bash()
    {
        var s = ProjectConfigBuilder.Construir(new EscenarioEquipo(
            TocaInfraestructura: false));
        Assert.DoesNotContain("Bash", s.AllowedTools);
    }

    [Fact]
    public void Exclude_Patterns_Cubren_Secretos_Clasicos()
    {
        var s = ProjectConfigBuilder.Construir(new EscenarioEquipo());
        Assert.Contains("*.env", s.ExcludePatterns);
        Assert.Contains("*.pfx", s.ExcludePatterns);
        Assert.Contains("local.settings.json", s.ExcludePatterns);
        Assert.Contains(".secrets/*", s.ExcludePatterns);
    }

    [Fact]
    public void Produccion_Anade_Hook_PreCommit_Validation()
    {
        var s = ProjectConfigBuilder.Construir(new EscenarioEquipo(
            CursoEnProduccion: true));
        Assert.Contains(s.HooksRecomendados, h =>
            h.Contains("pre-commit-validation", StringComparison.Ordinal));
    }

    [Fact]
    public void Compliance_Anade_Hook_Block_Secrets()
    {
        var s = ProjectConfigBuilder.Construir(new EscenarioEquipo(
            RequiereCompliance: true));
        Assert.Contains(s.HooksRecomendados, h =>
            h.Contains("block-secrets", StringComparison.Ordinal));
    }

    [Fact]
    public void System_Prompt_Menciona_Framework_Del_Equipo()
    {
        var s = ProjectConfigBuilder.Construir(new EscenarioEquipo(
            LenguajePrincipal: "csharp", Framework: "net10.0"));
        Assert.Contains("csharp", s.SystemPrompt, StringComparison.Ordinal);
        Assert.Contains("net10.0", s.SystemPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void Hook_Auto_Format_Esta_En_PostToolUse()
    {
        var s = ProjectConfigBuilder.Construir(new EscenarioEquipo());
        Assert.Contains(s.HooksRecomendados, h =>
            h.Contains("auto-format", StringComparison.Ordinal)
            && h.Contains("PostToolUse", StringComparison.Ordinal));
    }
}
