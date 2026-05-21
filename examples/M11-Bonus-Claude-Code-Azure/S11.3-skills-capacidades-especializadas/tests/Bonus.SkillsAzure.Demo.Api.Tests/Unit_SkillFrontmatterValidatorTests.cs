using Bonus.SkillsAzure.Demo.Api.Skills;

namespace Bonus.SkillsAzure.Demo.Api.Tests;

// CAPA 1 — slide 6: parser + validador del frontmatter.
[Trait("Category", "Unit")]
public class Unit_SkillFrontmatterValidatorTests
{
    private const string Bien =
        "---\n" +
        "name: deploy-to-azure\n" +
        "description: \"Deploy a .NET app to Azure App Service with validation\"\n" +
        "allowed-tools: Bash(az *), Read\n" +
        "---\n\n# Deploy\n\n1. Run dotnet test";

    [Fact]
    public void Frontmatter_Bien_Formado_Es_Valido_Y_Se_Parsea()
    {
        var r = SkillFrontmatterValidator.Validar(Bien);

        Assert.True(r.Valido);
        Assert.Equal("deploy-to-azure", r.Frontmatter.Name);
        Assert.StartsWith("Deploy a .NET app", r.Frontmatter.Description);
        Assert.Equal(2, r.Frontmatter.AllowedTools.Count);
        Assert.Contains("Bash(az *)", r.Frontmatter.AllowedTools);
        Assert.Contains("Read", r.Frontmatter.AllowedTools);
    }

    [Fact]
    public void Sin_Bloque_Frontmatter_Es_Error()
    {
        var r = SkillFrontmatterValidator.Validar("# Solo un título\n\nSin frontmatter.");

        Assert.False(r.Valido);
        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == SeveridadSkill.Error && h.Campo == "frontmatter");
    }

    [Fact]
    public void Falta_Name_Y_Description_Genera_Dos_Errores()
    {
        var r = SkillFrontmatterValidator.Validar("---\nallowed-tools: Read\n---\n\n# Algo");

        Assert.False(r.Valido);
        Assert.Contains(r.Hallazgos, h => h.Severidad == SeveridadSkill.Error && h.Campo == "name");
        Assert.Contains(r.Hallazgos, h => h.Severidad == SeveridadSkill.Error && h.Campo == "description");
    }

    [Fact]
    public void Context_Fork_Sin_Agent_Es_Advertencia()
    {
        var r = SkillFrontmatterValidator.Validar(
            "---\nname: research\ndescription: \"Research Azure architecture options\"\n" +
            "context: fork\nallowed-tools: WebFetch, Read\n---\n\nInvestiga.");

        Assert.True(r.Valido); // sigue siendo válido (no hay Error)
        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == SeveridadSkill.Advertencia && h.Campo == "agent");
    }

    [Fact]
    public void Context_Fork_Con_Agent_No_Avisa()
    {
        var r = SkillFrontmatterValidator.Validar(
            "---\nname: research\ndescription: \"Research Azure architecture options\"\n" +
            "context: fork\nagent: Explore\nallowed-tools: WebFetch, Read\n---\n\nInvestiga.");

        Assert.DoesNotContain(r.Hallazgos, h => h.Campo == "agent");
        Assert.Equal("Explore", r.Frontmatter.Agent);
    }

    [Fact]
    public void Sin_Allowed_Tools_Es_Advertencia_De_Menor_Privilegio()
    {
        var r = SkillFrontmatterValidator.Validar(
            "---\nname: conv\ndescription: \"Apply team conventions when reviewing code\"\n---\n\nConvenciones.");

        Assert.True(r.Valido);
        Assert.Empty(r.Frontmatter.AllowedTools);
        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == SeveridadSkill.Advertencia && h.Campo == "allowed-tools");
    }

    [Fact]
    public void Comentario_Inline_En_Valor_Se_Limpia()
    {
        var r = SkillFrontmatterValidator.Validar(
            "---\nname: x\ndescription: \"Deploy to Azure\"\nagent: Explore   # qué agent\n" +
            "context: fork\nallowed-tools: Read\n---\n\nX.");

        Assert.Equal("Explore", r.Frontmatter.Agent);
    }

    [Fact]
    public void Validar_Con_Vacio_Lanza()
    {
        Assert.Throws<ArgumentException>(() => SkillFrontmatterValidator.Validar("   "));
    }
}
