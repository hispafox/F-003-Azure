using EventDriven.Demo.Api.EventDriven;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EventDriven.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4: los unit tests
// usan las clases puras directamente; este resuelve IEventDrivenPlanner
// del WebApplicationFactory real (sin Docker).
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void EventDrivenPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IEventDrivenPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IEventDrivenPlanner>());

        var plan = planner.Planificar(
            new EscenarioDiseno(
                MultiplesConsumidores: true, ProcesamientoPesado: true,
                AuditTrailCompleto: true, PasosSaga: 6),
            [
                new DefinicionEvento("PedidoCreado", ["pedidoId", "version"]),
                new DefinicionEvento("CobrarTarjeta", ["tarjeta", "cvv"]),
            ]);

        Assert.True(plan.EventDrivenRecomendado);
        Assert.Equal(PatronEvento.EventSourcing, plan.PatronEvento);
        Assert.Equal(EstiloSaga.Orchestration, plan.EstiloSaga);
        Assert.Single(plan.EventosInvalidos);                 // CobrarTarjeta
        Assert.Equal("CobrarTarjeta", plan.EventosInvalidos[0].Tipo);
        Assert.NotEmpty(plan.Checklist);
    }
}
