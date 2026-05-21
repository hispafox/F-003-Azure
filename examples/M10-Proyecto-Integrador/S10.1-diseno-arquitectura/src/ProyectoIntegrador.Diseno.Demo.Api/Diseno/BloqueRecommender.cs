namespace ProyectoIntegrador.Diseno.Demo.Api.Diseno;

public enum Bloque
{
    A_Infraestructura,   // slide 5/6 — 45 min
    B_ApiYAuth,          // slide 5/7 — 60 min
    C_FunctionsYSb,      // slide 5/8 — 45 min
    D_PipelineYMonitor,  // slide 5/9 — 30 min
    Terminado,
}

public sealed record RecomendacionBloque(
    Bloque Bloque,
    string Duracion,
    IReadOnlyList<string> Tareas,
    string Justificacion);

// Slide 5 — recomendador del siguiente bloque según el estado del
// sistema. Los bloques tienen orden lógico: A (infra) → B (API +
// Cosmos + Auth) → C (Functions + SB) → D (Pipeline + Monitoring).
// Lógica pura.
public static class BloqueRecommender
{
    public static RecomendacionBloque Recomendar(EstadoSistema sistema)
    {
        ArgumentNullException.ThrowIfNull(sistema);

        // Bloque A — infraestructura base.
        if (sistema.Bicep != EstadoComponente.Desplegado)
            return new(
                Bloque: Bloque.A_Infraestructura,
                Duracion: "45 min",
                Tareas:
                [
                    "Crear `infrastructure/main.bicep` orquestador (slide 6).",
                    "Módulos por dominio: app-service, cosmos-db, functions, " +
                        "service-bus, key-vault, app-insights, rbac (slide 6/13).",
                    "Parámetros por entorno: `params.dev.json` (slide 6).",
                    "Validar con `az bicep build` + `az deployment group validate` " +
                        "+ `az deployment group what-if` (slide 6/8).",
                    "Desplegar: `az deployment group create -g rg-proyecto-<nombre>` (slide 6).",
                ],
                Justificacion: "Sin infra desplegada no hay donde meter la API ni las " +
                    "Functions. Bloque A primero (slide 5).");

        // Bloque B — API + Cosmos + Auth + MI.
        bool bloqueBOk =
            sistema.AppService == EstadoComponente.Desplegado
            && sistema.Cosmos == EstadoComponente.Desplegado
            && sistema.Entra == EstadoComponente.Desplegado
            && sistema.KeyVault == EstadoComponente.Desplegado
            && sistema.ManagedIdentity == EstadoComponente.Desplegado;

        if (!bloqueBOk)
            return new(
                Bloque: Bloque.B_ApiYAuth,
                Duracion: "60 min",
                Tareas:
                [
                    "`AddSingleton<CosmosClient>` con `DefaultAzureCredential()` — sin connection strings (slide 7).",
                    "`AddMicrosoftIdentityWebApiAuthentication` para JWT de Entra ID (slide 7).",
                    "`AddApplicationInsightsTelemetry()` (slide 7).",
                    "Endpoints: `GET /api/productos` (filtra activos) y `POST /api/pedidos` " +
                        "(extrae `sub` del JWT y persiste en Cosmos partition key `clienteId`) (slide 7).",
                    "`GET /health` para los smoke tests del pipeline (slide 7).",
                ],
                Justificacion: "Sin API funcional no hay nada para las Functions ni " +
                    "para los smoke tests del pipeline. Bloque B después de A (slide 5).");

        // Bloque C — Functions + Service Bus + Change Feed.
        bool bloqueCOk =
            sistema.Functions == EstadoComponente.Desplegado
            && sistema.ServiceBus == EstadoComponente.Desplegado;

        if (!bloqueCOk)
            return new(
                Bloque: Bloque.C_FunctionsYSb,
                Duracion: "45 min",
                Tareas:
                [
                    "Function `DetectarPedido` con `CosmosDBTrigger` (lease container) " +
                        "y `ServiceBusOutput` al topic `pedido-eventos` (slide 8).",
                    "Function `NotificarPedido` con `ServiceBusTrigger` en `sub-notificaciones` " +
                        "+ `TrackEvent('NotificacionEnviada')` (slide 8).",
                    "Function `ActualizarAnalytics` con `ServiceBusTrigger` en " +
                        "`sub-analytics` + `CosmosDBOutput` al container `analytics` (slide 8).",
                    "Las 3 functions usan MI (no connection strings con password).",
                ],
                Justificacion: "Sin Functions + SB no hay procesamiento async. " +
                    "Bloque C después de B (slide 5).");

        // Bloque D — Pipeline + Monitoring + alertas.
        bool bloqueDOk =
            sistema.Pipeline == EstadoComponente.Desplegado
            && sistema.AppInsights == EstadoComponente.Desplegado;

        if (!bloqueDOk)
            return new(
                Bloque: Bloque.D_PipelineYMonitor,
                Duracion: "30 min",
                Tareas:
                [
                    "`azure-pipelines.yml` con stages Build → Deploy(staging) → Swap (slide 9).",
                    "Smoke test post-deploy: `curl /health` debe devolver 200 (slide 9).",
                    "Configurar App Insights: connection string en App Service via MI/KV.",
                    "2 alertas mínimas: `5xx > 5 en 5 min` y `latencia avg > 2s en 10 min` (slide 10).",
                    "Dashboard del Portal con requests/min + P95 + error rate (slide 10).",
                ],
                Justificacion: "Pipeline + monitor cierran la entrega. Bloque D último (slide 5).");

        // Todo desplegado.
        return new(
            Bloque: Bloque.Terminado,
            Duracion: "0 min",
            Tareas: ["Pasa por `EntregaEvaluator` para verificar la puntuación final (slide 11)."],
            Justificacion: "Sistema completo: 10/10 componentes desplegados.");
    }
}
