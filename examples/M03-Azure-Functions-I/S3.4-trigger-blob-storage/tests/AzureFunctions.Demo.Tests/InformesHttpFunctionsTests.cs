using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class InformesHttpFunctionsTests
{
    private static (InformesHttpFunctions fn, IInformeService svc) Build()
    {
        var svc = new InMemoryInformeService(new InMemoryProductoService());
        return (new InformesHttpFunctions(svc), svc);
    }

    [Fact]
    public void Listar_Empty_Returns_Zero_Total()
    {
        var (fn, _) = Build();
        var result = fn.Listar(HttpRequestFactory.Empty());

        var ok = Assert.IsType<OkObjectResult>(result);
        var total = (int)ok.Value!.GetType().GetProperty("total")!.GetValue(ok.Value)!;
        Assert.Equal(0, total);
    }

    [Fact]
    public void Listar_Returns_Informes_From_Service()
    {
        var (fn, svc) = Build();
        svc.GenerarSiNoExiste(new DateOnly(2026, 4, 22));

        var result = fn.Listar(HttpRequestFactory.Empty());

        var ok = Assert.IsType<OkObjectResult>(result);
        var total = (int)ok.Value!.GetType().GetProperty("total")!.GetValue(ok.Value)!;
        Assert.Equal(1, total);
    }

    [Fact]
    public void GetPorFecha_With_Existing_Date_Returns_Informe()
    {
        var (fn, svc) = Build();
        svc.GenerarSiNoExiste(new DateOnly(2026, 4, 22));

        var result = fn.GetPorFecha(HttpRequestFactory.Empty(), "2026-04-22");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<Informe>(ok.Value);
    }

    [Fact]
    public void GetPorFecha_With_Invalid_Format_Returns_400()
    {
        var (fn, _) = Build();

        var result = fn.GetPorFecha(HttpRequestFactory.Empty(), "no-es-fecha");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void GetPorFecha_With_Missing_Date_Returns_404()
    {
        var (fn, _) = Build();

        var result = fn.GetPorFecha(HttpRequestFactory.Empty(), "2030-01-01");

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
