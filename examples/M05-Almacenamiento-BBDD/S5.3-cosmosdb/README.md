# S5.3 — Cosmos DB: partición, RU y consistencia

> **Submódulo de referencia:** [M05-S5.3](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.3-cosmosdb-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API + SDK Cosmos · **Coste:** ≈ 0 € (Cosmos **serverless**, pago por RU)

> ℹ️ Mismo stack que S5.1/S5.2 (Minimal API + repos + Testcontainers),
> pero el servicio es **NoSQL documental**: `Microsoft.Azure.Cosmos`,
> partition key, Request Units y niveles de consistencia.

## Objetivo

Una API de pedidos sobre Cosmos DB que materializa las **tres
decisiones de diseño** del submódulo (slide 2): partition key, modelo de
datos y consistencia.

| Concepto | Dónde |
| --- | --- |
| Documento desnormalizado (embed: items + cliente dentro) | [`Modelos.cs`](src/Cosmos.Demo.Api/Domain/Modelos.cs) |
| CRUD + read-by-id (1 RU) + query single-partition | [`IPedidoRepository.cs`](src/Cosmos.Demo.Api/Repositories/IPedidoRepository.cs) |
| Soft delete (Change Feed no ve DELETE) | `PedidoRepository.SoftDeleteAsync` |
| TransactionalBatch (ACID en una partición) | `PedidoRepository.CrearPedidoConMovimientoAsync` |
| CosmosClient singleton + retry de 429 | [`Program.cs`](src/Cosmos.Demo.Api/Program.cs) |
| ¿Buena partition key? (3 reglas) | [`PartitionKeyAdvisor.cs`](src/Cosmos.Demo.Api/Cosmos/PartitionKeyAdvisor.cs) |
| Nivel de consistencia recomendado | [`ConsistencyAdvisor.cs`](src/Cosmos.Demo.Api/Cosmos/ConsistencyAdvisor.cs) |
| Estimar RU por patrón de acceso | [`RuEstimator.cs`](src/Cosmos.Demo.Api/Cosmos/RuEstimator.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Account → DB → Container → Documents | 3 | `CosmosDefaults` + `01-provision.sh` |
| Partition key: la decisión más importante | 4-6 | [`PartitionKeyAdvisor.cs`](src/Cosmos.Demo.Api/Cosmos/PartitionKeyAdvisor.cs) + PK `/clienteId` |
| Request Units (RU) | 7 | [`RuEstimator.cs`](src/Cosmos.Demo.Api/Cosmos/RuEstimator.cs) + `RequestCharge` en el repo |
| Optimizar RU (read-by-id, single-partition) | 8 | `PedidoRepository` (`ReadItemAsync` 1 RU; query con `PartitionKey`) |
| SDK CRUD + queries SQL API | 9-10 | [`IPedidoRepository.cs`](src/Cosmos.Demo.Api/Repositories/IPedidoRepository.cs) |
| Niveles de consistencia | 11 | [`ConsistencyAdvisor.cs`](src/Cosmos.Demo.Api/Cosmos/ConsistencyAdvisor.cs) + `CosmosConsistency` config |
| Change Feed + soft delete | 12 | `PedidoRepository.SoftDeleteAsync` |
| Modelo desnormalizado (embed) | 14, 22, 31 | [`Modelos.cs`](src/Cosmos.Demo.Api/Domain/Modelos.cs) (`Pedido` con `Items`) |
| CosmosClient singleton en DI | 15 | [`Program.cs`](src/Cosmos.Demo.Api/Program.cs) |
| TransactionalBatch (ACID por partición) | 17 | `PedidoRepository.CrearPedidoConMovimientoAsync` |
| TTL (auto-limpieza) | 19 | `01-provision.sh` (`--ttl` comentado: para sesiones/logs) |
| Serverless: coste real | 7, 21 | `01-provision.sh` (`--capabilities EnableServerless`) |
| Anti-pattern: sin retry 429 | 32 | `Program.cs` (`MaxRetryAttemptsOnRateLimitedRequests = 9`) |

## Estructura

```
S5.3-cosmosdb/
├── src/Cosmos.Demo.Api/
│   ├── Cosmos/        PartitionKeyAdvisor, ConsistencyAdvisor, RuEstimator,
│   │                  CosmosDefaults  (lógica pura + constantes)
│   ├── Domain/        Pedido (embed), Movimiento, DTOs
│   ├── Repositories/  IPedidoRepository + impl (SDK Cosmos)
│   ├── Endpoints/     PedidosEndpoints (Minimal API)
│   └── Program.cs     CosmosClient singleton + retry + camelCase (slide 15)
├── tests/Cosmos.Demo.Api.Tests/
│   ├── Unit_*                  lógica pura (partition key, consistencia, RU)
│   ├── DiContainer_Tests       resuelve el contenedor real (sin Docker)
│   └── Integration_Cosmos…     Testcontainers emulador (SkippableFact)
└── scripts/           01-provision (serverless) / 02-smoke / 03-cleanup
```

## Tests

```bash
dotnet test     # 25 pass + 1 skip (emulador sin Docker) + 0 fail
```

- **CAPA 1 · Unit** (pura, sin Cosmos): `PartitionKeyAdvisor` (las 3
  reglas de la slide 5 + detección cross-partition), `ConsistencyAdvisor`
  (los 5 niveles + multiplicador RU, slide 11), `RuEstimator`
  (read-by-id ≪ cross-partition, slides 7-8).
- **CAPA 0 · DI**: resuelve `CosmosClient` + `Container` + el repo del
  **contenedor real** (`WebApplicationFactory`). El SDK de Cosmos es
  *lazy* (construir no conecta), así que corre **siempre, sin Docker** —
  cubre la [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA 2 · Integration** (`SkippableFact`): round-trip **real** contra
  el **emulador de Cosmos** en Docker (Testcontainers.CosmosDb) vía la
  API completa: crear (embed), read-by-id, query single-partition, soft
  delete (tras borrar → 404), TransactionalBatch. Se **salta** si Docker
  no está o el emulador no arranca → la suite siempre verde.

> 🧠 **No hay CAPA "component" tipo S5.2**: Cosmos **no tiene proveedor
> in-memory** equivalente a EF Core + SQLite. La lógica testable sin
> infra se extrae a clases puras (CAPA 1); el round-trip de datos exige
> el emulador (CAPA 2). El emulador de Cosmos es **pesado y a veces no
> arranca**: por eso el `SkippableFact` captura cualquier excepción de
> arranque y se salta.

> 🧠 **Newtonsoft.Json explícito**: `Microsoft.Azure.Cosmos` 3.x usa
> Newtonsoft como serializador por defecto y **exige** referenciarlo
> explícitamente (si no, error de build). Con
> `CosmosPropertyNamingPolicy.CamelCase`, `Id`→`"id"` (lo exige Cosmos)
> y `ClienteId`→`"clienteId"` sin atributos en el POCO.

## Ejecución local

```bash
# 1. Emulador de Cosmos en Docker (la API la lanzas tú)
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
#   La 1ª vez tarda en estar "ready". Importa su cert o usa Gateway.

# 2. La API (CosmosDbConnection vacío → emulador local)
dotnet run --project src/Cosmos.Demo.Api
# http://localhost:5083  — usa src/Cosmos.Demo.Api/api.http
```

`Program.cs` crea la base/contenedor **no** en el arranque; usa
`01-provision.sh` (Azure) o crea `tienda`/`pedidos` (PK `/clienteId`) en
el emulador antes de probar (el test de integración lo hace solo).

## Despliegue por Portal (Cosmos serverless)

1. **Crear cuenta** — Portal → *Create a resource* → **Azure Cosmos DB**
   → **Azure Cosmos DB for NoSQL**. *Capacity mode* → **Serverless**
   (pago por RU, ≈ 0 € sin uso — slides 7, 21).
2. **Networking** — para la práctica, *Allow access from All networks*
   (en producción: *Private endpoint* o IPs concretas).
3. **Crear** y esperar al despliegue.
4. **Data Explorer** → *New Database* `tienda` → *New Container*
   `pedidos` con **Partition key** `/clienteId` (slide 6). La elección
   de PK **no se puede cambiar** sin recrear.
5. **Consistencia** — *Settings → Default consistency* → **Session**
   (correcto para el 90%, slide 11). La app puede *debilitarla* con
   `CosmosConsistency` pero nunca reforzarla por encima de la cuenta.
6. **Conectar la app** — App Service / Functions → *Environment
   variables* → `CosmosDbConnection`. Recomendado **sin key**:
   `AccountEndpoint=https://<cuenta>.documents.azure.com:443/` +
   **Managed Identity** con rol *Cosmos DB Built-in Data Contributor*
   (se profundiza en **M05-S5.4**).

> Scripts `az` equivalentes en [`scripts/`](scripts) (`./demo.sh`):
> `01-provision.sh` crea cuenta serverless + db + container `/clienteId`;
> `02-smoke-test.sh` verifica el recurso; `03-cleanup.sh` borra el RG.
> Complemento de clase, nunca sustituto del Portal.

## Cuándo Cosmos (vs Azure SQL, slide 12 de S5.2)

```
Documentos JSON, schema flexible, multi-region write, Change Feed → Cosmos (esto)
Relacional, FK/JOIN, transacciones ACID amplias                    → Azure SQL (S5.2)
Tráfico intermitente / dev                                          → serverless (este ejemplo)
```

## Próximo paso

[`S5.4 — Managed Identity`](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.4-managed-identity-v3.md):
conectarse a SQL y Cosmos **sin keys ni passwords**, con la identidad
gestionada del recurso.
