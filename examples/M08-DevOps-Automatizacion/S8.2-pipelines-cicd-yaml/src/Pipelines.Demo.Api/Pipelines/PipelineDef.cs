namespace Pipelines.Demo.Api.Pipelines;

// Slide 3 — la jerarquía mínima de un pipeline YAML que necesitamos
// para validar y razonar sobre él. Plana y testeable.

public sealed record TriggerDef(
    bool Habilitado,                          // false si trigger: none
    IReadOnlyList<string> Branches,
    IReadOnlyList<string> Paths);

public sealed record ScheduleDef(string Cron, IReadOnlyList<string> Branches);

public sealed record StepDef(
    string Kind,                              // task | script | publish | checkout | download | publish | deploy
    string? Display,
    string? Body);                            // referencia (task name) o contenido (script)

public sealed record JobDef(
    string Name,
    bool IsDeployment,                        // slide 5/8 — deployment job
    string? Environment,                      // slide 8 — gate de aprobación
    IReadOnlyList<StepDef> Steps);

public sealed record StageDef(
    string Name,
    IReadOnlyList<string> DependsOn,          // slide 5
    string? Condition,                        // slide 13
    IReadOnlyList<JobDef> Jobs);

public sealed record PipelineDef(
    TriggerDef? Trigger,
    TriggerDef? Pr,
    IReadOnlyList<ScheduleDef> Schedules,
    string? PoolVmImage,                      // slide 3
    IReadOnlyList<string> VariableGroups,     // slide 9
    IReadOnlyList<StageDef> Stages);
