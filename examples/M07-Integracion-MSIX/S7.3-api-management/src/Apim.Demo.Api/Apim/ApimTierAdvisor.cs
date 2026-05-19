namespace Apim.Demo.Api.Apim;

// Slide 3/32 — tiers de APIM (2026).
public enum ApimTier { Consumption, Developer, Basic, Standard, Premium }

public sealed record EscenarioApim(
    bool Produccion = false,
    bool DevTest = false,
    bool RequiereVNet = false,
    bool MultiRegion = false,
    bool SelfHostedGateway = false,
    long LlamadasMes = 100_000,
    int LlamadasPorSegundo = 10);

public sealed record RecomendacionTier(
    ApimTier Tier, string CosteAproximado, IReadOnlyList<string> Razones);

public sealed record DecisionApim(bool Recomendado, IReadOnlyList<string> Razones);

// Slides 3, 16, 32 — elegir tier y decidir si APIM aporta. Tablas de
// decisión puras citando las slides.
public static class ApimTierAdvisor
{
    private static string Coste(ApimTier t) => t switch
    {
        ApimTier.Consumption => "0 € base · pago por llamada (1M gratis/mes)",
        ApimTier.Developer => "~40 €/mes (dev/test, sin SLA)",
        ApimTier.Basic => "~130 €/mes (producción pequeña)",
        ApimTier.Standard => "~550 €/mes (producción media)",
        ApimTier.Premium => "~2200 €/mes (enterprise, multi-región, VNet)",
        _ => "n/d",
    };

    // Árbol de decisión de la slide 32, por prioridad.
    public static RecomendacionTier RecomendarTier(EscenarioApim e)
    {
        ArgumentNullException.ThrowIfNull(e);
        var razones = new List<string>();

        if (e.RequiereVNet || e.MultiRegion || e.SelfHostedGateway)
        {
            if (e.RequiereVNet) razones.Add("VNet integration / red interna → Premium (slide 32).");
            if (e.MultiRegion) razones.Add("Multi-región con failover → Premium (slide 32).");
            if (e.SelfHostedGateway) razones.Add("Self-hosted gateway → solo Premium (slide 29/32).");
            return new(ApimTier.Premium, Coste(ApimTier.Premium), razones);
        }

        if (e.Produccion && e.LlamadasPorSegundo > 1000)
        {
            razones.Add("Producción > 1000 llamadas/seg → Premium (slide 32).");
            return new(ApimTier.Premium, Coste(ApimTier.Premium), razones);
        }

        if (e.DevTest && !e.Produccion)
        {
            razones.Add("Entorno dev/test: features completas sin SLA → Developer (slide 32).");
            return new(ApimTier.Developer, Coste(ApimTier.Developer), razones);
        }

        if (e.Produccion)
        {
            razones.Add("Producción media sin VNet/multi-región → Standard (slide 31/32: nunca Developer en prod).");
            return new(ApimTier.Standard, Coste(ApimTier.Standard), razones);
        }

        razones.Add($"≈ {e.LlamadasMes:N0} llamadas/mes, sin requisitos enterprise → Consumption (gratis hasta 1M, slide 3).");
        return new(ApimTier.Consumption, Coste(ApimTier.Consumption), razones);
    }

    // Slide 16 — ¿APIM aporta en este caso?
    public static DecisionApim EsBuenCaso(
        bool multiplesApis, bool necesitaRateLimitOCache,
        bool exponeATerceros, bool versionadoCentral, bool analytics,
        bool unaApiSimple, bool soloTraficoInterno, bool presupuestoLimitado)
    {
        var aFavor = new List<string>();
        if (multiplesApis) aFavor.Add("Varias APIs tras una única URL de entrada (slide 16).");
        if (necesitaRateLimitOCache) aFavor.Add("Necesita rate limiting / caching / transformaciones (slide 16).");
        if (exponeATerceros) aFavor.Add("Expone APIs a terceros (partners/clientes) (slide 16).");
        if (versionadoCentral) aFavor.Add("Versionado centralizado (slide 16).");
        if (analytics) aFavor.Add("Analytics de uso de APIs (slide 16).");

        var enContra = new List<string>();
        if (unaApiSimple) enContra.Add("Una sola API simple: el overhead no se justifica (slide 16 — NO).");
        if (soloTraficoInterno) enContra.Add("Tráfico exclusivamente interno service-to-service (slide 16 — NO).");
        if (presupuestoLimitado) enContra.Add("Presupuesto no permite tiers no-Consumption (slide 16 — NO).");

        bool recomendado = aFavor.Count > enContra.Count;
        var razones = recomendado ? aFavor
            : enContra.Count > 0 ? enContra
            : aFavor.Count > 0 ? aFavor
            : ["Señales equilibradas: empezar con Consumption y crecer (slide 33)."];
        return new DecisionApim(recomendado, razones);
    }
}
