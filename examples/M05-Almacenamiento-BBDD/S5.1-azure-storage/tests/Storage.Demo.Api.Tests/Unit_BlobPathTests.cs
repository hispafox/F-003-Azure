using Storage.Demo.Api.Storage;

namespace Storage.Demo.Api.Tests;

// CAPA 1 — lógica pura de "carpetas" en Blob (slide 6). Sin Azure.
[Trait("Category", "Unit")]
public class Unit_BlobPathTests
{
    [Fact]
    public void ParaFecha_Construye_Ruta_Jerarquica()
    {
        var r = BlobPath.ParaFecha(new DateOnly(2026, 5, 9), "factura-001.pdf");
        Assert.Equal("2026/05/factura-001.pdf", r);
    }

    [Fact]
    public void ParaFecha_Normaliza_Barra_Inicial_Y_Espacios()
        => Assert.Equal("2026/01/x.pdf",
            BlobPath.ParaFecha(new DateOnly(2026, 1, 1), "  /x.pdf  "));

    [Theory]
    [InlineData(2026, 5, "2026/05/")]
    [InlineData(2026, 12, "2026/12/")]
    public void PrefijoMes_Pad_A_Dos_Digitos(int a, int m, string esperado)
        => Assert.Equal(esperado, BlobPath.PrefijoMes(a, m));

    [Fact]
    public void PrefijoMes_Mes_Invalido_Lanza()
        => Assert.Throws<ArgumentOutOfRangeException>(() => BlobPath.PrefijoMes(2026, 13));

    [Theory]
    [InlineData("2026/05/f.pdf", "2026/05")]
    [InlineData("raiz.pdf", "")]
    public void Carpeta_Extrae_Todo_Menos_El_Ultimo_Segmento(string blob, string esperado)
        => Assert.Equal(esperado, BlobPath.Carpeta(blob));

    [Fact]
    public void ParaFecha_Archivo_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(
            () => BlobPath.ParaFecha(new DateOnly(2026, 1, 1), "  "));
}
