using AzureFunctions.Demo.Functions;

namespace AzureFunctions.Demo.Tests;

public class ProcesarCsvFunctionTests
{
    [Fact]
    public void Resumir_Extrae_Columnas_Y_Total_Filas()
    {
        var csv = """
            nombre,precio,stock
            Laptop,999,10
            Monitor,349,25
            Teclado,79,50
            """;

        var resumen = ProcesarCsvFunction.Resumir("test", csv);

        Assert.Equal("test.csv", resumen.Archivo);
        Assert.Equal(new[] { "nombre", "precio", "stock" }, resumen.Columnas);
        Assert.Equal(3, resumen.TotalFilas);
        Assert.Equal(3, resumen.Preview.Count);
    }

    [Fact]
    public void Resumir_Preview_Limita_A_3_Filas_Aunque_Haya_Mas()
    {
        var csv = "n\n1\n2\n3\n4\n5";

        var resumen = ProcesarCsvFunction.Resumir("muchos", csv);

        Assert.Equal(5, resumen.TotalFilas);
        Assert.Equal(3, resumen.Preview.Count);
    }

    [Fact]
    public void Resumir_Tolera_CRLF()
    {
        var csv = "a,b\r\n1,2\r\n3,4\r\n";

        var resumen = ProcesarCsvFunction.Resumir("crlf", csv);

        Assert.Equal(2, resumen.TotalFilas);
        Assert.Equal(new[] { "a", "b" }, resumen.Columnas);
    }

    [Fact]
    public void Resumir_Csv_Vacio_Devuelve_Cero_Filas()
    {
        var resumen = ProcesarCsvFunction.Resumir("vacio", "");

        Assert.Equal(0, resumen.TotalFilas);
        Assert.Empty(resumen.Columnas);
        Assert.Empty(resumen.Preview);
    }

    [Fact]
    public void Resumir_Solo_Cabecera_Devuelve_Cero_Filas()
    {
        var resumen = ProcesarCsvFunction.Resumir("solo-headers", "a,b,c");

        Assert.Equal(0, resumen.TotalFilas);
        Assert.Equal(3, resumen.Columnas.Count);
    }
}
