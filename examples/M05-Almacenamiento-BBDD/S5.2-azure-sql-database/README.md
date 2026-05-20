# S5.2 — Azure SQL Database con EF Core

> **Submódulo de referencia:** [M05-S5.2](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.2-azure-sql-database-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API + EF Core · **Coste:** ≈ 0 € (Azure SQL **serverless** con auto-pausa)

> ℹ️ Mismo stack que S5.1 (Minimal API + repos + Testcontainers), pero
> ahora el servicio de datos es **relacional**: EF Core sobre Azure SQL,
> con migraciones, retry de errores transitorios y connection pooling.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) —
> manual del alumno: el para qué, el porqué (ACID, EF Core, pool, retry,
> Managed Identity, no migrar al arrancar) y cómo ponerlo en marcha y
> probarlo.

## Objetivo

Una API de ventas (`Producto` 1—N `Pedido`) que muestra el ciclo
completo de Azure SQL desde .NET:

| Concepto | Dónde |
| --- | --- |
| Modelo + esquema explícito (clave, longitud, `decimal(18,2)`, índice, FK) | [`VentasDbContext.cs`](src/Sql.Demo.Api/Data/VentasDbContext.cs) |
| CRUD con EF Core (AsNoTracking, FindAsync, SaveChanges) | [`IProductoRepository.cs`](src/Sql.Demo.Api/Repositories/IProductoRepository.cs) |
| Regla de negocio transaccional (validar stock → descontar → total) | [`IPedidoRepository.cs`](src/Sql.Demo.Api/Repositories/IPedidoRepository.cs) |
| Migración `InitialCreate` | [`Migrations/`](src/Sql.Demo.Api/Migrations) |
| Retry de errores transitorios | [`AzureSqlRetryPolicy.cs`](src/Sql.Demo.Api/Sql/AzureSqlRetryPolicy.cs) + [`Program.cs`](src/Sql.Demo.Api/Program.cs) |
| Connection pooling + Encrypt + Managed Identity | [`SqlConnectionTuning.cs`](src/Sql.Demo.Api/Sql/SqlConnectionTuning.cs) |
| Elegir tier (DTU/vCore/Serverless/Hyperscale) | [`SqlTierAdvisor.cs`](src/Sql.Demo.Api/Sql/SqlTierAdvisor.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Servidor lógico + crear DB | 2-3 | [`scripts/01-provision.sh`](scripts/01-provision.sh) |
| DTU vs vCore; tier para el curso | 4 | [`SqlTierAdvisor.cs`](src/Sql.Demo.Api/Sql/SqlTierAdvisor.cs) |
| **Serverless** (auto-pausa, ≈ 0 €) | 5 | provision (`--compute-model Serverless`) + `SqlTierAdvisor` |
| Conexión desde .NET; Managed Identity | 6 | [`Program.cs`](src/Sql.Demo.Api/Program.cs) + `SqlConnectionTuning` |
| EF Core: DbContext + CRUD | 7 | [`VentasDbContext.cs`](src/Sql.Demo.Api/Data/VentasDbContext.cs) + repos |
| Migraciones | 8 | [`Migrations/`](src/Sql.Demo.Api/Migrations) + `02-smoke-test.sh` |
| Índices / query optimization | 9 | índices en `OnModelCreating` (Nombre, Fecha) |
| **Connection pooling** | 10 | [`SqlConnectionTuning.cs`](src/Sql.Demo.Api/Sql/SqlConnectionTuning.cs) |
| Azure SQL vs Cosmos (transacciones ACID) | 12 | `PedidoRepository` (stock+pedido en un `SaveChanges`) |
| **Resiliencia: `EnableRetryOnFailure`** | 13 | `AzureSqlRetryPolicy` + `Program.cs` |
| Checklist producción (Entra ID, no password) | 20 | `SqlConnectionTuning.UsaManagedIdentity` + `/sql/conn-info` |
| Hyperscale (> 1 TB) | 21 | `SqlTierAdvisor` (rama Hyperscale) |
| Anti-patterns (N+1, sin retry, migrar en runtime) | 31, 35 | repos (`Include`), `Program.cs` (no migra al arrancar) |

## Estructura

```
S5.2-azure-sql-database/
├── src/Sql.Demo.Api/
│   ├── Sql/          AzureSqlRetryPolicy, SqlConnectionTuning, SqlTierAdvisor  (lógica pura)
│   ├── Domain/       Producto, Pedido, DTOs
│   ├── Data/         VentasDbContext (OnModelCreating)
│   ├── Repositories/ IProducto/IPedido + impls (EF Core)
│   ├── Endpoints/    VentasEndpoints (Minimal API)
│   ├── Migrations/   InitialCreate (generada con dotnet ef)
│   └── Program.cs    UseSqlServer + EnableRetryOnFailure (slide 13)
├── tests/Sql.Demo.Api.Tests/
│   ├── Unit_*                       lógica pura (tier, tuning, retry)
│   ├── Component_RepositoriosSqlite SQLite in-memory (modelo + repos)
│   ├── DiContainer_Tests            resuelve el contenedor real (sin Docker)
│   └── Integration_SqlServerTests   Testcontainers.MsSql (SkippableFact)
└── scripts/          01-provision (serverless) / 02-smoke (migración) / 03-cleanup
```

## Tests

```bash
dotnet test     # 31 pass + 1 skip (integración sin Docker) + 0 fail
```

Cuatro capas — el patrón establecido en M04-S4.5 / M05-S5.1, adaptado a EF Core:

- **CAPA 1 · Unit** (lógica pura, sin Azure): `SqlTierAdvisor` (tabla de
  decisión de tier, slides 4-5-21), `SqlConnectionTuning` (pooling +
  Encrypt + detección de Managed Identity, slides 6-10-20) y
  `AzureSqlRetryPolicy` (errores transitorios, slide 13).
- **CAPA 2 · Component** (`SQLite in-memory`): el **modelo EF Core real**
  y las reglas de negocio de los repos (crear pedido descuenta stock y
  calcula total; sin stock no toca nada; `Include` sin N+1) contra una
  BD relacional de verdad, **sin Docker**. Rápido.
- **CAPA 0 · DI**: resuelve `VentasDbContext` + los repos del
  **contenedor real** (`WebApplicationFactory`) en un scope, sin tocar
  la BD. Cubre la [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md)
  para que el grafo se valide aunque la CAPA 3 se salte.
- **CAPA 3 · Integration** (`SkippableFact`): round-trip **real** contra
  **SQL Server en Docker** (Testcontainers.MsSql) vía la API completa.
  Aplica la migración `InitialCreate` de verdad (slide 8) y ejercita el
  provider SqlServer + el retry. Se **salta** si Docker no está → la
  suite siempre verde.

> 🧠 **Gotcha EF Core documentado**: `Pedido.Fecha` es `DateTime`, no
> `DateTimeOffset`. SQLite **no soporta `ORDER BY` sobre
> `DateTimeOffset`** (`NotSupportedException`); como la query de pedidos
> ordena por fecha, usar `DateTimeOffset` rompería la CAPA 2. `DateTime`
> (UTC) funciona en SQL Server (`datetime2`) y en SQLite.

> ⚠️ **SQLite ≠ SQL Server**: la CAPA 2 valida la *lógica*; las
> migraciones SQL Server-specific y el provider real solo se ejercitan
> en la CAPA 3 (Docker). Por eso CAPA 2 usa `EnsureCreated()` y CAPA 3
> usa `Database.Migrate()`.

## Ejecución local

```bash
# 1. SQL Server local en Docker (la API la lanzas tú)
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Tu_Password123" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

# 2. Aplicar la migración (slide 8 — no es "lanzar la app")
dotnet ef database update --project src/Sql.Demo.Api

# 3. La API
dotnet run --project src/Sql.Demo.Api
# http://localhost:5082  — usa src/Sql.Demo.Api/api.http
```

`appsettings.Development.json` ya trae una `SqlConnection` apuntando a
ese contenedor local (ajusta el password al tuyo).

## Despliegue por Portal (Azure SQL serverless)

1. **Crear servidor lógico** — Portal → *Create a resource* → **SQL
   Database**. En *Server* → *Create new*: nombre único, región, y
   *Authentication* → recomendado **Use Microsoft Entra-only
   authentication** (sin password, slide 6/20); para la práctica vale
   *Use both* con un admin SQL.
2. **Configurar la base de datos** — *Compute + storage* → *Configure
   database* → **Serverless** (General Purpose, Gen5, mín. 0.5 vCore,
   *Auto-pause* 1 h). Es lo que la deja a **≈ 0 €** parada (slide 5).
   *Backup storage redundancy* → **Locally-redundant** para la práctica.
3. **Networking** — pestaña *Networking* → *Public endpoint* →
   **Add current client IP address** y *Allow Azure services* = Yes
   (slide 3).
4. **Crear** y esperar al despliegue.
5. **Aplicar el esquema** — *SQL Database* → *Query editor* y pegar el
   script de la migración (`dotnet ef migrations script -o init.sql`),
   o ejecutar `dotnet ef database update --connection "<cs>"` desde tu
   máquina (slide 8). **Nunca** migrar en el arranque de la app
   (anti-pattern 8, slide 35 — por eso `Program.cs` no llama a
   `Migrate()`).
6. **Conectar la app** — en App Service / Functions → *Settings* →
   *Environment variables* → `SqlConnection` con
   `Server=tcp:<srv>.database.windows.net,1433;Database=<db>;Authentication=Active Directory Default;Encrypt=true;`
   y activar la **Managed Identity** del recurso, dándole acceso a la
   BD (se profundiza en **M05-S5.4**).

> Scripts `az` equivalentes en [`scripts/`](scripts) como complemento de
> clase (`./demo.sh`): `01-provision.sh` crea servidor + DB serverless +
> firewall; `02-smoke-test.sh` aplica `InitialCreate`; `03-cleanup.sh`
> borra el RG. Siempre como complemento, no sustituto del Portal.

## Cuándo Azure SQL (vs Cosmos, slide 12)

```
Datos relacionales, FK, JOINs, transacciones ACID  → Azure SQL  (esto)
Documentos JSON, multi-region write, Change Feed    → Cosmos DB  (S5.3)
Tráfico intermitente / dev / staging                → Serverless (este ejemplo)
> 1 TB con crecimiento rápido                        → Hyperscale
```

## Próximo paso

[`S5.3 — Cosmos DB`](../../../doc/M05-Almacenamiento-BBDD/v3-actual): el
modelo NoSQL distribuido — particionado, RU/s y consistencia.
