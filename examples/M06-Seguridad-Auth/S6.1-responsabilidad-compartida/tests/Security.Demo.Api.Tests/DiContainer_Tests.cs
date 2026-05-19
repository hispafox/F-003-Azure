using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Security.Demo.Api.Security;

namespace Security.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. No hay CAPA de integración
// (responsabilidad compartida / STRIDE / Secure Score no son emulables),
// así que este test es el único que ejercita el grafo DI. Lección
// M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void SecureScore_Se_Resuelve_Del_Contenedor_Real()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var svc = scope.ServiceProvider.GetRequiredService<ISecureScore>();
        Assert.NotNull(svc);
        Assert.Same(svc, factory.Services.GetRequiredService<ISecureScore>());

        var r = svc.Calcular(new ChecklistSeguridad(
            true, true, true, true, true, true, true, true, true, true, true));
        Assert.Equal(100, r.Puntuacion);
    }
}
