using Devops.Repos.Demo.Api.Repos;

namespace Devops.Repos.Demo.Api.Tests;

// CAPA 1 — monorepo vs multi-repo (slide 3).
[Trait("Category", "Unit")]
public class Unit_RepoStrategyTests
{
    [Fact]
    public void Equipo_Mediano_5_10_Con_Varios_Servicios_Es_MultiRepo()
    {
        var r = RepoStrategyAdvisor.Recomendar(
            new EscenarioEquipo(Personas: 7, Servicios: 5,
                CiCdIndependiente: true));
        Assert.Equal(EstrategiaRepo.MultiRepo, r.Estrategia);
        Assert.Contains(r.Razones, x => x.Contains("CI/CD"));
    }

    [Fact]
    public void Equipo_Pequeno_Con_Shared_Code_Es_Monorepo()
    {
        var r = RepoStrategyAdvisor.Recomendar(
            new EscenarioEquipo(Personas: 3, Servicios: 2,
                MuchaSharedCode: true, CiCdIndependiente: false));
        Assert.Equal(EstrategiaRepo.Monorepo, r.Estrategia);
        Assert.Contains(r.Razones, x => x.Contains("compartido"));
    }

    [Fact]
    public void Equipos_Independientes_Con_CiCd_Es_MultiRepo()
        => Assert.Equal(EstrategiaRepo.MultiRepo,
            RepoStrategyAdvisor.Recomendar(
                new EscenarioEquipo(Personas: 4, Servicios: 2,
                    EquiposIndependientes: true, CiCdIndependiente: true))
                .Estrategia);

    [Fact]
    public void Personas_Cero_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            RepoStrategyAdvisor.Recomendar(new EscenarioEquipo(0, 1)));

    [Fact]
    public void Servicios_Cero_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            RepoStrategyAdvisor.Recomendar(new EscenarioEquipo(1, 0)));
}
