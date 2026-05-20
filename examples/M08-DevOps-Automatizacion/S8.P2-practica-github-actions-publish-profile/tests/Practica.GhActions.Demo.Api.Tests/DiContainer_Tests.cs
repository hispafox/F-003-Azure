using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Practica.GhActions.Demo.Api.GhActions;

namespace Practica.GhActions.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    private const string PublishProfileMinimo = """
        <publishData>
          <publishProfile profileName="x" publishMethod="MSDeploy"
                          publishUrl="x.scm.azurewebsites.net:443"
                          userName="$x"
                          userPWD="realpwd123"
                          destinationAppUrl="https://x.azurewebsites.net" />
        </publishData>
        """;

    [Fact]
    public void PracticaGhActionsPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IPracticaGhActionsPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IPracticaGhActionsPlanner>());

        var plan = planner.Planificar(
            publishProfileXml: PublishProfileMinimo,
            opciones: new OpcionesWorkflow(
                AppName: "webapp-pedro", IncluirTests: true, SmokeAlFinal: true),
            escenarioAuth: new EscenarioAuth(SideProjectPersonal: true));

        Assert.NotNull(plan.Profile);
        Assert.True(plan.Profile!.EsValido);
        Assert.Equal(2, plan.Workflow.Jobs.Count); // build-test + deploy
        Assert.Equal(MetodoAuth.PublishProfile, plan.Recomendacion.Metodo);
        Assert.True(plan.Checklist.Count >= 10);
    }
}
