using Practica.GhActions.Demo.Api.GhActions;

namespace Practica.GhActions.Demo.Api.Tests;

// CAPA 1 — recomendador Publish Profile vs OIDC (slide 13/18).
[Trait("Category", "Unit")]
public class Unit_AuthRecommenderTests
{
    [Fact]
    public void Side_Project_Personal_Recomienda_Publish_Profile()
    {
        var r = MetodoAuthRecomendador.Recomendar(new EscenarioAuth(
            SideProjectPersonal: true));
        Assert.Equal(MetodoAuth.PublishProfile, r.Metodo);
    }

    [Fact]
    public void No_Controla_Entra_Recomienda_Publish_Profile()
    {
        var r = MetodoAuthRecomendador.Recomendar(new EscenarioAuth(
            SideProjectPersonal: false, ControlaEntraId: false));
        Assert.Equal(MetodoAuth.PublishProfile, r.Metodo);
    }

    [Fact]
    public void Produccion_Con_Entra_Recomienda_Oidc()
    {
        var r = MetodoAuthRecomendador.Recomendar(new EscenarioAuth(
            SideProjectPersonal: false,
            ControlaEntraId: true,
            ProyectoEnProduccion: true));
        Assert.Equal(MetodoAuth.Oidc, r.Metodo);
    }

    [Fact]
    public void Auditoria_Con_Entra_Recomienda_Oidc()
    {
        var r = MetodoAuthRecomendador.Recomendar(new EscenarioAuth(
            SideProjectPersonal: false,
            ControlaEntraId: true,
            AuditoriaRequerida: true));
        Assert.Equal(MetodoAuth.Oidc, r.Metodo);
    }

    [Fact]
    public void Multi_Environment_Con_Entra_Recomienda_Oidc()
    {
        var r = MetodoAuthRecomendador.Recomendar(new EscenarioAuth(
            SideProjectPersonal: false,
            ControlaEntraId: true,
            MultiEnvironment: true));
        Assert.Equal(MetodoAuth.Oidc, r.Metodo);
    }

    [Fact]
    public void Equipo_Grande_Sin_Entra_No_Es_Oidc()
    {
        // Cae al caso intermedio: Environment + secret + reviewers.
        var r = MetodoAuthRecomendador.Recomendar(new EscenarioAuth(
            SideProjectPersonal: false,
            ControlaEntraId: false,
            EquipoGrande: true,
            ProyectoEnProduccion: true));
        Assert.Equal(MetodoAuth.PublishProfile, r.Metodo);
    }

    [Fact]
    public void Recomendacion_Incluye_Razones_Y_Riesgos_No_Vacios()
    {
        var r = MetodoAuthRecomendador.Recomendar(new EscenarioAuth(
            SideProjectPersonal: true));
        Assert.NotEmpty(r.Razones);
        Assert.NotEmpty(r.Riesgos);
    }

    [Fact]
    public void Oidc_Menciona_Federated_Credentials_En_Las_Razones()
    {
        var r = MetodoAuthRecomendador.Recomendar(new EscenarioAuth(
            SideProjectPersonal: false,
            ControlaEntraId: true,
            ProyectoEnProduccion: true));
        Assert.Contains(r.Razones, s =>
            s.Contains("Federated", StringComparison.OrdinalIgnoreCase));
    }
}
