namespace Practica.Pipeline.Demo.Api.Pipeline;

public enum Plataforma { AzureDevOps, GitHubActions }

public sealed record EtapaPipeline(
    string Nombre,
    string Slide,
    IReadOnlyList<string> Pasos,
    bool RequiereAprobacion = false);

public sealed record EsqueletoPipeline(
    Plataforma Plataforma,
    IReadOnlyList<EtapaPipeline> Etapas);

public sealed record OpcionesPipeline(
    Plataforma Plataforma = Plataforma.AzureDevOps,
    bool UsarOidc = true,
    bool AprobacionEnProduccion = true,
    bool AutoRollbackEnFallo = true,
    bool NotificarTeamsEnFallo = false,
    bool EscanearVulnerables = false);

// Slides 4, 5, 6, 10, 17, 18 — esqueleto del pipeline CI/CD canónico.
// No genera YAML literal (eso es lo que el alumno escribe en clase);
// construye la **secuencia de etapas con sus pasos clave** para
// validar que la mental model está completa. Funciona igual para ADO
// y GitHub Actions adaptando los nombres de tareas.
public static class PipelineStageBuilder
{
    public static EsqueletoPipeline Construir(OpcionesPipeline opc)
    {
        ArgumentNullException.ThrowIfNull(opc);

        var etapas = new List<EtapaPipeline>
        {
            new(
                Nombre: "Build",
                Slide: "4",
                Pasos:
                [
                    PasoTarea(opc, "UseDotNet@2 (.NET 10)", "actions/setup-dotnet@v4"),
                    "dotnet restore",
                    "dotnet build -c Release --no-restore",
                    "dotnet test --collect:\"XPlat Code Coverage\" --logger trx",
                    PasoTarea(opc, "PublishTestResults@2", "actions/upload-artifact@v4"),
                    "dotnet publish -c Release -o $(out)/app",
                    PasoTarea(opc, "ArchiveFiles@2 → deploy.zip", "zip -r app.zip ./out/app"),
                    PasoTarea(opc, "publish artifact 'webapp'", "actions/upload-artifact@v4 (name: app)"),
                ]),

            new(
                Nombre: "DeployStaging",
                Slide: "5",
                Pasos:
                [
                    PasoTarea(opc,
                        opc.UsarOidc
                            ? "AzureWebApp@1 con Workload Identity Federation"
                            : "AzureWebApp@1 con Service Principal + secret",
                        opc.UsarOidc
                            ? "azure/login@v2 + azure/webapps-deploy@v3 (OIDC)"
                            : "azure/webapps-deploy@v3 con creds en secret"),
                    "Deploy package al slot 'staging'",
                    "Smoke test contra https://<app>-staging.azurewebsites.net/health (HTTP 200)",
                ]),

            new(
                Nombre: "SwapProduction",
                Slide: "6",
                RequiereAprobacion: opc.AprobacionEnProduccion,
                Pasos: BuildSwapSteps(opc)),
        };

        if (opc.EscanearVulnerables)
            etapas.Insert(1, new EtapaPipeline(
                Nombre: "SecurityScan",
                Slide: "15",
                Pasos:
                [
                    "dotnet list package --vulnerable --include-transitive",
                    "Buscar passwords/secret/apikey en *.cs/*.json (excluir Development)",
                    "Fallar si encuentra algo en main",
                ]));

        if (opc.NotificarTeamsEnFallo)
            etapas.Add(new EtapaPipeline(
                Nombre: "NotifyOnFailure",
                Slide: "9",
                Pasos:
                [
                    PasoTarea(opc,
                        "MSTeamsNotification@4 (condition: failed())",
                        "curl POST $TEAMS_WEBHOOK con job.status == failure"),
                    "Incluir BuildNumber + Branch + Commit + link a logs",
                ]));

        return new EsqueletoPipeline(opc.Plataforma, etapas);
    }

    private static IReadOnlyList<string> BuildSwapSteps(OpcionesPipeline opc)
    {
        var pasos = new List<string>
        {
            PasoTarea(opc,
                "AzureAppServiceManage@0 — Swap Slots staging→production",
                "az webapp deployment slot swap --slot staging --target-slot production"),
            "Post-swap health check contra https://<app>.azurewebsites.net/health",
        };

        if (opc.AutoRollbackEnFallo)
            pasos.Add(
                "Auto-rollback (condition: failed()): swap inverso production→staging + " +
                "exit 1 para marcar el run en rojo (slide 10).");

        return pasos;
    }

    private static string PasoTarea(OpcionesPipeline opc, string adoTask, string ghaStep)
        => opc.Plataforma == Plataforma.AzureDevOps ? adoTask : ghaStep;
}
