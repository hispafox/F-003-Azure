# S8.6 — Application Insights y Azure Monitor

> **Submódulo de referencia:** [M08-S8.6](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.6-app-insights-monitor-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (la lógica es pura; los scripts solo leen un App Insights existente)

> 🎓 **Submódulo conceptual** (lección 9 del HANDOFF): App Insights y KQL
> no se emulan local — no hay ingestión real sin un workspace de Azure.
> Por eso queda CAPA 1 (lógica pura) + CAPA 0 (contenedor DI) + CAPA E2E
> (Minimal API vía `WebApplicationFactory`), sin CAPA de integración.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el monitor de constantes vitales como analogía, las cinco queries KQL canónicas (P95, error rate, excepciones, dependencias, traza por operation_Id), la batería de alertas mínima, el runbook de 5 pasos y control de coste con sampling + daily cap.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Generador de queries KQL canónicas (slides 5, 26) | [`KqlQueryBuilder.cs`](src/Monitor.AppInsights.Demo.Api/Monitor/KqlQueryBuilder.cs) |
| Recomendador de alertas + Smart Detection + runbook (slides 8, 9, 21) | [`AlertRecommender.cs`](src/Monitor.AppInsights.Demo.Api/Monitor/AlertRecommender.cs) |
| Parser del shape de `az monitor app-insights query` (slides 5, 13) | [`MonitorResponseParser.cs`](src/Monitor.AppInsights.Demo.Api/Monitor/MonitorResponseParser.cs) |
| Plan + checklist del entregable | [`IAppInsightsPlanner.cs`](src/Monitor.AppInsights.Demo.Api/Monitor/IAppInsightsPlanner.cs) |
| API que expone la lógica (`/monitor/*`) | [`MonitorEndpoints.cs`](src/Monitor.AppInsights.Demo.Api/Endpoints/MonitorEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Tres pilares: métricas, logs, traces | 2 | `IAppInsightsPlanner.Checklist` |
| `AddApplicationInsightsTelemetry()` (auto-tracking) | 3 | `Checklist` (no se ejercita: sin Azure no ingesta) |
| Custom telemetry (`TrackEvent`, `GetMetric`, `StartOperation`) | 4 | `Checklist` |
| KQL: P95, error rate, exceptions, deps, correlation | 5 | `KqlQueryBuilder.*` |
| Application Map | 6 | `Checklist` (visual del Portal) |
| Live Metrics | 7 | `AlertRecommender.Runbook` paso "DETECTAR" |
| Alertas + Action Groups | 8 | `AlertRecommender.Recomendar` |
| Smart Detection (IA) | 9 | `AlertRecommender.SmartDetectionRecomendada` |
| Distributed tracing (operation_Id) | 10/19 | `KqlQueryBuilder.TrazaPorOperationId` |
| Workbooks | 11 | `Checklist` |
| Sampling adaptativo | 12 | `Checklist` |
| Azure Monitor + Log Analytics Workspace | 13 | `MonitorResponseParser` (shape común) |
| Dashboard de producción | 15 | `Checklist` |
| Costes 2.30 €/GB | 16/20 | `KqlQueryBuilder.UsoEingestaPorTipo` |
| Scheduled-query (KQL alerts) | 18 | `AlertRecommender` añade `pedidos-fallidos-query` si la API es pública |
| Runbook de respuesta a incidentes | 21 | `AlertRecommender.Runbook` |
| Workspace-based App Insights | 23 | `Checklist` |
| KQL avanzado (P50/P95/P99) | 26 | `KqlQueryBuilder.P95PorEndpoint` |
| SLA availability (multi-region) | 27 | `AlertRecommender` añade `sla-availability` si `ProductoConSlaContratado` |

## Estructura

```
S8.6-app-insights-monitor/
├── src/Monitor.AppInsights.Demo.Api/
│   ├── Monitor/    KqlQueryBuilder, AlertRecommender,
│   │              MonitorResponseParser
│   │              + IAppInsightsPlanner/AppInsightsPlanner
│   ├── Endpoints/  MonitorEndpoints (/health, /monitor/*)
│   └── Program.cs  AddSingleton<IAppInsightsPlanner> + enums por nombre
├── tests/Monitor.AppInsights.Demo.Api.Tests/
│   ├── Unit_*                lógica pura (KQL, alertas, parser)
│   ├── DiContainer_Tests     resuelve IAppInsightsPlanner (contenedor real)
│   └── Api_MonitorTests      E2E vía WebApplicationFactory
└── scripts/        queries KQL + listar alertas (SOLO LECTURA, az CLI)
```

## Tests

```bash
dotnet test     # 35 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `KqlQueryBuilder` (P95 con ventana+percentiles, tasa-error con
    `countif(resultCode >= 500)`, dependencias con umbral, correlation
    con `operation_Id` escapando comillas, `UsoEingesta` multiplica €/GB).
  - `AlertRecommender` (5xx + latencia + excepciones siempre; SLA
    contratado eleva 5xx a Sev0Crítico y añade `sla-availability`;
    `TiempoRealCritico` eleva la severidad de latencia; sin webhooks
    solo email; runbook = 5 pasos).
  - `MonitorResponseParser` (parsea tabla `PrimaryResult` con
    `long` → `Int64`, `real` → `double`, case-insensitive para
    `Tables`/`Columns`/`Rows`, rechaza JSON sin `tables`).
- **CAPA 0 · DI**: resuelve `IAppInsightsPlanner` del contenedor real
  (`Assert.Same` singleton) y compone queries + alertas + smart
  detection + runbook + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/monitor/{kql/*, alertas/*, respuesta/parsear, plan}`.

> 🧠 **Sin CAPA de integración a propósito**: App Insights solo ingesta
> contra Azure (no hay emulador). Forzar un test que siempre se salta
> no aporta valor — la lección 9 del HANDOFF lo documenta. Cuando un
> tema es transversal (auth, observabilidad), la verificación termina
> en CAPA E2E con `WebApplicationFactory`.

## Ejecución local

```bash
dotnet run --project src/Monitor.AppInsights.Demo.Api
# http://localhost:5110  — usa src/Monitor.AppInsights.Demo.Api/api.http
```

- `/monitor/kql/p95` devuelve el texto KQL listo para pegar en el portal.
- `/monitor/alertas/recomendar` adapta severidades y canales al escenario.
- `/monitor/respuesta/parsear` mastica el JSON de `az monitor app-insights query`.
- `/monitor/plan` compone todo + checklist.

## Ejecutar KQL contra Azure real (scripts)

```bash
./scripts/demo.sh
# 1) 01-query-kql.sh    → az monitor app-insights query (P95, errores, deps)
# 2) 02-alertas-listar.sh → metric alerts + scheduled-query + action groups
```

Necesita Azure CLI con la extensión `application-insights` (la
instala automáticamente la primera vez). **Solo lectura**: ejecuta
KQL contra el recurso y lista las reglas existentes. Nunca crea ni
modifica nada.

## Despliegue por Portal (entregable)

1. **Crear App Insights** (Portal → Application Insights → Create):
   resource group + nombre + región + **Workspace-based** + Log
   Analytics Workspace existente (slide 13/23). NUNCA Classic.
2. **Cablear en la app**: copia el `ConnectionString` a la App Setting
   `APPLICATIONINSIGHTS_CONNECTION_STRING` del App Service / Function
   App (slide 3). En .NET, añade
   `builder.Services.AddApplicationInsightsTelemetry();`.
3. **Pin del Application Map**: Portal → App Insights → Application
   Map → "Pin to dashboard" (slide 6).
4. **Habilita Smart Detection**: Portal → App Insights → Smart
   Detection → activa Failure Anomalies + Response Time + Memory leak
   (slide 9).
5. **Crea Action Group y alertas mínimas** (slide 8): 5xx > 5 en 5 min,
   latencia > 2 s, excepciones no controladas > 10 en 15 min. Action
   Group: email + Teams (webhook) + PagerDuty si hay on-call 24x7.
6. **Daily cap** (slide 20): Portal → App Insights → Usage and estimated
   costs → Daily cap → 5 GB/día (ajusta a tu tráfico). Evita sorpresas
   de coste.
7. **Workbook "Resumen producción"** (slide 11/15): Portal → App
   Insights → Workbooks → New → pega las queries KQL canónicas
   (`/monitor/kql/*` te las da listas).

## Ideas centrales

> Observabilidad = **métricas + logs + traces** (slide 2). App Insights
> los cubre los tres y se cablea con **una línea** en .NET (slide 3).
> KQL es el idioma común: P95 por endpoint, error rate, exceptions,
> dependencies, traza end-to-end por `operation_Id`. **Smart Detection
> es gratis** (slide 9) — actívalo. **Alertas mínimas** (slide 8):
> 5xx, latencia y excepciones; el runbook (slide 21) convierte la
> alerta en acción reproducible. **Coste**: sampling adaptativo +
> daily cap + filtrar `/health` te dejan en `< 5 GB/mes` gratis
> (slide 12/20). **Workspace-based** (slide 13/23) es obligatorio
> desde 2024 — Classic ya no se crea.

## Próximo paso

[`S8.P — Práctica Pipeline CI/CD`](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.P-practica-pipeline-cicd-v3.md):
montar el pipeline end-to-end (build → test → deploy → smoke test).
