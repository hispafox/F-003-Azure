using Bonus.IntroIaAgentica.Demo.Api.Intro;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Bonus.IntroIaAgentica.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void Planner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IIntroIaAgenticaPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IIntroIaAgenticaPlanner>());

        var plan = planner.Planificar(new PlanRequest(
            Uso: new EscenarioUso(EditaCodigo: true, EsDeveloper: true),
            Equipo: new EscenarioEquipo(
                ConfiguraSkills: true,
                ConfiguraMcp: true,
                SkillsEnGit: true,
                HumanoEnLoopAccionesConImpacto: true),
            DescripcionHerramientaActual: "Claude Code en terminal con MCP"));

        Assert.NotNull(plan.Clasificacion);
        Assert.Equal(GeneracionIa.Gen3Agente, plan.Clasificacion!.Generacion);
        Assert.Equal(Herramienta.ClaudeCode, plan.Recomendacion.Cual);
        Assert.Equal(NivelUso.Nivel2_Colega, plan.Nivel.Nivel);
        Assert.Equal(2, plan.Nivel.PrincipiosCumplidos);
        Assert.Equal(7, plan.ObjetivosM11.Count);
        Assert.True(plan.Checklist.Count >= 5);
    }
}
