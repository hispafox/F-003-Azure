using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Monitor.AppInsights.Demo.Api.Monitor;

namespace Monitor.AppInsights.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void AppInsightsPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IAppInsightsPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IAppInsightsPlanner>());

        var plan = planner.Planificar(
            new EscenarioAlertas(
                ApiPublica: true,
                ProductoConSlaContratado: true,
                EmailEquipo: "x@y.z"),
            VentanaTiempo.Ultimas24h);

        Assert.NotEmpty(plan.QueriesCanonicas);
        Assert.NotEmpty(plan.Alertas);
        Assert.NotEmpty(plan.SmartDetection);
        Assert.Equal(5, plan.Runbook.Count);
        Assert.NotEmpty(plan.Checklist);
        Assert.Contains(plan.Alertas, r => r.Nombre == "sla-availability");
    }
}
