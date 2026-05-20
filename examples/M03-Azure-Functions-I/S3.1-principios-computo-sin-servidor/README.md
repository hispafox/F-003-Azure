# S3.1 — Principios del cómputo sin servidor

> **Submódulo de referencia:** [M03-S3.1](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.1-principios-computo-sin-servidor-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier en Azure:** Consumption (gratuito hasta 1M ejecuciones/mes)

> ℹ️ El submódulo lectivo está sobre **.NET 8** y `Microsoft.Azure.Functions.Worker 1.x`.
> Aquí usamos **.NET 10** y **Worker SDK 2.0** (último estable). API casi idéntica.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el taxi vs el coche propio (Consumption vs App Service), el skeleton canónico de Functions isolated y la decisión "patrón de tráfico → hosting".

## Objetivo

Construir el **skeleton canónico** de Azure Functions isolated worker que se
reutilizará en los siguientes submódulos del módulo M03 (S3.2 HTTP, S3.3 Timer,
S3.4 Blob, S3.5 Cosmos, S3.6 Bindings).

S3.1 es el primer ejemplo del módulo y cambia el stack:

| | M02 (App Services) | M03 (Functions) |
| --- | --- | --- |
| SDK | `Microsoft.NET.Sdk.Web` | `Microsoft.NET.Sdk` |
| Bootstrap | `WebApplication.CreateBuilder` | `FunctionsApplication.CreateBuilder` |
| Hosting | App Service Plan (siempre on) | Consumption (scale-to-zero) |
| Coste base | ~13 €/mes (B1) | 0 € (1M ejecuciones gratis/mes) |
| Tests | `WebApplicationFactory<Program>` | invocación directa de la clase función |
| Cold start | Always On lo evita | 1-3 s en isolated .NET (slide 8) |

El ejemplo es deliberadamente minimalista: un solo HTTP trigger `Hello` que
sirve para verificar que la Function App arranca y los bindings funcionan.

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| HostBuilder + DI canónico | 12 | [`Program.cs`](src/AzureFunctions.Demo/Program.cs) |
| `host.json` con timeout, logging, http | 13 | [`host.json`](src/AzureFunctions.Demo/host.json) |
| `local.settings.json` ignorado por git | 14 | `.gitignore` + [`local.settings.json.example`](src/AzureFunctions.Demo/local.settings.json.example) |
| Anatomía: trigger + función + bindings | 9 | [`Functions/HelloFunction.cs`](src/AzureFunctions.Demo/Functions/HelloFunction.cs) |
| Isolated worker .NET | 11 | csproj (`OutputType=Exe`, `Worker.Sdk` analyzer) |
| `.csproj` bien estructurado | 28 | [`AzureFunctions.Demo.csproj`](src/AzureFunctions.Demo/AzureFunctions.Demo.csproj) |
| Plan Consumption (Linux) | 6 | [`scripts/01-provision.sh`](scripts/01-provision.sh) |
| Storage Account requerido | 14 | mismo script |
| Niveles de autorización HTTP | 24 | `[HttpTrigger(AuthorizationLevel.Anonymous, ...)]` |
| Cold start + warm instance | 5, 8 | README → "Cold start: lo que vas a observar" |
| Buenas prácticas (DI, idempotencia, logs) | 25 | comentarios inline en `Program.cs` |

## Estructura

```
S3.1-principios-computo-sin-servidor/
├── README.md
├── AzureFunctions.Demo.slnx
├── Directory.Build.props          (TFM net10.0)
├── global.json
├── .gitattributes
├── src/AzureFunctions.Demo/
│   ├── AzureFunctions.Demo.csproj
│   ├── Program.cs                 HostBuilder + DI + AppInsights
│   ├── host.json
│   ├── local.settings.json.example  ← copia a local.settings.json (no se commitea)
│   ├── .gitignore                 ignora local.settings.json
│   └── Functions/
│       └── HelloFunction.cs       GET /api/hello?name=...
├── tests/AzureFunctions.Demo.Tests/  (3 tests)
└── scripts/
    ├── .env.demo.example
    ├── _lib.sh
    ├── 01-provision.sh            RG + Storage + Function App Consumption
    ├── 02-deploy.sh               publish + zip + functionapp deployment source config-zip
    ├── 03-smoke-test.sh
    ├── 04-cleanup.sh
    └── demo.sh
```

## Requisitos previos

- **.NET SDK 10**.
- **Azure Functions Core Tools** (`func`) recomendado para `func start` local.
  El despliegue funciona con solo `dotnet` + `az` (los scripts no requieren `func`).
- **Azurite** (opcional) para emular Storage local: `npm install -g azurite`.

## Ejecución local

Antes de arrancar, copia `local.settings.json.example` a `local.settings.json`
(éste último está en `.gitignore`):

```bash
cp src/AzureFunctions.Demo/local.settings.json.example src/AzureFunctions.Demo/local.settings.json
```

Lanza Azurite en otra terminal:

```bash
azurite --silent
```

Y arranca la Function App:

```bash
cd src/AzureFunctions.Demo
func start
# Functions:
#   Hello: [GET] http://localhost:7071/api/hello

curl 'http://localhost:7071/api/hello?name=Pedro'
```

Si no tienes `func` instalado, también puedes hacer `dotnet run` (la salida
es similar pero menos limpia que con Core Tools).

## Tests

```bash
dotnet test
```

3 tests:

- `Hello_With_Name_Returns_Greeting` — verifica el saludo personalizado.
- `Hello_Without_Name_Defaults_To_Azure` — fallback cuando no hay query param.
- `Hello_Includes_Diagnostic_Fields` — los 6 campos diagnósticos del payload.

Los tests **no usan `WebApplicationFactory`** — instancian `HelloFunction`
directamente y le pasan un `HttpRequest` fabricado con `DefaultHttpContext`.
Es el patrón recomendado para Functions isolated worker: rápido y sin
dependencia del runtime de Azure Functions.

## Tour del código

### `Program.cs` (slide 12)

```csharp
var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
builder.Build().Run();
```

`FunctionsApplication.CreateBuilder` es la API moderna del Worker SDK 2.x —
sustituye al `HostBuilder` clásico que aparece en muchos tutoriales viejos.
`ConfigureFunctionsWebApplication()` activa el integration con ASP.NET Core
para que `[HttpTrigger]` pueda recibir `HttpRequest` y devolver `IActionResult`.

### `HelloFunction.cs` (slide 9)

```csharp
[Function(nameof(Hello))]
public IActionResult Hello(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hello")] HttpRequest req)
```

- `[Function(nameof(Hello))]` registra la función con ese nombre en el host.
- `[HttpTrigger]` define el evento que la dispara: GET en `/api/hello`.
- `AuthorizationLevel.Anonymous` (slide 24) — sin function key.
  En producción real usarías `Function` o un middleware de autenticación.

El payload incluye `runtime`, `os`, `functionsVersion`, `workerRuntime` —
útil tras un deploy para verificar que la Function App está en .NET 10
isolated, no en Windows con in-process.

### `host.json` (slide 13)

```json
{
  "version": "2.0",
  "functionTimeout": "00:10:00",
  "extensions": { "http": { "routePrefix": "api", "maxConcurrentRequests": 100 } }
}
```

- `functionTimeout`: 10 min, el máximo del plan Consumption.
- `routePrefix`: el prefijo `api` se mantiene por consistencia con los otros
  módulos (en App Service también usábamos `/api/...`).

### `.csproj` (slide 28)

Llama la atención el `<OutputType>Exe</OutputType>` — obligatorio en isolated
worker. Sin él, Functions runtime no encuentra el `Main` de tu proceso.

`Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` es la extensión
que permite usar `HttpRequest`/`IActionResult` (en lugar del legacy
`HttpRequestData`/`HttpResponseData`). Es la opción recomendada para .NET
moderno.

## Cold start: lo que vas a observar

Tras desplegar y llamar al endpoint por primera vez:

```bash
time curl 'https://<func>.azurewebsites.net/api/hello'
# real    0m3.412s   ← cold start (slide 8)
```

Repite la llamada en menos de 20 minutos:

```bash
time curl 'https://<func>.azurewebsites.net/api/hello'
# real    0m0.087s   ← warm instance
```

Esto es comportamiento normal y esperable en plan Consumption. Si tu
escenario no tolera el cold start (API de cara al usuario que tarda 3 s
en arrancar), las opciones son:

- Plan **Premium** o **Flex Consumption** con pre-warmed instances (slide 6).
- Mover ese trigger HTTP a **App Service** (que siempre está caliente).

## Despliegue por Portal de Azure

> Pasos canónicos. Si prefieres terminal, salta a la siguiente sección.

### Paso 1 — Resource Group + Storage Account

`Portal → Resource groups → Create` → `rg-curso-m03-s31`.

`Portal → Storage accounts → Create`:
- Name: `stcursom03s31<iniciales>`
- Performance: Standard
- Redundancy: LRS
- Region: la misma que el RG.

Functions necesita un Storage Account para metadatos internos (locks de
timer triggers, leases de Cosmos DB triggers, etc.). Slide 14.

### Paso 2 — Function App

`Portal → Function App → Create`:

| Campo | Valor |
| --- | --- |
| Name | `func-curso-m03-s31-<iniciales>` (único globalmente) |
| Hosting plan | **Consumption (Serverless)** |
| Runtime stack | **.NET** |
| Version | **10 isolated** (si no aparece, **8 isolated**) |
| Operating System | **Linux** |
| Storage account | el que creaste arriba |

### Paso 3 — Deploy

VS Code → panel Azure → Function App → tu app → botón derecho → **Deploy to
Function App…** apuntando a `src/AzureFunctions.Demo`.

VS Code se encarga de `dotnet publish` + zip + subida.

### Paso 4 — Verificar

```bash
curl 'https://<func>.azurewebsites.net/api/hello?name=Pedro'
```

El primer hit puede tardar 1-3 s (cold start). Los siguientes <100 ms.

### Paso 5 — Cleanup

`Portal → Resource groups → rg-curso-m03-s31 → Delete`.

## Despliegue alternativo con scripts `az`

```bash
cd scripts
cp .env.demo.example .env.demo
# editar SUBSCRIPTION_ID, STORAGE único, FUNC único

bash 01-provision.sh        # RG + Storage + Function App
bash 02-deploy.sh           # publish + zip + deploy
bash 03-smoke-test.sh       # 3 checks
bash 04-cleanup.sh          # cuando termines
```

`bash demo.sh` para el menú interactivo. Incluye opción "Log streaming"
que llama a `az functionapp log tail` (slide 23).

## Troubleshooting

| Síntoma | Causa típica | Fix |
| --- | --- | --- |
| `--runtime-version 10` falla en `01-provision.sh` | La región no soporta .NET 10 todavía en Functions | Cambia a `--runtime-version 8` en el script |
| Primera petición tarda 5+ segundos | Cold start del Consumption (slide 8) | Repite — la segunda baja a < 100 ms |
| `403 Forbidden` al llamar al endpoint | El runtime no está en `Anonymous` (cambió a `Function`) | Añade `?code=<function-key>` o cambia a `Anonymous` |
| Build de tests falla en CI con OOM | El analyzer `Microsoft.Azure.Functions.Worker.Sdk` consume memoria | Subir RAM del runner o build con `--no-restore` después del primer restore |
| Despliegue OK pero `/api/hello` da 404 | No se copió `host.json` ni el assembly | Verifica `<None Update="host.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` en el csproj |

## Hand-off al siguiente submódulo

[`S3.2 — Trigger HTTP`](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.2-trigger-http-v4.md)
profundiza en este mismo skeleton: enrutamiento avanzado, parámetros,
validación, `IResult` vs `IActionResult`, autenticación con function keys
y `AuthorizationLevel.Function`. Reutilizaremos la estructura del proyecto
de S3.1 — solo cambian las funciones.
