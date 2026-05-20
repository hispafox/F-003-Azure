using Deploy.Demo.Api.Deploy;

namespace Deploy.Demo.Api.Tests;

// CAPA 1 — health check + smoke test (slide 9).
[Trait("Category", "Unit")]
public class Unit_HealthCheckTests
{
    [Fact]
    public void Pasa_Al_Primer_Intento_Si_200()
    {
        var r = HealthCheckEvaluator.Evaluar(200, 5,
            [new(1, 200)]);
        Assert.True(r.Pasa);
        Assert.Equal(1, r.IntentosUsados);
    }

    [Fact]
    public void Pasa_Al_Segundo_Intento_Tras_503()
    {
        var r = HealthCheckEvaluator.Evaluar(200, 5,
            [new(1, 503), new(2, 200)]);
        Assert.True(r.Pasa);
        Assert.Equal(2, r.IntentosUsados);
    }

    [Fact]
    public void Falla_Tras_5_Intentos_Sin_200()
    {
        var r = HealthCheckEvaluator.Evaluar(200, 5,
            [new(1, 503), new(2, 503), new(3, 503), new(4, 503), new(5, 502)]);
        Assert.False(r.Pasa);
        Assert.Equal(5, r.IntentosUsados);
        Assert.Contains("502", r.Razon);
    }

    [Fact]
    public void Procesa_Intentos_En_Orden_Aunque_Lleguen_Desordenados()
    {
        var r = HealthCheckEvaluator.Evaluar(200, 5,
            [new(3, 200), new(1, 500), new(2, 500)]);
        Assert.True(r.Pasa);
        Assert.Equal(3, r.IntentosUsados);
    }

    [Fact]
    public void Smoke_Test_Pasa_Si_Todos_2xx()
    {
        var r = HealthCheckEvaluator.EvaluarSmoke(
            [new("/api/p", 200), new("/api/q", 204)]);
        Assert.True(r.Pasa);
        Assert.Empty(r.EndpointsFallidos);
    }

    [Fact]
    public void Smoke_Test_Falla_Si_Alguno_5xx()
    {
        var r = HealthCheckEvaluator.EvaluarSmoke(
            [new("/api/p", 200), new("/api/q", 500)]);
        Assert.False(r.Pasa);
        Assert.Contains(r.EndpointsFallidos, x => x.Contains("/api/q"));
    }

    [Fact]
    public void MaxIntentos_No_Positivo_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            HealthCheckEvaluator.Evaluar(200, 0, [new(1, 200)]));
}
