namespace Practica.GhActions.Demo.Api.GhActions;

public sealed record PlanPracticaGhActions(
    AnalisisPublishProfile? Profile,
    WorkflowGha Workflow,
    RecomendacionAuth Recomendacion,
    IReadOnlyList<string> Checklist);

// Compone PublishProfileParser + WorkflowBuilder + MetodoAuthRecomendador
// en el plan + checklist de la práctica. Servicio inyectable (seam del
// test DI — lección M03-S3.4 / patrón M06-M08).
public interface IPracticaGhActionsPlanner
{
    PlanPracticaGhActions Planificar(
        string? publishProfileXml,
        OpcionesWorkflow opciones,
        EscenarioAuth escenarioAuth);
}

public sealed class PracticaGhActionsPlanner : IPracticaGhActionsPlanner
{
    public PlanPracticaGhActions Planificar(
        string? publishProfileXml,
        OpcionesWorkflow opciones,
        EscenarioAuth escenarioAuth)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        ArgumentNullException.ThrowIfNull(escenarioAuth);

        var profile = !string.IsNullOrWhiteSpace(publishProfileXml)
            ? PublishProfileParser.Parsear(publishProfileXml)
            : null;

        var workflow = WorkflowBuilder.Construir(opciones);
        var recomendacion = MetodoAuthRecomendador.Recomendar(escenarioAuth);

        return new PlanPracticaGhActions(
            Profile: profile,
            Workflow: workflow,
            Recomendacion: recomendacion,
            // Slide 2/8/10/11/16/18 — checklist de la práctica.
            Checklist:
            [
                "Web App F1 creada o reutilizada (slide 4)",
                "Repo de GitHub creado con `gh repo create` o por web (slide 6)",
                "Publish profile descargado con `az webapp deployment list-publishing-profiles --xml` (slide 7)",
                "Secret `AZURE_WEBAPP_PUBLISH_PROFILE` creado en GitHub (slide 8)",
                "`publish-profile.xml` local borrado tras crear el secret (slide 8)",
                "Workflow en `.github/workflows/deploy.yml` (slide 9)",
                "`AZURE_WEBAPP_NAME` apunta a la app real (sin placeholder)",
                "Push a main dispara el workflow (slide 10)",
                "Deploy verde en GitHub Actions y app responde con el código nuevo (slide 10/11)",
                "Smoke test contra la URL pública pasa (slide 12)",
                "Rotar publish credentials cada 90 días (slide 18)",
                "Cleanup: `az group delete` + `gh repo delete` + `gh secret delete` (slide 16)",
            ]);
    }
}
