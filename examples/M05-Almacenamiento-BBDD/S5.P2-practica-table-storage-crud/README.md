# S5.P2 — Práctica: Table Storage CRUD

> **Submódulo de referencia:** [M05-S5.P2](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.P2-practica-table-storage-crud-v1.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** ≈ 0 € (Storage Standard_LRS)

> 🎓 **Práctica corta** que cierra M05: la BBDD NoSQL más simple y barata.
> CRUD con `Azure.Data.Tables` y connection string (sin RBAC). Reutiliza
> el patrón Table/Azurite de **S5.1**.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| `Producto : ITableEntity` (PartitionKey/RowKey, `double` precio) | [`Producto.cs`](src/Tables.Demo.Api/Domain/Producto.cs) |
| CRUD con `TableClient` | [`ProductosService.cs`](src/Tables.Demo.Api/Domain/ProductosService.cs) |
| 6 endpoints REST | [`ProductosEndpoints.cs`](src/Tables.Demo.Api/Endpoints/ProductosEndpoints.cs) |
| Validación de claves (caracteres prohibidos, timestamp invertido) | [`TableKeys.cs`](src/Tables.Demo.Api/Tables/TableKeys.cs) |
| Filtros OData seguros (anti-inyección) | [`ODataFilter.cs`](src/Tables.Demo.Api/Tables/ODataFilter.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Crear Storage Account | 4 | [`scripts/01-provision.sh`](scripts/01-provision.sh) |
| Crear tabla + entities; PartitionKey=categoría | 5 | `01-provision.sh` + `Producto` |
| Modelo `ITableEntity` (`double`, no `decimal`) | 7 | [`Producto.cs`](src/Tables.Demo.Api/Domain/Producto.cs) |
| Servicio CRUD | 7 | [`ProductosService.cs`](src/Tables.Demo.Api/Domain/ProductosService.cs) |
| 6 endpoints (GET/POST/PUT/DELETE) | 8 | [`ProductosEndpoints.cs`](src/Tables.Demo.Api/Endpoints/ProductosEndpoints.cs) |
| Azurite para desarrollo local | 11 | `appsettings.Development.json` + test de integración |
| OData filter (`eq`, `ge`, `le`, escaping) | 10 | [`ODataFilter.cs`](src/Tables.Demo.Api/Tables/ODataFilter.cs) |
| Smoke tests | 13 | [`scripts/02-smoke-test.sh`](scripts/02-smoke-test.sh) |
| Patrones (RowKey timestamp invertido) | 14 | `TableKeys.RowKeyTimestampInvertido` |
| Limitaciones (claves: sin `/ \ # ?`) | 15 | `TableKeys.EsValida` + endpoint POST (400) |
| Cleanup | 16 | [`scripts/03-cleanup.sh`](scripts/03-cleanup.sh) |
| Reto 1 — filtro por rango de precio | 20 | endpoint `GET /productos/precio` |

## Estructura

```
S5.P2-practica-table-storage-crud/
├── src/Tables.Demo.Api/
│   ├── Tables/     TableKeys, ODataFilter   (lógica pura)
│   ├── Domain/     Producto (ITableEntity), ProductosService (CRUD)
│   ├── Endpoints/  ProductosEndpoints (6 + pure-logic)
│   └── Program.cs  AddSingleton<IProductosService>
├── tests/Tables.Demo.Api.Tests/
│   ├── Unit_*            TableKeys, ODataFilter
│   ├── DiContainer_Tests servicio del contenedor real (sin Docker)
│   └── Integration_…     Azurite (Testcontainers, SkippableFact)
└── scripts/        01-provision (Storage+tabla+seed) / 02-smoke / 03-cleanup
```

## Tests

```bash
dotnet test     # 19 pass + 1 skip (Azurite sin Docker) + 0 fail
```

- **CAPA 1 · Unit**: `TableKeys` (claves válidas, sanitizado, RowKey
  timestamp invertido — slide 14) y `ODataFilter` (escapado de comillas
  anti-inyección, rango de precio en cultura invariante — slide 10).
- **CAPA 0 · DI**: resuelve `IProductosService` del **contenedor real**
  (`WebApplicationFactory`). El `TableClient` se construye *lazy* (no
  toca red en el ctor) → corre **sin Docker**. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA 2 · Integration** (`SkippableFact`): CRUD **real** contra
  **Azurite** (que sí emula Table Storage) vía la API completa, mismo
  patrón que [S5.1](../S5.1-azure-storage/README.md). Se **salta** sin
  Docker → la suite siempre verde.

> 🧠 La app **no crea la tabla** al arrancar (mismo criterio que
> S5.2/S5.3): la crea el script `01-provision.sh` o el test de
> integración. El ctor del servicio solo construye el `TableClient`
> (lazy) → el test de DI funciona sin Azure ni Docker.

## Ejecución local

```bash
# 1. Azurite (emula Table Storage)
azurite --silent --location ./.azurite
# 2. Crear la tabla 'productos' (una vez): az storage table create ... o Storage Explorer
# 3. La API
dotnet run --project src/Tables.Demo.Api
# http://localhost:5087  — usa src/Tables.Demo.Api/api.http
```

`appsettings.Development.json` ya apunta a Azurite
(`UseDevelopmentStorage=true`).

## Despliegue por Portal

1. **Storage Account** — *Create resource → Storage account*,
   **Standard / LRS** (lo más barato, slide 4).
2. **Tabla** — *Storage browser → Tables → + Add table* → `productos`.
   Inserta entities de prueba (PartitionKey = categoría, slide 5).
3. **Connection string** — *Security + networking → Access keys →
   Connection string*. Va en App Settings como `Storage:ConnectionString`
   (**contiene la AccountKey**: no la subas a git; en producción →
   Managed Identity, ver [S5.P](../S5.P-practica-cosmos-managed-identity/README.md)
   / [S5.4](../S5.4-managed-identity/README.md)).
4. **Verificar** — *Storage browser* muestra los cambios en vivo.

> Scripts `az` en [`scripts/`](scripts) (`./demo.sh`) hacen los pasos
> 1-2 + seed y el smoke CRUD. Complemento de clase, no sustituto.

## Table Storage vs Cosmos (slide 12)

```
Datos simples, queries por PK+RK, coste prioritario → Table Storage (esto, ~10x más barato)
Multi-región, SLA 99.999%, queries complejas        → Cosmos DB (S5.3 / S5.P)
Migración: Cosmos Table API es 1:1 (solo cambia la connection string)
```

## Módulo M05 completo ✅

| Sub | Tema |
| --- | --- |
| S5.1 | Azure Storage (Blob/Table/Queue/File) |
| S5.2 | Azure SQL Database (EF Core) |
| S5.3 | Cosmos DB (partición, RU, consistencia) |
| S5.4 | Managed Identity (keyless) |
| S5.5 | Backups, replicación y DR |
| S5.P | Práctica: Cosmos DB + Managed Identity |
| **S5.P2** | **Práctica: Table Storage CRUD** (este) |

**Siguiente módulo:** M06 — Seguridad, Autenticación e Identidad.
