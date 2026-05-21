namespace Practica.MiniNotas.Demo.Api.MiniNotas;

public enum Feature
{
    WebApp,
    Persistencia,
    EndpointsCrud,
    TestsUnitarios,
    Deploy,
    Auth,
    KeyVault,
    ServiceBus,
    Functions,
    CosmosDb,
    PipelineCiCd,
    AppInsights,
    SlotsSwap,
    ManagedIdentity,
}

public enum Recomendacion { Mini, Completo, EmpezarPorMini }

public sealed record AlcanceMiniNotas(
    IReadOnlyList<Feature> Incluidas,
    IReadOnlyList<Feature> NoIncluidas,
    Recomendacion Cual,
    IReadOnlyList<string> Justificacion);

public sealed record EscenarioObjetivo(
    bool QuieresUnEndToEndMinimo = false,
    bool TienesMenosDeUnaHora = false,
    bool YaConocesM01M02M05 = false,
    bool NecesitasAuthEntra = false,
    bool NecesitasFunctionsYSb = false,
    bool NecesitasPipelineCompleto = false,
    bool QuieresProyectoDeProduccion = false);

// Slide 2 — comparador de alcance: qué tiene la mini-práctica y qué
// NO tiene (vs el proyecto integrador completo de S10.1). Recomienda
// `Mini` para alumnos que quieren end-to-end mínimo en 1 hora,
// `Completo` para alumnos que quieren un proyecto de producción, y
// `EmpezarPorMini` cuando hay tiempo de ambas (mini primero, luego
// añadir capas hacia S10.1). Lógica pura.
public static class AlcanceComparator
{
    // Slide 2 — `Features` incluidas en S10.P2.
    public static IReadOnlyList<Feature> IncluidasEnMini { get; } =
    [
        Feature.WebApp,
        Feature.Persistencia,
        Feature.EndpointsCrud,
        Feature.TestsUnitarios,
        Feature.Deploy,
    ];

    // Slide 2 — `Features` que NO incluye S10.P2 (cubre S10.1).
    public static IReadOnlyList<Feature> NoIncluidasEnMini { get; } =
    [
        Feature.Auth,
        Feature.KeyVault,
        Feature.ServiceBus,
        Feature.Functions,
        Feature.CosmosDb,           // mini usa Table; integrador usa Cosmos
        Feature.PipelineCiCd,
        Feature.AppInsights,
        Feature.SlotsSwap,
        Feature.ManagedIdentity,
    ];

    public static AlcanceMiniNotas Comparar(EscenarioObjetivo e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var razones = new List<string>();

        // Si necesitas auth/SB/functions/pipeline/produccion → completo.
        if (e.QuieresProyectoDeProduccion
            || e.NecesitasAuthEntra
            || e.NecesitasFunctionsYSb
            || e.NecesitasPipelineCompleto)
        {
            razones.Add("Necesitas componentes que S10.P2 NO cubre " +
                "(Auth/Functions/SB/Pipeline) → S10.1 (slide 2).");
            razones.Add("S10.P2 es buen calentamiento, pero no es suficiente para " +
                "el proyecto de producción.");
            return new AlcanceMiniNotas(IncluidasEnMini, NoIncluidasEnMini,
                Recomendacion.Completo, razones);
        }

        // Si quieres end-to-end mínimo en menos de 1h → mini.
        if (e.QuieresUnEndToEndMinimo
            || e.TienesMenosDeUnaHora)
        {
            razones.Add("Objetivo: end-to-end mínimo en 60-75 min → S10.P2 (slide 1).");
            razones.Add("Tocas las 3 capas (Web App + persistencia + deploy) sin " +
                "morirte en el detalle.");
            return new AlcanceMiniNotas(IncluidasEnMini, NoIncluidasEnMini,
                Recomendacion.Mini, razones);
        }

        // Caso por defecto: empieza por mini y luego escala.
        razones.Add("Caso intermedio: arranca por S10.P2 para validar el " +
            "end-to-end básico (slide 2).");
        razones.Add("Después añade capas hacia S10.1: Auth (M06) → Functions (M03) " +
            "→ Pipeline (M08) → App Insights (M08-S8.6).");
        if (!e.YaConocesM01M02M05)
            razones.Add("Repasa M01/M02/M05 antes — esta práctica los integra.");

        return new AlcanceMiniNotas(IncluidasEnMini, NoIncluidasEnMini,
            Recomendacion.EmpezarPorMini, razones);
    }
}
