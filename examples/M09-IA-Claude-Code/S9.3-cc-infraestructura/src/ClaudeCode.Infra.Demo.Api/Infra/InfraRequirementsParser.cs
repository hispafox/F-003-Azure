namespace ClaudeCode.Infra.Demo.Api.Infra;

public enum TipoRecurso
{
    AppService, Functions, CosmosDb, SqlDatabase,
    Storage, ServiceBus, KeyVault, Redis,
    ApplicationInsights, LogAnalytics, Otro,
}

public sealed record RecursoDetectado(TipoRecurso Tipo, string PalabraClave);

public sealed record RequisitosInfra(
    IReadOnlyList<RecursoDetectado> Recursos,
    bool MultiRegion,
    bool ComplianceEuropa,
    bool ConSlots,
    bool ConHttpsOnly,
    bool ConManagedIdentity,
    bool ConAutoscale,
    IReadOnlyList<string> Avisos);

// Slides 2, 3, 9, 17 — parser de la descripción de requisitos de
// infraestructura. Detecta recursos por palabras clave y los knobs
// no-funcionales típicos. Lógica pura.
public static class InfraRequirementsParser
{
    private static readonly (string Patron, TipoRecurso Tipo)[] PatronesRecursos =
    [
        ("app service", TipoRecurso.AppService),
        ("web app", TipoRecurso.AppService),
        ("api rest", TipoRecurso.AppService),

        ("azure functions", TipoRecurso.Functions),
        ("functions", TipoRecurso.Functions),
        ("consumption plan", TipoRecurso.Functions),

        ("cosmos db", TipoRecurso.CosmosDb),
        ("cosmosdb", TipoRecurso.CosmosDb),
        ("cosmos", TipoRecurso.CosmosDb),

        ("sql database", TipoRecurso.SqlDatabase),
        ("sql server", TipoRecurso.SqlDatabase),
        ("base de datos sql", TipoRecurso.SqlDatabase),
        ("azure sql", TipoRecurso.SqlDatabase),

        ("storage account", TipoRecurso.Storage),
        ("blob storage", TipoRecurso.Storage),
        ("storage para", TipoRecurso.Storage),

        ("service bus", TipoRecurso.ServiceBus),
        ("topic", TipoRecurso.ServiceBus),
        ("cola para", TipoRecurso.ServiceBus),
        ("queue", TipoRecurso.ServiceBus),

        ("key vault", TipoRecurso.KeyVault),
        ("keyvault", TipoRecurso.KeyVault),
        ("secretos en", TipoRecurso.KeyVault),

        ("redis", TipoRecurso.Redis),
        ("cache redis", TipoRecurso.Redis),

        ("application insights", TipoRecurso.ApplicationInsights),
        ("app insights", TipoRecurso.ApplicationInsights),

        ("log analytics", TipoRecurso.LogAnalytics),
        ("workspace", TipoRecurso.LogAnalytics),
    ];

    public static RequisitosInfra Parsear(string descripcion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);

        var lower = descripcion.ToLowerInvariant();

        var recursos = new List<RecursoDetectado>();
        var tiposVistos = new HashSet<TipoRecurso>();
        foreach (var (patron, tipo) in PatronesRecursos)
        {
            if (lower.Contains(patron, StringComparison.Ordinal) && tiposVistos.Add(tipo))
                recursos.Add(new RecursoDetectado(tipo, patron));
        }

        bool multiRegion = lower.Contains("multi-region", StringComparison.Ordinal)
            || lower.Contains("multi region", StringComparison.Ordinal)
            || (lower.Contains("west europe", StringComparison.Ordinal)
                && lower.Contains("north europe", StringComparison.Ordinal));

        bool europa = lower.Contains("gdpr", StringComparison.Ordinal)
            || lower.Contains("compliance europ", StringComparison.OrdinalIgnoreCase)
            || lower.Contains("datos en europa", StringComparison.Ordinal);

        bool slots = lower.Contains("slot", StringComparison.Ordinal);
        bool httpsOnly = lower.Contains("https only", StringComparison.Ordinal)
            || lower.Contains("https forz", StringComparison.Ordinal)
            || lower.Contains("https-only", StringComparison.Ordinal);

        bool mi = lower.Contains("managed identity", StringComparison.Ordinal)
            || lower.Contains("managed-identity", StringComparison.Ordinal)
            || lower.Contains("identidad administrada", StringComparison.Ordinal);

        bool autoscale = lower.Contains("auto-scale", StringComparison.Ordinal)
            || lower.Contains("autoscale", StringComparison.Ordinal)
            || lower.Contains("auto scale", StringComparison.Ordinal)
            || lower.Contains("escalado", StringComparison.Ordinal);

        var avisos = new List<string>();
        if (!httpsOnly)
            avisos.Add("No se mencionó HTTPS only — por defecto añade `httpsOnly: true` " +
                "en el Bicep de App Service (slide 9/15).");
        if (!mi && recursos.Any(r =>
                r.Tipo is TipoRecurso.AppService or TipoRecurso.Functions))
            avisos.Add("Sin Managed Identity declarada — usa MI en vez de connection " +
                "strings con password (slide 15).");
        if (multiRegion && europa)
            avisos.Add("Multi-region + GDPR: confirma que las dos regiones están en la UE " +
                "(slide 17).");
        if (recursos.Any(r => r.Tipo == TipoRecurso.Storage) && !lower.Contains("private endpoint", StringComparison.Ordinal))
            avisos.Add("Storage detectado: cierra el acceso público y usa Private Endpoint " +
                "(slide 15).");

        return new RequisitosInfra(
            Recursos: recursos,
            MultiRegion: multiRegion,
            ComplianceEuropa: europa,
            ConSlots: slots,
            ConHttpsOnly: httpsOnly,
            ConManagedIdentity: mi,
            ConAutoscale: autoscale,
            Avisos: avisos);
    }
}
