# S4.1 — Integración con Event Grid y Service Bus

> **Submódulo de referencia:** [M04-S4.1](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.1-event-grid-service-bus-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption
> **Servicios:** Service Bus **Standard** · Event Grid · Azure Storage

> ⚠️ **Coste fijo**: este es el **primer ejemplo del curso con tarifa mensual no
> despreciable**. Service Bus Standard cuesta **~10 €/mes** aunque no envíes
> mensajes. Ejecuta `./04-cleanup.sh` (o borra el RG desde Portal) en cuanto
> termines la demo.

## Objetivo

Pasamos de **triggers aislados** (M03) a un **sistema asíncrono real**:

```
                  POST /api/pedidos
                         │
                         ▼
            ┌────────────────────────┐
            │ CrearPedidoFunction    │
            │ - valida               │
            │ - returns 202 Accepted │
            └─────────┬──────────────┘
                      │
            ┌─────────┴─────────────┐
            ▼                       ▼
  SB Queue                    SB Topic
  "pedidos-procesar"          "pedidos-eventos"
            │                       │
            ▼                       ▼
  ProcesarPedidoFunction    NotificarPedidoCreadoFunction
  (peek-lock, DLQ)          (sub-notificaciones)


  Blob subido a uploads/*.pdf | *.csv
            │
            ▼  (Event Grid BlobCreated)
  ┌─────────────────────────┐
  │ ClasificarArchivoFunction│
  │  └ fan-out por extensión │
  └─────────┬───────────────┘
            ├──→ SB queue "facturas-procesar"  (.pdf)
            └──→ SB queue "imports-procesar"   (.csv)
```

> 🎯 **Patrones clave**: HTTP responde en 202 al instante (slide 13), el
> trabajo viaja por **Service Bus Queue** para procesamiento exclusivo y por
> **Service Bus Topic** para fan-out a N suscriptores. Event Grid (slide 19)
> orquesta el clasificador con peek-lock real (Complete/Abandon/DeadLetter
> de slide 18).

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| HTTP 202 + ServiceBusOutput Queue/Topic | 13 | [`CrearPedidoFunction.cs`](src/AzureFunctions.Demo/Functions/CrearPedidoFunction.cs) |
| ServiceBusTrigger con peek-lock | 18 | [`ProcesarPedidoFunction.cs`](src/AzureFunctions.Demo/Functions/ProcesarPedidoFunction.cs) (`ServiceBusMessageActions` para Complete/Abandon/DeadLetter) |
| Queue vs Topic + Subscription | 11, 12 | `ProcesarPedido` (Queue) vs `NotificarPedidoCreado` (Topic + sub) |
| EventGridTrigger sobre BlobCreated | 6, 19 | [`ClasificarArchivoFunction.cs`](src/AzureFunctions.Demo/Functions/ClasificarArchivoFunction.cs) |
| Fan-out a queues distintas (EG → SB) | 19 | `ClasificarArchivoResult` con 2 outputs nullables |
| Manejo de errores: Complete / Abandon / DeadLetter | 18 | `ProcesarPedidoFunction.ProcesarAsync` |
| Suscripción de Event Grid via az | 8 | [`scripts/02-deploy.sh`](scripts/02-deploy.sh) (se crea tras el deploy) |
| Crear queues + topic + subscription | 14 | [`scripts/01-provision.sh`](scripts/01-provision.sh) |
| Inspección consolidada | 23 | `GET /api/estado` con counters del [`InMemoryEstadoTracker.cs`](src/AzureFunctions.Demo/Services/InMemoryEstadoTracker.cs) |
| Eventos vs Mensajes (la decisión clave) | 3, 21 | sección **Decisión: cuándo cada uno** abajo |

**Slides conceptuales no implementados** (deliberadamente):

- Slide 17 (Sessions) — sólo mención en el README.
- Slide 25 (CloudEvents schema v1.0) — se puede activar luego con
  `--input-schema CloudEventSchemaV1_0` en el topic.
- Slide 26 (Event Grid Namespaces / MQTT) — muy avanzado para el primer
  ejemplo del módulo.
- Slide 20 tiers Premium — Standard cubre todo el curso.

## Estructura

```
S4.1-event-grid-service-bus/
├── README.md
├── AzureFunctions.Demo.slnx
├── src/AzureFunctions.Demo/
│   ├── Functions/
│   │   ├── HelloFunction.cs                    (esqueleto)
│   │   ├── PingFunction.cs                     (health)
│   │   ├── CrearPedidoFunction.cs              ← HTTP → SB Queue + Topic
│   │   ├── ProcesarPedidoFunction.cs           ← SB Queue (peek-lock real)
│   │   ├── NotificarPedidoCreadoFunction.cs    ← SB Topic + Subscription
│   │   ├── ClasificarArchivoFunction.cs        ← Event Grid → fan-out
│   │   └── EstadoFunction.cs                   ← GET /estado consolidado
│   ├── Models/Pedido.cs                        (record + CrearPedidoDto)
│   ├── Services/
│   │   ├── IPedidosOrquestador / PedidosOrquestador
│   │   └── IEstadoTracker / InMemoryEstadoTracker
│   ├── Middleware/
│   ├── host.json                               (extensions.serviceBus)
│   ├── local.settings.json.example
│   └── api.http
├── tests/AzureFunctions.Demo.Tests/            (32 tests)
└── scripts/                                    (az CLI)
    ├── 01-provision.sh                         (Storage + SB Standard + Function App)
    ├── 02-deploy.sh                            (zip deploy + EG subscription)
    ├── 03-smoke-test.sh                        (toca los 4 caminos)
    └── 04-cleanup.sh                           (borra RG — RECUERDA usarlo)
```

## Requisitos

- .NET SDK 10
- Suscripción de Azure
- **Coste estimado de la demo (~1 día)**: ~0,30 € (Service Bus prorrateado +
  Function App Consumption sin tráfico + Storage trivial). Si dejas los
  recursos un mes entero: ~10-12 € por la SB Standard.

## Tests

```bash
dotnet test
```

32 tests sin tocar SB, Event Grid ni Azure:

- **`PedidosOrquestadorTests`** (7) — validación pura, multi-error, serialización
  camelCase, id único por llamada.
- **`CrearPedidoFunctionTests`** (4) — el shape `CrearPedidoResult` con
  HttpResponse 202 + Queue + Topic; los 3 outputs son `null` si la validación
  falla (slide 24 de S3.6).
- **`ProcesarPedidoFunctionTests`** (3) — peek-lock real con un
  `FakeServiceBusMessageActions` que captura Complete/Abandon/DeadLetter:
  mensaje válido → Complete; JSON malformado → DeadLetter con motivo
  `MalformedJson`; sin id → DeadLetter con motivo `EmptyPedido`.
- **`NotificarPedidoCreadoFunctionTests`** (3) — handler de topic+sub:
  válido / malformado / sin id.
- **`ClasificarArchivoFunctionTests`** (6) — fan-out por extensión, ignora
  tipos no relevantes, ignora eventos no-BlobCreated, BlobCreated sin URL
  se ignora, `Theory` con casos case-insensitive sobre la extensión.
- **`EstadoFunctionTests`** (1) — snapshot consolidado.
- **`HelloFunctionTests`** + **`PingFunctionTests`** (4) — heredados.

> 📦 **Truco de testing reusable**: para los tests del SB queue trigger se
> usa `ServiceBusModelFactory.ServiceBusReceivedMessage(...)` para fabricar
> mensajes "como si vinieran del wire" y un `FakeServiceBusMessageActions`
> derivado de la clase abstracta del binding. Esto evita Moq/NSubstitute
> sobre tipos sellados y deja los asserts en propiedades booleanas claras
> (`CompleteCalled`, `DeadLetterReason`).

## Despliegue por Portal de Azure

### 1) Resource Group

Portal → **Resource groups** → **Create** → `rg-curso-m04-s41`.

### 2) Storage Account

Portal → **Storage accounts** → **Create**:
- Name: `stcursom04s41{iniciales}`
- Performance: Standard LRS

Tras crearlo: **Containers** → **+ Container** → `uploads` (acceso privado).

### 3) Service Bus namespace (Standard)

Portal → **Service Bus** → **Create**:
- Namespace: `sb-curso-m04-s41-{iniciales}`
- Pricing tier: **Standard**

> ⚠️ Standard cuesta ~10 €/mes. Si solo vas a hacer la demo de unas horas,
> el coste prorrateado es ~0,30 €/día, pero **no te olvides de borrar el RG**.

Tras crear el namespace:

- **Queues** → **+ Queue** → `pedidos-procesar`. Repite para `facturas-procesar`
  y `imports-procesar`. Marca **Enable dead lettering on message expiration**.
- **Topics** → **+ Topic** → `pedidos-eventos`. Dentro, **+ Subscription** →
  `sub-notificaciones`.

Obtén el connection string: namespace → **Shared access policies** →
`RootManageSharedAccessKey` → **Primary Connection String**.

### 4) Function App

Portal → **Function App** → **Create**:
- Runtime: **.NET 10 Isolated** (o 8)
- OS: **Linux**
- Plan: **Consumption**
- Storage: el del paso 2.

### 5) Conectar Service Bus

Function App → **Configuration** → **+ New application setting**:
- Name: **`ServiceBusConnection`**
- Value: el connection string copiado en el paso 3.

### 6) Deploy

VS Code → click derecho en el proyecto → **Deploy to Function App**.

### 7) Crear suscripción de Event Grid

Una vez desplegado el código (la función `ClasificarArchivo` ya existe en
el Function App):

Portal → tu Storage account → **Events** → **+ Event Subscription**:
- Name: `sub-blob-uploads-s41`
- Event Schema: **Event Grid Schema**
- Event Types: filtra solo **Blob Created**
- Endpoint type: **Azure Function**
- Endpoint: selecciona tu Function App → `ClasificarArchivo`
- Filters → **Subject Filters** → Subject Begins With:
  `/blobServices/default/containers/uploads/`

### 8) Probar

```bash
KEY="<function-key>"
APP="https://func-curso-m04-s41-{iniciales}.azurewebsites.net"

# HTTP → SB Queue + Topic
curl -X POST "$APP/api/pedidos?code=$KEY" \
  -H "Content-Type: application/json" \
  -d '{"clienteId":"c1","clienteEmail":"a@b.c","total":100,"notas":"demo"}'

# Espera ~30s y consulta estado consolidado
sleep 30
curl "$APP/api/estado?code=$KEY"
# Debería ver: encolados=1, procesados=1, notificaciones=1

# Event Grid: subir un PDF al container 'uploads'
echo "fake" > x.pdf
az storage blob upload --account-name stcursom04s41{iniciales} \
  --container-name uploads --file x.pdf --name factura-test.pdf \
  --auth-mode login
sleep 30
curl "$APP/api/estado?code=$KEY"
# clasificados=1 (categoría "factura")
```

### 9) Limpieza obligatoria

Portal → **Resource groups** → `rg-curso-m04-s41` → **Delete resource group**.

## Decisión: cuándo cada uno (slide 3 y 21)

```
¿Quiero NOTIFICAR que algo pasó (a quien le interese)?
  → Event Grid
  Ej: blob creado, usuario registrado, pedido creado (para múltiples consumidores
       independientes que no quiero acoplar)

¿Quiero ENCOLAR trabajo (un consumidor específico lo hará)?
  → Service Bus Queue
  Ej: enviar email, generar factura, procesar pago

¿Quiero NOTIFICAR a múltiples consumidores que harán cosas distintas?
  → Service Bus Topic + Subscriptions
  Ej: "pedido creado" → suscripciones para email + analytics + warehouse,
       cada una con su propio filtro
```

Para esta demo el flujo combina los tres: HTTP encola a **Queue** (un único
consumidor procesa) y al mismo tiempo publica al **Topic** (N suscriptores
reaccionan en paralelo). El **Event Grid** desacopla el "alguien subió un
blob" del "qué hago con ese blob".

## Sessions, deduplicación y otros temas avanzados

Quedan referenciados en las slides 15 (MessageId/CorrelationId/TTL/ScheduledEnqueueTime),
17 (Sessions para FIFO por entidad) y 16 (filtros SQL en suscripciones), pero
no los implementamos aquí. Son extensiones del mismo patrón — añade
`SessionId` al `ServiceBusMessage` y activa `IsSessionsEnabled = true` en
el trigger; añade `--filter-sql-expression` al crear la subscription.

## Próximo paso

[`S4.2 — Durable Functions`](../../../doc/M04-Azure-Functions-II/M04-S4.2-durable-functions-v4.md)
introduce el siguiente nivel: **orquestadores stateful** que coordinan
varias funciones (fan-out/fan-in, async APIs, human-in-the-loop, monitor).
Es el reemplazo declarativo de "Function A llama a Function B que enchufa
a Function C vía Service Bus" cuando el flujo es complejo.
