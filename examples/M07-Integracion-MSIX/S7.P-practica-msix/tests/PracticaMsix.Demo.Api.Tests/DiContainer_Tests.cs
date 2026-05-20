using PracticaMsix.Demo.Api.Practica;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace PracticaMsix.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void PracticaMsixPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IPracticaMsixPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IPracticaMsixPlanner>());

        var plan = planner.Planificar(
            new ParametrosPractica("Empresa", "MsixDemo", "1.0.0.0",
                "https://stventasprod.blob.core.windows.net/msix"),
            subjectCertificado: "CN=Empresa");

        Assert.Equal(8, plan.Pasos.Count);
        Assert.True(plan.PublisherCertCheck.Ok);
        Assert.Contains("Empresa.MsixDemo", plan.ManifestEjemplo);
        Assert.Contains("1.0.0.0", plan.AppInstallerEjemplo);
        Assert.NotEmpty(plan.Checklist);
    }

    [Fact]
    public void Detecta_Publisher_Cert_Desalineados()
    {
        using var factory = new WebApplicationFactory<Program>();
        var planner = factory.Services.GetRequiredService<IPracticaMsixPlanner>();

        var plan = planner.Planificar(
            new ParametrosPractica("Empresa", "MsixDemo", "1.0.0.0", "https://x"),
            subjectCertificado: "CN=Otro");

        Assert.False(plan.PublisherCertCheck.Ok);
    }
}
