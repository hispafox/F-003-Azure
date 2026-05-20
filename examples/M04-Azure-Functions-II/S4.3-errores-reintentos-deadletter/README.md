# S4.3 — Gestión de errores, reintentos y dead-letter queues

> **Submódulo de referencia:** [M04-S4.3](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.3-errores-reintentos-deadletter-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption
> **Servicios:** Service Bus **Standard** (cola con dead-lettering)

> ⚠️ **Coste fijo**: Service Bus Standard ~10 €/mes aunque no envíes nada.
> `./04-cleanup.sh` o borra el RG al acabar.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el triaje de urgencias como analogía, la clasificación transitorio/permanente/desconocido, idempotencia con `TryAdd`, circuit breaker con Polly y el procesador de la dead-letter queue.

## Objetivo

Materializar la **"estrategia completa de errores"** del submódulo (slide 13):
un procesador de mensajes resiliente que clasifica los fallos y los maneja
distinto según su naturaleza, más el **poison-message processor** que recoge
lo que acaba en la dead-letter queue.

```
Mensaje en cola "pedidos-procesar"
        │
        ▼
ProcesarPedido
  ├─ JSON malformado ───────────────► dead-letter inmediato (PERMANENTE)
  ├─ id ya procesado ───────────────► Complete (idempotencia, slide 10)
  ├─ trabajo OK ────────────────────► Complete + registra idempotencia
  └─ excepción → IErrorClassifier:
        Transitorio  → Abandon (SB reintenta, maxDeliveryCount)
        Permanente   → dead-letter (con motivo)
        Desconocido  → log CRITICAL + Abandon

  tras maxDeliveryCount fallos
        ▼
pedidos-procesar/$deadletterqueue
        ▼
ProcesarDeadLetter → IPoisonClassifier:
   Discard         (JSON: nunca parseará)
   NotifyAndRetry  (timeout: pudo ser pico)
   Quarantine      (MaxDeliveryCount / negocio / desconocido)
```

> 🎯 **La idea central (slide 3)**: no todos los errores se tratan igual.
> Reintentar un JSON malformado es malgastar recursos; mandar a dead-letter
> un timeout transitorio es perder trabajo recuperable. La clave es
> **clasificar** y actuar en consecuencia. Esa clasificación es lógica pura
> → 100 % testeable sin Azure.

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Tipos de error (transitorio/permanente) | 3 | [`ErrorClassifier.cs`](src/AzureFunctions.Demo/Services/IErrorClassifier.cs) |
| Reintentos por trigger (`maxDeliveryCount`) | 4 | cola con `--max-delivery-count 5` en [`01-provision.sh`](scripts/01-provision.sh) |
| Retry policy global | 5 | [`host.json`](src/AzureFunctions.Demo/host.json) → `retry` |
| Backoff exponencial + jitter | 6 | [`PollyResilientApiClient.cs`](src/AzureFunctions.Demo/Services/PollyResilientApiClient.cs) (`UseJitter = true`) |
| Dead-letter queue | 7 | `[ServiceBusTrigger("pedidos-procesar/$deadletterqueue")]` |
| Procesar la DLQ automáticamente | 8, 16 | [`ProcesarDeadLetterFunction.cs`](src/AzureFunctions.Demo/Functions/ProcesarDeadLetterFunction.cs) + [`PoisonClassifier.cs`](src/AzureFunctions.Demo/Services/IPoisonClassifier.cs) |
| Circuit breaker | 9 | `PollyResilientApiClient` (`AddCircuitBreaker`) |
| Idempotencia | 10 | [`InMemoryIdempotencyStore.cs`](src/AzureFunctions.Demo/Services/IIdempotencyStore.cs) (`TryAdd`) |
| Logging estructurado con scope | 11 | `_logger.BeginScope(...)` en ambas funciones |
| Estrategia completa de errores | 13 | `ProcesarPedidoFunction.ProcesarAsync` (el switch por `TipoError`) |
| Checklist de resiliencia | 15 | sección **Checklist** abajo |

**Slides no implementados** (mención en README): Outbox pattern (slide 17 —
requiere BD transaccional, sería otro ejemplo entero), alertas
`az monitor metrics alert` (slide 12 — se documentan, no se ejecutan),
Saga/compensación (slide 14 — cubierto a fondo en S4.2).

## Una nota importante: `[ExponentialBackoffRetry]` y Service Bus

El slide 5 muestra `[ExponentialBackoffRetry]` sobre un `ServiceBusTrigger`.
**En el isolated worker eso NO compila** (el analyzer `AZFW0012` lo rechaza):
los atributos de retry a nivel de función solo aplican a triggers **sin**
mecanismo propio de reintentos (Timer, Event Hub). Service Bus ya trae el
suyo: `maxDeliveryCount` de la cola + el `Abandon` explícito que hacemos.
Lo dejamos documentado en el código en vez de romper el build — es
exactamente el tipo de detalle que un alumno necesita saber.

## Estructura

```
S4.3-errores-reintentos-deadletter/
├── README.md
├── src/AzureFunctions.Demo/
│   ├── Functions/
│   │   ├── HelloFunction.cs / PingFunction.cs   (esqueleto)
│   │   ├── ProcesarPedidoFunction.cs            ← clasifica errores + idempotencia
│   │   ├── ProcesarDeadLetterFunction.cs        ← poison-message processor
│   │   └── EstadoFunction.cs                    ← GET /estado
│   ├── Models/Pedido.cs                         (Pedido, TipoError, PoisonAction)
│   ├── Services/
│   │   ├── IErrorClassifier / ErrorClassifier
│   │   ├── IPoisonClassifier / PoisonClassifier
│   │   ├── IIdempotencyStore / InMemoryIdempotencyStore
│   │   ├── IResilientApiClient / PollyResilientApiClient  (Polly v8)
│   │   ├── IEstadoTracker / InMemoryEstadoTracker
│   │   └── Excepciones.cs                        (Transitorio/Permanente/Circuito)
│   ├── Middleware/                               (heredado, Correlation-Id)
│   ├── host.json                                 (retry global + serviceBus)
│   └── local.settings.json.example
├── tests/AzureFunctions.Demo.Tests/             (44 tests)
└── scripts/                                     (az CLI — SB Standard)
```

## Tests

```bash
dotnet test
```

44 tests, **sin Azure ni Service Bus real**:

- **`ErrorClassifierTests`** (8) — tabla de clasificación: JSON/negocio/
  validación → Permanente; transitorio/timeout/429/503/circuito → Transitorio;
  HTTP sin status (conexión) → Transitorio; 404 y lo no catalogado → Desconocido.
- **`PoisonClassifierTests`** (6) — JSON→Discard, timeout→NotifyAndRetry,
  MaxDeliveryCount/negocio/desconocido→Quarantine, nulls no rompen.
- **`IdempotencyStoreTests`** (4) — incluye 200 hilos sobre la misma clave →
  exactamente 1 gana (la lección `TryAdd` vs `GetOrAdd` de S3.5).
- **`PollyResilientApiClientTests`** (5) — éxito a la primera, retry de
  transitorios hasta éxito, permanente NO se reintenta, **el circuito abre
  tras fallos sostenidos** y lanza `CircuitoAbiertoException`.
- **`ProcesarPedidoFunctionTests`** (7) — el switch completo de la estrategia
  de errores con `FakeServiceBusMessageActions` (Complete/Abandon/DeadLetter)
  + el camino de idempotencia (duplicado se salta).
- **`ProcesarDeadLetterFunctionTests`** (4) — clasificación poison + el
  invariante "siempre Complete" (la DLQ nunca crece sin fin).
- **`HelloFunctionTests`** + **`PingFunctionTests`** (4) — heredados.

> 📦 **Detalle de testing reutilizable**: `ServiceBusModelFactory` **no**
> tiene parámetros `deadLetterReason`/`deadLetterErrorDescription`. Esas
> propiedades de `ServiceBusReceivedMessage` se leen de `ApplicationProperties`
> con claves bien conocidas (`"DeadLetterReason"`, `"DeadLetterErrorDescription"`),
> así que en los tests se pasan por el diccionario `properties:`. Documentado
> para no repetir el descubrimiento en S4.4/S4.5.
>
> Y otro: `Activator.CreateInstance(tipo, "msg")` **no** honra parámetros
> opcionales del constructor — las excepciones custom con `(string, Exception? = null)`
> fallan. Los `[Theory]` usan `TheoryData<Exception>` con instancias
> construidas explícitamente.

## Despliegue por Portal de Azure

### 1) Resource Group

Portal → **Resource groups** → **Create** → `rg-curso-m04-s43`.

### 2) Storage Account

Portal → **Storage accounts** → **Create** → `stcursom04s43{iniciales}`,
Standard LRS. (Solo lo necesita el runtime de Functions.)

### 3) Service Bus (Standard) con dead-lettering

Portal → **Service Bus** → **Create**:
- Namespace: `sb-curso-m04-s43-{iniciales}`
- Pricing tier: **Standard** (~10 €/mes)

Tras crearlo: **Queues** → **+ Queue** → `pedidos-procesar`:
- **Max delivery count**: 5 (tras 5 entregas fallidas → DLQ)
- **Enable dead lettering on message expiration**: ✓

Copia el connection string: namespace → **Shared access policies** →
`RootManageSharedAccessKey` → **Primary Connection String**.

### 4) Function App + conectar Service Bus

Portal → **Function App** → **Create** (.NET 10 Isolated, Linux,
Consumption, Storage del paso 2).

Function App → **Configuration** → **+ New application setting**:
- Name: **`ServiceBusConnection`** · Value: el connection string del paso 3.

### 5) Deploy

VS Code → click derecho → **Deploy to Function App**.

### 6) Probar los caminos

Portal → Service Bus → `sb-...` → Queues → `pedidos-procesar` →
**Service Bus Explorer** → **Send messages**:

```jsonc
// [1] OK → se procesa
{"id":"ped-1","clienteId":"c","clienteEmail":"a@b.c","total":100}

// [2] el MISMO mensaje otra vez → duplicado saltado (idempotencia)

// [3] JSON malformado → dead-letter inmediato (permanente)
{ broken json
```

Tras unos segundos:

```bash
curl "https://func-curso-m04-s43-{iniciales}.azurewebsites.net/api/estado?code=<key>"
# procesados=1, duplicadosSaltados=1, enviadosADeadLetter=1, poisonProcesados=1
```

El mensaje [3] que fue a `pedidos-procesar/$deadletterqueue` dispara
`ProcesarDeadLetter`, que lo clasifica (Discard, por JSON) y lo completa.
Mira la DLQ en Service Bus Explorer: debería vaciarse sola.

### 7) Limpieza obligatoria

Portal → **Resource groups** → `rg-curso-m04-s43` → **Delete resource group**.

## Checklist de resiliencia (slide 15)

```
[x] Retry policy (host.json: exponential backoff) + jitter (Polly)
[x] Dead-letter queue habilitada en la cola
[x] Función de procesamiento de dead-letter implementada
[x] Circuit breaker para servicios externos (Polly v8)
[x] Idempotencia (TryAdd atómico, validado con 200 hilos)
[x] Logging estructurado con scope (MessageId, DeliveryCount, TipoError)
[x] Clasificación transitorio / permanente / desconocido
[ ] Alertas az monitor (documentadas, no ejecutadas — slide 12)
[ ] Outbox pattern (slide 17 — fuera de alcance, requiere BD transaccional)
```

## Próximo paso

[`S4.4 — Despliegue y versionado`](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.4-despliegue-versionado-v4.md):
slots de despliegue para Functions, versionado de la API, blue/green y
rollback — cómo llevar todo lo de M03/M04 a producción de forma segura.
