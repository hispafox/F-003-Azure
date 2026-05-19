using Entra.Demo.Api.Entra;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Entra.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Sin CAPA de integración (Entra ID
// no es emulable), este test es el único que ejercita el grafo DI.
// Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void AppRolesAuthorizer_Se_Resuelve_Del_Contenedor_Real()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var auth = scope.ServiceProvider.GetRequiredService<IAppRolesAuthorizer>();
        Assert.NotNull(auth);
        Assert.Same(auth, factory.Services.GetRequiredService<IAppRolesAuthorizer>());
        Assert.True(auth.Autorizar(["Admin"], "Admin").Autorizado);
    }
}
