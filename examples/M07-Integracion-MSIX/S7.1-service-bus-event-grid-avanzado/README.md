# S7.1 — Service Bus y Event Grid: patrones avanzados

> **Submódulo de referencia:** [M07-S7.1](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.1-service-bus-event-grid-avanzado-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** Service Bus **Standard ~10 €/mes** (necesario para topics + filtros SQL — bórralo al acabar)

> 🎓 **Primer submódulo de M07.** Sube de nivel respecto a M04: ya no
> son triggers, son **patrones enterprise de mensajería**. El valor
> docente son las *decisiones* (filtros SQL de suscripción, ventana de
> deduplicación, elección de servicio, workflow de DLQ) — lógica pura
> testeable **sin broker**.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Filtros SQL de suscripción evaluados en el broker (slides 3-5) | [`SqlFilterEvaluator.cs`](src/Messaging.Demo.Api/Messaging/SqlFilterEvaluator.cs) |
| Deduplicación por MessageId + ventana (slide 10) | [`MessageDeduplicator.cs`](src/Messaging.Demo.Api/Messaging/MessageDeduplicator.cs) |
| Elegir servicio (SB/Event Grid/Event Hubs/Storage) + DLQ (slides 9/16/17/30/32) | [`MessagingServiceAdvisor.cs`](src/Messaging.Demo.Api/Messaging/MessagingServiceAdvisor.cs) |
| Plan + checklist del entregable (anti-patterns slide 31) | [`IMessagingPlanner.cs`](src/Messaging.Demo.Api/Messaging/IMessagingPlanner.cs) |
| API que expone los patrones (/messaging/*) | [`MessagingEndpoints.cs`](src/Messaging.Demo.Api/Endpoints/MessagingEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Topic → N suscripciones, copia + DLQ por sub | 3 | `MessagingServiceAdvisor` (fan-out → Topic) |
| Filtros SQL en suscripciones (`total > 100`, `pais = 'ES'`) | 4-5 | [`SqlFilterEvaluator.cs`](src/Messaging.Demo.Api/Messaging/SqlFilterEvaluator.cs) |
| Dead-Letter workflow (motivo → acción, re-submit) | 9, 30 | `MessagingServiceAdvisor.ClasificarDeadLetter` |
| Message Deduplication (ventana 20 s – 7 días) | 10 | [`MessageDeduplicator.cs`](src/Messaging.Demo.Api/Messaging/MessageDeduplicator.cs) |
| Event Grid vs Service Bus por escenario | 16-17 | `MessagingServiceAdvisor.Recomendar` |
| Monitorización DLQ (alerta > 10) | 19 | `IMessagingPlanner.Checklist` |
| Premium: VNet, > 256 KB, Geo-DR | 23-25 | `Recomendar` (VNet/tamaño → Premium) |
| Sessions / FIFO ordering | 26 | `Recomendar` (FIFO → Sessions) |
| Anti-patterns (singleton, MI, lock, idempotencia) | 31 | `IMessagingPlanner.Checklist` |
| Árbol de decisión final | 32 | `MessagingServiceAdvisor.Recomendar` |

## Estructura

```
S7.1-service-bus-event-grid-avanzado/
├── src/Messaging.Demo.Api/
│   ├── Messaging/  SqlFilterEvaluator, MessageDeduplicator,
│   │               MessagingServiceAdvisor (lógica pura)
│   │               + IMessagingPlanner/MessagingPlanner
│   ├── Endpoints/  MessagingEndpoints (/health, /messaging/*)
│   └── Program.cs  AddSingleton<IMessagingPlanner> + enums por nombre
├── tests/Messaging.Demo.Api.Tests/
│   ├── Unit_*            lógica pura (filtro SQL, dedup, advisor)
│   ├── DiContainer_Tests resuelve IMessagingPlanner (contenedor real)
│   └── Api_MessagingTests E2E vía WebApplicationFactory
└── scripts/        01-verify-messaging (entregable — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 54 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `SqlFilterEvaluator` (comparaciones, AND/OR/NOT,
  paréntesis, LIKE, `IS [NOT] NULL`, lógica de 3 valores, sintaxis
  inválida → `FormatException`); `MessageDeduplicator` (duplicado en
  ventana, reentrega fuera de ventana, orden de encolado, rango 20 s –
  7 días); `MessagingServiceAdvisor` (árbol de decisión slide 32 +
  clasificación de DLQ).
- **CAPA 0 · DI**: resuelve `IMessagingPlanner` del contenedor real
  (`Assert.Same` singleton) y planifica. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/messaging/filtro`, `/dedup`, `/recomendar`, `/dlq`, `/plan`.

> 🧠 **Sin CAPA de integración a propósito.** El emulador oficial de
> Service Bus existe, pero exige un sidecar SQL + la topología
> (colas/topics/subs) en un JSON estático y **solo ejercitaría el SDK**,
> no los patrones que enseña S7.1. El valor está en la *decisión*
> (filtro, dedup, elección de servicio), que es lógica pura. Mismo
> criterio que M06 (Entra/KV no emulables): no se inventa una CAPA que
> no aporta. El round-trip real se prueba a mano contra un namespace
> Standard (scripts `az`).

## Ejecución local

```bash
dotnet run --project src/Messaging.Demo.Api
# http://localhost:5096  — usa src/Messaging.Demo.Api/api.http
```

`/health` público. `/messaging/filtro` evalúa un filtro SQL como lo hace
el broker; `/messaging/recomendar` aplica el árbol de la slide 32;
`/messaging/plan` compone el plan + checklist del entregable.

## Despliegue por Portal (entregable)

> ⚠️ **Coste:** un namespace Service Bus **Standard** cuesta **~10 €/mes
> fijos** aunque no lo uses (Basic no soporta topics ni filtros SQL).
> **Bórralo desde el Portal** en cuanto termines la práctica.

1. **Service Bus namespace** *Standard* (no Basic — slide 17).
2. **Topic** `pedidos-eventos` + suscripciones `sub-pedidos-grandes`,
   `sub-espana` con **reglas SQL** (`total > 100`, `pais = 'ES'`,
   slide 4).
3. **Cola** `pedidos-dedup` con *Duplicate detection* ON y ventana
   `P1D` (slide 10).
4. **Managed Identity** de la app con rol *Azure Service Bus Data
   Sender/Receiver* (anti-pattern 5: nada de connection strings).
5. **Alerta** si DLQ count > 10 (slide 19/31).
6. **Verificar** (scripts `az`): SKU Standard, topic + subs + filtros,
   dedup en la cola, contadores de DLQ.

> Scripts `az` en [`scripts/`](scripts) (`./demo.sh`) — **solo lectura**:
> `01-verify-messaging.sh` inventaría namespace/SKU, topic+suscripciones,
> reglas/filtros SQL, deduplicación y DLQ. No crea recursos → sin cleanup
> (el namespace lo borras tú desde el Portal por el coste fijo).

## Ideas centrales

> Service Bus = mensajes (trabajo, garantías de entrega, DLQ por
> suscripción, FIFO con Sessions); Event Grid = eventos (fan-out push a
> webhooks); Event Hubs = streaming/replay; Storage Queue = colas
> simples baratas. Los **filtros SQL se evalúan en el broker** (menos
> tráfico). La **deduplicación** por `MessageId` evita doble
> procesamiento en at-least-once. Monitoriza la **DLQ** y usa **Managed
> Identity** — los anti-patterns de la slide 31 cuestan dinero real.

## Próximo paso

[`S7.2 — Diseño event-driven`](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.2-diseno-event-driven-v3.md):
arquitectura orientada a eventos sobre estos primitivos.
