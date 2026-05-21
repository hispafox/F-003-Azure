using Bonus.SkillsAzure.Demo.Api.Skills;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Bonus.SkillsAzure.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void Planner_Se_Resuelve_Y_Es_Singleton()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<ISkillLibraryPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<ISkillLibraryPlanner>());
    }

    [Fact]
    public void Planner_Con_SkillMd_Compone_Frontmatter_Description_AntiPatrones()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        var planner = scope.ServiceProvider.GetRequiredService<ISkillLibraryPlanner>();

        var plan = planner.Planificar(new PlanRequest(SkillMd:
            "---\nname: convenciones-equipo\n" +
            "description: \"Apply our team .NET and Azure coding conventions when reviewing code\"\n" +
            "allowed-tools: Read\n---\n\n# Convenciones\n\n- async/await\n- records para DTOs"));

        Assert.NotNull(plan.Frontmatter);
        Assert.True(plan.Frontmatter!.Valido);
        Assert.NotNull(plan.Description);
        Assert.True(plan.Description!.SeActivaraFiable);
        Assert.NotNull(plan.AntiPatrones);
        Assert.True(plan.AntiPatrones!.Limpio);
        Assert.Equal(8, plan.SkillsMicrosoft.Count);
        Assert.Equal(5, plan.SkillsRecomendadosEquipo.Count);
        Assert.Equal(4, plan.Roadmap.Count);
        Assert.True(plan.Checklist.Count >= 8);
    }

    [Fact]
    public void Planner_Sin_SkillMd_Devuelve_Catalogo_Y_Roadmap()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        var planner = scope.ServiceProvider.GetRequiredService<ISkillLibraryPlanner>();

        var plan = planner.Planificar(new PlanRequest());

        Assert.Null(plan.Frontmatter);
        Assert.Null(plan.Description);
        Assert.Null(plan.AntiPatrones);
        Assert.Equal(8, plan.SkillsMicrosoft.Count);
        Assert.Equal(4, plan.Roadmap.Count);
    }
}
