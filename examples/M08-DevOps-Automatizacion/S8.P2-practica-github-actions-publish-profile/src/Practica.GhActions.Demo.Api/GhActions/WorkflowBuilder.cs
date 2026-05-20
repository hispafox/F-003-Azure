namespace Practica.GhActions.Demo.Api.GhActions;

public sealed record JobWorkflow(
    string Nombre,
    string? Necesita,
    IReadOnlyList<string> Steps);

public sealed record WorkflowGha(
    string Nombre,
    IReadOnlyList<string> Triggers,
    IReadOnlyList<JobWorkflow> Jobs,
    string? Environment);

public sealed record OpcionesWorkflow(
    string AppName = "<CAMBIAD_POR_VUESTRO_APP_NAME>",
    string DotnetVersion = "10.0.x",
    bool IncluirTests = false,
    bool SoloEnTags = false,
    bool SmokeAlFinal = false,
    bool EnvironmentProduccion = false);

// Slides 9, 14, 15, 18 — generador de la estructura del workflow GHA.
// Devuelve el árbol lógico (jobs + steps), no el YAML literal (el alumno
// escribe el YAML; aquí validamos que la cabeza tiene claros los
// triggers, dependencias entre jobs y la cadena de steps).
public static class WorkflowBuilder
{
    public static WorkflowGha Construir(OpcionesWorkflow o)
    {
        ArgumentNullException.ThrowIfNull(o);

        var triggers = new List<string>();
        if (o.SoloEnTags)
            triggers.Add("push.tags: ['v*']  (solo tags v1.0, v2.0, ...)");
        else
            triggers.Add("push.branches: [main]");
        triggers.Add("workflow_dispatch  (manual desde la UI)");

        var jobs = new List<JobWorkflow>();

        if (o.IncluirTests)
        {
            jobs.Add(new JobWorkflow(
                Nombre: "build-test",
                Necesita: null,
                Steps:
                [
                    "actions/checkout@v4",
                    $"actions/setup-dotnet@v4 (version: {o.DotnetVersion})",
                    "dotnet restore",
                    "dotnet build --no-restore --configuration Release",
                    "dotnet test --no-build --configuration Release",
                ]));

            jobs.Add(new JobWorkflow(
                Nombre: "deploy",
                Necesita: "build-test",
                Steps: PasosDeploy(o)));
        }
        else
        {
            jobs.Add(new JobWorkflow(
                Nombre: "build-and-deploy",
                Necesita: null,
                Steps:
                [
                    "actions/checkout@v4",
                    $"actions/setup-dotnet@v4 (version: {o.DotnetVersion})",
                    "dotnet restore",
                    "dotnet build --no-restore --configuration Release",
                    "dotnet publish -c Release -o ./publish --no-build",
                    ..PasosDeploy(o).Where(s =>
                        !s.Contains("dotnet publish", StringComparison.Ordinal)),
                ]));
        }

        return new WorkflowGha(
            Nombre: "Deploy to Azure Web App",
            Triggers: triggers,
            Jobs: jobs,
            Environment: o.EnvironmentProduccion ? "production" : null);
    }

    private static IReadOnlyList<string> PasosDeploy(OpcionesWorkflow o)
    {
        var pasos = new List<string>
        {
            "actions/checkout@v4",
            $"actions/setup-dotnet@v4 (version: {o.DotnetVersion})",
            "dotnet publish -c Release -o ./publish",
            $"azure/webapps-deploy@v3 (app-name: {o.AppName}, " +
                "publish-profile: secrets.AZURE_WEBAPP_PUBLISH_PROFILE, " +
                "package: ./publish)",
        };

        if (o.SmokeAlFinal)
            pasos.Add(
                "Smoke test (slide 12): sleep 30 + curl health + curl /, " +
                "fallar si HTTP != 200 o latencia > umbral.");

        return pasos;
    }
}
