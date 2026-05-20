using ClaudeCode.Infra.Demo.Api.Infra;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeCode.Infra.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void InfraPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IInfraPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IInfraPlanner>());

        var plan = planner.Planificar(
            descripcionRequisitos: "API REST con App Service (HTTPS only) y Managed " +
                "Identity, Cosmos DB serverless, Key Vault. Multi-region UE (GDPR). " +
                "Slots y auto-scale.",
            recursosExistentes:
            [
                new EstadoRecurso("app-bad", "Microsoft.Web/sites", HttpsOnly: false),
            ]);

        Assert.True(plan.Requisitos.MultiRegion);
        Assert.True(plan.Requisitos.ComplianceEuropa);
        Assert.True(plan.Requisitos.ConHttpsOnly);
        Assert.True(plan.Requisitos.ConManagedIdentity);
        Assert.Equal(EscenarioInfra.BicepDesdeRequirements, plan.PromptBicep.Escenario);
        Assert.Equal(EscenarioInfra.GhActionsPipeline, plan.PromptPipeline.Escenario);
        Assert.NotNull(plan.Audit);
        Assert.False(plan.Audit!.Limpio);
        Assert.True(plan.Checklist.Count >= 8);
    }
}
