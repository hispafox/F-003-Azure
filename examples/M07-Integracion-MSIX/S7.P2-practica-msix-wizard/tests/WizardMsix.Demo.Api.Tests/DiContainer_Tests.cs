using WizardMsix.Demo.Api.Wizard;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace WizardMsix.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void PracticaMsixWizardPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IPracticaMsixWizardPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IPracticaMsixWizardPlanner>());

        var plan = planner.Planificar(
            new ContextoEmpaquetado(AprendizajeInicial: true, AppSimpleSingleArch: true),
            new ParametrosWizard("MiEmpresa", "MiApp", "1.0.0.0",
                @"C:\bin\Release\x64", @"C:\src\cert.pfx",
                @"C:\out\MiApp_1.0.0.0_x64.msix"));

        Assert.Equal(FlujoEmpaquetado.Wizard, plan.FlujoRecomendado);
        Assert.Equal(4, plan.ComandosEquivalentes.Count);
        Assert.NotEmpty(plan.LimitacionesWizard);
        Assert.NotEmpty(plan.Checklist);
    }

    [Fact]
    public void Pipeline_CiCd_Empuja_A_Cli()
    {
        using var factory = new WebApplicationFactory<Program>();
        var planner = factory.Services.GetRequiredService<IPracticaMsixWizardPlanner>();

        var plan = planner.Planificar(
            new ContextoEmpaquetado(PipelineCiCd: true, CertDesdeKeyVault: true),
            new ParametrosWizard("E", "A", "1.0.0.0", "out", "c.pfx", "msix"));

        Assert.Equal(FlujoEmpaquetado.Cli, plan.FlujoRecomendado);
    }
}
