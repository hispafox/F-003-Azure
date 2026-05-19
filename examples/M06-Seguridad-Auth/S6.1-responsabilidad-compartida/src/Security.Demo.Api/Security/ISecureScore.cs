namespace Security.Demo.Api.Security;

// Slide 17 — el checklist de seguridad del equipo. Cada bool = un control.
public sealed record ChecklistSeguridad(
    bool MfaAdmins,
    bool RbacMinimoPrivilegio,
    bool ManagedIdentity,
    bool KeyVaultSecretos,
    bool HttpsForzado,
    bool StoragePublicoDeshabilitado,
    bool SqlFirewallYEntraId,
    bool AzurePolicy,
    bool LogsYAuditoria,
    bool DependenciasAuditadas,
    bool PlanRespuestaIncidentes);

public sealed record ResultadoSecureScore(
    int Puntuacion,                        // 0-100 (slide 10)
    int Cumplidos,
    int Total,
    IReadOnlyList<string> Faltantes,
    string Veredicto);                     // > 70 recomendado (slide 10/17)

// Slides 10, 17 — el "Secure Score" de Defender for Cloud modelado: a
// partir del checklist calcula 0-100 y lista lo que falta. Servicio
// inyectable (seam para el test de contenedor).
public interface ISecureScore
{
    ResultadoSecureScore Calcular(ChecklistSeguridad c);
}

public sealed class SecureScoreCalculator : ISecureScore
{
    public ResultadoSecureScore Calcular(ChecklistSeguridad c)
    {
        ArgumentNullException.ThrowIfNull(c);

        var items = new (bool ok, string nombre)[]
        {
            (c.MfaAdmins, "MFA en todos los administradores"),
            (c.RbacMinimoPrivilegio, "RBAC de mínimo privilegio (nadie Owner sin necesidad)"),
            (c.ManagedIdentity, "Managed Identity en las apps (cero connection strings con password)"),
            (c.KeyVaultSecretos, "Key Vault para secretos de terceros"),
            (c.HttpsForzado, "HTTPS forzado en Web Apps y Functions"),
            (c.StoragePublicoDeshabilitado, "Storage: acceso público deshabilitado, firewall Deny"),
            (c.SqlFirewallYEntraId, "SQL: firewall + Entra ID auth + threat detection"),
            (c.AzurePolicy, "Azure Policy (HTTPS, TLS 1.2, etiquetas)"),
            (c.LogsYAuditoria, "Logs y auditoría habilitados"),
            (c.DependenciasAuditadas, "Dependencias auditadas (dotnet list package --vulnerable)"),
            (c.PlanRespuestaIncidentes, "Plan de respuesta a incidentes documentado"),
        };

        var total = items.Length;
        var cumplidos = items.Count(i => i.ok);
        var puntuacion = (int)Math.Round(100.0 * cumplidos / total);
        var faltantes = items.Where(i => !i.ok).Select(i => i.nombre).ToArray();

        var veredicto = puntuacion switch
        {
            >= 90 => "Excelente",
            >= 70 => "Aceptable (objetivo mínimo, slide 17)",
            >= 40 => "Riesgo: prioriza las recomendaciones",
            _ => "Crítico: superficie de ataque amplia",
        };

        return new ResultadoSecureScore(
            puntuacion, cumplidos, total, faltantes, veredicto);
    }
}
