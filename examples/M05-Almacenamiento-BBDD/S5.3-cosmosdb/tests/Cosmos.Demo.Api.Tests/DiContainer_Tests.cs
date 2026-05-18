using Cosmos.Demo.Api.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;

namespace Cosmos.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. La integración (CAPA 2) se SALTA sin
// Docker, así que sin esto el grafo DI no se ejercitaría nunca (lección
// M03-S3.4). Resolver el CosmosClient/Container/repo NO abre conexiones
// (el SDK es lazy), así que esto corre siempre, sin Docker ni red.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void Cada_Servicio_Inyectado_Se_Resuelve()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.NotNull(sp.GetRequiredService<CosmosClient>());
        Assert.NotNull(sp.GetRequiredService<Container>());
        Assert.NotNull(sp.GetRequiredService<IPedidoRepository>());
    }
}
