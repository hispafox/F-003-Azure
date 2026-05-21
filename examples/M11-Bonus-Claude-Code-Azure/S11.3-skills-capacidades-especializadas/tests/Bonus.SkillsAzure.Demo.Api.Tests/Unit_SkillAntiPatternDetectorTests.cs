using Bonus.SkillsAzure.Demo.Api.Skills;

namespace Bonus.SkillsAzure.Demo.Api.Tests;

// CAPA 1 — slide 17: detector de anti-patrones del SKILL.md.
[Trait("Category", "Unit")]
public class Unit_SkillAntiPatternDetectorTests
{
    private const string Limpio =
        "---\nname: deploy\ndescription: \"Deploy a .NET app to Azure App Service\"\n" +
        "allowed-tools: Bash(az *), Read\n---\n\n# Deploy\n\n1. Run az bicep build";

    [Fact]
    public void Skill_Limpio_No_Tiene_Hallazgos()
    {
        var r = SkillAntiPatternDetector.Detectar(Limpio);

        Assert.True(r.Limpio);
        Assert.Empty(r.Hallazgos);
    }

    [Fact]
    public void Credencial_Literal_Es_Error()
    {
        var r = SkillAntiPatternDetector.Detectar(
            "---\nname: deploy\ndescription: \"Deploy\"\nallowed-tools: Read\n---\n\n" +
            "Connection string: Server=tcp:sql;Password=secret123;");

        Assert.False(r.Limpio);
        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == SeveridadSkill.Error && h.Campo == "credenciales");
    }

    [Theory]
    [InlineData("Bash(*)")]
    [InlineData("Write(**)")]
    [InlineData("Edit(**)")]
    public void Tools_Demasiado_Amplios_Es_Advertencia(string tool)
    {
        var r = SkillAntiPatternDetector.Detectar(
            $"---\nname: deploy\ndescription: \"Deploy to Azure\"\nallowed-tools: {tool}\n---\n\nDespliega.");

        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == SeveridadSkill.Advertencia && h.Campo == "allowed-tools");
    }

    [Fact]
    public void Skill_Enorme_Es_Advertencia_De_Tamano()
    {
        var cuerpo = string.Join("\n", Enumerable.Repeat("- paso del procedimiento.", 520));
        var r = SkillAntiPatternDetector.Detectar(
            "---\nname: huge\ndescription: \"Deploy to Azure\"\nallowed-tools: Read\n---\n\n" + cuerpo);

        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == SeveridadSkill.Advertencia && h.Campo == "tamaño");
    }

    [Fact]
    public void Tools_Bien_Restringidos_No_Generan_Aviso_De_Tools()
    {
        var r = SkillAntiPatternDetector.Detectar(Limpio);

        Assert.DoesNotContain(r.Hallazgos, h => h.Campo == "allowed-tools");
    }

    [Fact]
    public void Detectar_Con_Vacio_Lanza()
    {
        Assert.Throws<ArgumentException>(() => SkillAntiPatternDetector.Detectar("  "));
    }
}
