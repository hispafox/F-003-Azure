using AzureFunctions.Demo.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AzureFunctions.Demo.Tests;

// CAPA 1 — testear la LÓGICA del Timer y del Blob como servicio, sin
// esperar al CRON ni levantar Azurite (slides 10 y 11).
[Trait("Category", "Unit")]
public class Unit_ServiciosExtraidosTests
{
    // Slide 10 — Timer: probamos el servicio, no el trigger.
    [Fact]
    public void Limpieza_Elimina_Solo_Los_Anteriores_Al_Cutoff()
    {
        var svc = new InMemoryLimpiezaService(
            NullLogger<InMemoryLimpiezaService>.Instance);

        // El seed tiene registros de -10d, -5d, -1d, -1h.
        var eliminados = svc.Limpiar(DateTimeOffset.UtcNow.AddDays(-3));

        Assert.Equal(2, eliminados);  // los de -10d y -5d
    }

    [Fact]
    public void Limpieza_Es_Idempotente_Una_Segunda_Pasada_No_Borra_Mas()
    {
        var svc = new InMemoryLimpiezaService(
            NullLogger<InMemoryLimpiezaService>.Instance);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-3);

        Assert.Equal(2, svc.Limpiar(cutoff));
        Assert.Equal(0, svc.Limpiar(cutoff));  // ya no quedan antiguos
    }

    // Slide 11 — Blob: la lógica de parseo recibe el contenido como string.
    [Fact]
    public void Csv_Resumen_Cuenta_Filas_Y_Columnas()
    {
        var svc = new CsvResumenService();

        var r = svc.Procesar("nombre,precio\nLaptop,999\nMonitor,349", "test.csv");

        Assert.Equal("test.csv", r.Archivo);
        Assert.Equal(2, r.TotalFilas);
        Assert.Equal(["nombre", "precio"], r.Columnas);
    }

    [Fact]
    public void Csv_Tolera_CRLF()
    {
        var svc = new CsvResumenService();
        var r = svc.Procesar("a,b\r\n1,2\r\n3,4\r\n", "x.csv");
        Assert.Equal(2, r.TotalFilas);
    }

    [Fact]
    public void Csv_Vacio_Lanza_ArgumentException()
    {
        var svc = new CsvResumenService();
        Assert.Throws<ArgumentException>(() => svc.Procesar("", "v.csv"));
    }
}
