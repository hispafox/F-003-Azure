namespace Apim.Demo.Api.Apim;

public sealed record PlanApim(
    ApimTier Tier,
    string CosteAproximado,
    IReadOnlyList<string> RazonesTier,
    bool ApimRecomendado,
    EsquemaVersionado EsquemaVersionado,
    IReadOnlyList<string> PoliciesInbound,
    IReadOnlyList<string> Checklist);

// Compone ApimTierAdvisor + ApimVersioningResolver + ApimPolicyEvaluator
// en un plan de despliegue + checklist del entregable. Servicio
// inyectable (seam del test DI — lección M03-S3.4).
public interface IApimPlanner
{
    PlanApim Planificar(EscenarioApim escenario, EscenarioUsoApim uso);
}

public sealed record EscenarioUsoApim(
    bool MultiplesApis = true,
    bool NecesitaRateLimitOCache = true,
    bool ExponeATerceros = false,
    bool VersionadoCentral = true,
    bool Analytics = true,
    bool UnaApiSimple = false,
    bool SoloTraficoInterno = false,
    bool PresupuestoLimitado = false);

public sealed class ApimPlanner : IApimPlanner
{
    public PlanApim Planificar(EscenarioApim escenario, EscenarioUsoApim uso)
    {
        ArgumentNullException.ThrowIfNull(escenario);
        ArgumentNullException.ThrowIfNull(uso);

        var tier = ApimTierAdvisor.RecomendarTier(escenario);
        var caso = ApimTierAdvisor.EsBuenCaso(
            uso.MultiplesApis, uso.NecesitaRateLimitOCache, uso.ExponeATerceros,
            uso.VersionadoCentral, uso.Analytics, uso.UnaApiSimple,
            uso.SoloTraficoInterno, uso.PresupuestoLimitado);

        return new PlanApim(
            tier.Tier,
            tier.CosteAproximado,
            tier.Razones,
            caso.Recomendado,
            ApimVersioningResolver.Recomendado,
            // Slides 5-6/9 — orden inbound recomendado.
            PoliciesInbound:
            [
                "ip-filter (whitelist/blacklist) (slide 6)",
                "validate-jwt: openid-config + claim 'aud' (slide 5)",
                "cors: orígenes permitidos del frontend (slide 5)",
                "rate-limit-by-key 100/60s (premium 1000/60s) (slide 9)",
                "quota-by-key 10000/día (slide 9)",
                "cache-lookup / cache-store 300s en GET de catálogo (slide 10)",
            ],
            // Slide 31 anti-patterns + slide 8/13.
            Checklist:
            [
                "Tier de producción Standard/Premium, nunca Developer (slide 31.1)",
                "Products por tier/cliente; suscripciones granulares (slide 31.2)",
                "Subscription keys vía Key Vault references, no en código (slide 31.3)",
                "Rate limiting por subscription/IP activo (slide 31.4)",
                "Logs: solo headers + muestra de body, sin PII (slide 31.5)",
                "Backend pool (failover/load balancing), no backend único (slide 31.6/17)",
                "APIM transforma/abstrae el backend, no acoplado (slide 31.7)",
                "Subscription key (la app) + OAuth2 JWT (el usuario), ambos (slide 8)",
                "Configuración como código (Bicep + GitOps), no Portal manual (slide 31.10)",
                "Alertas: 4xx > 5%, 5xx > 0 sostenido, BackendDuration > 2s (slide 13)",
                "Developer portal publicado para self-service (slide 12/31.12)",
            ]);
    }
}
