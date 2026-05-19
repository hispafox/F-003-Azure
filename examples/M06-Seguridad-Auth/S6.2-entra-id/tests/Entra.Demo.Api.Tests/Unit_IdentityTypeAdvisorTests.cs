using Entra.Demo.Api.Entra;

namespace Entra.Demo.Api.Tests;

// CAPA 1 — MI vs SP vs App Registration (slide 10).
[Trait("Category", "Unit")]
public class Unit_IdentityTypeAdvisorTests
{
    [Theory]
    [InlineData(Escenario.RecursoAzureAccedeAOtro, TipoIdentidad.ManagedIdentity)]
    [InlineData(Escenario.ScriptOPipeline, TipoIdentidad.ServicePrincipal)]
    [InlineData(Escenario.AppAutenticaUsuarios, TipoIdentidad.AppRegistration)]
    public void Recomendar(Escenario e, TipoIdentidad esperado)
        => Assert.Equal(esperado, IdentityTypeAdvisor.Recomendar(e));

    [Theory]
    [InlineData(TipoIdentidad.ManagedIdentity, false)]   // sin secreto (slide 10)
    [InlineData(TipoIdentidad.ServicePrincipal, true)]
    [InlineData(TipoIdentidad.AppRegistration, true)]
    public void TieneSecreto(TipoIdentidad t, bool esperado)
        => Assert.Equal(esperado, IdentityTypeAdvisor.TieneSecreto(t));

    [Fact]
    public void Prioridad_MI_Primero()
        => Assert.Equal(TipoIdentidad.ManagedIdentity, IdentityTypeAdvisor.Prioridad[0]);
}
