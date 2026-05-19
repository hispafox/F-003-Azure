using Messaging.Demo.Api.Messaging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4: los unit tests
// usan las clases puras directamente y NO ejercen el grafo DI; este
// resuelve IMessagingPlanner del WebApplicationFactory real (sin Docker).
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void MessagingPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IMessagingPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IMessagingPlanner>());

        var plan = planner.Planificar(
            new EscenarioMensajeria(TipoMensaje.EventoNegocio,
                FanOutMultiplesSuscriptores: true),
            "pedidos-eventos",
            [("sub-grandes", "total > 100"), ("sub-rota", "total >>> 1")],
            TimeSpan.FromDays(1));

        Assert.Equal(ServicioMensajeria.ServiceBusTopic, plan.ServicioRecomendado);
        Assert.Equal(86400, plan.VentanaDedupSegundos);
        Assert.True(plan.Suscripciones[0].FiltroValido);
        Assert.False(plan.Suscripciones[1].FiltroValido);   // sintaxis rota
        Assert.NotEmpty(plan.Checklist);
    }
}
