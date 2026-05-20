# S3.6 — Bindings de entrada y salida

> **Submódulo de referencia:** [M03-S3.6](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.6-bindings-entrada-salida-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption (gratuito)
> **Servicios:** Cosmos DB · Azure Storage (Queue + Blob)

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: la mesa con tres salidas (MultiResponse pattern), input bindings sin abrir cliente, binding expressions con fecha y queue trigger anti-pattern aware.

## Objetivo

Es el submódulo de **consolidación** del bloque de triggers. En vez de un caso de
negocio profundo, este ejemplo es un **proyecto de referencia** con varias
funciones pequeñas, cada una demostrando una combinación de bindings que vas a
reusar en el día a día:

| Función | Demuestra | Slides |
| --- | --- | --- |
| `GET /api/pedidos/{clienteId}/{id}` | `[CosmosDBInput]` por id | 4 |
| `GET /api/clientes/{clienteId}/pedidos` | `[CosmosDBInput]` por `SqlQuery` con placeholders dinámicos | 4, 10 |
| `POST /api/pedidos` | **Multi-output**: HTTP + `[CosmosDBOutput]` + `[QueueOutput]` en una sola función | 6, 24 |
| `GET /api/exportar/{clienteId}/{id}` | Pipeline: `[CosmosDBInput]` → `[BlobOutput]` con expresión `{DateTime:yyyy-MM-dd}` | 7, 10, 16 |
| `ProcesarPedidoCola` | `[QueueTrigger]` leyendo `string` raw + try/catch (anti-pattern aware) | 19, 21 |

> 🎯 **Patrón clave: MultiResponse (slide 6)**. Una sola función puede producir
> 3 efectos a la vez declarando un POCO con propiedades anotadas (`[HttpResult]`,
> `[CosmosDBOutput]`, `[QueueOutput]`). Si una propiedad es `null`, el output
> binding **no se materializa** — lo que da una validación "fail-safe": si tu
> validación falla, no escribes basura en Cosmos ni encolas un mensaje malo.

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| Catálogo de input bindings | 4 | `GetPedidoByIdFunction`, `GetPedidosPorClienteFunction` |
| Catálogo de output bindings | 5 | `CrearPedidoFunction`, `ExportarPedidoFunction`, `ProcesarPedidoColaFunction` |
| Multi-output con POCO | 6 | `CrearPedidoResult`, `ExportarPedidoResult` |
| Combinaciones (HTTP→Input→Output) | 7 | `ExportarPedidoFunction` (Cosmos→Blob) |
| Bindings vs SDK | 8 | README sección "Cuándo no usar bindings" |
| Connection strings (`Connection = "..."`) | 9 | atributos en todas las funciones; `local.settings.json.example` |
| Binding expressions `{id}`, `{clienteId}`, `{DateTime}` | 10, 16 | `ExportarPedidoFunction` → `exports/{DateTime:yyyy-MM-dd}/pedido-{clienteId}-{id}.json` |
| Patrón híbrido binding + DI | 23 | `CrearPedidoFunction` usa `IPedidosHandler` inyectado |
| Validación antes del output | 24 | `PedidosHandler.ValidarYConstruir` + null en outputs si falla |
| Records en bindings | 25 | `CrearPedidoDto`, `MensajePedidoCola` (records) |
| Estrategias de tests (1+2) | 26 | `PedidosHandlerTests` (estrategia 1), `CrearPedidoFunctionTests` (estrategia 2) |
| Anti-patterns | 21 | `ProcesarPedidoColaFunction` lee `string` raw + try/catch + log del payload |
| Poison queue / `maxDequeueCount` | 17, 19 | `host.json` → `extensions.queues` |

**Slides conceptuales que NO implementamos** (deliberadamente, para no inflar el ejemplo):

- Slide 12 (Managed Identity sin connection strings) — patrón, se menciona en README.
- Slide 13 (SignalR) — requiere infraestructura aparte.
- Slide 14 (SQL bindings) — Azure SQL no se ha usado en módulos previos.
- Slide 15 (custom bindings) — demasiado avanzado para un ejemplo.
- Slide 17 (retry policies dedicadas) — la config básica está en `host.json`.
- Slide 20 (Event Hub bindings) — escala fuera del alcance del módulo.
- Slide 24 con FluentValidation — usamos validación manual; con esto cubrimos la
  intención sin añadir una dependencia.

## Estructura

```
S3.6-bindings-entrada-salida/
├── README.md
├── AzureFunctions.Demo.slnx
├── Directory.Build.props
├── global.json
├── src/AzureFunctions.Demo/
│   ├── Functions/
│   │   ├── HelloFunction.cs                  (esqueleto del módulo)
│   │   ├── PingFunction.cs                   (health check Anonymous)
│   │   ├── GetPedidoByIdFunction.cs          (CosmosDBInput por id)
│   │   ├── GetPedidosPorClienteFunction.cs   (CosmosDBInput por SqlQuery)
│   │   ├── CrearPedidoFunction.cs            (MultiResponse: HTTP + Cosmos + Queue)
│   │   ├── ExportarPedidoFunction.cs         (CosmosDBInput + BlobOutput dinámico)
│   │   └── ProcesarPedidoColaFunction.cs     (QueueTrigger raw string + try/catch)
│   ├── Models/
│   │   ├── Pedido.cs                         (documento Cosmos)
│   │   └── CrearPedidoDto.cs                 (record, slide 25)
│   ├── Services/
│   │   ├── IPedidosHandler.cs                (slide 26 — estrategia 1)
│   │   └── PedidosHandler.cs                 (validación pura)
│   ├── Middleware/
│   ├── host.json                             (extensions.queues — slide 17)
│   ├── local.settings.json.example
│   └── api.http
├── tests/AzureFunctions.Demo.Tests/          (27 tests)
└── scripts/                                  (az CLI didáctico)
    ├── 01-provision.sh                       (RG + Storage[queue+blob] + Cosmos + Function App)
    ├── 02-deploy.sh
    ├── 03-smoke-test.sh                      (POST verifica Cosmos+Queue, GET, export verifica Blob)
    └── 04-cleanup.sh
```

## El patrón MultiResponse en 30 segundos

```
            POST /api/pedidos
            { clienteId, total, notas }
                    │
                    ▼
        ┌────────────────────────────┐
        │ CrearPedidoFunction        │
        │                            │
        │ 1) Deserializa body        │
        │ 2) PedidosHandler.Validar  │
        │ 3) Si OK, build Pedido     │
        │ 4) return CrearPedidoResult│
        └────────────┬───────────────┘
                     │
        ┌────────────┼─────────────────────┐
        │            │                     │
        ▼            ▼                     ▼
  HttpResponse  PedidoCosmos          MensajeCola
   201 Created  [CosmosDBOutput]      [QueueOutput]
                tienda/pedidos        pedidos-pendientes
```

Si la validación falla, `PedidoCosmos = null` y `MensajeCola = null`. Functions
**no llama** a esos outputs. Sólo se materializa la `HttpResponse` con el 400.

## Requisitos

- .NET SDK 10
- Suscripción de Azure (Cosmos serverless + Storage es trivial)
- (Opcional para local) Azurite + Cosmos emulator

## Ejecución local

```bash
cp src/AzureFunctions.Demo/local.settings.json.example src/AzureFunctions.Demo/local.settings.json
```

Asegúrate de tener Azurite arrancado (Storage local) y el emulador de Cosmos en
`https://localhost:8081/` (o un Cosmos real). Crea la database `tienda` y el
container `pedidos` (PK `/clienteId`). El container de blobs `exports` y la
queue `pedidos-pendientes` los crea Azurite/Storage al primer uso, pero por
idempotencia el script `01-provision.sh` los crea explícitamente en Azure.

```bash
func start --csharp
```

> ⚠️ Yo no lanzo apps. Tú haces `func start`.

Usa el archivo [`api.http`](src/AzureFunctions.Demo/api.http) desde VS Code REST
Client para probar los 4 endpoints HTTP.

## Tests

```bash
dotnet test
```

27 tests, sin runtime de Functions ni emulador:

- **`PedidosHandlerTests`** (8) — validación pura (slide 26 estrategia 1):
  cliente vacío, cliente corto, total negativo, notas largas, multiples errores,
  id único por llamada.
- **`CrearPedidoFunctionTests`** (4) — shape de `CrearPedidoResult` (slide 26
  estrategia 2): body válido produce los 3 outputs, body inválido produce
  sólo el 400 con `PedidoCosmos=null` y `MensajeCola=null`, body JSON malformado
  no rompe la función, el `pedidoId` del mensaje de cola correlaciona con el
  `Id` del documento Cosmos.
- **`ExportarPedidoFunctionTests`** (2) — pipeline Cosmos→Blob: si el binding de
  entrada nos pasa null, el blob NO se materializa (`BlobJson = null`).
- **`GetPedidoFunctionsTests`** (4) — endpoints de lectura: 200 con pedido,
  404 cuando el binding entrega null, listado con total.
- **`ProcesarPedidoColaFunctionTests`** (5) — anti-pattern aware (slide 21):
  mensaje válido, vacío, JSON malformado, sin PedidoId, case-insensitive.
- **`HelloFunctionTests`** + **`PingFunctionTests`** (4) — heredados del esqueleto.

## Despliegue por Portal de Azure

### 1) Crear Resource Group

Portal → **Resource groups** → **Create** → `rg-curso-m03-s36`.

### 2) Crear Storage Account

Portal → **Storage accounts** → **Create**:
- Name: `stcursom03s36{iniciales}` (3-24 chars, minúsculas y números)
- Performance: Standard
- Replication: LRS (suficiente para demo)

Una vez creada, en el panel izquierdo:
- **Queues** → **+ Queue** → `pedidos-pendientes`
- **Containers** → **+ Container** → `exports` (acceso privado)

### 3) Crear Cosmos DB

Portal → **Cosmos DB** → **Create** → **Azure Cosmos DB for NoSQL**:
- Account name: `cosmos-curso-m03-s36-{iniciales}`
- Capacity mode: **Serverless**

Tras crearse:
- **Data Explorer** → **+ New Container**
  - Database id: `tienda` (Create new)
  - Container id: `pedidos`
  - Partition key: `/clienteId`

### 4) Crear Function App

Portal → **Function App** → **Create**:
- Runtime stack: **.NET 10 Isolated** (si no aparece, **8 Isolated**)
- OS: **Linux**
- Plan: **Consumption**
- Storage: **usa el mismo Storage Account** del paso 2 (importante — así el
  output a Queue y Blob ya tiene `AzureWebJobsStorage` apuntando al lugar correcto).

### 5) Wire CosmosDbConnection

Cosmos account → **Keys** → copia **Primary Connection String**.

Function App → **Configuration** → **+ New application setting**:
- Name: **`CosmosDbConnection`**
- Value: el connection string copiado

> ℹ️ `AzureWebJobsStorage` ya está configurado automáticamente (paso 4).
> Esa es la connection que usa `[QueueOutput]` y `[BlobOutput]`.

### 6) Deploy desde VS Code

VS Code → extensión **Azure Functions** → click derecho en el proyecto →
**Deploy to Function App** → selecciona `func-curso-m03-s36-{iniciales}`.

### 7) Probar el MultiResponse

Function App → **Functions** → `CrearPedido` → **Get Function Url** → copia.

```bash
curl -X POST "https://func-curso-m03-s36-{iniciales}.azurewebsites.net/api/pedidos?code={key}" \
  -H "Content-Type: application/json" \
  -d '{"clienteId":"cliente-A","total":150.00,"notas":"demo portal"}'
```

Respuesta esperada: **HTTP 201** con el pedido en el body. Luego comprueba:

- **Cosmos DB Data Explorer** → `tienda > pedidos`: el documento aparece.
- **Storage > Queues > pedidos-pendientes**: el mensaje aparece (y desaparece
  en segundos cuando `ProcesarPedidoCola` lo consume).
- **Storage > Containers > exports**: aún vacío. Llama al exportador:

```bash
curl "https://func-curso-m03-s36-{iniciales}.azurewebsites.net/api/exportar/cliente-A/{pedidoId}?code={key}"
```

- **Storage > Containers > exports** → entra a `{yyyy-MM-dd}/pedido-cliente-A-{pedidoId}.json`.

### 8) Limpieza

Portal → **Resource groups** → `rg-curso-m03-s36` → **Delete resource group**.

## Cuándo NO usar bindings (slide 8)

Los bindings son brillantes para el 70 % de los casos. Hay 4 escenarios donde
es preferible inyectar el SDK (`CosmosClient`, `BlobServiceClient`...) por DI:

1. **Operaciones condicionales** complejas (`upsert si X, sino delete`).
2. **Paginación** o queries con `ContinuationToken`.
3. **Batch operations** transaccionales.
4. **Tests unitarios** con mocks del cliente.

En este ejemplo, los bindings cubren todo. En proyectos reales, lo típico es
empezar con bindings y migrar a SDK cuando una función crezca.

## Managed Identity en producción (slide 12)

Los connection strings de este ejemplo son cómodos para desarrollo. En
producción, configura los App Settings con sufijos en vez del CS completo:

```
CosmosDbConnection__accountEndpoint = https://cosmos-prod.documents.azure.com:443/
AzureWebJobsStorage__blobServiceUri = https://stprod.blob.core.windows.net
AzureWebJobsStorage__queueServiceUri = https://stprod.queue.core.windows.net
```

Functions usa `DefaultAzureCredential` (la Managed Identity de la Function App)
en vez del CS. Cero secretos en configuración.

## Próximo paso

[`S3.P — Práctica: 4 triggers`](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.P-practica-4-triggers-v4.md)
consolida en un único proyecto los 4 triggers vistos en S3.2-S3.5 (HTTP +
Timer + Blob + Cosmos), aplicando todos los bindings de S3.6.
