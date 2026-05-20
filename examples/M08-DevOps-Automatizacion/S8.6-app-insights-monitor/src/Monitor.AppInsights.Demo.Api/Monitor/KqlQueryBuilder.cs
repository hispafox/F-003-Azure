namespace Monitor.AppInsights.Demo.Api.Monitor;

public sealed record VentanaTiempo(string Expresion)
{
    public static VentanaTiempo UltimaHora { get; } = new("1h");
    public static VentanaTiempo Ultimas24h { get; } = new("24h");
    public static VentanaTiempo Ultimos7d { get; } = new("7d");
    public static VentanaTiempo Ultimos30d { get; } = new("30d");
}

public sealed record KqlQuery(
    string Nombre, string Slide, string Tabla, string Texto);

// Slide 5, 26 — generador de queries KQL canónicas para App Insights.
// Lógica pura: produce el texto KQL listo para pegar en
// `az monitor app-insights query --analytics-query "..."` o el portal.
// No ejecuta nada (App Insights no se emula local — lección 9 del HANDOFF).
public static class KqlQueryBuilder
{
    private static string Trunca(double d) =>
        d.ToString("0.##",
            System.Globalization.CultureInfo.InvariantCulture);

    // Slide 5/26 — Top endpoints más lentos por percentil P95.
    public static KqlQuery P95PorEndpoint(VentanaTiempo ventana, int minimoTrafico = 100)
    {
        ArgumentNullException.ThrowIfNull(ventana);
        if (minimoTrafico < 0) throw new ArgumentOutOfRangeException(nameof(minimoTrafico));

        return new(
            Nombre: "p95-por-endpoint",
            Slide: "5/26",
            Tabla: "requests",
            Texto:
                $"requests\n" +
                $"| where timestamp > ago({ventana.Expresion})\n" +
                $"| summarize p50=percentile(duration, 50), " +
                    $"p95=percentile(duration, 95), " +
                    $"p99=percentile(duration, 99), " +
                    $"count_=count() by name\n" +
                $"| where count_ > {minimoTrafico}\n" +
                $"| order by p95 desc\n" +
                $"| take 10");
    }

    // Slide 5/26 — Tasa de errores por hora (5xx vs total).
    public static KqlQuery TasaErrorPorHora(VentanaTiempo ventana)
    {
        ArgumentNullException.ThrowIfNull(ventana);

        return new(
            Nombre: "tasa-error-por-hora",
            Slide: "5/26",
            Tabla: "requests",
            Texto:
                $"requests\n" +
                $"| where timestamp > ago({ventana.Expresion})\n" +
                $"| summarize total=count(), " +
                    $"errores=countif(resultCode >= 500) " +
                    $"by bin(timestamp, 1h)\n" +
                $"| extend tasaError = round(errores * 100.0 / total, 2)\n" +
                $"| where tasaError > 0\n" +
                $"| render timechart");
    }

    // Slide 5 — Excepciones agrupadas por tipo.
    public static KqlQuery ExcepcionesPorTipo(VentanaTiempo ventana)
    {
        ArgumentNullException.ThrowIfNull(ventana);

        return new(
            Nombre: "excepciones-por-tipo",
            Slide: "5",
            Tabla: "exceptions",
            Texto:
                $"exceptions\n" +
                $"| where timestamp > ago({ventana.Expresion})\n" +
                $"| summarize count_=count() by type, outerMessage\n" +
                $"| order by count_ desc");
    }

    // Slide 5 — Dependencias lentas (DB, APIs externas, etc.).
    public static KqlQuery DependenciasLentas(VentanaTiempo ventana, int umbralMs = 1000)
    {
        ArgumentNullException.ThrowIfNull(ventana);
        if (umbralMs <= 0) throw new ArgumentOutOfRangeException(nameof(umbralMs));

        return new(
            Nombre: "dependencias-lentas",
            Slide: "5",
            Tabla: "dependencies",
            Texto:
                $"dependencies\n" +
                $"| where timestamp > ago({ventana.Expresion})\n" +
                $"| where duration > {umbralMs}\n" +
                $"| summarize avgDur=avg(duration), count_=count() " +
                    $"by target, type, name\n" +
                $"| order by avgDur desc");
    }

    // Slide 5/19 — Búsqueda por correlation/operation Id (traza end-to-end).
    public static KqlQuery TrazaPorOperationId(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        // El operation_Id es el correlation Id; protege contra inyección
        // sencilla escapando comillas dobles.
        var seguro = operationId.Replace("\"", "\\\"", StringComparison.Ordinal);

        return new(
            Nombre: "traza-por-operation-id",
            Slide: "5/19",
            Tabla: "(union)",
            Texto:
                $"union requests, dependencies, exceptions, traces\n" +
                $"| where operation_Id == \"{seguro}\"\n" +
                $"| order by timestamp asc");
    }

    // Slide 20 — Consumo (GB ingestados) → coste estimado. €/GB es input.
    public static KqlQuery UsoEingestaPorTipo(VentanaTiempo ventana, double eurosPorGb)
    {
        ArgumentNullException.ThrowIfNull(ventana);
        if (eurosPorGb < 0) throw new ArgumentOutOfRangeException(nameof(eurosPorGb));

        return new(
            Nombre: "uso-ingesta-por-tipo",
            Slide: "20",
            Tabla: "Usage",
            Texto:
                $"Usage\n" +
                $"| where TimeGenerated > ago({ventana.Expresion})\n" +
                $"| summarize gb=sum(Quantity)/1000 by Solution, DataType\n" +
                $"| extend eurEstimado = gb * {Trunca(eurosPorGb)}\n" +
                $"| order by eurEstimado desc");
    }
}
