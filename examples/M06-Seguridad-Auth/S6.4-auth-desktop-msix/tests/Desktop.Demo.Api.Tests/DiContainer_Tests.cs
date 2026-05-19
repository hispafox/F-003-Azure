using Desktop.Demo.Api.Desktop;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Desktop.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Sin CAPA de integración (login
// interactivo desktop no es emulable), este test es el único que
// ejercita el grafo DI. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void DesktopAuthPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IDesktopAuthPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IDesktopAuthPlanner>());

        // Windows Entra-joined + token en cache → WAM + cache silent.
        var p = planner.Planificar(
            ContextoDesktop.WindowsEntraJoined, "cid-1",
            new EstadoToken(true, true, true, false));

        Assert.Equal(nameof(MetodoAuthDesktop.Wam), p.Metodo);
        Assert.StartsWith("ms-appx-web://microsoft.aad.brokerplugin/", p.RedirectUri);
        Assert.Equal(nameof(AccionToken.UsarCacheSilent), p.AccionToken);
        Assert.False(p.RequiereUi);
        Assert.True(p.ClientePublico);
    }
}
