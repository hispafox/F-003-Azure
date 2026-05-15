using System.Text;
using System.Text.Json;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class OperacionesFunctionsTests
{
    private static HttpRequest Req() => new DefaultHttpContext().Request;

    private static HttpRequest JsonReq<T>(T body)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.ContentType = "application/json";
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body));
        ctx.Request.Body = new MemoryStream(bytes);
        ctx.Request.ContentLength = bytes.Length;
        return ctx.Request;
    }

    // ── Feature flag: rollback sin redeploy (slide 16) ──

    [Fact]
    public async Task Flag_Apagado_Usa_Procesador_Legacy()
    {
        var fn = TestHost.NewOperaciones(new FakeFeatureFlags(/* ninguno */));
        var req = JsonReq(new Pedido("p1", "c1", 200m));

        var r = (await fn.ProcesarPedido(req) as OkObjectResult)!.Value as ResultadoProceso;

        Assert.NotNull(r);
        Assert.Equal("legacy", r!.ProcesadoPor);
        Assert.Equal(200m, r.Total); // legacy no aplica descuento
    }

    [Fact]
    public async Task Flag_Encendido_Usa_Procesador_Nuevo()
    {
        var fn = TestHost.NewOperaciones(
            new FakeFeatureFlags(ProcesadorSelector.Flag));
        var req = JsonReq(new Pedido("p1", "c1", 200m));

        var r = (await fn.ProcesarPedido(req) as OkObjectResult)!.Value as ResultadoProceso;

        Assert.NotNull(r);
        Assert.Equal("nuevo", r!.ProcesadoPor);
        Assert.Equal(190m, r.Total); // 5% descuento de fidelización
    }

    [Fact]
    public async Task ProcesarPedido_Body_Invalido_Devuelve_400()
    {
        var fn = TestHost.NewOperaciones(new FakeFeatureFlags());
        var req = JsonReq(new Pedido("", "c1", 1m));

        Assert.IsType<BadRequestObjectResult>(await fn.ProcesarPedido(req));
    }

    // ── Health (verificación post-deploy, slide 10) ──

    [Fact]
    public void Health_Todos_Ok_Devuelve_200()
    {
        var agg = new HealthAggregator([new FixedHealthCheck("a", true), new FixedHealthCheck("b", true)]);
        var fn = TestHost.NewOperaciones(new FakeFeatureFlags(), agg);

        var result = fn.Health(Req());

        var ok = Assert.IsType<OkObjectResult>(result);
        var r = Assert.IsType<HealthResultado>(ok.Value);
        Assert.Equal("Healthy", r.Estado);
    }

    [Fact]
    public void Health_Un_Check_Falla_Devuelve_503()
    {
        var agg = new HealthAggregator([new FixedHealthCheck("a", true), new FixedHealthCheck("b", false)]);
        var fn = TestHost.NewOperaciones(new FakeFeatureFlags(), agg);

        var result = fn.Health(Req()) as ObjectResult;

        Assert.NotNull(result);
        Assert.Equal(503, result!.StatusCode);
        var r = Assert.IsType<HealthResultado>(result.Value);
        Assert.Equal("Unhealthy", r.Estado);
        Assert.Equal("fail", r.Checks["b"]);
    }

    [Fact]
    public void Health_Check_Que_Lanza_Cuenta_Como_Unhealthy_No_500()
    {
        var agg = new HealthAggregator([new ThrowingHealthCheck()]);
        var fn = TestHost.NewOperaciones(new FakeFeatureFlags(), agg);

        var result = fn.Health(Req()) as ObjectResult;

        Assert.Equal(503, result!.StatusCode);
    }

    // ── Version (slide 14) ──

    [Fact]
    public void Version_Devuelve_Info_Y_Estado_De_Flags()
    {
        var fn = TestHost.NewOperaciones(
            new FakeFeatureFlags(ProcesadorSelector.Flag));

        var ok = fn.Version(Req()) as OkObjectResult;

        Assert.NotNull(ok);
        var json = JsonSerializer.Serialize(ok!.Value);
        Assert.Contains("\"version\"", json);
        Assert.Contains("\"nuevoProcesamiento\":true", json);
    }
}

internal sealed class ThrowingHealthCheck : IHealthCheck
{
    public string Nombre => "explota";
    public bool Comprobar() => throw new InvalidOperationException("dependencia caída");
}
