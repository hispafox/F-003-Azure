using System.Text.Json;
using AzureFunctions.Demo.Functions;
using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

public class CsvImportFunctionTests
{
    private static (CsvImportFunction fn, IImportSummaryService summary, IProductoService productos) Build()
    {
        var productos = new InMemoryProductoService();
        var summary = new InMemoryImportSummaryService();
        var importer = new CsvProductosImporter(productos);
        var fn = new CsvImportFunction(importer, summary, NullLogger<CsvImportFunction>.Instance);
        return (fn, summary, productos);
    }

    [Fact]
    public void ImportarCsv_Returns_Json_Summary_And_Registers_Productos()
    {
        var (fn, summary, productos) = Build();
        var csv = """
            nombre,categoria,precio,stock
            Libro AZ-204,libros,29.99,50
            Mug Azure,merchandising,9.50,200
            """;

        var json = fn.ImportarCsv(csv, "ventas-junio");

        // El return va al BlobOutput → procesados/ventas-junio-resumen.json
        Assert.NotNull(json);
        Assert.Contains("\"archivo\"", json);
        Assert.Contains("ventas-junio.csv", json);

        // El summary in-memory tiene el resultado
        var resumen = summary.GetByArchivo("ventas-junio.csv");
        Assert.NotNull(resumen);
        Assert.Equal(2, resumen!.LineasOk);

        // Los productos del CSV se crearon en el catálogo (3 seed + 2 nuevos)
        Assert.Equal(5, productos.GetStats().Total);
    }

    [Fact]
    public void ImportarCsv_With_Errors_Still_Persists_Summary()
    {
        var (fn, summary, _) = Build();
        var csv = """
            nombre,categoria,precio,stock
            Bueno,libros,1.00,1
            X,libros,1.00,1
            """;

        var json = fn.ImportarCsv(csv, "mixto");

        var resumen = summary.GetByArchivo("mixto.csv");
        Assert.NotNull(resumen);
        Assert.Equal(1, resumen!.LineasOk);
        Assert.Equal(1, resumen.LineasError);

        // El JSON output también lo refleja
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("lineasOk").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("lineasError").GetInt32());
    }
}
