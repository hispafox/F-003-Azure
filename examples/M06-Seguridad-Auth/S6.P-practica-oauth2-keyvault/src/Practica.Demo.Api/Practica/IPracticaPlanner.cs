namespace Practica.Demo.Api.Practica;

public sealed record PlanPractica(
    string TipoApp,
    string AccionEasyAuth,
    string Issuer,
    IReadOnlyDictionary<string, string> AppSettings,
    bool SoloReferencias,
    IReadOnlyList<string> Checklist);

// Compone EasyAuthAdvisor + KeyVaultRefAppSettings en el plan de la
// práctica (slides 7-8-11). Servicio inyectable (seam para el test DI).
public interface IPracticaPlanner
{
    PlanPractica Planificar(
        TipoApp tipo, string tenantId, string clientId, string vault);
}

public sealed class PracticaPlanner : IPracticaPlanner
{
    public PlanPractica Planificar(
        TipoApp tipo, string tenantId, string clientId, string vault)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(vault);

        var settings = KeyVaultRefAppSettings.Construir(tenantId, clientId, vault);

        return new PlanPractica(
            tipo.ToString(),
            EasyAuthAdvisor.AccionNoAutenticado(tipo),
            EasyAuthAdvisor.Issuer(tenantId),
            settings,
            KeyVaultRefAppSettings.SoloReferencias(settings),
            // Slide 11 — verificaciones del entregable.
            [
                "GET /health sin token → 200 (público)",
                "GET /api/perfil sin token → 401",
                "GET /api/perfil con token → 200 + nombre del usuario",
                "App Settings: Key Vault References en verde",
                "Cero passwords en App Settings (solo @Microsoft.KeyVault)",
            ]);
    }
}
