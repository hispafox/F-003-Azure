using ClaudeCode.Infra.Demo.Api.Infra;

namespace ClaudeCode.Infra.Demo.Api.Tests;

// CAPA 1 — audit checker (slide 15).
[Trait("Category", "Unit")]
public class Unit_AuditCheckerTests
{
    [Fact]
    public void Recurso_Conforme_No_Genera_Hallazgos()
    {
        var r = InfraAuditChecker.Auditar(
        [
            new EstadoRecurso(
                Nombre: "app-ok",
                Tipo: "Microsoft.Web/sites",
                HttpsOnly: true,
                TieneManagedIdentity: true,
                TieneTags: true,
                TlsVersion: "1.2"),
        ]);

        Assert.True(r.Limpio);
        Assert.Empty(r.Hallazgos);
    }

    [Fact]
    public void Web_App_Sin_Https_Es_Critico()
    {
        var r = InfraAuditChecker.Auditar(
        [
            new EstadoRecurso("app-bad", "Microsoft.Web/sites", HttpsOnly: false),
        ]);
        Assert.False(r.Limpio);
        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == Severidad.Critico
            && h.Comprobacion.Contains("HTTPS", StringComparison.Ordinal));
    }

    [Fact]
    public void Storage_Con_Acceso_Publico_Es_Critico()
    {
        var r = InfraAuditChecker.Auditar(
        [
            new EstadoRecurso("st-bad", "Microsoft.Storage/storageAccounts",
                AccesoPublico: true),
        ]);
        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == Severidad.Critico
            && h.Comprobacion.Contains("acceso público", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Sql_Sin_Firewall_Es_Alto()
    {
        var r = InfraAuditChecker.Auditar(
        [
            new EstadoRecurso("sql-bad", "Microsoft.Sql/servers",
                FirewallConfigurado: false),
        ]);
        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == Severidad.Alto
            && h.Comprobacion.Contains("firewall", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Web_App_Sin_Mi_Es_Alto()
    {
        var r = InfraAuditChecker.Auditar(
        [
            new EstadoRecurso("app-no-mi", "Microsoft.Web/sites",
                HttpsOnly: true, TieneManagedIdentity: false),
        ]);
        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == Severidad.Alto
            && h.Comprobacion.Contains("Managed Identity", StringComparison.Ordinal));
    }

    [Fact]
    public void Recurso_Sin_Tags_Es_Medio()
    {
        var r = InfraAuditChecker.Auditar(
        [
            new EstadoRecurso("kv-no-tags", "Microsoft.KeyVault/vaults",
                TieneTags: false),
        ]);
        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == Severidad.Medio
            && h.Comprobacion.Contains("tags", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Tls_Deprecated_Es_Alto()
    {
        var r = InfraAuditChecker.Auditar(
        [
            new EstadoRecurso("app-tls", "Microsoft.Web/sites",
                HttpsOnly: true, TieneManagedIdentity: true,
                TieneTags: true, TlsVersion: "1.0"),
        ]);
        Assert.Contains(r.Hallazgos, h =>
            h.Severidad == Severidad.Alto
            && h.Comprobacion.Contains("TLS", StringComparison.Ordinal));
    }

    [Fact]
    public void Informe_Cuenta_Criticos_Y_Altos_Correctamente()
    {
        var r = InfraAuditChecker.Auditar(
        [
            new EstadoRecurso("app1", "Microsoft.Web/sites", HttpsOnly: false),
            new EstadoRecurso("st1", "Microsoft.Storage/storageAccounts", AccesoPublico: true),
            new EstadoRecurso("sql1", "Microsoft.Sql/servers", FirewallConfigurado: false),
        ]);
        Assert.Equal(2, r.Criticos);
        Assert.True(r.Altos >= 1);
    }
}
