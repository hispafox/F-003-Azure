using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ProyectoIntegrador.Diseno.Demo.Api.Diseno;

namespace ProyectoIntegrador.Diseno.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void Planner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IProyectoIntegradorPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IProyectoIntegradorPlanner>());

        var plan = planner.Planificar(new PlanRequest(
            Sistema: new EstadoSistema(
                Bicep: EstadoComponente.Desplegado,
                AppService: EstadoComponente.Desplegado,
                Cosmos: EstadoComponente.Desplegado,
                ManagedIdentity: EstadoComponente.Desplegado),
            Entrega: new EvidenciaEntrega(
                BicepDesplegadoConWhatIf: true,
                ApiCrudDevuelve2xx: true,
                DatosPersistenEnCosmos: true,
                SinConnectionStringConPassword: true)));

        Assert.Equal(10, plan.Arquitectura.Count);
        Assert.Equal(40, plan.PorcentajeDesplegado);
        Assert.Equal(Bloque.B_ApiYAuth, plan.BloqueSiguiente.Bloque);
        Assert.NotNull(plan.Entrega);
        Assert.Equal(50, plan.Entrega!.PorcentajeTotal);  // 15+15+10+10 = 50
        Assert.Equal(5, plan.Retos.Count);
    }
}
