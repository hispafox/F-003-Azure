using AzureFunctions.Demo.Services;

namespace AzureFunctions.Demo.Tests;

public class CsvProductosImporterTests
{
    private static (CsvProductosImporter importer, IProductoService productos) Build()
    {
        var productos = new InMemoryProductoService();
        return (new CsvProductosImporter(productos), productos);
    }

    [Fact]
    public void Import_With_Valid_Rows_Creates_All_Products()
    {
        var (importer, productos) = Build();
        var inicial = productos.GetStats().Total;

        var csv = """
            nombre,categoria,precio,stock
            Libro AZ-204,libros,29.99,50
            Mug Azure,merchandising,9.50,200
            Stickers,merchandising,2.00,500
            """;

        var resultado = importer.Import("test.csv", csv);

        Assert.Equal(3, resultado.LineasTotales);
        Assert.Equal(3, resultado.LineasOk);
        Assert.Equal(0, resultado.LineasError);
        Assert.Equal(3, resultado.ProductosCreados.Count);
        Assert.Equal(inicial + 3, productos.GetStats().Total);
    }

    [Fact]
    public void Import_Reports_Invalid_Header()
    {
        var (importer, _) = Build();
        var csv = "id,nombre,categoria,precio,stock\nrow1,a,b,1,1";

        var resultado = importer.Import("test.csv", csv);

        Assert.Equal(1, resultado.LineasError);
        Assert.Single(resultado.Errores);
        Assert.Contains("Cabecera", resultado.Errores[0].Detalle);
    }

    [Fact]
    public void Import_Skips_Lines_With_Bad_Numbers()
    {
        var (importer, _) = Build();
        var csv = """
            nombre,categoria,precio,stock
            Libro,libros,29.99,50
            BadPrice,libros,no-num,1
            BadStock,libros,1.00,no-int
            """;

        var resultado = importer.Import("test.csv", csv);

        Assert.Equal(3, resultado.LineasTotales);
        Assert.Equal(1, resultado.LineasOk);
        Assert.Equal(2, resultado.LineasError);
        Assert.Equal(2, resultado.Errores.Count);
        Assert.Contains(resultado.Errores, e => e.Detalle.Contains("Precio"));
        Assert.Contains(resultado.Errores, e => e.Detalle.Contains("Stock"));
    }

    [Fact]
    public void Import_Rejects_Negative_Or_Zero_Price()
    {
        var (importer, _) = Build();
        var csv = """
            nombre,categoria,precio,stock
            Cero,libros,0,1
            Negativo,libros,-1,1
            """;

        var resultado = importer.Import("test.csv", csv);

        Assert.Equal(0, resultado.LineasOk);
        Assert.Equal(2, resultado.LineasError);
    }

    [Fact]
    public void Import_Rejects_Short_Names()
    {
        var (importer, _) = Build();
        var csv = """
            nombre,categoria,precio,stock
            X,libros,1,1
            """;

        var resultado = importer.Import("test.csv", csv);

        Assert.Equal(1, resultado.LineasError);
        Assert.Contains("Nombre", resultado.Errores[0].Detalle);
    }

    [Fact]
    public void Import_Of_Empty_File_Returns_Zero_Lines()
    {
        var (importer, _) = Build();

        var resultado = importer.Import("empty.csv", "");

        Assert.Equal(0, resultado.LineasTotales);
        Assert.Equal(0, resultado.LineasOk);
        Assert.Equal(0, resultado.LineasError);
    }

    [Fact]
    public void Import_Tolerates_CRLF_Line_Endings()
    {
        var (importer, _) = Build();
        var csv = "nombre,categoria,precio,stock\r\nLibro,libros,29.99,50\r\n";

        var resultado = importer.Import("test.csv", csv);

        Assert.Equal(1, resultado.LineasOk);
    }
}
