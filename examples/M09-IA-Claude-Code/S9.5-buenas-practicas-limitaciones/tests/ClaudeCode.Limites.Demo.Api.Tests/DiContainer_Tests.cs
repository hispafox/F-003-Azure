using ClaudeCode.Limites.Demo.Api.Limites;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeCode.Limites.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void LimitesPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<ILimitesPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<ILimitesPlanner>());

        var plan = planner.Planificar(
            descripcionUso: "Le paso la connection string real, sin tests.",
            promptDelAlumno: "CONTEXTO: .NET 10. OBJETIVO: crea endpoint. " +
                "Constraints: no romper API. Output: archivos. " +
                "Criterio éxito: tests verdes.",
            tipoTarea: TipoTareaIa.Boilerplate);

        Assert.NotNull(plan.AntiPatterns);
        Assert.False(plan.AntiPatterns!.Limpio);
        Assert.NotNull(plan.Estructura);
        Assert.True(plan.Estructura!.Puntuacion >= 60);
        Assert.NotNull(plan.Clasificacion);
        Assert.Equal(ImpactoIa.Acelera, plan.Clasificacion!.Impacto);
        Assert.Equal(7, plan.ReglasDeOro.Count);
        Assert.True(plan.Checklist.Count >= 10);
    }
}
