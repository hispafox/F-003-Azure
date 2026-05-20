namespace ClaudeCode.CasosUso.Demo.Api.CasosUso;

public sealed record PromptTemplate(
    CasoUso Caso, string Slide, string Texto, IReadOnlyList<string> Placeholders);

// Slides 2-16 — generador del template canónico de prompt para cada
// caso de uso. Lógica pura. Cada template incluye los 4 ingredientes
// de un prompt sólido: contexto, constraints, formato de salida,
// criterio de éxito (slide 25 conceptual).
public static class PromptTemplateBuilder
{
    public static PromptTemplate ParaCaso(CasoUso caso) => caso switch
    {
        CasoUso.MigracionLegacyANet => new(caso, "2",
            "Analiza {{archivo}} que usa .NET Framework {{versionLegacy}}. " +
            "Migralo a .NET 10:\n" +
            "- Reemplaza {{patronLegacy}} por {{patronModerno}}\n" +
            "- Usa async/await donde haya I/O sincrono\n" +
            "- Mantén la funcionalidad y los nombres públicos.\n" +
            "Criterio éxito: el código compila sin warnings y los tests siguen verdes.",
            ["archivo", "versionLegacy", "patronLegacy", "patronModerno"]),

        CasoUso.DocumentacionDesdeCodigo => new(caso, "3",
            "Genera documentación Markdown para las APIs públicas de {{proyecto}}:\n" +
            "- Cada endpoint con método, URL, parámetros, body, responses\n" +
            "- Tabla de parámetros + ejemplos curl\n" +
            "- Guarda en {{rutaSalida}}.\n" +
            "No inventes endpoints: léelos del código fuente.",
            ["proyecto", "rutaSalida"]),

        CasoUso.CodeReview => new(caso, "4",
            "Revisa los últimos {{n}} commits del repo y analiza:\n" +
            "- Seguridad (SQLi, XSS, secretos en código)\n" +
            "- Rendimiento (N+1, memory leaks potenciales)\n" +
            "- Convenciones del proyecto\n" +
            "- Tests para el código nuevo\n" +
            "Output JSON: [{ severidad, archivo, linea, descripcion }] con severidad en " +
            "{critico, medio, bajo}.",
            ["n"]),

        CasoUso.GenerarDatosPrueba => new(caso, "5",
            "Genera un script C# que cree {{nDocumentos}} documentos de prueba en {{destino}}:\n" +
            "- Datos realistas en español (nombres, emails, direcciones)\n" +
            "- Distribuciones: {{distribucion}}\n" +
            "- Idempotente y con logging de progreso.\n" +
            "Criterio éxito: ejecutar el script dos veces no duplica documentos.",
            ["nDocumentos", "destino", "distribucion"]),

        CasoUso.TroubleshootingLogs => new(caso, "6",
            "Tengo estos logs desde {{horaInicio}}. Analiza el patrón y sugiere causa raíz:\n" +
            "{{logs}}\n\n" +
            "Output: 1) Causa probable, 2) Evidencia en los logs, 3) Solución concreta " +
            "(comando o cambio de código).",
            ["horaInicio", "logs"]),

        CasoUso.PipelineCiCd => new(caso, "7",
            "Crea un pipeline {{plataforma}} para {{proyecto}}:\n" +
            "1) Trigger en push a main + PRs\n" +
            "2) Build {{framework}} + tests con coverage\n" +
            "3) Security scan (vulnerable packages)\n" +
            "4) Deploy a slot staging\n" +
            "5) Smoke test contra /health\n" +
            "6) Swap a producción con aprobación manual\n" +
            "7) Notificación a Teams si falla.\n" +
            "Usa templates para los pasos comunes.",
            ["plataforma", "proyecto", "framework"]),

        CasoUso.BicepDesdeInfra => new(caso, "8",
            "Exporta la infraestructura del resource group {{rg}} a Bicep:\n" +
            "1) `az group export --name {{rg}} > exported.json`\n" +
            "2) `az bicep decompile --file exported.json`\n" +
            "3) Organiza el resultado en módulos por dominio.\n" +
            "Criterio éxito: `az deployment group validate` pasa sin errores.",
            ["rg"]),

        CasoUso.PairProgramming => new(caso, "9",
            "[modo interactive — iteramos paso a paso]\n" +
            "Vamos a implementar {{feature}} en {{proyecto}}.\n" +
            "Empezamos por: 1) modelo de datos → 2) repository con paginación → " +
            "3) validación → 4) tests → 5) `dotnet test` y arreglar lo que falle.\n" +
            "Tras cada paso, espera mi confirmación.",
            ["feature", "proyecto"]),

        CasoUso.ApiCompletaDesdeSpec => new(caso, "10",
            "Tengo esta especificación OpenAPI:\n{{spec}}\n\n" +
            "Genera: 1) Modelos (entity + DTOs), 2) Service con persistencia en " +
            "{{persistencia}}, 3) Endpoints minimal API con [Authorize], 4) Validators " +
            "FluentValidation, 5) Tests xUnit + Moq + FluentAssertions, 6) Atributos OpenAPI.",
            ["spec", "persistencia"]),

        CasoUso.MigracionEsquemaBd => new(caso, "11",
            "Migra el schema de {{db}}:\n" +
            "- Cambio: {{cambio}}\n" +
            "- Backup en container/tabla {{backup}}\n" +
            "- Batch {{batch}} con logging de progreso\n" +
            "- Idempotente.\n" +
            "Salida: un script ejecutable (timer trigger, una sola vez).",
            ["db", "cambio", "backup", "batch"]),

        CasoUso.TestsIntegracionE2e => new(caso, "12",
            "Genera tests de integración para {{flujo}} usando WebApplicationFactory:\n" +
            "1) Autenticación con test user (client credentials)\n" +
            "2) Operaciones HTTP\n" +
            "3) Verificación en {{persistencia}} (Cosmos emulador / Azurite / etc.)\n" +
            "4) Verificación de side effects (Change Feed, Service Bus)\n" +
            "Cada test es independiente y limpia su estado.",
            ["flujo", "persistencia"]),

        CasoUso.OptimizacionRendimiento => new(caso, "13",
            "Analiza {{endpoint}} y sugiere optimizaciones:\n" +
            "Métricas actuales: P50={{p50}}ms, P95={{p95}}ms, P99={{p99}}ms, " +
            "{{ruPorQuery}} RU/query, {{qpm}} queries/minuto.\n" +
            "Objetivo: reducir P99 a < {{objetivoP99}}ms.\n" +
            "Output: cambios concretos con estimación de impacto.",
            ["endpoint", "p50", "p95", "p99", "ruPorQuery", "qpm", "objetivoP99"]),

        CasoUso.DocumentacionTecnica => new(caso, "14",
            "Genera documentación técnica completa de {{proyecto}}:\n" +
            "1) README.md (setup local + arquitectura + contribución)\n" +
            "2) docs/architecture.md (diagrama Mermaid + decisiones)\n" +
            "3) docs/api-reference.md (todos los endpoints)\n" +
            "4) docs/runbooks/ (procedimientos operativos)\n" +
            "5) docs/adr/ (3 ADRs de las decisiones más importantes).\n" +
            "Lee el código fuente, no inventes.",
            ["proyecto"]),

        CasoUso.AnalisisCosteAzure => new(caso, "15",
            "Analiza la infraestructura Bicep de {{proyecto}} y estima coste mensual:\n" +
            "Recursos: {{listaRecursos}}.\n" +
            "Output: rango min-max en EUR + optimizaciones concretas con ahorro estimado.",
            ["proyecto", "listaRecursos"]),

        CasoUso.ExpandContractRefactor => new(caso, "16",
            "Necesito expand-contract sobre {{recurso}}:\n" +
            "- Cambio: {{cambio}}\n" +
            "- {{nServicios}} servicios consumen este recurso\n" +
            "- Producción tiene {{volumen}}\n" +
            "- Sin downtime\n" +
            "- Tengo {{sprints}} sprints.\n" +
            "Plan en 4 fases (Expand → Dual write → Switch reads → Contract) con " +
            "checklist y subagents paralelos para escanear los servicios.",
            ["recurso", "cambio", "nServicios", "volumen", "sprints"]),

        _ => new(CasoUso.Otro, "0",
            "Describe la tarea con: 1) contexto (qué hace el sistema), 2) constraints " +
            "(qué NO debe romper), 3) formato de salida esperado, 4) criterio de éxito.",
            ["contexto", "constraints", "formatoSalida", "criterioExito"]),
    };
}
