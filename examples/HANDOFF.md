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

## Estado actual (HANDOFF para chat nuevo — leer esto primero)

| Módulo | Estado |
| --- | --- |
| M01 Intro Azure | 2 ejemplos (S1.P Hello World, S1.P2 Cloud Shell) |
| M02 App Services | ✅ completo 7/7 |
| M03 Azure Functions I | ✅ completo 8/8 |
| M04 Azure Functions II | ✅ completo 7/7 |
| M05 Almacenamiento y BBDD | ✅ completo 7/7 |
| M06 Seguridad y Auth | ✅ completo 8/8 (S6.1–S6.6 + S6.P + S6.P2) |
| M07 Integración y MSIX | ✅ completo 9/9 (S7.1–S7.7 + S7.P + S7.P2) |
| M08 DevOps y Automatización | 🚧 7/8 (S8.1–S8.6 + S8.P hechos; S8.P2 pendiente) |
| M09–M11 | pendientes |

### Estado git EXACTO (verificar con `git fetch` + `git status`)

- **`origin/main` = local `main` = commit `539473c`** = mi push previo
  de `M08-S8.6 App Insights y Azure Monitor`. M02–M07 + M08-S8.1..S8.6
  en remoto.
- **S8.P está CONSTRUIDO, VERDE (32 tests pass + 0 fail + 0 warn) pero
  SIN COMMITEAR** en el working tree. Conceptual (lección 9 del
  HANDOFF): el pipeline real corre en ADO/GHA, lo testeable son las
  piezas decisorias (preflight, stages, smoke evaluator). Sin
  commitear ahora mismo (acotado a S8.P + 3 índices):
  - `?? examples/M08-DevOps-Automatizacion/S8.P-practica-pipeline-cicd/` (nuevo)
  - ` M examples/M08-DevOps-Automatizacion/README.md` (fila S8.P + "7/8")
  - ` M examples/README.md` (fila S8.P + footer "⏳ M08 7/8")
  - ` M examples/HANDOFF.md` (este archivo)
  - **IMPORTANTE — NO stagear**: el otro chat sigue activo con
    `MANUAL.md` y `.claude/skills/**`. NUNCA `git add -A`.
- Cuando el usuario diga **"sube"**: `git fetch`, comprobar
  ahead/behind, y commit ACOTADO + push:
  ```
  cd c:/w/repos/F-003-Azure
  git add examples/M08-DevOps-Automatizacion/S8.P-practica-pipeline-cicd \
          examples/M08-DevOps-Automatizacion/README.md \
          examples/README.md examples/HANDOFF.md
  # commit -F - con cuerpo en inglés + trailer Co-Authored-By (ver paso 10)
  git push origin main
  ```

**Siguiente tarea concreta:** `M08-S8.P2` — leer primero
`doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.P2-practica-github-actions-publish-profile-v1.md`.
Es práctica → GitHub Actions con publish profile (sin OIDC, para
repos personales o forks). **Probable patrón**: validador del
secreto `AZURE_WEBAPP_PUBLISH_PROFILE`, generador del workflow
`.github/workflows/deploy.yml`, parser del XML del publish profile
(extrae app name + slot, detecta credenciales rotadas), recomendador
de qué pasar a OIDC vs cuándo mantener publish profile. Puerto
launchSettings siguiente libre: **5112**.

**Lección S8.6 (Coercer JSON → object?)**: en un `switch` expression
con `JsonValueKind.Number`, una rama ternaria `TryGetInt64(out i) ? i
: v.GetDouble()` **promociona el `long` a `double`** porque C# fuerza
un tipo común de retorno antes de boxear a `object`. El test
`Assert.IsType<long>` falla aunque el código "parezca" correcto.
**Fix**: convertir la rama del switch en un bloque imperativo (`if
(esEntero && v.TryGetInt64(out var i)) return i;`), donde cada
`return` se boxea por separado. Aplicable a cualquier parser que
devuelva `object?` con varios tipos numéricos.

> **Regla de proceso (memoria `feedback-esperar-confirmacion-push`)**:
> en este repo **NUNCA `git push` sin un "sube" explícito** del usuario.
> El usuario trabaja en paralelo desde otro chat (sube presentaciones a
> `doc/**`) → stagear SIEMPRE con rutas explícitas, jamás `git add -A`;
> `git fetch` antes de push; commit acotado por carpeta de ejemplo +
> los 2-3 índices + HANDOFF. Trigger "sigue"/"sigamos" = construir el
> siguiente submódulo (NO implica push); "sube" = pushear lo que esté
> verde y sin commitear.

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
7. **Cosmos gotchas (S5.3)** — para los ejemplos con `Microsoft.Azure.Cosmos`:
   - **Newtonsoft.Json explícito obligatorio**: el SDK 3.x usa Newtonsoft
     como serializador por defecto y **falla el build** si no lo
     referencias (o `AzureCosmosDisableNewtonsoftJsonCheck=true`). Añadir
     `Newtonsoft.Json 13.0.3`. El POCO va sin atributos: con
     `CosmosPropertyNamingPolicy.CamelCase`, `Id`→`"id"` (lo exige Cosmos).
   - **`Testcontainers.CosmosDb`/`MsSql` ctor sin args = `[Obsolete]`**
     (CS0618 = error con TreatWarningsAsErrors). Usar el ctor con imagen:
     `new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest")`.
   - **`ConfigureTestServices`** vive en `Microsoft.AspNetCore.TestHost`
     (añadir el `using`); reemplaza singletons del Program para el test
     (aquí, el `CosmosClient` por uno apuntando al emulador con
     `ConnectionMode.Gateway` + `HttpClientFactory => emulador.HttpClient`).
   - **Cosmos NO tiene proveedor in-memory** (no hay CAPA 2 tipo SQLite):
     lógica testable → clases puras (CAPA 1); round-trip → emulador
     (CAPA "Integration") con `SkippableFact` capturando CUALQUIER
     excepción de arranque (el emulador es pesado y a menudo no arranca).
   - **El SDK de Cosmos es lazy**: construir `CosmosClient` + `GetContainer`
     no abre conexión → el test de CAPA 0 DI corre sin Docker. Usar la
     clave **pública** del emulador como cs por defecto (no es secreto).
