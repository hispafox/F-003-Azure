namespace Pipelines.Demo.Api.Pipelines;

public enum EscenarioTrigger { CiPrincipal, ValidacionPr, NightlyBuild, ManualOnly }

public sealed record RecomendacionTrigger(
    EscenarioTrigger Escenario,
    string Yaml,
    string Razon);

// Slide 4 — recomienda el bloque YAML de `trigger:` / `pr:` /
// `schedules:` según el escenario. Lógica pura: genera texto.
public static class TriggerAdvisor
{
    public static RecomendacionTrigger Recomendar(EscenarioTrigger escenario) => escenario switch
    {
        EscenarioTrigger.CiPrincipal => new(escenario,
            """
            trigger:
              branches:
                include: [main]
              paths:
                include: [src/*, tests/*]
                exclude: ['*.md', docs/*]
            """,
            "CI en push a main; ignora cambios solo de documentación (slide 4)."),

        EscenarioTrigger.ValidacionPr => new(escenario,
            """
            pr:
              branches:
                include: [main]
              paths:
                include: [src/*]
            """,
            "Valida los PRs hacia main antes del merge (slide 4)."),

        EscenarioTrigger.NightlyBuild => new(escenario,
            """
            schedules:
            - cron: '0 2 * * *'
              displayName: 'Nightly Build'
              branches:
                include: [main]
              always: true
            """,
            "Build nocturno aunque no haya cambios (slide 4)."),

        EscenarioTrigger.ManualOnly => new(escenario,
            "trigger: none",
            "Sin trigger automático: el pipeline se lanza manualmente (slide 4)."),

        _ => throw new ArgumentOutOfRangeException(nameof(escenario)),
    };

    // Slide 4 — todos los triggers que se suelen combinar en un repo
    // serio: CI + PR + nightly.
    public static IReadOnlyList<RecomendacionTrigger> RecomendacionEstandar() =>
    [
        Recomendar(EscenarioTrigger.CiPrincipal),
        Recomendar(EscenarioTrigger.ValidacionPr),
        Recomendar(EscenarioTrigger.NightlyBuild),
    ];
}
