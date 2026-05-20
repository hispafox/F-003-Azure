namespace WizardMsix.Demo.Api.Wizard;

public sealed record PlanWizard(
    FlujoEmpaquetado FlujoRecomendado,
    IReadOnlyList<string> RazonesFlujo,
    IReadOnlyList<ComandoCli> ComandosEquivalentes,
    IReadOnlyList<string> LimitacionesWizard,
    IReadOnlyList<string> Checklist);

// Compone WizardComandosExpander + WizardVsCliAdvisor +
// MsixErrorTroubleshooter en un plan + checklist. Servicio inyectable
// (seam del test DI — lección M03-S3.4).
public interface IPracticaMsixWizardPlanner
{
    PlanWizard Planificar(
        ContextoEmpaquetado contexto, ParametrosWizard parametros);
}

public sealed class PracticaMsixWizardPlanner : IPracticaMsixWizardPlanner
{
    public PlanWizard Planificar(
        ContextoEmpaquetado contexto, ParametrosWizard parametros)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(parametros);

        var flujo = WizardVsCliAdvisor.Recomendar(contexto);
        var comandos = WizardComandosExpander.Expandir(parametros);

        return new PlanWizard(
            flujo.Flujo,
            flujo.Razones,
            comandos,
            WizardVsCliAdvisor.LimitacionesWizard,
            // Slide 19 — checklist de la práctica.
            Checklist:
            [
                "WPF mínimo + Packaging Project creados (slide 4/5)",
                "Cert self-signed generado por el wizard (slide 7)",
                "Build Release/x64 + Create App Packages → Sideloading (slide 7)",
                "Certificado importado en Cert:\\LocalMachine\\TrustedPeople (slide 8)",
                "Add-AppPackage instala sin error 0x80073CFD (slide 9/16.1)",
                "App aparece en Start Menu y arranca (slide 9)",
                "Inspeccionar lo que generó el wizard: .msix + .cer + Install.ps1 (slide 10)",
                "Smoke tests automatizados pasan (slide 11)",
                "Cambio + re-empaquetar v1.0.1.0 (slide 12)",
                "Cleanup: Remove-AppPackage + Remove-Item del cert (slide 14)",
                "Entender lo que el wizard hizo por debajo: makeappx + signtool (slide 15)",
            ]);
    }

    // Atajo para el endpoint de troubleshooting.
    public DiagnosticoError? Diagnosticar(string codigoOMensaje) =>
        MsixErrorTroubleshooter.Diagnosticar(codigoOMensaje);
}
