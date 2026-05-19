using EasyAuth.Demo.Api.EasyAuth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace EasyAuth.Demo.Api.Tests;

// CAPA 0 — el contenedor DE VERDAD. Lección M03-S3.4.
[Trait("Category", "Component")]
public class DiContainer_Tests
{
    [Fact]
    public void EasyAuthSetupPlanner_Se_Resuelve_Y_Planifica()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();

        var planner = scope.ServiceProvider.GetRequiredService<IEasyAuthSetupPlanner>();
        Assert.NotNull(planner);
        Assert.Same(planner, factory.Services.GetRequiredService<IEasyAuthSetupPlanner>());

        var plan = planner.Planificar(
            TipoApp.SitioWeb, "aad", "https://app.azurewebsites.net");
        Assert.Equal("RedirectToLoginPage", plan.AccionNoAutenticado);
        Assert.Equal(302, plan.CodigoHttp);
        Assert.True(plan.TokenStore);
        Assert.NotEmpty(plan.Checklist);
    }
}
