using Monitor.AppInsights.Demo.Api.Monitor;

namespace Monitor.AppInsights.Demo.Api.Tests;

// CAPA 1 — parser del shape de `az monitor app-insights query`.
[Trait("Category", "Unit")]
public class Unit_ParserTests
{
    private const string EjemploCli = """
        {
          "tables": [
            {
              "name": "PrimaryResult",
              "columns": [
                {"name": "name", "type": "string"},
                {"name": "p95", "type": "real"},
                {"name": "count_", "type": "long"}
              ],
              "rows": [
                ["GET /api/foo", 450.5, 2100],
                ["POST /api/bar", 1200.2, 580]
              ]
            }
          ]
        }
        """;

    [Fact]
    public void Parsea_Tabla_PrimaryResult_Con_Dos_Filas()
    {
        var r = MonitorResponseParser.Parsear(EjemploCli);
        var tabla = Assert.Single(r.Tablas);
        Assert.Equal("PrimaryResult", tabla.Nombre);
        Assert.Equal(2, tabla.Filas.Count);
        Assert.Equal(2, r.FilasTotales);
    }

    [Fact]
    public void Columnas_Conservan_Orden_Y_Nombres()
    {
        var r = MonitorResponseParser.Parsear(EjemploCli);
        Assert.Equal(new[] { "name", "p95", "count_" },
            r.Tablas[0].Columnas.ToArray());
    }

    [Fact]
    public void Tipo_Long_Convierte_Numero_A_Int64()
    {
        var r = MonitorResponseParser.Parsear(EjemploCli);
        var fila = r.Tablas[0].Filas[0];
        Assert.IsType<long>(fila["count_"]);
        Assert.Equal(2100L, fila["count_"]);
    }

    [Fact]
    public void Tipo_Real_Convierte_A_Double()
    {
        var r = MonitorResponseParser.Parsear(EjemploCli);
        var fila = r.Tablas[0].Filas[0];
        Assert.IsType<double>(fila["p95"]);
        Assert.Equal(450.5, (double)fila["p95"]!);
    }

    [Fact]
    public void Sin_Propiedad_Tables_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => MonitorResponseParser.Parsear("{ \"otra\": [] }"));
    }

    [Fact]
    public void Json_Vacio_Lanza_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => MonitorResponseParser.Parsear(" "));
    }

    [Fact]
    public void Acepta_Tables_Con_Mayuscula_Inicial()
    {
        const string conMayuscula = """
            { "Tables": [ { "Name": "T", "Columns": [], "Rows": [] } ] }
            """;
        var r = MonitorResponseParser.Parsear(conMayuscula);
        Assert.Single(r.Tablas);
    }

    [Fact]
    public void EndpointsLentos_Top3_Devuelve_Strings_Formato_Endpoint_P95()
    {
        var r = MonitorResponseParser.Parsear(EjemploCli);
        var top = MonitorResponseParser.EndpointsLentos(r, topN: 3);
        Assert.Equal(2, top.Count);
        Assert.Contains("GET /api/foo", top[0], StringComparison.Ordinal);
        Assert.Contains("P95=", top[0], StringComparison.Ordinal);
    }

    [Fact]
    public void EndpointsLentos_Tabla_Sin_P95_Devuelve_Vacio()
    {
        const string sinP95 = """
            { "tables": [ { "name": "T",
                "columns": [{"name":"x","type":"string"}],
                "rows": [["y"]] } ] }
            """;
        var r = MonitorResponseParser.Parsear(sinP95);
        Assert.Empty(MonitorResponseParser.EndpointsLentos(r));
    }
}
