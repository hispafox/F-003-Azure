# S4.2 — Durable Functions: orquestación de flujos

> **Submódulo de referencia:** [M04-S4.2](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.2-durable-functions-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption
> **Coste:** ~0 € (Durable usa el Storage del Function App; sin Service Bus ni Cosmos)

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: la analogía del director de orquesta, la regla del determinismo, los cinco patrones (chaining, retry, human interaction, saga, fan-out/fan-in) y los tres trucos para mockear `TaskOrchestrationContext` con NSubstitute.

## Objetivo

Convertir Azure Functions en un **motor de workflows**. Un único caso de
negocio —la **saga de procesamiento de pedido**— combina de forma coherente
los patrones más usados de Durable Functions:

```
POST /api/pedidos/procesar
        │
        ▼
ProcesarPedido (Orchestrator, DETERMINISTA)
        │
        ├─ ValidarPedido        (activity + retry)
        ├─ ReservarInventario   (activity + retry)
        ├─ ¿total > 5000? ──► NotificarManager
        │                     WaitForExternalEvent("AprobacionManager") | timer 72h
        │                       ├─ aprobado=false / timeout → Compensar → "rechazado"
        │                       └─ aprobado=true → continúa
        ├─ ProcesarPago         (activity + retry)
        │     └─ falla tras reintentos → CompensarPedido (libera reserva)
        │                                NotificarRechazo → "compensado"  (SAGA)
        └─ EnviarConfirmacion → "completado"

POST /api/facturas/lote
        ▼
ProcesarLoteFacturas (Orchestrator) — fan-out/fan-in en chunks de 50
```

> 🎯 **La regla de oro (slide 5)**: el orquestador es **determinista**. Nada de
> `DateTime.UtcNow`, `Random`, ni I/O. Solo `context.*` y `CallActivityAsync`.
> Toda la lógica de negocio vive en **servicios inyectados** que las
> **activities** (adaptadores finos) invocan. Por eso el orquestador y los
> servicios se testean sin runtime de Functions ni Storage.

## Patrones cubiertos

| Patrón | Slide | Dónde |
| --- | --- | --- |
| **Chaining** (pasos secuenciales) | 6 | [`ProcesarPedidoOrchestrator.cs`](src/AzureFunctions.Demo/Functions/ProcesarPedidoOrchestrator.cs) |
| **Fan-out / Fan-in** + control de paralelismo | 7 | [`ProcesarLoteFacturasOrchestrator.cs`](src/AzureFunctions.Demo/Functions/ProcesarLoteFacturasOrchestrator.cs) (chunks de 50) |
| **Human interaction** (esperar evento + timeout) | 9 | `EsperarAprobacionAsync` (WaitForExternalEvent + CreateTimer + Task.WhenAny) |
| **Retry policies** (backoff exponencial) | 13 | `RetryActivities` (3 intentos, 5s/10s/20s) |
| **Saga / compensación** | 13 | catch `TaskFailedException` → `CompensarPedido` |
| **Durable Entity** (estado persistente) | 17 | [`ContadorPedidosEntity.cs`](src/AzureFunctions.Demo/Functions/ContadorPedidosEntity.cs) |
| **Starter / status / raise event** | 11 | [`PedidoStarterFunctions.cs`](src/AzureFunctions.Demo/Functions/PedidoStarterFunctions.cs) |
| **Estados de orquestación** | 12 | `customStatus`: esperando-aprobacion / compensando / completado |
| **Persistencia en Storage** | 14 | [`host.json`](src/AzureFunctions.Demo/host.json) → `extensions.durableTask` |

**Slides no implementados** (deliberado, mención en README): Monitor pattern
(slide 8 — es chaining + `CreateTimer` en bucle, ya cubierto conceptualmente),
sub-orquestaciones (slide 10), Eternal Orchestrations / `ContinueAsNew`,
Netherite (slide 36).

## Estructura

```
S4.2-durable-functions/
├── README.md
├── src/AzureFunctions.Demo/
│   ├── Functions/
│   │   ├── HelloFunction.cs / PingFunction.cs   (esqueleto)
│   │   ├── PedidoActivities.cs                  ← activities (adaptadores)
│   │   ├── FacturaActivities.cs                 ← activity del fan-out
│   │   ├── ProcesarPedidoOrchestrator.cs        ← saga (chaining+human+retry+compensación)
│   │   ├── ProcesarLoteFacturasOrchestrator.cs  ← fan-out/fan-in
│   │   ├── ContadorPedidosEntity.cs             ← Durable Entity
│   │   └── PedidoStarterFunctions.cs            ← HTTP starters
│   ├── Models/                                  (Pedido, Reserva, Pago, Factura, …)
│   ├── Services/                                (validador, inventario, pago, …)
│   ├── host.json                                (extensions.durableTask)
│   ├── local.settings.json.example
│   └── api.http
├── tests/AzureFunctions.Demo.Tests/             (22 tests, usa NSubstitute)
└── scripts/                                     (az CLI — provision sin SB/Cosmos)
```

## Por qué la lógica está en servicios y no en el orquestador

El orquestador se ejecuta **muchas veces** (replay, slide 4). Si metiéramos
`DateTime.UtcNow`, `Random` o I/O dentro, cada replay daría un resultado
distinto y el historial dejaría de cuadrar. La solución:

- **Orquestador**: solo decide (`if total > 5000`, `try/catch`, `Task.WhenAny`).
  Usa `context.CurrentUtcDateTime`, `context.CallActivityAsync`, etc.
- **Activities**: adaptadores finos. Llaman a un servicio inyectado.
- **Servicios** (`InMemoryPagoService`, …): TODA la lógica e I/O. Testables.

El fallo de pago es **determinista a propósito**: `InMemoryPagoService`
rechaza cualquier total cuyos céntimos sean `.99`. Así la demo y los tests
pueden forzar el camino de compensación sin azar.

## Tests

```bash
dotnet test
```

22 tests, **sin Storage ni runtime de Durable**:

- **`PedidoServicesTests`** (12) — validador, inventario (reservar/liberar
  idempotente = compensación), pago (`.99` → rechazado, reserva no
  confirmada → rechazado), facturación.
- **`ProcesarPedidoOrchestratorTests`** (5) — el orquestador con un
  `TaskOrchestrationContext` mockeado vía **NSubstitute**:
  - camino feliz: chaining completo, sin pedir aprobación.
  - `TaskFailedException` en el pago → compensación (libera reserva +
    notifica) → estado `compensado`.
  - total > 5000 + evento `true` → continúa hasta `completado`.
  - total > 5000 + evento `false` → compensa, nunca cobra → `rechazado`.
  - total > 5000 + timeout (timer gana el `Task.WhenAny`) → `rechazado`.
- **`ProcesarLoteFacturasOrchestratorTests`** (3) — fan-out/fan-in:
  consolidación correcta, lote vacío, 120 facturas en chunks.
- **`ContadorPedidosStateTests`** (2) — la lógica de la Entity (el State
  POCO; el dispatcher es solo el adaptador).

> 📦 **NSubstitute** se añadió **solo al proyecto de tests** (no afecta al
> runtime). Es el enfoque estándar para Durable: la superficie de
> `TaskOrchestrationContext` (≈20 miembros virtuales) es demasiado grande
> para un fake hand-rolled como el `FakeServiceBusMessageActions` de S4.1.
> Truco clave: `ctx.CreateReplaySafeLogger<T>()` devuelve `null` por defecto
> en el mock → hay que configurarlo a `NullLogger<T>.Instance` o el
> orquestador peta al loguear. `TaskFailedException` tiene ctor público
> `(taskName, taskId, innerException)` — se usa para simular el fallo de
> la activity tras agotar reintentos.

## Despliegue por Portal de Azure

### 1) Resource Group

Portal → **Resource groups** → **Create** → `rg-curso-m04-s42`.

### 2) Storage Account

Portal → **Storage accounts** → **Create**:
- Name: `stcursom04s42{iniciales}`
- Standard LRS

> ℹ️ Durable Functions **necesita** este Storage: persiste el historial de
> cada orquestación en Table Storage y encola el trabajo en Queue Storage
> (slide 14). No hay que crear contenedores a mano — el runtime los crea.

### 3) Function App

Portal → **Function App** → **Create**:
- Runtime: **.NET 10 Isolated** (o 8)
- OS: **Linux** · Plan: **Consumption**
- Storage: el del paso 2.

### 4) Deploy

VS Code → click derecho → **Deploy to Function App**.

### 5) Probar la saga

```bash
KEY="<function-key>"
APP="https://func-curso-m04-s42-{iniciales}.azurewebsites.net/api"

# Pedido normal → completado
R=$(curl -s -X POST "$APP/pedidos/procesar?code=$KEY" \
  -H "Content-Type: application/json" \
  -d '{"id":"p1","clienteId":"cA","clienteEmail":"a@b.c","total":1200,
       "items":[{"sku":"S1","cantidad":1,"precioUnitario":1200}]}')
echo $R    # contiene instanceId
ID=$(echo $R | jq -r .instanceId)

sleep 10
curl -s "$APP/pedidos/estado/$ID?code=$KEY" | jq
# runtimeStatus=Completed, output incluye "completado"

# Pedido que fuerza compensación (total .99)
curl -s -X POST "$APP/pedidos/procesar?code=$KEY" \
  -H "Content-Type: application/json" \
  -d '{"id":"p2","clienteId":"cB","clienteEmail":"b@b.c","total":99.99,
       "items":[{"sku":"S2","cantidad":1,"precioUnitario":99.99}]}'
# Tras unos segundos el estado será output "compensado"

# Pedido > 5000 → queda esperando aprobación
R=$(curl -s -X POST "$APP/pedidos/procesar?code=$KEY" \
  -H "Content-Type: application/json" \
  -d '{"id":"p3","clienteId":"cC","clienteEmail":"c@b.c","total":8500,
       "items":[{"sku":"S3","cantidad":1,"precioUnitario":8500}]}')
ID3=$(echo $R | jq -r .instanceId)
# customStatus = "esperando-aprobacion"

# Mandar la aprobación (el evento que el orquestador espera)
curl -s -X POST "$APP/pedidos/$ID3/aprobar?code=$KEY" \
  -H "Content-Type: application/json" -d '{"aprobado":true}'
# Ahora la orquestación continúa y termina como "completado"
```

### 6) Ver las orquestaciones en el Portal

Portal → Function App → **Durable Functions** (o **Functions** →
`ProcesarPedido` → **Monitor**): verás cada instancia con su estado
(Running / Completed / Failed) y el historial paso a paso.

### 7) Limpieza

Portal → **Resource groups** → `rg-curso-m04-s42` → **Delete resource group**.

## Cuándo Durable y cuándo no (slide 16 / 36)

```
✅ Durable cuando:
   - Coordinas pasos con estado entre ellos (saga, ETL)
   - El flujo dura minutos → días (approval workflows)
   - Esperas un evento externo con timeout
   - Fan-out/fan-in con límites de paralelismo
   - Necesitas compensación automática ante fallos

❌ NO Durable cuando:
   - Simple fan-out sin coordinación → Service Bus topic (S4.1)
   - Real-time < 100ms → el replay + Storage añaden ~50-200ms
   - Pasos independientes sin estado → cola de mensajes basta
   - Cron simple → Timer Trigger (S3.3)
```

S4.1 (Service Bus) y S4.2 (Durable) resuelven problemas distintos: SB
**desacopla** productores/consumidores; Durable **coordina** un flujo con
estado. El patrón híbrido típico: SB para la mensajería entre servicios,
Durable dentro de un servicio para orquestar su flujo interno.

## Próximo paso

[`S4.3 — Errores, reintentos y dead-letter`](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.3-errores-reintentos-deadletter-v4.md)
profundiza en el manejo de errores transversal: políticas de retry,
circuit breaker, dead-letter queues y observabilidad de fallos — aplicable
tanto a los triggers de M03 como a las sagas de este submódulo.
