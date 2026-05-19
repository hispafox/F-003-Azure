namespace Apim.Demo.Api.Apim;

// Contexto de la petición que llega al gateway (lo que APIM ve en
// `context.Request` / `context.Subscription`).
public sealed record PolicyContext(
    string? SubscriptionKey,
    string Ip,
    string? UserTier = null,                 // header X-User-Tier (slide 9)
    string? JwtAudience = null,              // claim `aud` del Bearer
    int LlamadasEnVentana = 0,              // contador rate-limit actual
    int LlamadasEnCuota = 0);               // contador quota actual

// Config de las policies inbound (slides 5-6, 9, 19).
public sealed record PolicyConfig(
    bool SubscriptionRequired = true,
    IReadOnlyList<string>? IpBlacklist = null,
    string? RequiredAudience = null,        // validate-jwt (slide 5)
    int RateLimitCalls = 100,
    int RateLimitPeriodSeg = 60,
    int RateLimitCallsPremium = 1000,       // rama premium (slide 9)
    int QuotaCalls = 10000,
    int QuotaPeriodSeg = 86400);

public sealed record PolicyDecision(int Status, string Razon, int? RetryAfter = null)
{
    public bool Permitida => Status == 200;
}

// Slides 5-6, 9-10, 19 — evalúa las policies INBOUND en el orden de
// APIM: subscription key → ip-filter → validate-jwt → rate-limit →
// quota. Lógica pura y determinista (los contadores entran por el
// contexto; no hay reloj ni estado).
public static class ApimPolicyEvaluator
{
    public static PolicyDecision Evaluar(PolicyContext ctx, PolicyConfig cfg)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(cfg);

        // 1) Subscription key (slide 8) — 401 si falta y es obligatoria.
        if (cfg.SubscriptionRequired && string.IsNullOrWhiteSpace(ctx.SubscriptionKey))
            return new PolicyDecision(401, "Falta Ocp-Apim-Subscription-Key (slide 8).");

        // 2) ip-filter (slide 6) — 403 si la IP está en la blacklist.
        if (cfg.IpBlacklist is { Count: > 0 } &&
            cfg.IpBlacklist.Contains(ctx.Ip, StringComparer.OrdinalIgnoreCase))
            return new PolicyDecision(403, $"IP {ctx.Ip} bloqueada por ip-filter (slide 6).");

        // 3) validate-jwt (slide 5) — 401 si el claim `aud` no coincide.
        if (!string.IsNullOrWhiteSpace(cfg.RequiredAudience) &&
            !string.Equals(ctx.JwtAudience, cfg.RequiredAudience, StringComparison.Ordinal))
            return new PolicyDecision(401, "validate-jwt: claim 'aud' inválido o ausente (slide 5).");

        // 4) rate-limit-by-key con rama premium (slide 9) — 429.
        bool premium = string.Equals(ctx.UserTier, "premium", StringComparison.OrdinalIgnoreCase);
        int limite = premium ? cfg.RateLimitCallsPremium : cfg.RateLimitCalls;
        if (ctx.LlamadasEnVentana >= limite)
            return new PolicyDecision(429,
                $"Rate limit superado ({limite}/{cfg.RateLimitPeriodSeg}s, tier={(premium ? "premium" : "estándar")}).",
                RetryAfter: cfg.RateLimitPeriodSeg);

        // 5) quota-by-key (slide 9) — 429 con Retry-After del período.
        if (ctx.LlamadasEnCuota >= cfg.QuotaCalls)
            return new PolicyDecision(429,
                $"Quota superada ({cfg.QuotaCalls}/{cfg.QuotaPeriodSeg}s).",
                RetryAfter: cfg.QuotaPeriodSeg);

        return new PolicyDecision(200, "OK — petición reenviada al backend.");
    }

    // Slide 18 — circuit breaker: ¿reintentar este status de backend?
    public static bool DebeReintentar(int statusBackend, int intentos, int maxIntentos)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(intentos);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxIntentos);
        return statusBackend >= 500 && intentos < maxIntentos;
    }
}
