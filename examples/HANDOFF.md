# HANDOFF — construcción de ejemplos del curso F-003-Azure

> Documento de traspaso entre sesiones. Si retomas esto en una sesión
> nueva: **lee este archivo entero antes de tocar nada**. Reconstruye la
> "receta" exacta que se ha seguido para que los ejemplos sigan siendo
> consistentes. Todo lo demás (código, tests) ya está en git.

## Qué se está haciendo

El repo `doc/` tiene 11 módulos de teoría (markdown con slides). Estamos
creando, **un submódulo a la vez y en orden**, un ejemplo de código
ejecutable en `examples/MXX-*/SY.Z-*/` que materializa esa teoría.

Disparador del usuario: dice **"sigamos"** / "sigue" → tomar el
**siguiente submódulo en orden** y construir su ejemplo completo, commit
y push. El usuario confía en el criterio; no hace falta pedir aprobación
de scope salvo que haya una dependencia nueva cara o una decisión de
arquitectura no obvia (entonces se propone en 1 párrafo y se ejecuta).

## Estado actual (tras S5.2)

| Módulo | Estado |
| --- | --- |
| M01 Intro Azure | 2 ejemplos (S1.P Hello World, S1.P2 Cloud Shell) |
| M02 App Services | ✅ completo 7/7 |
| M03 Azure Functions I | ✅ completo 8/8 |
| M04 Azure Functions II | ✅ completo 7/7 |
| M05 Almacenamiento y BBDD | 🚧 2/7 — hechos **S5.1** (Storage), **S5.2** (Azure SQL) |
| M06–M11 | pendientes |

**Siguiente tarea concreta:** `M05-S5.3 — Cosmos DB`
(`doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.3-*-v3.md`). Patrón
esperado: Minimal API `Cosmos.Demo.Api` con el SDK
`Microsoft.Azure.Cosmos` (o EF Core Cosmos provider) — particionado,
RU/s, consistencia, Change Feed; tests unit de lógica pura (clave de
partición, política de consistencia) + componente + integración con
**Testcontainers** del emulador de Cosmos (`SkippableFact`); scripts
provisión Cosmos **serverless** (~0€). Luego S5.4 Managed Identity,
S5.5 Backups, S5.P, S5.P2.

## La receta (cómo se construye CADA ejemplo)

1. **Leer el doc** del submódulo en `doc/MXX-*/v*-actual/MXX-SY.Z-*.md`
   (suelen ser 1000-1800 líneas → leer por tramos).
2. **Bootstrap**: `cp -r` de un ejemplo previo del mismo stack y
   *strip* (borrar `bin/obj/*.lscache`, funciones/servicios/tests que no
   apliquen, recrear `GlobalUsings.cs`). Bases por stack:
   - Functions (M03/M04): copiar de un S3.x/S4.x cercano.
   - Minimal API (M02/M05): copiar de M02-S2.1 o crear scaffolding nuevo.
3. **csproj**: ajustar `PackageReference` al stack del submódulo.
4. **Código**: lógica de negocio en **servicios/clases puras**
   (testables sin Azure); las Functions/endpoints son "pegamento" fino.
5. **Tests obligatorios** (regla del usuario, no opcional):
   - Unit de la lógica pura — la mayoría.
   - Functions: instanciar la clase con `new` + `DefaultHttpContext`
     fabricado (NO WebApplicationFactory en Functions).
   - Minimal API: `WebApplicationFactory<Program>` (necesita
     `public partial class Program { }` al final de Program.cs).
   - Integración con emulador (Azurite/Cosmos/MsSql) vía
     **Testcontainers** + **`Xunit.SkippableFact`**: si no hay Docker,
     `Skip.If(true, ...)` → la suite SIEMPRE queda verde sin Docker.
6. **DI**: tras escribir las funciones, **cruzar A MANO cada parámetro
   de constructor de cada `[Function]`/endpoint (y de los servicios que
   estos inyectan) contra los `AddSingleton/Scoped/Transient` de
   `Program.cs`**. Crítico: ver "Lección DI" abajo.
7. **Verificar**: `dotnet build <slnx>` (0 warnings — `TreatWarningsAsErrors`)
   y `dotnet test`. Todo verde (skips OK).
8. **Scripts `az`** en `scripts/`: `_lib.sh`, `.env.demo.example`,
   `.gitignore` (`.env.demo`), `01-provision.sh`, smoke-test, cleanup,
   `demo.sh` (menú). Despliegue Azure **por Portal en el README**; los
   scripts `az` son complemento didáctico.
9. **README del ejemplo**: mapeo a slides, estructura, tests, despliegue
   Portal, "cuándo usar", próximo paso. Tres niveles de índice:
   `examples/README.md` (global) + `examples/MXX-*/README.md` (módulo) +
   README del ejemplo.
10. **Commit + push** con trailer
    `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>`.
    Verificar que no se cuela `bin/obj/.vs/lscache/.env.demo/local.settings.json`.

## Reglas duras del usuario (no negociables)

- **TFM `net10.0` siempre**, aunque la doc lectiva diga .NET 8.
- **No lanzar apps**: nunca `dotnet run`/`func start`/`npm run`. El
  alumno las lanza. La verificación automática se queda en build + test.
