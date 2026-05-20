namespace Plataforma.Demo.Api.Plataforma;

// Nota: usamos `TipoPlataforma` (no `Plataforma`) para no chocar con
// el segmento de namespace `Plataforma` (el compilador lo resuelve
// primero como namespace y rompe la expresión Plataforma.AzureDevOps).
public enum TipoPlataforma { AzureDevOps, GitHubActions, Hybrid }

public sealed record EscenarioPlataforma(
    bool YaUsasAdo = false,
    bool OpenSource = false,
    bool NecesitaBoardsCompletos = false,
    bool QuiereDependabotCodeQL = false,
    bool EquipoDistribuidoYaEnGitHub = false,
    bool NecesitaTestPlans = false,
    bool OnPremises = false,
    int Personas = 6);

public sealed record RecomendacionPlataforma(
    TipoPlataforma Plataforma, IReadOnlyList<string> Razones);

// Slides 4, 5, 8, 11, 19 — tabla de decisión ADO vs GitHub vs Hybrid.
// Lógica pura: cuenta señales y elige.
public static class PlatformAdvisor
{
    public static RecomendacionPlataforma Recomendar(EscenarioPlataforma e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(e.Personas);

        var aAdo = new List<string>();
        var aGh = new List<string>();

        if (e.YaUsasAdo)
            aAdo.Add("Ya usas Azure DevOps y funciona (slide 4): no hay beneficio para migrar.");
        if (e.NecesitaBoardsCompletos)
            aAdo.Add("Boards completos (sprints + velocity + burndown) → ADO es superior (slide 11).");
        if (e.NecesitaTestPlans)
            aAdo.Add("Test Plans integrados → exclusivo de ADO (slide 4).");
        if (e.OnPremises)
            aAdo.Add("On-premises → Azure DevOps Server (slide 4).");

        if (e.OpenSource)
            aGh.Add("Proyecto open source o mixto → GitHub (slide 5).");
        if (e.QuiereDependabotCodeQL)
            aGh.Add("Dependabot + CodeQL nativos → GitHub (slide 5/9).");
        if (e.EquipoDistribuidoYaEnGitHub)
            aGh.Add("Equipo distribuido ya en GitHub → coherencia (slide 5).");

        // Slide 8 — híbrido: el caso "tengo Boards en ADO pero quiero
        // Copilot/CodeQL en GitHub" → repos en GitHub + Pipelines/Boards
        // en ADO. Requiere AL MENOS una señal de cada lado.
        if (aAdo.Count > 0 && aGh.Count > 0)
        {
            var razones = new List<string>
            {
                "Tienes señales fuertes en ambos lados (slide 8): repos en GitHub + Pipelines/Boards en ADO.",
            };
            razones.AddRange(aAdo);
            razones.AddRange(aGh);
            return new RecomendacionPlataforma(TipoPlataforma.Hybrid, razones);
        }

        if (aAdo.Count > aGh.Count)
            return new RecomendacionPlataforma(TipoPlataforma.AzureDevOps,
                aAdo.Count > 0 ? aAdo
                    : ["Sin señales: empezar con Azure DevOps (mejor para Boards de sprint, slide 19)."]);

        if (aGh.Count > aAdo.Count)
            return new RecomendacionPlataforma(TipoPlataforma.GitHubActions,
                aGh.Count > 0 ? aGh
                    : ["Sin señales: GitHub Actions (marketplace y comunidad, slide 19)."]);

        // Empate sin Boards / sin OSS — recomendación equipo 6-10 personas (slide 19).
        return new RecomendacionPlataforma(TipoPlataforma.AzureDevOps,
            ["Equipo pequeño/mediano con Azure: ADO es más barato y trae Boards (slide 12/19)."]);
    }
}
