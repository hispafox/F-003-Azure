namespace Devops.Repos.Demo.Api.Repos;

public enum EstrategiaRepo { Monorepo, MultiRepo }

public sealed record EscenarioEquipo(
    int Personas,
    int Servicios,
    bool MuchaSharedCode = false,
    bool CiCdIndependiente = true,    // slide 3: pipeline por repo
    bool EquiposIndependientes = false);

public sealed record RecomendacionRepo(
    EstrategiaRepo Estrategia, IReadOnlyList<string> Razones);

// Slide 3 — monorepo vs multi-repo según el equipo. Lógica pura.
public static class RepoStrategyAdvisor
{
    public static RecomendacionRepo Recomendar(EscenarioEquipo e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Personas);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Servicios);

        var aMonorepo = new List<string>();
        var aMultiRepo = new List<string>();

        if (e.MuchaSharedCode)
            aMonorepo.Add("Mucho código compartido entre proyectos → monorepo lo facilita (slide 3).");
        if (e.Personas <= 4 && e.Servicios <= 3)
            aMonorepo.Add("Equipo pequeño con pocos servicios → setup más simple (slide 3).");

        if (e.EquiposIndependientes)
            aMultiRepo.Add("Equipos independientes → multi-repo evita acoplamiento (slide 3).");
        if (e.CiCdIndependiente)
            aMultiRepo.Add("CI/CD independiente por servicio → multi-repo simplifica los pipelines (slide 3).");
        if (e.Servicios >= 4)
            aMultiRepo.Add($"{e.Servicios} servicios distintos → un repo por servicio es más limpio (slide 3).");
        if (e.Personas is >= 5 and <= 10)
            aMultiRepo.Add("Equipo 5-10 personas → multi-repo es la recomendación para vuestro tamaño (slide 3).");

        bool multi = aMultiRepo.Count > aMonorepo.Count;
        return multi
            ? new RecomendacionRepo(EstrategiaRepo.MultiRepo, aMultiRepo)
            : new RecomendacionRepo(EstrategiaRepo.Monorepo,
                aMonorepo.Count > 0 ? aMonorepo
                    : ["Sin señales fuertes: empezar con monorepo y dividir si crece."]);
    }
}
