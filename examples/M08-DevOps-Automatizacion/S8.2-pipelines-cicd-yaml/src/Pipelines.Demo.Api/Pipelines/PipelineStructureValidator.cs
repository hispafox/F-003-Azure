namespace Pipelines.Demo.Api.Pipelines;

public sealed record ResultadoValidacion(
    bool Valido,
    IReadOnlyList<string> Errores,
    IReadOnlyList<string> Avisos);

// Slides 3, 5, 6, 7, 8, 13 — valida la estructura del pipeline:
// - tiene stages → jobs → steps no vacíos
// - dependsOn referencia stages existentes
// - jobs que despliegan usan `deployment:` y especifican environment
// - environments de producción tienen aprobación (aviso si "prod"
//   aparece sin que el job sea deployment)
// - el stage de Build (o equivalente) tiene un step de test
public static class PipelineStructureValidator
{
    private static readonly HashSet<string> ProdAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "prod", "production", "produccion",
    };

    public static ResultadoValidacion Validar(PipelineDef p)
    {
        ArgumentNullException.ThrowIfNull(p);
        var errores = new List<string>();
        var avisos = new List<string>();

        if (p.Stages.Count == 0)
            errores.Add("El pipeline no tiene `stages` (slide 5).");

        var nombresStages = p.Stages.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var stage in p.Stages)
        {
            if (string.IsNullOrWhiteSpace(stage.Name))
                errores.Add("Hay un stage sin `stage:` (nombre) (slide 5).");

            foreach (var dep in stage.DependsOn)
                if (!nombresStages.Contains(dep))
                    errores.Add($"Stage '{stage.Name}' depende de '{dep}' que no existe (slide 5).");

            if (stage.Jobs.Count == 0)
                errores.Add($"Stage '{stage.Name}' no tiene `jobs` (slide 5).");

            foreach (var job in stage.Jobs)
            {
                if (job.Steps.Count == 0)
                    errores.Add($"Job '{job.Name}' en stage '{stage.Name}' no tiene `steps` (slide 5).");

                bool nombreEsDeProd = !string.IsNullOrWhiteSpace(job.Environment) &&
                    ProdAliases.Any(a => job.Environment.Contains(a, StringComparison.OrdinalIgnoreCase));

                if (nombreEsDeProd && !job.IsDeployment)
                    avisos.Add($"Job '{job.Name}' apunta al entorno de producción '{job.Environment}' " +
                        "pero no es `deployment:` — no podrá usar aprobaciones (slide 8).");

                if (job.IsDeployment && string.IsNullOrWhiteSpace(job.Environment))
                    errores.Add($"Deployment job '{job.Name}' sin `environment:` (slide 8).");
            }
        }

        // Slide 6 — el primer stage típicamente es Build con tests.
        var build = p.Stages.FirstOrDefault();
        if (build is not null && !TieneStepDeTest(build))
            avisos.Add($"Stage '{build.Name}' no parece tener un step de tests " +
                "(dotnet test / VSTest / PublishTestResults) — slide 6.");

        return new ResultadoValidacion(
            Valido: errores.Count == 0,
            Errores: errores,
            Avisos: avisos);
    }

    private static bool TieneStepDeTest(StageDef stage)
    {
        foreach (var job in stage.Jobs)
            foreach (var step in job.Steps)
            {
                string body = step.Body ?? "";
                string display = step.Display ?? "";
                if (body.Contains("dotnet test", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("VSTest", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("PublishTestResults", StringComparison.OrdinalIgnoreCase) ||
                    display.Contains("test", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        return false;
    }
}
