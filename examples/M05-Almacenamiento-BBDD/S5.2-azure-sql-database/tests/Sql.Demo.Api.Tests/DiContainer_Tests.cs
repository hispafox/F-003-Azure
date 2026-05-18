using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Sql.Demo.Api.Data;
using Sql.Demo.Api.Repositories;

namespace Sql.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. El test de integración (CAPA 3) se
// SALTA sin Docker, así que sin esto el grafo DI no se ejercitaría nunca
// (lección de M03-S3.4: tests con `new` no cogen un registro olvidado).
// Aquí resolvemos cada servicio del contenedor real (WebApplication
// factory) en un scope, SIN tocar la BD (resolver != consultar).
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void Cada_Servicio_Inyectado_Se_Resuelve()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Si Program.cs olvidara un AddScoped/AddDbContext, esto revienta
        // igual que reventaría el Function App / API real en arranque.
        Assert.NotNull(sp.GetRequiredService<VentasDbContext>());
        Assert.NotNull(sp.GetRequiredService<IProductoRepository>());
        Assert.NotNull(sp.GetRequiredService<IPedidoRepository>());
    }
}
