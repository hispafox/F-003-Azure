using Distribution.Demo.Api.Distribution;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Distribution.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4: los unit tests
// usan las clases puras directamente; este resuelve IDistributionPlanner
// del WebApplicationFactory real (sin Docker).
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void DistributionPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IDistributionPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IDistributionPlanner>());

        var plan = planner.Planificar(new FactoresMigracion(
            IntunePlaneado: true,
            DotNet8Planeado: true,
            EsAppNueva: false,
            SobreDotNetFramework: true,
            TieneTiempoEquipo: true,
            ClickOnceFuncionaBien: false,
            EscenarioFirma: EscenarioFirma.DistribucionInterna));

        Assert.True(plan.MigrarRecomendado);
        Assert.Equal(EscenarioMigracion.B_DotNet8MasMsix, plan.Escenario);
        Assert.Equal(TipoCertificado.EnterpriseCa, plan.Certificado.Tipo);
        Assert.True(plan.VentajasMsixSobreClickOnce >= 7);
        Assert.NotEmpty(plan.Checklist);
    }
}
