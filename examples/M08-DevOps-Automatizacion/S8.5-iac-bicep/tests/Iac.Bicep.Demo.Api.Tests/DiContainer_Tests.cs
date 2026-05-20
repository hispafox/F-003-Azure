using Iac.Bicep.Demo.Api.Iac;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Iac.Bicep.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void IacPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IIacPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IIacPlanner>());

        const string bicep = """
            targetScope = 'resourceGroup'
            @secure()
            param connectionString string

            param appName string

            resource a 'Microsoft.Web/sites@2023-12-01' = {
              name: appName
              location: resourceGroup().location
            }
            """;
        var plan = planner.Planificar(
            new EscenarioIac(SoloAzure: true), bicep,
            "  + /subscriptions/x/sites/app [Microsoft.Web/sites]");

        Assert.Equal(HerramientaIac.Bicep, plan.Herramienta.Herramienta);
        Assert.True(plan.ValidacionDelArchivo.Valido);
        Assert.NotNull(plan.WhatIf);
        Assert.Single(plan.WhatIf!.Cambios);
        Assert.NotEmpty(plan.Checklist);
    }
}
