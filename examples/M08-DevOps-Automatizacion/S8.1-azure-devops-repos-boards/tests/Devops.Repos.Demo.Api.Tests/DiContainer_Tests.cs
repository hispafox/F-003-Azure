using Devops.Repos.Demo.Api.Repos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Devops.Repos.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void RepoBoardsPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IRepoBoardsPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IRepoBoardsPlanner>());

        var plan = planner.Planificar(new EscenarioEquipo(
            Personas: 7, Servicios: 5, CiCdIndependiente: true));

        Assert.Equal(EstrategiaRepo.MultiRepo, plan.EstrategiaRecomendada);
        Assert.Contains(BranchPolicy.RequiredReviewers, plan.PoliciesMinimas);
        Assert.Contains(BranchPolicy.LinkedWorkItems, plan.PoliciesRecomendadas);
        Assert.NotEmpty(plan.Checklist);
    }
}
