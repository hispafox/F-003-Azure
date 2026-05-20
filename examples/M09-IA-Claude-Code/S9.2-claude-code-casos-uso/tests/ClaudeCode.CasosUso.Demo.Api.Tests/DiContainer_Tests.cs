using ClaudeCode.CasosUso.Demo.Api.CasosUso;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ClaudeCode.CasosUso.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void CasosUsoPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<ICasosUsoPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<ICasosUsoPlanner>());

        var plan = planner.Planificar(
            descripcionTarea: "Optimiza el endpoint, P99 está demasiado alto",
            promptDelAlumno: "En este proyecto .NET 10, analiza /api/pedidos. " +
                "No rompas los tests. Output: cambios concretos con impacto estimado. " +
                "Criterio éxito: P99 < 500ms.");

        Assert.Equal(CasoUso.OptimizacionRendimiento, plan.Clasificacion.Caso);
        Assert.Equal(CasoUso.OptimizacionRendimiento, plan.Template.Caso);
        Assert.NotNull(plan.EvaluacionDelPromptDelAlumno);
        Assert.Equal(NivelCalidad.Excelente, plan.EvaluacionDelPromptDelAlumno!.Nivel);
        Assert.True(plan.Checklist.Count >= 8);
    }
}
