using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Practica.CcMcp.Demo.Api.Practica;

namespace Practica.CcMcp.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void PracticaCcMcpPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IPracticaCcMcpPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IPracticaCcMcpPlanner>());

        var plan = planner.Planificar(new EvaluacionRequest(
            Preflight: new EscenarioPreflight(
                TieneNode18OSuperior: true,
                ClaudeInstaladoYAutenticado: true,
                TieneApiKey: true,
                TieneAzCli: true,
                TieneRepoLocal: true,
                ClaudeMdConfigurado: true),
            Evidencias:
            [
                new EvidenciaEjercicio(Ejercicio.GenerarServicioCompleto, true, true, true),
                new EvidenciaEjercicio(Ejercicio.GenerarBicep, true, false, true),
            ],
            PromptVago: "crea algo",
            PromptMedio: "Crea un servicio en .NET 10",
            PromptDetallado: "CONTEXTO: .NET 10. Mantén. Output: archivos. " +
                "Criterio éxito: tests verdes."));

        Assert.True(plan.Preflight.ListoParaArrancar);
        Assert.Equal(2, plan.Ejercicios.Count);
        Assert.Contains(plan.Ejercicios, e => e.Resultado == ResultadoEjercicio.Pasa);
        Assert.Contains(plan.Ejercicios, e => e.Resultado == ResultadoEjercicio.Pendiente);
        Assert.NotNull(plan.Comparativa);
        Assert.True(plan.Comparativa!.DeltaVagoADetallado > 0);
        Assert.True(plan.Checklist.Count >= 8);
    }
}
