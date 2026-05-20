namespace Iac.Bicep.Demo.Api.Iac;

public enum HerramientaIac { Bicep, ArmTemplates, Terraform }

public sealed record FeatureRow(
    string Feature, string Arm, string Bicep, string Terraform);

public sealed record RecomendacionIac(
    HerramientaIac Herramienta, IReadOnlyList<string> Razones);

public sealed record EscenarioIac(
    bool SoloAzure = true,
    bool MultiCloud = false,
    bool EquipoYaUsaTerraform = false,
    bool LegacyArmJson = false);

// Slides 3, 19, 22 — comparativa Bicep / ARM / Terraform y
// recomendación por escenario. Lógica pura.
public static class ToolingComparison
{
    // Slide 3 — la tabla canónica.
    public static IReadOnlyList<FeatureRow> Comparativa { get; } =
    [
        new("Formato", "JSON (verbose)", "DSL legible", "HCL legible"),
        new("Proveedor", "Solo Azure", "Solo Azure", "Multi-cloud"),
        new("State management", "No (Azure es state)", "No (Azure es state)", "terraform.tfstate"),
        new("Módulos", "Linked templates (complejo)", "Módulos nativos", "Módulos maduros"),
        new("Learning curve", "Alta (JSON)", "Baja", "Media"),
        new("Microsoft support", "Legacy", "Recomendado", "3rd-party"),
        new("What-if preview", "Sí", "Sí (slide 14)", "terraform plan"),
        new("VS Code tooling", "Limitado", "Bicep extension", "Terraform extension"),
    ];

    public static RecomendacionIac Recomendar(EscenarioIac e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (e.MultiCloud || e.EquipoYaUsaTerraform)
            return new(HerramientaIac.Terraform,
                [
                    e.MultiCloud
                        ? "Multi-cloud → Terraform (slide 3)."
                        : "Equipo ya en Terraform → mantener (slide 3).",
                    "State management aporta visibilidad cross-cloud.",
                ]);

        if (e.LegacyArmJson && !e.SoloAzure)
            return new(HerramientaIac.ArmTemplates,
                ["Mantenimiento de plantillas ARM legacy: no migrar 'por modernizar' (slide 3/19)."]);

        return new(HerramientaIac.Bicep,
            [
                "Solo Azure + sin estado de Terraform que mantener → Bicep (slide 3).",
                "Sintaxis legible (DSL), módulos nativos, VS Code extension (slide 13).",
                "Soporte oficial Microsoft + alineado con Azure (slide 3).",
                "Migración ARM → Bicep con `az bicep decompile` (slide 21).",
            ]);
    }
}