8. **Submódulos transversales / no emulables (S5.4 Managed Identity)** —
   cuando el tema NO es un servicio de datos nuevo sino *cómo* se conecta
   (auth, seguridad, cifrado):
   - **Entra ID / Managed Identity NO se emula** (Azurite/Cosmos emulator
     usan key fija). NO forzar una CAPA de integración: queda
     **CAPA 1 (lógica pura) + CAPA 0 (DI container)**, y se documenta el
     porqué en README y csproj (igual que "Cosmos no tiene in-memory").
     Sin `SkippableFact` si no hay nada que saltar — no inventes un test
     que siempre se salta.
   - **`DefaultAzureCredential`/clientes SDK son lazy**: construir no
     autentica → el test DI corre sin Azure. Patrón slide 21:
     `AddSingleton<TokenCredential>` una vez y todos los clientes
     (`BlobServiceClient`, `CosmosClient`...) lo comparten; el test DI
     verifica `Assert.Same` la credencial.
   - **Lógica pura aunque el tema sea "conceptual"**: siempre hay algo
     testeable — mapear config→opciones de credencial, escanear secretos
     en connection strings, recomendar el rol RBAC mínimo. Tres clases
     puras (`*Factory`/`*Scanner`/`*Advisor`) mantienen el patrón de S5.2/S5.3.
   - **Endpoint "demo real" que requiere Azure**: incluirlo pero que
     devuelva 503 claro si falta config (no romper `dotnet run` sin
     Azure); se prueba a mano con `az login`.
9. **Patrón M06 conceptual (S6.1–S6.P, replicar en S6.P2 y similares)** —
   todo M06 es seguridad transversal, no servicios de datos. Receta fija
   por submódulo (todos verdes, 0 warn, sin integración):
   - **Proyecto**: `Minimal API` `<Tema>.Demo.Api` (Web SDK), **sin
     PackageReference externas** (lógica pura; sólo ASP.NET). `.slnx`,
     `Directory.Build.props`, `global.json`, `.gitattributes` estándar.
     Puertos launchSettings: S6.1=5088 … S6.P=5094 (S6.P2 → 5095).
   - **3 clases puras** en `src/.../<Tema>/` (estilo `*Advisor` /
     `*Validator` / `*Policy` / `*Inspector`): tablas de decisión y
     parsing testeables. Convención de nombres en español, slides citadas
     en comentarios.
   - **1 servicio inyectable** `I<X>` + `<X>` (`AddSingleton`) que
     **compone** las clases puras → "plan/assessment". Es el seam del
     test de contenedor.
   - **Endpoints**: `/health` + grupo temático con GETs (query params)
     y POSTs (DTO body) que delegan en lo puro/servicio. Si el tema lo
     pide, un endpoint "protegido" que lee cabeceras (p.ej. Easy Auth
     `X-MS-CLIENT-PRINCIPAL-*`) y responde 401/200.
   - **Tests**: `Unit_*` por cada clase pura (incluye casos límite +
     `Assert.Throws`); `DiContainer_Tests` que resuelve el servicio del
     `WebApplicationFactory` real y `Assert.Same` el singleton (cubre la
     lección DI sin Docker); si hay endpoint con cabeceras, un
     `Api_*Tests` E2E con `WebApplicationFactory` simulándolas.
     `GlobalUsings.cs` = `global using Xunit;`.
   - **SIN CAPA de integración** y se documenta el porqué en README +
     csproj ("Entra ID/OAuth/KV no emulable"). **No** inventar un
     `SkippableFact` que siempre se salta.
   - **Scripts `az` SOLO LECTURA** (`_lib.sh`, `.env.demo.example`,
     `.gitignore`, `01-*.sh` read-only, `demo.sh`): inventario/postura,
     **no crean recursos → sin `03-cleanup.sh`**; el menú lo dice. Nunca
     leer valores de secretos.
   - **README** con mapeo a slides, "sin CAPA de integración a propósito",
     despliegue por Portal, ideas centrales, próximo paso. Actualizar
     `examples/M06-Seguridad-Auth/README.md` (ya existe) + `examples/README.md`
     (fila + footer N/8) + este HANDOFF.
   - Pure helpers ya escritos reutilizables como referencia: S6.2
     `JwtInspector`, S6.3 `PkceGenerator`/`AuthorizeUrlBuilder`, S6.6
     `KeyVaultReference` (GeneratedRegex en `static partial class`),
     S6.P `EasyAuthPrincipal`/`KeyVaultRefAppSettings`.

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
