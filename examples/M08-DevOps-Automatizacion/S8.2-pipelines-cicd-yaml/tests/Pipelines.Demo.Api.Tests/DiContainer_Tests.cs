using Pipelines.Demo.Api.Pipelines;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Pipelines.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void PipelinePlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IPipelinePlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IPipelinePlanner>());

        var plan = planner.PlanificarDesdeYaml("""
            trigger: { branches: { include: [main] } }
            pool: { vmImage: 'ubuntu-latest' }
            stages:
            - stage: Build
              jobs:
              - job: B
                steps:
                - script: dotnet build
                - script: dotnet test
            """);

        Assert.Single(plan.Estructura.Stages);
        Assert.True(plan.Validacion.Valido);
        Assert.Equal(3, plan.TriggersEstandar.Count);
        Assert.NotEmpty(plan.Checklist);
    }
}
