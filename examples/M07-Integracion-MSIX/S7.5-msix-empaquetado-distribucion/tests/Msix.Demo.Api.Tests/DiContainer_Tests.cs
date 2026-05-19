using Msix.Demo.Api.Msix;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Msix.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void MsixPackagingPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IMsixPackagingPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IMsixPackagingPlanner>());

        var plan = planner.Planificar(
            new AppxManifest("MiEmpresa.App", "CN=MiEmpresa", "1.0.0.0",
                "x64", "10.0.17763.0", ["internetClient"]),
            new EscenarioDistribucion(MdmIntune: true, HostingAzureBlob: true));

        Assert.True(plan.ManifestValido);
        Assert.Equal("MiEmpresa.App_1.0.0.0_x64.msix", plan.NombreArchivo);
        Assert.Contains(CanalDistribucion.Intune, plan.Canales);
        Assert.Contains(CanalDistribucion.AppInstaller, plan.Canales);
        Assert.NotEmpty(plan.Checklist);
    }
}
