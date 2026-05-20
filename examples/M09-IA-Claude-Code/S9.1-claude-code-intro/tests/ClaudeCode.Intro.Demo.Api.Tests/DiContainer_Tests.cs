using ClaudeCode.Intro.Demo.Api.ClaudeCode;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeCode.Intro.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void ClaudeCodePlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IClaudeCodePlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IClaudeCodePlanner>());

        var plan = planner.Planificar(
            herramienta: new EscenarioElegirHerramienta(
                QuieresAutocompletadoEnIde: true,
                NecesitasAgenteQueEjecuta: true,
                NecesitasMcp: true),
            equipo: new EscenarioEquipo(
                LenguajePrincipal: "csharp", Framework: "net10.0",
                CursoEnProduccion: true, TocaInfraestructura: true),
            tareaConcreta: new EscenarioTarea(
                TipoTarea.GenerarIac, EsRecurrente: true));

        Assert.Equal(HerramientaIa.Combinacion, plan.Herramienta.Herramienta);
        Assert.NotNull(plan.Feature);
        Assert.Contains("Bash", plan.Settings.AllowedTools);
        Assert.True(plan.Checklist.Count >= 8);
    }
}
