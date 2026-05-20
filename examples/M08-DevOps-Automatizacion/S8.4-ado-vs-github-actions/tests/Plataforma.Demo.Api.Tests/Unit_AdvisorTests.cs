using Plataforma.Demo.Api.Plataforma;

namespace Plataforma.Demo.Api.Tests;

// CAPA 1 — decisión ADO vs GitHub vs Híbrido (slides 4, 5, 8, 11, 19).
[Trait("Category", "Unit")]
public class Unit_AdvisorTests
{
    [Fact]
    public void Ya_Usas_Ado_Mas_Boards_Es_AzureDevOps()
    {
        var r = PlatformAdvisor.Recomendar(new EscenarioPlataforma(
            YaUsasAdo: true, NecesitaBoardsCompletos: true, Personas: 8));
        Assert.Equal(TipoPlataforma.AzureDevOps, r.Plataforma);
        Assert.Contains(r.Razones, x => x.Contains("Boards"));
    }

    [Fact]
    public void Open_Source_Mas_CodeQL_Es_GitHub()
    {
        var r = PlatformAdvisor.Recomendar(new EscenarioPlataforma(
            OpenSource: true, QuiereDependabotCodeQL: true, Personas: 6));
        Assert.Equal(TipoPlataforma.GitHubActions, r.Plataforma);
        Assert.Contains(r.Razones, x => x.Contains("open source"));
    }

    [Fact]
    public void Senales_En_Ambos_Lados_Es_Hibrido_Slide_8()
    {
        var r = PlatformAdvisor.Recomendar(new EscenarioPlataforma(
            YaUsasAdo: true,
            NecesitaBoardsCompletos: true,
            QuiereDependabotCodeQL: true,
            Personas: 8));
        Assert.Equal(TipoPlataforma.Hybrid, r.Plataforma);
        Assert.Contains(r.Razones, x => x.Contains("repos en GitHub"));
    }

    [Fact]
    public void On_Premises_Es_AzureDevOps()
        => Assert.Equal(TipoPlataforma.AzureDevOps,
            PlatformAdvisor.Recomendar(new EscenarioPlataforma(
                OnPremises: true, Personas: 10)).Plataforma);

    [Fact]
    public void Sin_Senales_Cae_En_AzureDevOps_Por_Defecto_Slide_19()
        => Assert.Equal(TipoPlataforma.AzureDevOps,
            PlatformAdvisor.Recomendar(new EscenarioPlataforma(Personas: 5)).Plataforma);

    [Fact]
    public void Personas_Cero_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            PlatformAdvisor.Recomendar(new EscenarioPlataforma(Personas: 0)));
}
