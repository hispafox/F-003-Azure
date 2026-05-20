using YamlDotNet.RepresentationModel;

namespace Pipelines.Demo.Api.Pipelines;

// Slide 3 — parser del `azure-pipelines.yml` a nuestro DTO `PipelineDef`.
// Solo extrae lo que necesitamos para validar: triggers, pool, stages,
// jobs (incluyendo deployment), steps, dependsOn, condition, env.
// Lógica pura (no hace IO; recibe el YAML como string).
public static class PipelineYamlParser
{
    public static PipelineDef Parsear(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(yaml));
        }
        catch (Exception ex)
        {
            throw new FormatException("YAML inválido.", ex);
        }

        if (stream.Documents.Count == 0 ||
            stream.Documents[0].RootNode is not YamlMappingNode root)
            throw new FormatException("Se esperaba un mapping en la raíz del pipeline.");

        return new PipelineDef(
            Trigger: ParsearTrigger(root, "trigger"),
            Pr: ParsearTrigger(root, "pr"),
            Schedules: ParsearSchedules(root),
            PoolVmImage: ParsearPoolVmImage(root),
            VariableGroups: ParsearVariableGroups(root),
            Stages: ParsearStages(root));
    }

    private static TriggerDef? ParsearTrigger(YamlMappingNode root, string clave)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode(clave), out var node))
            return null;

        // trigger: none → deshabilitado
        if (node is YamlScalarNode s && string.Equals(s.Value, "none", StringComparison.Ordinal))
            return new TriggerDef(false, [], []);

        // trigger: [main] (lista) → branches
        if (node is YamlSequenceNode seq)
            return new TriggerDef(true, [.. seq.Children.OfType<YamlScalarNode>().Select(x => x.Value ?? "")], []);

        if (node is not YamlMappingNode m) return new TriggerDef(true, [], []);

        var branches = SubLista(m, "branches", "include");
        var paths = SubLista(m, "paths", "include");
        return new TriggerDef(true, branches, paths);
    }

    private static IReadOnlyList<ScheduleDef> ParsearSchedules(YamlMappingNode root)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode("schedules"), out var node) ||
            node is not YamlSequenceNode seq)
            return [];

        return [..
            seq.Children.OfType<YamlMappingNode>().Select(item =>
                new ScheduleDef(
                    Cron: (item.Children.TryGetValue(new YamlScalarNode("cron"), out var c)
                            && c is YamlScalarNode cs ? cs.Value : null) ?? "",
                    Branches: SubLista(item, "branches", "include")))];
    }

    private static string? ParsearPoolVmImage(YamlMappingNode root)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode("pool"), out var node))
            return null;
        if (node is YamlMappingNode m &&
            m.Children.TryGetValue(new YamlScalarNode("vmImage"), out var img) &&
            img is YamlScalarNode imgS)
            return imgS.Value;
        return null;
    }

    private static IReadOnlyList<string> ParsearVariableGroups(YamlMappingNode root)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode("variables"), out var node) ||
            node is not YamlSequenceNode seq)
            return [];

        return [..
            seq.Children.OfType<YamlMappingNode>()
                .Where(m => m.Children.ContainsKey(new YamlScalarNode("group")))
                .Select(m => ((YamlScalarNode)m.Children[new YamlScalarNode("group")]).Value ?? "")];
    }

    private static IReadOnlyList<StageDef> ParsearStages(YamlMappingNode root)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode("stages"), out var node) ||
            node is not YamlSequenceNode seq)
            return [];

        return [.. seq.Children.OfType<YamlMappingNode>().Select(ParsearStage)];
    }

    private static StageDef ParsearStage(YamlMappingNode m)
    {
        string name = Escalar(m, "stage") ?? "";
        var dependsOn = ListaOEscalar(m, "dependsOn");
        string? condition = Escalar(m, "condition");
        var jobs = ParsearJobs(m);
        return new StageDef(name, dependsOn, condition, jobs);
    }

    private static IReadOnlyList<JobDef> ParsearJobs(YamlMappingNode stage)
    {
        if (!stage.Children.TryGetValue(new YamlScalarNode("jobs"), out var node) ||
            node is not YamlSequenceNode seq)
            return [];

        return [.. seq.Children.OfType<YamlMappingNode>().Select(ParsearJob)];
    }

    private static JobDef ParsearJob(YamlMappingNode m)
    {
        // Deployment job: tiene clave "deployment" en lugar de "job".
        bool isDeployment = m.Children.ContainsKey(new YamlScalarNode("deployment"));
        string name = isDeployment
            ? Escalar(m, "deployment") ?? ""
            : Escalar(m, "job") ?? "";
        string? env = Escalar(m, "environment");
        var steps = ParsearSteps(m, isDeployment);
        return new JobDef(name, isDeployment, env, steps);
    }

    private static IReadOnlyList<StepDef> ParsearSteps(YamlMappingNode job, bool isDeployment)
    {
        // En deployments los steps viven dentro de strategy.runOnce.deploy.steps.
        if (isDeployment &&
            job.Children.TryGetValue(new YamlScalarNode("strategy"), out var strategy) &&
            strategy is YamlMappingNode stratM &&
            stratM.Children.TryGetValue(new YamlScalarNode("runOnce"), out var ro) &&
            ro is YamlMappingNode roM &&
            roM.Children.TryGetValue(new YamlScalarNode("deploy"), out var deploy) &&
            deploy is YamlMappingNode deployM &&
            deployM.Children.TryGetValue(new YamlScalarNode("steps"), out var s1) &&
            s1 is YamlSequenceNode seq1)
            return [.. seq1.Children.OfType<YamlMappingNode>().Select(ParsearStep)];

        if (job.Children.TryGetValue(new YamlScalarNode("steps"), out var s2) &&
            s2 is YamlSequenceNode seq2)
            return [.. seq2.Children.OfType<YamlMappingNode>().Select(ParsearStep)];

        return [];
    }

    private static StepDef ParsearStep(YamlMappingNode m)
    {
        foreach (var kind in new[] { "task", "script", "publish", "checkout", "download" })
        {
            if (m.Children.TryGetValue(new YamlScalarNode(kind), out var v))
            {
                string? body = v switch
                {
                    YamlScalarNode s => s.Value,
                    YamlMappingNode mm => mm.ToString(),
                    _ => null,
                };
                string? display = Escalar(m, "displayName");
                return new StepDef(kind, display, body);
            }
        }
        return new StepDef("unknown", Escalar(m, "displayName"), null);
    }

    // ---- helpers --------------------------------------------------

    private static string? Escalar(YamlMappingNode m, string clave) =>
        m.Children.TryGetValue(new YamlScalarNode(clave), out var n) &&
        n is YamlScalarNode s ? s.Value : null;

    private static IReadOnlyList<string> ListaOEscalar(YamlMappingNode m, string clave)
    {
        if (!m.Children.TryGetValue(new YamlScalarNode(clave), out var n))
            return [];
        return n switch
        {
            YamlScalarNode s when s.Value is not null => [s.Value],
            YamlSequenceNode seq => [.. seq.Children.OfType<YamlScalarNode>()
                .Select(x => x.Value ?? "")],
            _ => [],
        };
    }

    private static IReadOnlyList<string> SubLista(YamlMappingNode m, string seccion, string subclave)
    {
        if (!m.Children.TryGetValue(new YamlScalarNode(seccion), out var n) ||
            n is not YamlMappingNode sub)
            return [];
        if (!sub.Children.TryGetValue(new YamlScalarNode(subclave), out var lst) ||
            lst is not YamlSequenceNode seq)
            return [];
        return [.. seq.Children.OfType<YamlScalarNode>().Select(x => x.Value ?? "")];
    }
}
