namespace Iac.Bicep.Demo.Api.Iac;

public sealed record PlanIac(
    RecomendacionIac Herramienta,
    ValidacionBicep ValidacionDelArchivo,
    ReporteWhatIf? WhatIf,
    IReadOnlyList<string> Checklist);

// Compone ToolingComparison + BicepFileValidator + WhatIfDiffParser
// en el plan + checklist del entregable. Servicio inyectable (seam
// del test DI — lección M03-S3.4).
public interface IIacPlanner
{
    PlanIac Planificar(
        EscenarioIac escenario,
        string? bicepTexto,
        string? whatIfOutput);
}

public sealed class IacPlanner : IIacPlanner
{
    public PlanIac Planificar(
        EscenarioIac escenario,
        string? bicepTexto,
        string? whatIfOutput)
    {
        ArgumentNullException.ThrowIfNull(escenario);

        var herramienta = ToolingComparison.Recomendar(escenario);
        var val = !string.IsNullOrWhiteSpace(bicepTexto)
            ? BicepFileValidator.Validar(bicepTexto)
            : new ValidacionBicep(true, [], []);
        var whatIf = !string.IsNullOrWhiteSpace(whatIfOutput)
            ? WhatIfDiffParser.Parsear(whatIfOutput)
            : null;

        return new PlanIac(
            herramienta,
            val,
            whatIf,
            // Slides 2, 5, 6, 7, 11, 12, 14, 18, 19, 22 — checklist.
            Checklist:
            [
                "Infra en archivos .bicep versionados en Git (slide 2/19)",
                "`az deployment group validate` antes de aplicar (slide 5)",
                "`az deployment group what-if` muestra los cambios — revisar 'Delete' (slide 5/14)",
                "Parámetros con secreto SIEMPRE @secure() (slide 6/11)",
                "Secretos vía Key Vault Reference o `existing` + Managed Identity (slide 11)",
                "Módulos por dominio: app-service/cosmos/storage/key-vault (slide 7)",
                "Un archivo de parámetros por entorno (dev/staging/prod) (slide 12)",
                "Pipeline what-if obligatorio en stage previo al deploy (slide 19)",
                "Deployment stacks para gestionar lifecycle (slide 18/23)",
                "Azure Verified Modules (AVM) cuando aplique (slide 22)",
            ]);
    }
}
