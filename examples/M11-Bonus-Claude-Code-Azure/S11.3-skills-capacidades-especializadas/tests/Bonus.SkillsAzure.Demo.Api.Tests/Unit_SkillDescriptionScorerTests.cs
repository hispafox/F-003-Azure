using Bonus.SkillsAzure.Demo.Api.Skills;

namespace Bonus.SkillsAzure.Demo.Api.Tests;

// CAPA 1 — slide 16/24: scorer de la `description`.
[Trait("Category", "Unit")]
public class Unit_SkillDescriptionScorerTests
{
    [Fact]
    public void Description_Especifica_Con_Keywords_Es_Fiable()
    {
        var r = SkillDescriptionScorer.Evaluar(
            "Deploy a .NET 8 application to Azure App Service including Bicep " +
            "validation, what-if preview, slot swap, and post-deploy smoke tests");

        Assert.True(r.Puntuacion >= 60);
        Assert.True(r.SeActivaraFiable);
        Assert.NotEmpty(r.KeywordsDetectadas);
        Assert.Empty(r.PalabrasVagas);
    }

    [Fact]
    public void Description_Vaga_No_Es_Fiable()
    {
        var r = SkillDescriptionScorer.Evaluar("Helps with deployments and maybe other things");

        Assert.False(r.SeActivaraFiable);
        Assert.Contains("help", r.PalabrasVagas);
        Assert.Contains("maybe", r.PalabrasVagas);
        Assert.Contains(r.Sugerencias, s =>
            s.Contains("vago", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Lenguaje_Vago_Penaliza_La_Puntuacion()
    {
        var concreta = SkillDescriptionScorer.Evaluar(
            "Review Bicep modules for Managed Identity usage and naming conventions");
        var vaga = SkillDescriptionScorer.Evaluar(
            "Maybe helpful for various infrastructure things");

        Assert.True(concreta.Puntuacion > vaga.Puntuacion);
    }

    [Fact]
    public void Description_Corta_Sin_Keywords_Sugiere_Mejoras()
    {
        var r = SkillDescriptionScorer.Evaluar("Does X");

        Assert.False(r.SeActivaraFiable);
        Assert.Contains(r.Sugerencias, s =>
            s.Contains("keywords", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Verbo_De_Accion_Inicial_Suma()
    {
        var conVerbo = SkillDescriptionScorer.Evaluar(
            "Deploy to Azure App Service with Bicep");
        var sinVerbo = SkillDescriptionScorer.Evaluar(
            "Azure App Service with Bicep");

        Assert.True(conVerbo.Puntuacion >= sinVerbo.Puntuacion);
    }

    [Fact]
    public void Puntuacion_Esta_Acotada_0_100()
    {
        var r = SkillDescriptionScorer.Evaluar(
            "Deploy and review and validate and migrate Azure App Service Cosmos " +
            "Functions Service Bus Key Vault RBAC Managed Identity smoke test");

        Assert.InRange(r.Puntuacion, 0, 100);
    }

    [Fact]
    public void Evaluar_Con_Vacio_Lanza()
    {
        Assert.Throws<ArgumentException>(() => SkillDescriptionScorer.Evaluar("  "));
    }
}
