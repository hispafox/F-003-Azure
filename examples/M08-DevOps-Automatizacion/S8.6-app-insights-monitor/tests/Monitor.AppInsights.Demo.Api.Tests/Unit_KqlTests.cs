using Monitor.AppInsights.Demo.Api.Monitor;

namespace Monitor.AppInsights.Demo.Api.Tests;

// CAPA 1 — KQL canónico generado por `KqlQueryBuilder`. Lógica pura.
[Trait("Category", "Unit")]
public class Unit_KqlTests
{
    [Fact]
    public void P95_Por_Endpoint_Genera_Query_Con_Percentiles_Y_Ventana()
    {
        var q = KqlQueryBuilder.P95PorEndpoint(VentanaTiempo.Ultimas24h, minimoTrafico: 50);
        Assert.Equal("requests", q.Tabla);
        Assert.Contains("ago(24h)", q.Texto, StringComparison.Ordinal);
        Assert.Contains("percentile(duration, 95)", q.Texto, StringComparison.Ordinal);
        Assert.Contains("count_ > 50", q.Texto, StringComparison.Ordinal);
        Assert.Contains("order by p95 desc", q.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Tasa_Error_Por_Hora_Cuenta_5xx_Por_Bin_De_Hora()
    {
        var q = KqlQueryBuilder.TasaErrorPorHora(VentanaTiempo.Ultimos7d);
        Assert.Contains("ago(7d)", q.Texto, StringComparison.Ordinal);
        Assert.Contains("countif(resultCode >= 500)", q.Texto, StringComparison.Ordinal);
        Assert.Contains("bin(timestamp, 1h)", q.Texto, StringComparison.Ordinal);
        Assert.Contains("render timechart", q.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Excepciones_Por_Tipo_Agrupa_Por_Type_Y_OuterMessage()
    {
        var q = KqlQueryBuilder.ExcepcionesPorTipo(VentanaTiempo.Ultimas24h);
        Assert.Equal("exceptions", q.Tabla);
        Assert.Contains("summarize count_=count() by type, outerMessage",
            q.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Dependencias_Lentas_Filtra_Por_Umbral_Y_Ordena()
    {
        var q = KqlQueryBuilder.DependenciasLentas(VentanaTiempo.UltimaHora, umbralMs: 500);
        Assert.Contains("where duration > 500", q.Texto, StringComparison.Ordinal);
        Assert.Contains("by target, type, name", q.Texto, StringComparison.Ordinal);
        Assert.Contains("order by avgDur desc", q.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Correlacion_Genera_Union_Por_OperationId()
    {
        var q = KqlQueryBuilder.TrazaPorOperationId("abc-123");
        Assert.Contains("union requests, dependencies, exceptions, traces",
            q.Texto, StringComparison.Ordinal);
        Assert.Contains("operation_Id == \"abc-123\"", q.Texto, StringComparison.Ordinal);
        Assert.Contains("order by timestamp asc", q.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Correlacion_Escapa_Comillas_Dobles_En_OperationId()
    {
        // Defensa básica frente a inyección por valores accidentales.
        var q = KqlQueryBuilder.TrazaPorOperationId("ab\"c");
        Assert.Contains("ab\\\"c", q.Texto, StringComparison.Ordinal);
    }

    [Fact]
    public void Correlacion_Rechaza_OperationId_Vacio()
    {
        Assert.Throws<ArgumentException>(() => KqlQueryBuilder.TrazaPorOperationId(" "));
    }

    [Fact]
    public void P95_Rechaza_Minimo_Negativo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            KqlQueryBuilder.P95PorEndpoint(VentanaTiempo.UltimaHora, minimoTrafico: -1));
    }

    [Fact]
    public void Uso_Ingesta_Multiplica_GB_Por_Euros_Por_GB()
    {
        var q = KqlQueryBuilder.UsoEingestaPorTipo(VentanaTiempo.Ultimos30d, eurosPorGb: 2.5);
        Assert.Equal("Usage", q.Tabla);
        Assert.Contains("ago(30d)", q.Texto, StringComparison.Ordinal);
        Assert.Contains("gb * 2.5", q.Texto, StringComparison.Ordinal);
        Assert.Contains("order by eurEstimado desc", q.Texto, StringComparison.Ordinal);
    }
}
