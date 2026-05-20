namespace Monitor.AppInsights.Demo.Api.Monitor;

public enum Severidad
{
    Sev0Critico = 0, // pierde tráfico → page inmediatamente
    Sev1Alto = 1,    // degradación clara → revisar en horario
    Sev2Medio = 2,   // tendencia preocupante
    Sev3Bajo = 3,    // ruido bajo / informativo
    Sev4Info = 4,
}

public sealed record CanalAccion(string Tipo, string Destino);

public sealed record ReglaAlerta(
    string Nombre,
    string Slide,
    Severidad Severidad,
    string Condicion,
    string Ventana,
    string Frecuencia,
    string Descripcion,
    IReadOnlyList<CanalAccion> Acciones);

public sealed record EscenarioAlertas(
    bool ApiPublica = true,
    bool TiempoRealCritico = false,
    bool ProductoConSlaContratado = false,
    string EmailEquipo = "equipo@empresa.com",
    string? WebhookTeams = null,
    string? WebhookPagerDuty = null);

// Slide 8/18/21 — recomendador de alertas para Application Insights.
// Genera la batería mínima de reglas (5xx, latencia, smart detection) y
// adapta severidades y canales según el escenario. Lógica pura.
public static class AlertRecommender
{
    // Slide 8 — la batería canónica para una API en producción.
    public static IReadOnlyList<ReglaAlerta> Recomendar(EscenarioAlertas escenario)
    {
        ArgumentNullException.ThrowIfNull(escenario);

        var acciones = CanalesDeAccion(escenario);
        var sev5xx = escenario.ProductoConSlaContratado || escenario.TiempoRealCritico
            ? Severidad.Sev0Critico
            : Severidad.Sev1Alto;
        var sevLatencia = escenario.TiempoRealCritico
            ? Severidad.Sev1Alto
            : Severidad.Sev2Medio;

        var reglas = new List<ReglaAlerta>
        {
            new(
                Nombre: "5xx-alta-tasa",
                Slide: "8",
                Severidad: sev5xx,
                Condicion: "count requests/failed > 5",
                Ventana: "5m",
                Frecuencia: "1m",
                Descripcion: "Más de 5 errores 5xx en 5 minutos (slide 8).",
                Acciones: acciones),

            new(
                Nombre: "latencia-alta",
                Slide: "8",
                Severidad: sevLatencia,
                Condicion: "avg requests/duration > 2000",
                Ventana: "10m",
                Frecuencia: "1m",
                Descripcion: "Duración media de requests > 2s (slide 8).",
                Acciones: acciones),

            new(
                Nombre: "excepciones-no-controladas",
                Slide: "8/9",
                Severidad: Severidad.Sev2Medio,
                Condicion: "count exceptions/server > 10",
                Ventana: "15m",
                Frecuencia: "5m",
                Descripcion: "Más de 10 excepciones no controladas en 15 min.",
                Acciones: acciones),
        };

        if (escenario.ApiPublica)
            reglas.Add(new(
                Nombre: "pedidos-fallidos-query",
                Slide: "18",
                Severidad: Severidad.Sev1Alto,
                Condicion: "count > 10 (scheduled-query, KQL)",
                Ventana: "15m",
                Frecuencia: "5m",
                Descripcion: "Alerta basada en KQL: requests | where resultCode >= 500 (slide 18).",
                Acciones: acciones));

        if (escenario.ProductoConSlaContratado)
            reglas.Add(new(
                Nombre: "sla-availability",
                Slide: "27",
                Severidad: Severidad.Sev0Critico,
                Condicion: "AvailabilityPct < 99.9 (KQL diario)",
                Ventana: "1d",
                Frecuencia: "1h",
                Descripcion: "Disponibilidad por debajo de SLA contractual (slide 27).",
                Acciones: acciones));

        return reglas;
    }

    // Slide 9 — recordatorio: activar Smart Detection (alertas IA "gratis").
    public static IReadOnlyList<string> SmartDetectionRecomendada { get; } =
    [
        "Failure Anomalies (5xx fuera del baseline) — slide 9.",
        "Response Time degradation — slide 9.",
        "Memory leak detection — slide 9.",
        "Dependency failure (API externa o DB que empieza a fallar) — slide 9.",
        "Security: anomalías de tráfico, intentos SQLi — slide 9.",
    ];

    // Slide 21 — pasos del runbook de respuesta a incidentes.
    public static IReadOnlyList<string> Runbook { get; } =
    [
        "DETECTAR (0-2 min): Live Metrics + Failures → ¿qué endpoint? (slide 7/21).",
        "DIAGNOSTICAR (2-10 min): Transaction Search por operation_Id; ¿deploy reciente? (slide 21).",
        "MITIGAR (10-20 min): rollback (swap) / escalar / feature flag OFF (slide 21).",
        "RESOLVER: RCA + fix + tests; actualiza runbook si es escenario nuevo (slide 21).",
        "POST-MORTEM: documentar qué pasó y acción preventiva (slide 21).",
    ];

    private static IReadOnlyList<CanalAccion> CanalesDeAccion(EscenarioAlertas e)
    {
        var canales = new List<CanalAccion>
        {
            new("email", e.EmailEquipo),
        };

        if (!string.IsNullOrWhiteSpace(e.WebhookTeams))
            canales.Add(new("teams", e.WebhookTeams!));
        if (!string.IsNullOrWhiteSpace(e.WebhookPagerDuty))
            canales.Add(new("pagerduty", e.WebhookPagerDuty!));

        return canales;
    }
}
