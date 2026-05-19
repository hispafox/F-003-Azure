# S7.2 — Diseño basado en eventos: arquitectura event-driven

> **Submódulo de referencia:** [M07-S7.2](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.2-diseno-event-driven-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (diseño puro; la arquitectura de referencia usa SB Standard ~10 €/mes)

> 🎓 **Submódulo conceptual.** No es un servicio nuevo: es *cómo se
> diseña* un sistema event-driven. El valor docente son las decisiones
> (patrón de evento, cuándo aplicarlo, Saga, anti-patterns) y un **Event
> Store en memoria con replay + snapshot** — lógica pura, sin Azure.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Patrón de evento + cuándo event-driven + Saga (slides 6/8/13/22) | [`EventDesignAdvisor.cs`](src/EventDriven.Demo.Api/EventDriven/EventDesignAdvisor.cs) |
| Anti-patterns: comando disfrazado, dato sensible, sin versión, cadena larga (slide 20) | [`EventValidator.cs`](src/EventDriven.Demo.Api/EventDriven/EventValidator.cs) |
| Event Sourcing: replay + snapshot (slides 14/15/21) | [`EventStore.cs`](src/EventDriven.Demo.Api/EventDriven/EventStore.cs) |
| Plan + checklist del entregable | [`IEventDrivenPlanner.cs`](src/EventDriven.Demo.Api/EventDriven/IEventDrivenPlanner.cs) |
| API que expone los patrones (/eventdriven/*) | [`EventDrivenEndpoints.cs`](src/EventDriven.Demo.Api/Endpoints/EventDrivenEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Síncrono vs event-driven (latencia) | 2-4 | `EventDesignAdvisor.EsBuenCaso` |
| Riesgos / trade-offs | 5 | `IEventDrivenPlanner.Checklist` |
| Notification / Carried-State / Sourcing | 6 | `EventDesignAdvisor.RecomendarPatron` |
| Saga choreography vs orchestration | 8, 22 | `EventDesignAdvisor.RecomendarSaga` |
| Correlation ID / idempotencia / Outbox | 9-11 | `IEventDrivenPlanner.Checklist` |
| Cuándo usar event-driven y cuándo NO | 13 | `EventDesignAdvisor.EsBuenCaso` |
| Event Sourcing (replay) | 14-15 | `PedidoProjection.Reconstruir` |
| Eventual consistency (UX) | 16 | `IEventDrivenPlanner.Checklist` |
| Error handling / compensación | 17, 22 | `EventDesignAdvisor.SecuenciaCompensacion` |
| Anti-patterns | 20 | [`EventValidator.cs`](src/EventDriven.Demo.Api/EventDriven/EventValidator.cs) |
| Snapshotting + proyecciones | 21 | `EventStore` (snapshot cada N) |

## Estructura

```
S7.2-diseno-event-driven/
├── src/EventDriven.Demo.Api/
│   ├── EventDriven/  EventDesignAdvisor, EventValidator,
│   │                 EventStore (+ PedidoProjection, eventos)
│   │                 + IEventDrivenPlanner/EventDrivenPlanner
│   ├── Endpoints/    EventDrivenEndpoints (/health, /eventdriven/*)
│   └── Program.cs    AddSingleton<IEventDrivenPlanner> + enums por nombre
├── tests/EventDriven.Demo.Api.Tests/
│   ├── Unit_*            lógica pura (advisor, validator, event store)
│   ├── DiContainer_Tests resuelve IEventDrivenPlanner (contenedor real)
│   └── Api_EventDrivenTests E2E vía WebApplicationFactory
└── scripts/        01-verify-eventdriven (arquitectura — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 36 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `EventDesignAdvisor` (patrón, buen/mal caso, saga,
  compensación inversa); `EventValidator` (comando disfrazado, dato
  sensible, sin versión, longitud de cadena); `EventStore` (replay
  reconstruye estado, snapshot reduce el replay, total no negativo).
- **CAPA 0 · DI**: resuelve `IEventDrivenPlanner` del contenedor real
  (`Assert.Same` singleton) y compone decisión + patrón + validación.
  Cubre la [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/eventdriven/{patron,caso,saga,compensacion,validar,cadena,sourcing,plan}`.

> 🧠 **Sin CAPA de integración a propósito.** S7.2 es diseño puro: no
> hay un servicio Azure que emular que aporte valor; el Event Store es
> en memoria (el patrón es lo que se enseña, no Cosmos). Mismo criterio
> que M06 y S7.1: no se inventa una CAPA que no aporta.

## Ejecución local

```bash
dotnet run --project src/EventDriven.Demo.Api
# http://localhost:5097  — usa src/EventDriven.Demo.Api/api.http
```

`/eventdriven/sourcing` muestra cómo el snapshot reduce los eventos
reproducidos; `/eventdriven/validar` aplica los anti-patterns de la
slide 20; `/eventdriven/plan` compone el plan + checklist.

## Despliegue por Portal (arquitectura de referencia, slide 12)

> ⚠️ **Coste:** la arquitectura de referencia usa **Service Bus
> Standard (~10 €/mes fijos)**; Cosmos serverless ≈ 0 €. **Bórralo
> desde el Portal** al terminar.

1. **Cosmos DB** (serverless) `pedidos` — write model + Change Feed.
2. **Event Grid / Service Bus Topic** `pedido-eventos` con
   suscripciones `cobros`, `inventario`, `emails` (fan-out, slide 12).
3. **Functions** consumidoras (cobrar / reservar / email) +
   **Cosmos `analytics`** como read model (proyección).
4. **Outbox** = Change Feed de Cosmos (slide 11): nada de "guardar y
   publicar" en dos pasos.
5. **Correlation ID** propagado y **DLQ** monitorizada por suscripción.

> Scripts `az` en [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**:
> `01-verify-eventdriven.sh` inventaría topic+suscripciones, DLQ y la
> cuenta Cosmos. No crea recursos → sin cleanup (borra tú el namespace
> por el coste fijo).

## Ideas centrales

> Event-driven cambia latencia de usuario por complejidad: desacopla,
> escala y resiste, pero exige **eventual consistency, idempotencia,
> Correlation ID y testing distribuido**. Empieza con **Event-Carried
> State Transfer**; usa **Saga** (choreography ≤4 pasos, orchestration
> si más); el **Outbox** fiable es el Change Feed de Cosmos; un evento
> describe algo que **ya pasó** (no es un comando) y va **versionado**.
> Para CRUD simple, un monolito es más sencillo.

## Próximo paso

[`S7.3 — API Management`](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.3-api-management-v3.md):
gateway, políticas y versionado de APIs.