- **Tests con cada feature**, no como issue aparte.
- **Azure por Portal** en los READMEs (no `az`), salvo el Service
  Principal de CI. Scripts `az` como complemento, nunca sustituto.
- **No pedir datos que se pueden leer** del filesystem.
- Idioma de los ejemplos/README/commits: español (commits cuerpo
  pueden ir en inglés como se ha venido haciendo en M03+).

## Lecciones aprendidas (críticas — ya documentadas en READMEs)

1. **Bug de DI latente** (encontrado en S3.4, ya corregido): los tests
   de Functions instancian con `new`, **no ejercitan el contenedor DI**.
   Un `Program.cs` con un servicio sin registrar pasa los tests pero el
   Function App real revienta en runtime ("Unable to resolve service").
   → Paso 6 de la receta es obligatorio. (Minimal API de M02/M05 no
   sufre esto: sus tests con WebApplicationFactory sí ejercen el DI.)
2. **`SkippableFact` para integración**: integración real con
   Testcontainers pero se salta si no hay Docker → `dotnet test` siempre
   verde. Patrón establecido en M04-S4.5 y M05-S5.1.
3. **Catálogo de gotchas de testing**: el README de
   `examples/M04-Azure-Functions-II/S4.5-testing-depuracion/` tiene el
   catálogo consolidado (FakeServiceBusMessageActions, ServiceBusModelFactory
   sin params de deadLetter → ApplicationProperties, NSubstitute para
   TaskOrchestrationContext + CreateReplaySafeLogger null,
   Activator.CreateInstance ignora params opcionales,
   `[ExponentialBackoffRetry]` inválido en ServiceBusTrigger = AZFW0012).
   **Leerlo antes de testear cualquier ejemplo de Functions.**
4. **`TryAdd` vs `GetOrAdd`** para idempotencia concurrente (lección
   S3.5): `GetOrAdd` con factory puede invocar el factory varias veces;
   `ConcurrentDictionary.TryAdd` gana exactamente una vez.
5. **Coste**: Service Bus Standard ~10€/mes fijo (S4.1, S4.3) — aviso
   prominente en README y `demo.sh`. Cosmos serverless / Azure SQL
   serverless / Storage ≈ 0€. Cada ejemplo trae `cleanup`.
6. **EF Core gotchas (S5.2)** — para los ejemplos con EF Core:
   - **SQLite no soporta `ORDER BY DateTimeOffset`**
     (`NotSupportedException`). Si una entidad se ordena por fecha y se
     testea con SQLite in-memory (CAPA 2), usar `DateTime` (UTC), no
     `DateTimeOffset`. Funciona en SQL Server (`datetime2`) y SQLite.
   - **No migrar en el arranque** (`Database.Migrate()` en `Program.cs`)
     = anti-pattern 8 (slide 35). El test de integración aplica la
     migración en su propio scope; CAPA 2 usa `EnsureCreated()` (las
     migraciones son SQL Server-specific).
   - **Versionado**: `Microsoft.EntityFrameworkCore.SqlServer` arrastra
     `Microsoft.Data.SqlClient` que exige `Azure.Identity >= 1.14.2`.
     Si pones Azure.Identity explícito, ≥ esa versión (NU1605 con
     `TreatWarningsAsErrors`). EF Core/tools **10.0.2** (alinear paquete
     y `dotnet ef` a la misma versión).
   - **Testcontainers.MsSql 4.11**: el ctor sin parámetros está
     `[Obsolete]` (CS0618 = error). Usar `new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")`.
   - **CAPA 0 DI sin Docker**: como la integración (CAPA 3) se salta sin
     Docker, añadir un test que resuelva el contenedor real
     (`WebApplicationFactory` + `CreateScope` + `GetRequiredService`,
     sin tocar la BD). Cierra la lección DI aunque no haya Docker.
   - **`dotnet ef migrations remove`** intenta conectar a la BD para ver
     si está aplicada; con cs placeholder cuelga. Regenerar borrando la
     carpeta `Migrations/` y `dotnet ef migrations add` (no conecta).

## Convenciones de scaffolding (copiar tal cual)

- `Directory.Build.props`: net10.0, Nullable enable, ImplicitUsings,
  `TreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`.
- `global.json`: SDK `10.0.300-preview.0.26177.108`, allowPrerelease.
- `.gitattributes`: `eol=lf`.
- `.slnx` con los 2 proyectos (src + tests).
- Functions worker isolated 2.0; paquetes Worker.Extensions.* por trigger.
- Versiones de paquetes ya fijadas y verificadas en ejemplos previos
  (reutilizar las mismas: Worker 2.0.0, Sdk 2.0.5, CosmosDB ext 4.11.0,
  ServiceBus ext 5.22.0, DurableTask ext 1.5.0, Polly.Core 8.5.0,
  EF Core 9.0.x, NSubstitute 5.3.0, Xunit.SkippableFact 1.5.23,
  Testcontainers.* 4.x, Azure.Storage.Blobs 12.23.0...).

## Cómo se mide "done" de un ejemplo

`dotnet build` 0 err/0 warn · `dotnet test` todo verde (skips OK) ·
scripts ejecutables (`chmod +x`) · README con mapeo a slides ·
3 índices actualizados · commit+push limpio sin junk.
