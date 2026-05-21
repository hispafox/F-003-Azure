using Bonus.SetupAzure.Demo.Api.Setup;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Bonus.SetupAzure.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void Planner_Se_Resuelve_Y_Es_Singleton()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<ISetupAzurePlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<ISetupAzurePlanner>());
    }

    [Fact]
    public void Planner_Compone_Estructura_Settings_ClaudeMd_Skills_Y_Checklist()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        var planner = scope.ServiceProvider.GetRequiredService<ISetupAzurePlanner>();

        var plan = planner.Planificar(new PlanRequest(
            Equipo: new EscenarioEquipo(
                TieneAgentsCustom: true,
                TieneSkillsPropios: true,
                QuiereHooks: true,
                UsaMcpServers: true),
            Settings: new EscenarioSettings(
                Allow: ["Bash(dotnet *)", "Read(**)"],
                Deny:
                [
                    "Bash(rm -rf *)",
                    "Bash(az group delete *)",
                    "Bash(az resource delete *)",
                    "Bash(drop database *)",
                    "Read(**/*.env)",
                    "Read(**/*.pfx)",
                    "Read(**/*.key)",
                    "Read(**/local.settings.json)",
                ],
                Model: "claude-sonnet-4-6"),
            ClaudeMdContenido:
                "# Proyecto\n## Stack\n.NET 8.\n## Convenciones\nasync/await.\n" +
                "## Comandos\ndotnet build\n## No tocar sin preguntar\nrbac.bicep"));

        Assert.NotEmpty(plan.Estructura.Items);
        Assert.NotNull(plan.Settings);
        Assert.True(plan.Settings!.Seguro);
        Assert.NotNull(plan.ClaudeMd);
        Assert.True(plan.ClaudeMd!.Puntuacion >= 70);
        Assert.Equal(20, plan.AzureSkillsDisponibles.Count);
        Assert.True(plan.Checklist.Count >= 8);
    }
}
