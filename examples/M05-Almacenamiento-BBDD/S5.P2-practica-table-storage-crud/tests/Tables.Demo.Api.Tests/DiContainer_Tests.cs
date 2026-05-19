using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tables.Demo.Api.Domain;

namespace Tables.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. El ctor del servicio construye el
// TableClient lazy (NO toca red) → corre sin Docker; la integración
// (CAPA 2) se salta sin Azurite. Cubre la lección DI de M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void ProductosService_Se_Resuelve_Del_Contenedor_Real()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var svc = scope.ServiceProvider.GetRequiredService<IProductosService>();
        Assert.NotNull(svc);
        Assert.Same(svc, factory.Services.GetRequiredService<IProductosService>());
    }
}
