using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Practica.PrimerComando.Demo.Api.PrimerComando;

namespace Practica.PrimerComando.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void Planner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IPracticaPrimerComandoPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IPracticaPrimerComandoPlanner>());

        var plan = planner.Planificar(new PlanRequest(
            Preflight: new EscenarioPreflight(
                TieneNode18OSuperior: true,
                TieneCuentaAnthropic: true,
                Auth: MetodoAuth.ClaudeAi,
                TieneTerminalModerna: true,
                TieneGit: true,
                TieneRepoPracticar: true),
            Evidencias:
            [
                new EvidenciaPaso(Paso.InstalarCli, true, true),
                new EvidenciaPaso(Paso.LoginYPrimeraSesion, true, true),
                new EvidenciaPaso(Paso.CrearClaudeMd, true, false),
            ],
            PromptDelAlumno: "Antes de implementar, dime cómo lo harías"));

        Assert.True(plan.Preflight.ListoParaArrancar);
        Assert.Equal(3, plan.Pasos.Count);
        Assert.NotNull(plan.AnalisisDelPromptDelAlumno);
        Assert.False(plan.AnalisisDelPromptDelAlumno!.TieneAntiPatterns);
        Assert.Equal(8, plan.SlashCommandsEsenciales.Count);
        Assert.True(plan.Checklist.Count >= 10);
    }
}
