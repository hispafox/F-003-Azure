namespace ClaudeCode.Infra.Demo.Api.Infra;

public enum EscenarioInfra
{
    BicepDesdeRequirements,    // slide 2/3/17
    DockerfileMultiStage,      // slide 5
    GhActionsPipeline,         // slide 12/17
    ReverseArmABicep,          // slide 16
    AuditarRecursos,           // slide 15
    RunbookOperaciones,        // slide 11
    ScriptOps,                 // slide 14
}

public sealed record PromptInfra(
    EscenarioInfra Escenario, string Slide, string Texto);

// Slides 2-17 — generador de prompts canónicos para Claude Code en
// los 7 escenarios IaC más comunes. Lógica pura; produce un texto
// listo para pegar al CLI de Claude Code.
public static class InfraPromptBuilder
{
    public static PromptInfra ParaEscenario(EscenarioInfra esc, RequisitosInfra? req = null)
    {
        return esc switch
        {
            EscenarioInfra.BicepDesdeRequirements => new(esc, "2/3/17",
                BicepDesdeRequirements(req)),

            EscenarioInfra.DockerfileMultiStage => new(esc, "5",
                "Crea un Dockerfile multi-stage para esta API .NET 10:\n" +
                "- Stage 1 (build): mcr.microsoft.com/dotnet/sdk:10.0\n" +
                "- Stage 2 (runtime): mcr.microsoft.com/dotnet/aspnet:10.0-alpine\n" +
                "- Usuario non-root\n" +
                "- HEALTHCHECK contra /health\n" +
                "- Optimizado para caching de layers (COPY *.csproj y restore ANTES " +
                "  de COPY del resto).\n" +
                "Criterio éxito: `docker build .` y `docker run` funcionan y la app " +
                "responde 200 en /health."),

            EscenarioInfra.GhActionsPipeline => new(esc, "12/17",
                "Crea un GitHub Actions workflow `.github/workflows/deploy.yml` para " +
                "este proyecto .NET 10:\n" +
                "- Trigger: push a main + PRs\n" +
                "- Job 1: build + tests + coverage\n" +
                "- Job 2 (`needs: build`): deploy a slot staging con OIDC (`azure/login@v2`)\n" +
                "- Smoke test contra `https://<app>-staging.azurewebsites.net/health`\n" +
                "- Job 3 (Environment `production`, requires approval): swap a producción\n" +
                "- Auto-rollback (`condition: failure()`) que hace swap inverso.\n" +
                "Usa `vars.AZURE_CLIENT_ID`, `vars.AZURE_TENANT_ID`, " +
                "`vars.AZURE_SUBSCRIPTION_ID` (no secretos)."),

            EscenarioInfra.ReverseArmABicep => new(esc, "16",
                "Lee `exported-arm.bicep` (auto-generado por `az bicep decompile`) y " +
                "reescríbelo siguiendo nuestras convenciones:\n" +
                "1) Modulariza por tipo de recurso en `modules/`\n" +
                "2) Usa AVM modules (`br/public:avm/...`) cuando aplique\n" +
                "3) Hardcoded values → `param` con `@description`\n" +
                "4) Tags obligatorios: env, costCenter, owner, app\n" +
                "5) `@secure()` en todo lo sensible\n" +
                "6) `uniqueString()` para nombres\n" +
                "Estructura: `main.bicep` + `main.dev.bicepparam` + " +
                "`main.prod.bicepparam` + `modules/`.\n" +
                "Verificación obligatoria al final: `az deployment group what-if` " +
                "y reporta CAMBIOS INESPERADOS."),

            EscenarioInfra.AuditarRecursos => new(esc, "15",
                "Audita el resource group {{rg}} en Azure:\n" +
                "1) Ejecuta `az resource list -g {{rg}}` y analiza\n" +
                "2) Verifica: recursos sin tags obligatorios\n" +
                "3) Verifica: Storage Accounts con acceso público\n" +
                "4) Verifica: SQL Servers sin firewall configurado\n" +
                "5) Verifica: Web Apps sin `httpsOnly`\n" +
                "6) Verifica: recursos que aún usan TLS 1.0 o 1.1\n" +
                "7) Verifica: recursos sin Managed Identity donde aplique.\n" +
                "Output: tabla Markdown con `Severidad | Recurso | Hallazgo | Comando fix`."),

            EscenarioInfra.RunbookOperaciones => new(esc, "11",
                "Genera un runbook en Markdown para el escenario:\n" +
                "{{escenario}}\n\n" +
                "Incluye:\n" +
                "- Síntomas y cómo detectarlo (KQL queries si aplica)\n" +
                "- Diagnóstico paso a paso\n" +
                "- Acción inmediata (con comandos `az` específicos)\n" +
                "- Acción a medio plazo (cambios de config o código)\n" +
                "- Criterio de escalado al siguiente nivel.\n" +
                "Idioma: español. Tono operativo, sin paja."),

            EscenarioInfra.ScriptOps => new(esc, "14",
                "Genera un script bash `ops-toolkit.sh` con estas funciones:\n" +
                "- `check_health`: verifica health de API + Functions + Cosmos\n" +
                "- `view_logs`: últimas 50 líneas de logs de App Service\n" +
                "- `scale_up` / `scale_down`: cambia el SKU del plan\n" +
                "- `deploy_quick`: deploy de la rama actual al slot staging\n" +
                "- `rollback`: swap inverso de slots\n" +
                "Con colores, logging y `confirm()` ANTES de cualquier acción " +
                "destructiva (slide 11/19 de S9.1: hooks). Solo lectura por defecto."),

            _ => throw new ArgumentOutOfRangeException(nameof(esc),
                $"Escenario desconocido: {esc}"),
        };
    }

    private static string BicepDesdeRequirements(RequisitosInfra? req)
    {
        var recursos = req?.Recursos.Count > 0
            ? string.Join(", ", req.Recursos.Select(r => r.Tipo.ToString()))
            : "(rellena con tu lista de recursos)";

        var noFuncional = new List<string>();
        if (req?.MultiRegion == true) noFuncional.Add("multi-region (UE)");
        if (req?.ComplianceEuropa == true) noFuncional.Add("GDPR: datos en la UE");
        if (req?.ConSlots == true) noFuncional.Add("slots de staging + producción");
        if (req?.ConAutoscale == true) noFuncional.Add("auto-scale en horas pico");
        if (req?.ConHttpsOnly == true) noFuncional.Add("HTTPS only en App Services");
        if (req?.ConManagedIdentity == true) noFuncional.Add("Managed Identity (sin connection strings)");
        var nf = noFuncional.Count == 0
            ? "(rellena con tus requisitos no funcionales)"
            : string.Join(", ", noFuncional);

        return
            "Necesito infraestructura Azure con estos requisitos:\n\n" +
            $"Recursos: {recursos}.\n" +
            $"No funcional: {nf}.\n\n" +
            "Genera:\n" +
            "1) Bicep modular: `main.bicep` + `main.dev.bicepparam` + " +
            "`main.prod.bicepparam` + `modules/` por tipo de recurso.\n" +
            "2) Tags obligatorios: env, costCenter, owner, app.\n" +
            "3) AVM modules (`br/public:avm/...`) donde aplique.\n" +
            "4) `@secure()` en todo lo sensible; Key Vault Reference en params.\n" +
            "5) `uniqueString()` para nombres.\n" +
            "6) RBAC por Managed Identity (sin connection strings con password).\n" +
            "Criterio éxito: `az bicep build` sin warnings y " +
            "`az deployment group what-if` sin Delete inesperados.";
    }
}
