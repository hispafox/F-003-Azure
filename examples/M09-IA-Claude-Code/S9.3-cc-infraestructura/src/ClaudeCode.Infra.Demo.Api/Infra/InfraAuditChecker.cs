namespace ClaudeCode.Infra.Demo.Api.Infra;

public enum Severidad { Critico, Alto, Medio, Bajo }

public sealed record Hallazgo(
    Severidad Severidad, string Comprobacion, string Recurso, string ComandoFix);

public sealed record InformeAudit(
    bool Limpio,
    IReadOnlyList<Hallazgo> Hallazgos,
    int Criticos,
    int Altos);

public sealed record EstadoRecurso(
    string Nombre,
    string Tipo,                       // ej. "Microsoft.Web/sites"
    bool HttpsOnly = true,
    bool TieneManagedIdentity = true,
    bool TieneTags = true,
    bool AccesoPublico = false,         // p.ej. Storage
    string TlsVersion = "1.2",
    bool FirewallConfigurado = true);   // p.ej. SQL

// Slide 15 — audit checker básico que evalúa el estado de un recurso
// contra las reglas mínimas (HTTPS, MI, tags, TLS 1.2, sin acceso
// público, firewall). Lógica pura: recibe estados, no llama a Azure.
public static class InfraAuditChecker
{
    public static InformeAudit Auditar(IEnumerable<EstadoRecurso> recursos)
    {
        ArgumentNullException.ThrowIfNull(recursos);

        var hallazgos = new List<Hallazgo>();

        foreach (var r in recursos)
        {
            bool esWebApp = r.Tipo.Contains("Microsoft.Web/sites",
                StringComparison.OrdinalIgnoreCase);
            bool esStorage = r.Tipo.Contains("Microsoft.Storage/storageAccounts",
                StringComparison.OrdinalIgnoreCase);
            bool esSql = r.Tipo.StartsWith("Microsoft.Sql/servers",
                StringComparison.OrdinalIgnoreCase);

            if (esWebApp && !r.HttpsOnly)
                hallazgos.Add(new(Severidad.Critico,
                    "Web App sin HTTPS forzado",
                    r.Nombre,
                    $"az webapp update -n {r.Nombre} --set httpsOnly=true"));

            if (esStorage && r.AccesoPublico)
                hallazgos.Add(new(Severidad.Critico,
                    "Storage con acceso público",
                    r.Nombre,
                    $"az storage account update -n {r.Nombre} " +
                    "--allow-blob-public-access false"));

            if (esSql && !r.FirewallConfigurado)
                hallazgos.Add(new(Severidad.Alto,
                    "SQL Server sin firewall configurado",
                    r.Nombre,
                    $"az sql server firewall-rule create --server {r.Nombre} ..."));

            if (!r.TieneManagedIdentity && (esWebApp || r.Tipo.Contains("Microsoft.Web/sites/functions",
                    StringComparison.OrdinalIgnoreCase)))
                hallazgos.Add(new(Severidad.Alto,
                    "Recurso sin Managed Identity",
                    r.Nombre,
                    $"az webapp identity assign -n {r.Nombre}"));

            if (!r.TieneTags)
                hallazgos.Add(new(Severidad.Medio,
                    "Sin tags obligatorios (env, costCenter, owner, app)",
                    r.Nombre,
                    $"az resource tag --tags env=prod ... --ids <resourceId>"));

            if (!string.Equals(r.TlsVersion, "1.2", StringComparison.Ordinal)
                && !string.Equals(r.TlsVersion, "1.3", StringComparison.Ordinal))
                hallazgos.Add(new(Severidad.Alto,
                    $"TLS deprecated ({r.TlsVersion})",
                    r.Nombre,
                    $"Forzar TLS 1.2 mínimo en {r.Tipo}"));
        }

        int criticos = hallazgos.Count(h => h.Severidad == Severidad.Critico);
        int altos = hallazgos.Count(h => h.Severidad == Severidad.Alto);
        bool limpio = hallazgos.Count == 0;

        return new InformeAudit(limpio, hallazgos, criticos, altos);
    }
}
