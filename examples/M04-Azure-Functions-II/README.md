# M04 — Azure Functions II · ejemplos

Ejemplos de código que acompañan al
[Módulo 4 — Azure Functions II](../../doc/M04-Azure-Functions-II).

Continuación natural de M03: pasamos de **triggers aislados** a **sistemas
conectados**. Service Bus para mensajería, Event Grid para eventos, Durable
Functions para orquestar, retry policies y dead-letter, deploy/versionado y
testing avanzado.

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| [S4.1](../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.1-event-grid-service-bus-v4.md) | Event Grid + Service Bus | [`S4.1-event-grid-service-bus/`](S4.1-event-grid-service-bus/README.md) | ✅ Disponible |
| [S4.2](../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.2-durable-functions-v4.md) | Durable Functions | [`S4.2-durable-functions/`](S4.2-durable-functions/README.md) | ✅ Disponible |
| [S4.3](../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.3-errores-reintentos-deadletter-v4.md) | Errores, reintentos, dead-letter | [`S4.3-errores-reintentos-deadletter/`](S4.3-errores-reintentos-deadletter/README.md) | ✅ Disponible |
| [S4.4](../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.4-despliegue-versionado-v4.md) | Despliegue y versionado | [`S4.4-despliegue-versionado/`](S4.4-despliegue-versionado/README.md) | ✅ Disponible |
| [S4.5](../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.5-testing-depuracion-v4.md) | Testing y depuración | [`S4.5-testing-depuracion/`](S4.5-testing-depuracion/README.md) | ✅ Disponible |
| [S4.P](../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.P-practica-flujo-completo-v4.md) | Práctica — flujo completo | [`S4.P-practica-flujo-completo/`](S4.P-practica-flujo-completo/README.md) | ✅ Disponible |
| [S4.P2](../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.P2-practica-durable-hello-world-v1.md) | Práctica — Durable Hello World | [`S4.P2-practica-durable-hello-world/`](S4.P2-practica-durable-hello-world/README.md) | ✅ Disponible |

> ✅ **Módulo M04 completo** (5 submódulos + 2 prácticas, 7/7).

## Coste a tener en cuenta

A diferencia de M03 (donde casi todo era serverless/scale-to-zero), **M04
introduce servicios con tarifa mensual mínima**:

- **Service Bus Standard**: ~10 €/mes fijos (no hay free tier serverless real).
  Premium ~670 €/mes — usad Standard salvo necesidad de aislamiento/VNet.
- **Event Grid**: scale-to-zero, ~0,60 €/M eventos (despreciable para demos).
- **Durable Functions** (próximamente S4.2): consume Storage Account (tablas
  + queues internas) → coste despreciable (~0,05 €/mes).

**Regla del módulo**: ejecutar `./04-cleanup.sh` o `az group delete` en cuanto
acabe la demo. Una semana olvidado son ~2,30 € — no es catastrófico pero
mejor evitarlo.

## Hilo conductor

S4.1 establece el **stack de mensajería** (SB Queue + Topic + EG) sobre el
dominio de pedidos heredado de M03. S4.2 introduce **Durable Functions**
para reemplazar las cadenas de SB cuando el flujo es complejo. S4.3 endurece
los handlers con retry policies y DLQ. S4.4 trata deploy slots y versionado.
S4.5 cierra con testing avanzado e integration tests.

## Requisitos comunes

- .NET SDK 10
- Suscripción de Azure
- Azure CLI (`az`)
- Para tests locales: Service Bus NO tiene emulador — apunta a SB real o
  ejecuta solo los tests unitarios (que no requieren conexión).
