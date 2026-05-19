using Apim.Demo.Api.Apim;

namespace Apim.Demo.Api.Tests;

// CAPA 1 — policies inbound (slides 5-6, 9, 18).
[Trait("Category", "Unit")]
public class Unit_PolicyTests
{
    private static readonly PolicyConfig Base = new(
        SubscriptionRequired: true, RequiredAudience: "client-1",
        RateLimitCalls: 100, RateLimitCallsPremium: 1000, QuotaCalls: 10000);

    [Fact]
    public void Todo_Ok_Reenvia_Al_Backend()
    {
        var d = ApimPolicyEvaluator.Evaluar(
            new PolicyContext("k", "10.0.0.1", JwtAudience: "client-1",
                LlamadasEnVentana: 10), Base);
        Assert.True(d.Permitida);
        Assert.Equal(200, d.Status);
    }

    [Fact]
    public void Sin_Subscription_Key_Es_401()
    {
        var d = ApimPolicyEvaluator.Evaluar(
            new PolicyContext(null, "10.0.0.1"), Base);
        Assert.Equal(401, d.Status);
        Assert.Contains("Subscription-Key", d.Razon);
    }

    [Fact]
    public void Ip_En_Blacklist_Es_403()
    {
        var cfg = Base with { IpBlacklist = ["9.9.9.9"] };
        var d = ApimPolicyEvaluator.Evaluar(
            new PolicyContext("k", "9.9.9.9", JwtAudience: "client-1"), cfg);
        Assert.Equal(403, d.Status);
    }

    [Fact]
    public void Jwt_Audience_Invalida_Es_401()
    {
        var d = ApimPolicyEvaluator.Evaluar(
            new PolicyContext("k", "10.0.0.1", JwtAudience: "otro"), Base);
        Assert.Equal(401, d.Status);
        Assert.Contains("aud", d.Razon);
    }

    [Fact]
    public void Rate_Limit_Estandar_Superado_Es_429_Con_RetryAfter()
    {
        var d = ApimPolicyEvaluator.Evaluar(
            new PolicyContext("k", "1.1.1.1", JwtAudience: "client-1",
                LlamadasEnVentana: 100), Base);
        Assert.Equal(429, d.Status);
        Assert.Equal(60, d.RetryAfter);
    }

    [Fact]
    public void Premium_Aguanta_Mas_Que_Estandar()
    {
        var ctx = new PolicyContext("k", "1.1.1.1", UserTier: "premium",
            JwtAudience: "client-1", LlamadasEnVentana: 500);
        var d = ApimPolicyEvaluator.Evaluar(ctx, Base);
        Assert.True(d.Permitida);   // 500 < 1000 (premium)
    }

    [Fact]
    public void Quota_Superada_Es_429_Con_RetryAfter_Del_Periodo()
    {
        var d = ApimPolicyEvaluator.Evaluar(
            new PolicyContext("k", "1.1.1.1", JwtAudience: "client-1",
                LlamadasEnVentana: 1, LlamadasEnCuota: 10000), Base);
        Assert.Equal(429, d.Status);
        Assert.Equal(86400, d.RetryAfter);
    }

    [Theory]
    [InlineData(503, 0, 3, true)]
    [InlineData(503, 3, 3, false)]   // agotados los intentos
    [InlineData(404, 0, 3, false)]   // 4xx no se reintenta
    public void Circuit_Breaker(int status, int intentos, int max, bool esperado)
        => Assert.Equal(esperado,
            ApimPolicyEvaluator.DebeReintentar(status, intentos, max));
}
