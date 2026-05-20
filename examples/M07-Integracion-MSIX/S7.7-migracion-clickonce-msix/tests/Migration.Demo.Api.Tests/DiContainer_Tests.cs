using Migration.Demo.Api.Migration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Migration.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void MigrationPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IMigrationPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IMigrationPlanner>());

        var plan = planner.Planificar(new EscenarioMigracion(
            ClickOnce: new ClickOnceManifest("VentasDesktop", "Mi Empresa, S.L.", "2.4.1"),
            Comportamientos: [ComportamientoApp.Wpf, ComportamientoApp.EscribeHKLM],
            FaseActual: FaseMigracion.Empaquetado));

        Assert.Equal("MiEmpresaSL.VentasDesktop", plan.Manifest.IdentityName);
        Assert.Equal("2.4.1.0", plan.Manifest.Version);                  // completado
        Assert.StartsWith("CN=", plan.Manifest.Publisher);
        Assert.Equal(NivelRiesgo.Precaucion, plan.Compatibilidad.Riesgo); // HKLM
        Assert.True(plan.Compatibilidad.RequierePsf);
        Assert.Equal(FaseMigracion.Empaquetado, plan.Fase.Fase);
        Assert.NotEmpty(plan.Checklist);
    }
}
