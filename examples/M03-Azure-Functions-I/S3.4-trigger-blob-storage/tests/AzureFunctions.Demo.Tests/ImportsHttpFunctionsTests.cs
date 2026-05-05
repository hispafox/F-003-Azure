using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Models;
using AzureFunctions.Demo.Services;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Demo.Tests;

public class ImportsHttpFunctionsTests
{
    private static (ImportsHttpFunctions fn, IImportSummaryService summary) Build()
    {
        var summary = new InMemoryImportSummaryService();
        return (new ImportsHttpFunctions(summary), summary);
    }

    [Fact]
    public void Listar_Empty_Returns_Zero_Total()
    {
        var (fn, _) = Build();

        var result = fn.ListarImports(HttpRequestFactory.Empty());

        var ok = Assert.IsType<OkObjectResult>(result);
        var total = (int)ok.Value!.GetType().GetProperty("total")!.GetValue(ok.Value)!;
        Assert.Equal(0, total);
    }

    [Fact]
    public void Listar_Returns_Items_From_Service()
    {
        var (fn, summary) = Build();
        summary.Registrar(new ImportResultado(
            "test.csv", 1, 1, 0, [], ["p-x"], DateTimeOffset.UtcNow));

        var result = fn.ListarImports(HttpRequestFactory.Empty());

        var ok = Assert.IsType<OkObjectResult>(result);
        var total = (int)ok.Value!.GetType().GetProperty("total")!.GetValue(ok.Value)!;
        Assert.Equal(1, total);
    }

    [Fact]
    public void GetImportPorArchivo_Existing_Returns_Resultado()
    {
        var (fn, summary) = Build();
        summary.Registrar(new ImportResultado(
            "ventas.csv", 5, 5, 0, [], ["p-1"], DateTimeOffset.UtcNow));

        var result = fn.GetImportPorArchivo(HttpRequestFactory.Empty(), "ventas.csv");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ImportResultado>(ok.Value);
    }

    [Fact]
    public void GetImportPorArchivo_Missing_Returns_404()
    {
        var (fn, _) = Build();

        var result = fn.GetImportPorArchivo(HttpRequestFactory.Empty(), "missing.csv");

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
