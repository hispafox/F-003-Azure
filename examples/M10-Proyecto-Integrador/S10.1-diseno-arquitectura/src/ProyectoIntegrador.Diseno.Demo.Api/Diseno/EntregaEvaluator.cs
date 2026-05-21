namespace ProyectoIntegrador.Diseno.Demo.Api.Diseno;

public enum Criterio
{
    BicepDesplegado,        // slide 11 — 15%
    ApiCrud,                // 15%
    AuthJwt,                // 10%
    CosmosPersistencia,     // 10%
    FunctionsChangeFeed,    // 15%
    ManagedIdentityCero,    // 10%
    PipelineAutomatizado,   // 15%
    AppInsightsAlertas,     // 10%
}

public sealed record EvaluacionCriterio(
    Criterio Criterio, int Peso, bool Cumple, string Detalle);

public sealed record InformeEntrega(
    int PorcentajeTotal,
    bool Aprobada,
    IReadOnlyList<EvaluacionCriterio> Criterios,
    IReadOnlyList<string> PuntosPendientes);

public sealed record EvidenciaEntrega(
    bool BicepDesplegadoConWhatIf = false,
    bool ApiCrudDevuelve2xx = false,
    bool JwtValidaConEntra = false,
    bool DatosPersistenEnCosmos = false,
    bool ChangeFeedTriggerFunctions = false,
    bool SinConnectionStringConPassword = false,
    bool PipelineDesplegaAStaging = false,
    bool AppInsightsTieneTelemetryYAlertas = false);

// Slide 11 — evaluador de la entrega final del proyecto. Lógica pura.
// Los 8 criterios canónicos con sus pesos (15/15/10/10/15/10/15/10 = 100%).
// Devuelve el porcentaje obtenido + las áreas pendientes con detalle.
public static class EntregaEvaluator
{
    private static readonly (Criterio C, int Peso, string Detalle)[] CriteriosPesados =
    [
        (Criterio.BicepDesplegado, 15,
            "Bicep modular desplegado con what-if previo (slide 11)."),
        (Criterio.ApiCrud, 15,
            "API responde a CRUD productos y POST pedidos con 2xx (slide 11)."),
        (Criterio.AuthJwt, 10,
            "Endpoint protegido devuelve 401 sin Bearer; 200 con JWT válido (slide 11)."),
        (Criterio.CosmosPersistencia, 10,
            "Datos persistidos en Cosmos con partition key correcta (slide 11)."),
        (Criterio.FunctionsChangeFeed, 15,
            "Change Feed dispara function que publica en Service Bus (slide 11)."),
        (Criterio.ManagedIdentityCero, 10,
            "Cero connection strings con password en el código y la config (slide 11)."),
        (Criterio.PipelineAutomatizado, 15,
            "Pipeline Build + Test + Deploy a staging + smoke test verde (slide 11)."),
        (Criterio.AppInsightsAlertas, 10,
            "Application Insights con telemetría + al menos 2 alertas activas (slide 11)."),
    ];

    public static InformeEntrega Evaluar(EvidenciaEntrega e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var resultados = new List<EvaluacionCriterio>();
        var pendientes = new List<string>();
        int total = 0;

        foreach (var (c, peso, detalle) in CriteriosPesados)
        {
            bool cumple = CumpleCriterio(c, e);
            resultados.Add(new EvaluacionCriterio(c, peso, cumple, detalle));
            if (cumple) total += peso;
            else pendientes.Add($"{c} ({peso}%): {detalle}");
        }

        return new InformeEntrega(
            PorcentajeTotal: total,
            Aprobada: total >= 70,        // umbral típico de proyecto integrador
            Criterios: resultados,
            PuntosPendientes: pendientes);
    }

    private static bool CumpleCriterio(Criterio c, EvidenciaEntrega e) => c switch
    {
        Criterio.BicepDesplegado => e.BicepDesplegadoConWhatIf,
        Criterio.ApiCrud => e.ApiCrudDevuelve2xx,
        Criterio.AuthJwt => e.JwtValidaConEntra,
        Criterio.CosmosPersistencia => e.DatosPersistenEnCosmos,
        Criterio.FunctionsChangeFeed => e.ChangeFeedTriggerFunctions,
        Criterio.ManagedIdentityCero => e.SinConnectionStringConPassword,
        Criterio.PipelineAutomatizado => e.PipelineDesplegaAStaging,
        Criterio.AppInsightsAlertas => e.AppInsightsTieneTelemetryYAlertas,
        _ => false,
    };
}
