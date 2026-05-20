namespace ClaudeCode.CasosUso.Demo.Api.CasosUso;

public enum CasoUso
{
    MigracionLegacyANet,           // slide 2
    DocumentacionDesdeCodigo,      // slide 3
    CodeReview,                    // slide 4
    GenerarDatosPrueba,            // slide 5
    TroubleshootingLogs,           // slide 6
    PipelineCiCd,                  // slide 7
    BicepDesdeInfra,               // slide 8
    PairProgramming,               // slide 9
    ApiCompletaDesdeSpec,          // slide 10
    MigracionEsquemaBd,            // slide 11
    TestsIntegracionE2e,           // slide 12
    OptimizacionRendimiento,       // slide 13
    DocumentacionTecnica,          // slide 14
    AnalisisCosteAzure,            // slide 15
    ExpandContractRefactor,        // slide 16
    Otro,
}

public sealed record ClasificacionCaso(
    CasoUso Caso,
    string Slide,
    IReadOnlyList<string> PalabrasClaveDetectadas);

// Slides 2-16 — clasificador del caso de uso por palabras clave en la
// descripción de la tarea. Lógica pura: no llama a Claude Code; sólo
// mapea descripción → caso para que el alumno reconozca qué template
// aplicar.
public static class CaseClassifier
{
    // Mapeo `palabra-clave → (caso, slide)`. Ordenado por especificidad
    // (los casos más específicos primero — primer match gana).
    private static readonly (string Patron, CasoUso Caso, string Slide)[] Reglas =
    [
        ("expand-contract",         CasoUso.ExpandContractRefactor, "16"),
        ("expand contract",         CasoUso.ExpandContractRefactor, "16"),
        ("rename column",           CasoUso.ExpandContractRefactor, "16"),
        ("sin downtime",            CasoUso.ExpandContractRefactor, "16"),
        ("zero-downtime",           CasoUso.ExpandContractRefactor, "16"),

        (".net framework",          CasoUso.MigracionLegacyANet, "2"),
        ("net framework 4",         CasoUso.MigracionLegacyANet, "2"),
        ("migrar a .net 8",         CasoUso.MigracionLegacyANet, "2"),
        ("migrar a .net 10",        CasoUso.MigracionLegacyANet, "2"),
        ("webclient",               CasoUso.MigracionLegacyANet, "2"),
        ("configurationmanager",    CasoUso.MigracionLegacyANet, "2"),

        ("openapi",                 CasoUso.ApiCompletaDesdeSpec, "10"),
        ("especificación openapi",  CasoUso.ApiCompletaDesdeSpec, "10"),
        ("genera api completa",     CasoUso.ApiCompletaDesdeSpec, "10"),

        ("integration tests",       CasoUso.TestsIntegracionE2e, "12"),
        ("tests de integración",    CasoUso.TestsIntegracionE2e, "12"),
        ("end-to-end",              CasoUso.TestsIntegracionE2e, "12"),
        ("e2e",                     CasoUso.TestsIntegracionE2e, "12"),
        ("webapplicationfactory",   CasoUso.TestsIntegracionE2e, "12"),

        ("code review",             CasoUso.CodeReview, "4"),
        ("revisa los últimos",      CasoUso.CodeReview, "4"),
        ("review",                  CasoUso.CodeReview, "4"),

        ("azure-pipelines",         CasoUso.PipelineCiCd, "7"),
        ("github actions",          CasoUso.PipelineCiCd, "7"),
        ("pipeline ci/cd",          CasoUso.PipelineCiCd, "7"),
        ("pipeline",                CasoUso.PipelineCiCd, "7"),

        // Coste antes que Bicep: "estima el coste mensual de la
        // infraestructura" tiene ambos contextos y "coste" es más
        // específico que la palabra "infraestructura".
        ("coste mensual",           CasoUso.AnalisisCosteAzure, "15"),
        ("coste azure",             CasoUso.AnalisisCosteAzure, "15"),
        ("estima el coste",         CasoUso.AnalisisCosteAzure, "15"),

        ("bicep",                   CasoUso.BicepDesdeInfra, "8"),
        ("infraestructura",         CasoUso.BicepDesdeInfra, "8"),
        ("az group export",         CasoUso.BicepDesdeInfra, "8"),
        ("infrastructure as code",  CasoUso.BicepDesdeInfra, "8"),

        ("optimiza",                CasoUso.OptimizacionRendimiento, "13"),
        ("rendimiento",             CasoUso.OptimizacionRendimiento, "13"),
        ("latency",                 CasoUso.OptimizacionRendimiento, "13"),
        ("p95",                     CasoUso.OptimizacionRendimiento, "13"),
        ("p99",                     CasoUso.OptimizacionRendimiento, "13"),
        ("performance",             CasoUso.OptimizacionRendimiento, "13"),

        ("datos de prueba",         CasoUso.GenerarDatosPrueba, "5"),
        ("seed data",               CasoUso.GenerarDatosPrueba, "5"),
        ("datos sintéticos",        CasoUso.GenerarDatosPrueba, "5"),
        ("fake data",               CasoUso.GenerarDatosPrueba, "5"),

        ("logs",                    CasoUso.TroubleshootingLogs, "6"),
        ("stack trace",             CasoUso.TroubleshootingLogs, "6"),
        ("error en producción",     CasoUso.TroubleshootingLogs, "6"),
        ("troubleshoot",            CasoUso.TroubleshootingLogs, "6"),

        ("schema migration",        CasoUso.MigracionEsquemaBd, "11"),
        ("migración de schema",     CasoUso.MigracionEsquemaBd, "11"),
        ("renombrar campo",         CasoUso.MigracionEsquemaBd, "11"),
        ("renombrar columna",       CasoUso.MigracionEsquemaBd, "11"),

        ("readme.md",               CasoUso.DocumentacionTecnica, "14"),
        ("architecture.md",         CasoUso.DocumentacionTecnica, "14"),
        ("adr",                     CasoUso.DocumentacionTecnica, "14"),
        ("documentación técnica",   CasoUso.DocumentacionTecnica, "14"),

        ("documentación",           CasoUso.DocumentacionDesdeCodigo, "3"),
        ("api-reference",           CasoUso.DocumentacionDesdeCodigo, "3"),
        ("documenta los endpoints", CasoUso.DocumentacionDesdeCodigo, "3"),

        ("vamos a implementar",     CasoUso.PairProgramming, "9"),
        ("paso a paso",             CasoUso.PairProgramming, "9"),
        ("iterativamente",          CasoUso.PairProgramming, "9"),
        ("pair programming",        CasoUso.PairProgramming, "9"),
    ];

    public static ClasificacionCaso Clasificar(string descripcion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);

        var lower = descripcion.ToLowerInvariant();
        CasoUso? caso = null;
        string slide = "";
        var matched = new List<string>();

        foreach (var (patron, c, s) in Reglas)
        {
            if (lower.Contains(patron, StringComparison.Ordinal))
            {
                matched.Add(patron);
                if (caso is null) { caso = c; slide = s; }
            }
        }

        return new ClasificacionCaso(
            Caso: caso ?? CasoUso.Otro,
            Slide: slide,
            PalabrasClaveDetectadas: matched);
    }
}
