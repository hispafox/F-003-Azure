# S3.5 — Trigger Cosmos DB: Change Feed

> **Submódulo de referencia:** [M03-S3.5](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.5-trigger-cosmosdb-changefeed-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption (gratuito)
> **Cosmos DB:** SQL API · serverless

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el periódico con varios lectores, la regla del lease container distinto, idempotencia por id estable y at-least-once delivery.

## Objetivo

Demostrar el patrón **un Change Feed → varios consumidores independientes** que el
submódulo enseña en las slides 5, 9 y 17. Usamos un único contenedor `pedidos` como
fuente, y dos triggers leen sus cambios con **lease containers distintos**:

- **Notificaciones** (slide 8) — emite una notificación por cada cambio de estado
  del pedido. Idempotencia por construcción (slide 10): si el Change Feed reentrega
  el mismo cambio, el segundo intento es un noop.
- **Materializar resúmenes** (slide 9) — agrupa el batch por cliente y escribe una
  vista desnormalizada en `resumenes-clientes` mediante `[CosmosDBOutput]`. El id
  del documento (`resumen-{clienteId}`) es estable para que el upsert sea idempotente.

Para que puedas inspeccionar el efecto del Change Feed desde la línea de comandos
sin entrar al portal de Cosmos, hay además 3 endpoints HTTP que devuelven el
estado en memoria del Function App (`/api/notificaciones`, `/api/resumenes`,
`/api/resumenes/{clienteId}`). En producción harías queries directas al contenedor.

> 🎯 **Patrón clave**: la potencia del Change Feed no es un trigger, son
> **N consumidores independientes** consumiendo el mismo log con sus propios
> checkpoints (slide 17). Si el consumidor de analytics se cae, el de
> notificaciones sigue funcionando.

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| Trigger mínimo `[CosmosDBTrigger]` | 4 | [`NotificacionesPedidoFunction.cs`](src/AzureFunctions.Demo/Functions/NotificacionesPedidoFunction.cs) |
| Lease container + auto-create | 5, 17 | atributos `LeaseContainerName = "leases-notificaciones"` y `"leases-resumenes"` |
| Configuración avanzada (`MaxItemsPerInvocation`, `feedPollDelay`) | 6, 16 | [`host.json`](src/AzureFunctions.Demo/host.json) |
| Patrón notificación de cambios | 7, 8 | [`NotificacionesPedidoFunction.cs`](src/AzureFunctions.Demo/Functions/NotificacionesPedidoFunction.cs) |
| Patrón materializar vistas + `[CosmosDBOutput]` | 7, 9 | [`MaterializarResumenClienteFunction.cs`](src/AzureFunctions.Demo/Functions/MaterializarResumenClienteFunction.cs) |
| Idempotencia por `(PedidoId, Estado)` | 10 | [`InMemoryNotificacionService.cs`](src/AzureFunctions.Demo/Services/InMemoryNotificacionService.cs) (`ConcurrentDictionary.GetOrAdd`) |
| Idempotencia por id estable de upsert | 10 | `Id = $"resumen-{clienteId}"` en `MaterializarResumenClienteFunction` |
| Manejo de errores (tragar + continuar) | 12 | `try/catch` por pedido en `NotificacionesPedidoFunction.Procesar` |
| Desarrollo local con emulador | 13 | [`scripts/99-emulator.sh`](scripts/99-emulator.sh) + `local.settings.json.example` |
| Múltiples consumidores independientes | 17 | leases distintos para los dos triggers |
| Filtrado en código por estado | 25 | `MensajePorEstado` retorna `null` para estados no relevantes |

Slides 11 (escalado), 14 (monitorización), 15 (troubleshooting), 18 (estimator),
19 (all-versions-and-deletes mode), 20-23 (comparativas y casos), 26 (migración
SQL→Cosmos) son **conceptuales** y se cubren en el material de clase. Aquí
implementamos lo accionable.

## Estructura

```
S3.5-trigger-cosmosdb-changefeed/
├── README.md
├── AzureFunctions.Demo.slnx
├── Directory.Build.props
├── global.json
├── src/AzureFunctions.Demo/
│   ├── Functions/
│   │   ├── HelloFunction.cs                          (slide 9 de S3.1, diagnóstico)
│   │   ├── PingFunction.cs                           (Anonymous, health check)
│   │   ├── NotificacionesPedidoFunction.cs           (CONSUMIDOR 1 del Change Feed)
│   │   ├── MaterializarResumenClienteFunction.cs     (CONSUMIDOR 2 + CosmosDBOutput)
│   │   └── InspeccionHttpFunctions.cs                (GET /notificaciones, /resumenes)
│   ├── Models/
│   │   ├── Pedido.cs                                 (documento del Change Feed)
│   │   ├── Notificacion.cs                           (registro idempotente)
│   │   └── ResumenCliente.cs                         (vista materializada)
│   ├── Services/
│   │   ├── INotificacionService.cs / InMemoryNotificacionService.cs
│   │   └── IResumenClienteService.cs / InMemoryResumenClienteService.cs
│   ├── Middleware/                                   (heredado del esqueleto)
│   ├── host.json                                     (extensions.cosmosDB)
│   ├── local.settings.json.example
│   └── api.http
├── tests/AzureFunctions.Demo.Tests/                  (28 tests)
└── scripts/                                          (az CLI didáctico)
    ├── .env.demo.example
    ├── 01-provision.sh                               (RG + Storage + Cosmos + Function App)
    ├── 02-deploy.sh
    ├── 03-smoke-test.sh                              (inserta pedidos y espera al feed)
    ├── 04-cleanup.sh
    ├── 99-emulator.sh                                (Cosmos emulator opcional)
    └── demo.sh                                       (menú interactivo)
```

## El patrón en 30 segundos

```
              ┌──────────────────────┐
              │  Cosmos DB           │
              │  database: tienda    │
              │  container: pedidos  │  (PK = /clienteId)
              └──────────┬───────────┘
                         │  Change Feed
       ┌─────────────────┼─────────────────┐
       │                                   │
       ▼                                   ▼
[lease: leases-notificaciones]      [lease: leases-resumenes]
       │                                   │
       ▼                                   ▼
┌──────────────────────┐           ┌──────────────────────────────┐
│ Notificaciones       │           │ Materializar resumen cliente │
│ - idempotencia       │           │ - agrupa por clienteId       │
│   (PedidoId, Estado) │           │ - upsert en otra collection  │
│ - filtra estados     │           │ - id estable → idempotente   │
└──────────────────────┘           └─────────────┬────────────────┘
                                                 │  [CosmosDBOutput]
                                                 ▼
                                   ┌──────────────────────────┐
                                   │ container: resumenes-... │
                                   │ (vista desnormalizada)   │
                                   └──────────────────────────┘
```

## Requisitos

- .NET SDK 10
- Suscripción de Azure (gratuita es suficiente — Cosmos serverless ronda los
  céntimos por demo)
- (Opcional) Docker para arrancar el emulador local

## Local: con el emulador (slide 13)

```bash
cd scripts
./99-emulator.sh up
```

Esto arranca el emulador de Cosmos en `https://localhost:8081/`. La
`AccountKey` pública del emulador ya está en
[`local.settings.json.example`](src/AzureFunctions.Demo/local.settings.json.example).

```bash
cp src/AzureFunctions.Demo/local.settings.json.example src/AzureFunctions.Demo/local.settings.json
```

Crea las tres colecciones (a mano por el explorer del emulador o por SDK):

- `tienda` (database)
  - `pedidos` (PK `/clienteId`) — origen del Change Feed
  - `resumenes-clientes` (PK `/clienteId`) — escrito por el output binding
  - `leases-notificaciones`, `leases-resumenes` (PK `/id`) — los crea el runtime

Arranca:

```bash
func start --csharp
```

> ⚠️ **No lanzo apps**: tú haces `func start`. Yo solo verifico build + tests.

Inserta un pedido en el explorer del emulador (`tienda` > `pedidos` > New Item):

```json
{
  "id": "ped-001",
  "clienteId": "cliente-A",
  "estado": "confirmado",
  "total": 150.00
}
```

Después de ~5 segundos (`feedPollDelay` en `host.json`), comprueba:

```bash
curl http://localhost:7071/api/notificaciones
curl http://localhost:7071/api/resumenes
curl http://localhost:7071/api/resumenes/cliente-A
```

## Tests

```bash
dotnet test
```

28 tests cubren los handlers puros (sin runtime de Functions, sin emulador):

- **`NotificacionesPedidoFunctionTests`** (8 tests) — patrón consumidor 1:
  notificación por estado, idempotencia sobre el mismo batch, varios estados
  del mismo pedido generan varias notificaciones, batch vacío, error individual
  no aborta el batch.
- **`MaterializarResumenClienteFunctionTests`** (6 tests) — patrón consumidor 2:
  agrupación por cliente, total/acumulado correcto, idempotencia por id estable,
  upsert vs append, descarte de pedidos sin clienteId.
- **`InMemoryNotificacionServiceTests`** (5 tests) — incluye un test de
  concurrencia (100 hilos sobre la misma clave → exactamente 1 insert) para
  validar la idempotencia bajo paralelismo del trigger (slide 11).
- **`InspeccionHttpFunctionsTests`** (5 tests) — endpoints HTTP de inspección.
- **`HelloFunctionTests`** + **`PingFunctionTests`** (4 tests) — heredados del
  esqueleto del módulo.

> ⚠️ **No incluimos tests con emulador real**. Arrancar el emulador en CI
> es frágil (~1-2 min de boot, problemas de cert, dependencia de Docker
> en runners). El smoke test contra Cosmos real cubre el binding end-to-end.

## Despliegue por Portal de Azure

### 1) Crear Resource Group

Portal → **Resource groups** → **Create**:
- Nombre: `rg-curso-m03-s35`
- Region: la que prefieras

### 2) Crear cuenta de Cosmos DB (slide 5)

Portal → **Cosmos DB** → **Create** → **Azure Cosmos DB for NoSQL**:
- Subscription: tu suscripción
- Resource Group: `rg-curso-m03-s35`
- Account Name: `cosmos-curso-m03-s35-{tus-iniciales}` (único globalmente)
- Location: la misma del RG
- **Capacity mode: Serverless** (gratuito hasta cierto consumo, ideal para demo)
- Apply Free Tier Discount: si está disponible
- Limit total account throughput: marca para evitar sustos

Click **Review + create** → **Create**. Tarda ~5 min.

### 3) Crear database y containers

Una vez creada la cuenta:

Portal → tu Cosmos account → **Data Explorer** → **+ New Container**:

**Container 1 — pedidos** (origen del Change Feed):
- Database id: `tienda` (Create new)
- Container id: `pedidos`
- Partition key: `/clienteId`
- Container throughput (autoscale): no aplica en serverless

Click **OK**.

**Container 2 — resumenes-clientes** (vista materializada):
- Database id: `tienda` (Use existing)
- Container id: `resumenes-clientes`
- Partition key: `/clienteId`

Click **OK**.

> ℹ️ Los **lease containers** (`leases-notificaciones`, `leases-resumenes`) los
> crea el runtime del trigger automáticamente la primera vez que arranca,
> porque pusimos `CreateLeaseContainerIfNotExists = true` en los atributos
> (slide 5).

### 4) Crear Function App

Portal → **Function App** → **Create**:
- Subscription: tu suscripción
- Resource Group: `rg-curso-m03-s35`
- Function App name: `func-curso-m03-s35-{tus-iniciales}`
- Code/Container: **Code**
- Runtime stack: **.NET**
- Version: **10 Isolated** (si no aparece, **8 Isolated** y luego cambia el `runtime-version` por CLI)
- Region: la misma
- Operating System: **Linux**
- Plan type: **Consumption (Serverless)**

Pestaña **Storage**: deja que cree una storage account nueva (lo necesita el
runtime de Functions; **NO** se mezcla con Cosmos).

Click **Review + create** → **Create**. Tarda ~3 min.

### 5) Configurar la connection string de Cosmos

Necesitamos pasar el connection string de Cosmos al Function App con el
nombre **`CosmosDbConnection`** (el nombre referenciado en los atributos
`Connection = "CosmosDbConnection"` de las funciones).

Portal → tu Cosmos account → **Keys** → copia **Primary Connection String**.

Portal → tu Function App → **Configuration** → **Application settings** → **+ New application setting**:
- Name: `CosmosDbConnection`
- Value: el connection string que copiaste
- Save → Continue

> ⚠️ En producción, usa **Managed Identity** en vez de connection string.
> `Connection = "CosmosDbConnection"` permite tanto un connection string como
> un setting con sufijos `__accountEndpoint` para identidad. Aquí usamos
> connection string para simplicidad de demo.

### 6) Deploy desde VS Code

VS Code → extensión **Azure Functions** → click derecho en el proyecto →
**Deploy to Function App** → selecciona `func-curso-m03-s35-{tus-iniciales}`.

Espera 1-2 minutos al primer arranque (cold start).

### 7) Probar

Portal → tu Cosmos account → **Data Explorer** → `tienda` > `pedidos` >
**Items** > **New Item**, pega:

```json
{
  "id": "ped-001",
  "clienteId": "cliente-A",
  "estado": "confirmado",
  "total": 150.00
}
```

Save. **Espera 10-30 segundos** (cold start + `feedPollDelay`).

Portal → tu Function App → **Functions** → click en
`MaterializarResumen` o `NotificarCambioPedido` → **Monitor**:
deberías ver una invocación con el batch que contiene tu pedido.

Y en `tienda > resumenes-clientes` verás un documento
`resumen-cliente-A` con `totalPedidos: 1` y `importeAcumulado: 150`.

Para ver las notificaciones desde curl:

```bash
# Necesitas la function key
curl "https://func-curso-m03-s35-{tus-iniciales}.azurewebsites.net/api/notificaciones?code={key}"
```

### 8) Ver el "Change Feed Estimator" (slide 18)

Portal → tu Cosmos account → **Insights** → **Change Feed**: muestra el lag
por partición. Si crece sostenidamente, escala (más instancias) o ajusta
`MaxItemsPerInvocation` (slide 22).

### 9) Limpieza

Portal → **Resource groups** → `rg-curso-m03-s35` → **Delete resource group**.

> ⚠️ Cosmos serverless no tiene coste fijo, pero un account abandonado en
> producción puede acumular RUs. Borrar el RG en cuanto acabes la demo es
> la opción más segura.

## Despliegue por scripts (CLI, opcional)

Si prefieres hacer todo por línea de comandos (útil para repetir la demo):

```bash
cd scripts
cp .env.demo.example .env.demo
# Edita .env.demo con tus valores
./demo.sh
```

El menú te lleva paso a paso: provisionar → deploy → smoke test → cleanup.

> ⚠️ El smoke test inserta pedidos vía `az cosmosdb sql container create-item-or-update`,
> que requiere `az` 2.60+. Si tu CLI es más antigua, inserta los pedidos
> manualmente desde el Portal (slide 7 paso anterior) y vuelve a ejecutar
> el script (saltará a la fase de polling).

## Errores comunes

**"El trigger no se ejecuta tras desplegar":**
→ Falta `CosmosDbConnection` en App Settings, o el nombre no coincide
exactamente con el del atributo `Connection = "..."`. Verifica que la
database (`tienda`) y el container (`pedidos`) existen con esos nombres
exactos (slide 15).

**"Procesa documentos viejos al primer arranque":**
→ Si el lease container se borró (o nunca existió), el trigger empieza
desde el cambio actual. Si quieres reprocesar todo, pon
`StartFromBeginning = true` en el atributo (slide 6).

**"El mismo pedido aparece dos veces":**
→ At-least-once delivery (slide 10). Es **normal**. La idempotencia es
responsabilidad de tu código, no del trigger. Mira
[`InMemoryNotificacionService`](src/AzureFunctions.Demo/Services/InMemoryNotificacionService.cs).

**"Tarda mucho en detectar cambios":**
→ Reduce `feedPollDelay` en `host.json` (slide 6/16). Default 5s; en alta
producción puedes bajarlo a 1s a costa de más RUs.

**"Eliminaciones no se notifican":**
→ Por diseño (slide 2). Workaround: soft delete con un campo `eliminado:
true` y reaccionas a la actualización. Para deletes reales hay que activar
`AllVersionsAndDeletes` mode (slide 19), que requiere continuous backup
y solo funciona en containers nuevos.

## Próximo paso

[`S3.6 — Bindings de entrada y salida`](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.6-bindings-entrada-salida-v4.md)
profundiza en los bindings de input/output (más allá de
`[CosmosDBOutput]` que ya hemos visto aquí). Veremos input bindings de
Cosmos para leer documentos individuales sin abrir un cliente, y outputs
hacia Service Bus, Event Grid y Tables.
