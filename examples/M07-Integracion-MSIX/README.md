# M07 — Integración y MSIX · ejemplos

Ejemplos de código que acompañan al
[Módulo 7 — Integración y MSIX](../../doc/M07-Integracion-MSIX).

Dos dominios en un módulo: **integración** (mensajería enterprise —
Service Bus/Event Grid avanzado, diseño event-driven, API Management) y
**distribución de escritorio** (ClickOnce vs MSIX, empaquetado,
auto-update, migración). Como en M06, varios submódulos son
**conceptuales**: el valor está en lógica de decisión pura testeable +
el grafo DI real (patrón M05-S5.4/S5.5 / todo M06: CAPA 1 + CAPA 0, sin
forzar integración cuando no aporta).

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| [S7.1](../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.1-service-bus-event-grid-avanzado-v3.md) | Service Bus / Event Grid avanzado (filtros SQL, dedup, request/reply, DLQ) | [`S7.1-service-bus-event-grid-avanzado/`](S7.1-service-bus-event-grid-avanzado/README.md) | ✅ Disponible |
| [S7.2](../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.2-diseno-event-driven-v3.md) | Diseño event-driven (patrones, Saga, anti-patterns, Event Sourcing) | [`S7.2-diseno-event-driven/`](S7.2-diseno-event-driven/README.md) | ✅ Disponible |
| [S7.3](../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.3-api-management-v3.md) | Azure API Management (policies, versionado, tier) | [`S7.3-api-management/`](S7.3-api-management/README.md) | ✅ Disponible |
| [S7.4](../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.4-clickonce-vs-msix-v3.md) | ClickOnce vs MSIX (comparativa, migración, firma) | [`S7.4-clickonce-vs-msix/`](S7.4-clickonce-vs-msix/README.md) | ✅ Disponible |
| [S7.5](../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.5-msix-empaquetado-distribucion-v3.md) | MSIX empaquetado y distribución (manifest, naming, canales) | [`S7.5-msix-empaquetado-distribucion/`](S7.5-msix-empaquetado-distribucion/README.md) | ✅ Disponible |
| [S7.6](../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.6-msix-auto-update-v3.md) | MSIX auto-update (.appinstaller, canary, rollback) | [`S7.6-msix-auto-update/`](S7.6-msix-auto-update/README.md) | ✅ Disponible |
| [S7.7](../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.7-migracion-clickonce-msix-v3.md) | Migración ClickOnce → MSIX (mapper, roadmap, compat check) | [`S7.7-migracion-clickonce-msix/`](S7.7-migracion-clickonce-msix/README.md) | ✅ Disponible |
| S7.P | Práctica — MSIX | — | ⏳ Pendiente |
| S7.P2 | Práctica — MSIX wizard | — | ⏳ Pendiente |

⏳ **Módulo M07 en construcción** (7/9).

## Patrón de tests

- **CAPA 1 · Unit**: la lógica de decisión de mensajería como funciones
  puras (evaluación de filtros SQL de suscripción, ventana de
  deduplicación, elección de servicio de mensajería, clasificación de
  DLQ).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve el grafo real (cubre
  la [lección DI de M03-S3.4](../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md))
  — corre sin Docker.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` (sin broker).
- **Integración**: solo donde haya algo emulable y que aporte valor
  didáctico; en submódulos cuyo valor son **patrones de decisión** (no
  el round-trip del SDK) **no** se fuerza una CAPA de integración
  (documentado en cada README).

## Requisitos comunes

- .NET SDK 10
- (Para los despliegues) suscripción de Azure + Portal
- Docker solo para los submódulos con integración (si aplica)
