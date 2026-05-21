namespace Practica.MiniNotas.Demo.Api.MiniNotas;

public sealed record PlanMiniNotas(
    ReportePreflight Preflight,
    IReadOnlyList<InformePaso> Pasos,
    AlcanceMiniNotas? Alcance,
    IReadOnlyList<string> CaminoHaciaS101,
    IReadOnlyList<string> Checklist);

public sealed record PlanRequest(
    EscenarioPreflight Preflight,
    IReadOnlyList<EvidenciaPaso> Evidencias,
    EscenarioObjetivo? Objetivo = null);

// Compone MiniNotasPreflight + PasoChecker + AlcanceComparator en el
// plan + checklist (slide 2) + camino de extensión hacia S10.1 (slide
// 2 negativo: qué añadir si quieres llegar al proyecto completo).
// Servicio inyectable.
public interface IPracticaMiniNotasPlanner
{
    PlanMiniNotas Planificar(PlanRequest req);
}

public sealed class PracticaMiniNotasPlanner : IPracticaMiniNotasPlanner
{
    // Slide 2 — qué añadir para llegar del mini a S10.1.
    public static IReadOnlyList<string> CaminoHaciaS101Slide2 { get; } =
    [
        "1. Sustituye Table Storage por Cosmos DB (M05-S5.3) y mantén la API estable.",
        "2. Añade auth con Microsoft Entra (M06-S6.2) y `[Authorize]` en los endpoints.",
        "3. Guarda los secretos en Key Vault y conéctate con Managed Identity (M05-S5.4 + M06-S6.6).",
        "4. Añade Service Bus + Functions Change Feed para procesamiento async (M03 + M04 + M07).",
        "5. Monta el pipeline CI/CD con OIDC + slots staging/prod (M08-S8.P).",
        "6. Activa Application Insights + las 2 alertas mínimas (M08-S8.6).",
        "7. Cuando todo esté, pasa por S10.1 (`EntregaEvaluator`) para autoevaluar.",
    ];

    public PlanMiniNotas Planificar(PlanRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);

        var preflight = MiniNotasPreflight.Comprobar(req.Preflight);
        var pasos = req.Evidencias.Select(PasoChecker.Evaluar).ToList();
        var alcance = req.Objetivo is not null
            ? AlcanceComparator.Comparar(req.Objetivo)
            : null;

        return new PlanMiniNotas(
            Preflight: preflight,
            Pasos: pasos,
            Alcance: alcance,
            CaminoHaciaS101: CaminoHaciaS101Slide2,
            // Slide 2 — checklist canónica de los 11 pasos.
            Checklist:
            [
                "Modelo `Note` diseñado con PartitionKey + RowKey + campos de dominio (slide 4).",
                "Solución `.sln` con `src/MiniNotas` + `tests/MiniNotas.Tests` (slide 5).",
                "`Note.cs` implementa `ITableEntity` y DTOs separados para Create/Update (slide 6).",
                "`NotesRepository` + interface con 5 operaciones CRUD (slide 7).",
                "5 endpoints REST en `Program.cs` con minimal API (slide 8).",
                "Tests unitarios del repositorio: GetAll vacío, Create + GetById, Delete (slide 9).",
                "`smoke-tests.sh` con los 5 pasos del CRUD + verificación 200/204/404 (slide 10).",
                "Infra Azure: RG + Storage + plan F1 + Web App (slide 11).",
                "Deploy con `az webapp deploy --type zip` (slide 12).",
                "Validación end-to-end con el smoke test contra la URL pública (slide 13).",
                "Cleanup: `az group delete` + verificar que el RG ya no existe (slide 14).",
            ]);
    }
}
