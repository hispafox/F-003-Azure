# S2.5 — Monitorización y diagnóstico

> **Submódulo de referencia:** [M02-S2.5](../../../doc/M02-App-Services/v4-actual/M02-S2.5-monitorizacion-diagnostico-v4.md)
> **TFM:** `net10.0` · **Tipo:** Minimal API · **Tier:** Standard S1
> **Cierra el módulo M02.**

> ℹ️ El submódulo está redactado sobre **.NET 8**, código en **.NET 10**.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el panel del coche moderno (Live Metrics, Application Map, Logs KQL, alertas), OpenTelemetry condicional, custom metrics con dimensiones, PII scrubbing antes de loguear y la asimetría "conectar AI el día uno vs reconstruir histórico después".

## Objetivo

Cerrar el módulo conectando todo lo que tienes hasta ahora a **Application
Insights con OpenTelemetry**:

- **OpenTelemetry + Azure Monitor exporter** (slide 20) — la opción moderna.
  `Program.cs` lo activa solo si `APPLICATIONINSIGHTS_CONNECTION_STRING` está
  presente, así que en local sigue funcionando sin Azure.
- **`AppMeter`** con `IMeterFactory` y custom metrics de negocio (slide 22):
  pedidos creados, suma de importe, histograma de duración.
- **`/demo/orders`** que incrementa los counters y enriquece el span de
  tracing con `Activity.Current.SetTag` (slide 21).
- **`/demo/error?type=...`** para escenificar dashboards y alertas: `500`,
  `exception`, `slow` (5 s), `dependency-fail`.
- **`PiiScrubber`** + **`/demo/log`** (slide 25) — redacta emails, tarjetas y
  JWTs antes de loggear.
- **Scripts `az`** que provisionan Application Insights workspace-based,
  conectan la app, crean Action Group + alertas + Availability Test, y un
  generador de tráfico realista para llenar el dashboard en clase.

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| Métricas built-in de App Service | 4 | README → "Despliegue por Portal" |
| Health Check (heredado de S2.1) | 6 | [`Endpoints/HealthEndpoints.cs`](src/AppService.Demo.Api/Endpoints/HealthEndpoints.cs) |
| Logging desde código | 7, 8 | `Program.cs` (AzureWebAppDiagnostics) + `ILogger` en cada endpoint |
| App Service Diagnostics + Kudu | 9, 10 | README → "Despliegue por Portal" |
| Application Insights — preview | 11 | [`scripts/01-provision.sh`](scripts/01-provision.sh) crea el workspace + AI |
| Alertas | 12, 27 | [`scripts/05-create-alerts.sh`](scripts/05-create-alerts.sh) (3 metric alerts) |
| Application Insights integración | 15, 18 | `Program.cs` (UseAzureMonitor) + Live Metrics en Portal |
| Log Analytics queries (KQL) | 16 | [`scripts/08-show-kql-queries.sh`](scripts/08-show-kql-queries.sh) |
| Smart Detection | 17 | Automático cuando AI está conectado; README lo explica |
| Availability Tests | 19 | [`scripts/06-create-availability-test.sh`](scripts/06-create-availability-test.sh) |
| OpenTelemetry + Azure Monitor | 20 | [`Program.cs`](src/AppService.Demo.Api/Program.cs) → `UseAzureMonitor()` |
| Distributed tracing con tags | 21 | [`Endpoints/OrdersEndpoints.cs`](src/AppService.Demo.Api/Endpoints/OrdersEndpoints.cs) (`Activity.Current.SetTag`) |
| Custom metrics (Counter / Histogram) | 22 | [`Telemetry/AppMeter.cs`](src/AppService.Demo.Api/Telemetry/AppMeter.cs) |
| Structured logging | 23 | `logger.LogInformation("... {Field} ...", value)` en endpoints |
| PII scrubbing | 25 | [`Telemetry/PiiScrubber.cs`](src/AppService.Demo.Api/Telemetry/PiiScrubber.cs) + [`Endpoints/LogDemoEndpoints.cs`](src/AppService.Demo.Api/Endpoints/LogDemoEndpoints.cs) |
| Action Groups | 26 | [`scripts/04-create-action-group.sh`](scripts/04-create-action-group.sh) |
| Profiler / Snapshot Debugger | 29 | App Settings opcionales en `03-configure-app-insights.sh` |

## Estructura

```
S2.5-monitorizacion-diagnostico/
├── README.md
├── AppService.Demo.Monitor.slnx
├── Directory.Build.props
├── global.json
├── .gitattributes
├── src/AppService.Demo.Api/
│   ├── AppService.Demo.Api.csproj         (+ Azure.Monitor.OpenTelemetry.AspNetCore + OpenTelemetry.Instrumentation.Runtime)
│   ├── Program.cs                          (UseAzureMonitor condicional)
│   ├── appsettings.json                    (+ ApplicationInsights:ConnectionString placeholder)
│   ├── appsettings.Development.json
│   ├── Properties/launchSettings.json
│   ├── Configuration/                      (igual que S2.4)
│   ├── Telemetry/                          ← NUEVO
│   │   ├── AppMeter.cs                     IMeterFactory + counters/histogram
│   │   └── PiiScrubber.cs                  regex compilados
│   ├── Endpoints/
│   │   ├── (todos los anteriores)
│   │   ├── OrdersEndpoints.cs              ← NUEVO POST /demo/orders
│   │   ├── DemoErrorEndpoints.cs           ← NUEVO GET  /demo/error?type=...
│   │   └── LogDemoEndpoints.cs             ← NUEVO POST /demo/log
│   └── Services/                            (igual que S2.4)
├── tests/AppService.Demo.Api.Tests/         (58 tests, 58 verdes)
└── scripts/
    ├── .env.demo.example                    + LAW + AI + ACTION_GROUP + NOTIFY_EMAIL
    ├── _lib.sh
    ├── 01-provision.sh                      RG + plan + app + LAW + AI workspace-based
    ├── 02-deploy.sh
    ├── 03-configure-app-insights.sh         APPLICATIONINSIGHTS_CONNECTION_STRING + Profiler
    ├── 04-create-action-group.sh            email
    ├── 05-create-alerts.sh                  Http5xx + latencia + CPU
    ├── 06-create-availability-test.sh       ping multi-región a /health
    ├── 07-generate-traffic.sh               curl loops mezclando endpoints OK y de error
    ├── 08-show-kql-queries.sh               imprime queries KQL para copiar
    ├── 09-cleanup.sh
    └── demo.sh                              menú interactivo
```

## Requisitos previos

- .NET SDK 10
- Suscripción de Azure
- Para los scripts: Azure CLI (`az`), `jq`, `curl`, `zip`. Permisos para crear
  Log Analytics, Application Insights, alertas y action groups.

## Ejecución local

```bash
dotnet run --project src/AppService.Demo.Api --launch-profile http
# → http://localhost:5080
```

Sin `APPLICATIONINSIGHTS_CONNECTION_STRING`, OpenTelemetry no se inicializa pero
el código funciona igual: los counters se incrementan, los logs salen por
consola, etc. Para conectar a tu propia AI desde local:

```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=xxx;IngestionEndpoint=https://..."
dotnet run --project src/AppService.Demo.Api
```

### Endpoints nuevos

| Verbo | Ruta | Notas |
| --- | --- | --- |
| POST | `/demo/orders` | body `{ sku, quantity, unitPrice, priority }` → incrementa metrics y devuelve `orderId` + `processingMs` |
| GET | `/demo/error?type=500\|exception\|slow\|dependency-fail` | reproduce cuatro tipos de fallo |
| POST | `/demo/log` | body `{ message }` → loguea con PII scrubbing aplicado y devuelve `{ originalLength, scrubbed, redactionsApplied }` |

## Tests

```bash
dotnet test
```

58 tests:

- Heredados del S2.4 (41).
- **`PiiScrubberTests` (8)**: emails, tarjetas (varios formatos), JWT, mezcla, texto seguro, null/empty.
- **`OrdersEndpointTests` (2)**: orderId con prefijo `ORD-`, importe calculado, 400 con cantidad 0.
- **`DemoErrorEndpointTests` (4)**: type=500 → 500, type=exception → 500, type desconocido → 400, type=dependency-fail → 502.
- **`LogDemoEndpointTests` (2)**: scrub de email confirmado en respuesta, sin redacciones cuando el mensaje es seguro.

## Tour del código

### `AppMeter` ([código](src/AppService.Demo.Api/Telemetry/AppMeter.cs))

```csharp
public AppMeter(IMeterFactory factory)
{
    var meter = factory.Create("AppService.Demo.Api");
    OrdersCreated = meter.CreateCounter<long>("demo.orders.created", unit: "{order}");
    OrderAmountTotal = meter.CreateCounter<double>("demo.orders.amount.total", unit: "EUR");
    OrderProcessingDuration = meter.CreateHistogram<double>("demo.orders.duration", unit: "ms");
}
```

El meter `AppService.Demo.Api` se registra en OTel con `.AddMeter(...)` para
que Azure Monitor reciba estas métricas. Aparecen en App Insights como
**Custom metrics** y se pueden graficar en dashboards.

### `OrdersEndpoints` — tracing + metrics juntos

```csharp
var activity = Activity.Current;
activity?.SetTag("order.id", orderId);
activity?.SetTag("order.sku", request.Sku);
activity?.SetTag("order.priority", request.Priority);

var priorityTag = new KeyValuePair<string, object?>("priority", request.Priority);
metrics.OrdersCreated.Add(1, priorityTag);
metrics.OrderProcessingDuration.Record(sw.Elapsed.TotalMilliseconds, priorityTag);
```

Las **tags** del `Activity` enriquecen el span en distributed tracing —
filtrables en App Insights con `requests | where customDimensions.["order.priority"] == "high"`.
Las dimensions del counter generan métricas separadas por valor de prioridad —
permiten alertar p.ej. si los pedidos `high` dejan de fluir.

### `PiiScrubber` ([código](src/AppService.Demo.Api/Telemetry/PiiScrubber.cs))

Tres regex compilados con `[GeneratedRegex]`:

| Patrón | Reemplazo |
| --- | --- |
| Email (`a.b@c.d`) | `[REDACTED:EMAIL]` |
| Tarjeta de crédito (`9999-9999-9999-9999`, con o sin separadores) | `[REDACTED:CC]` |
| Bearer JWT (3 segmentos base64) | `[REDACTED:TOKEN]` |

Orden de aplicación: JWT → tarjeta → email (los más específicos primero).

### `Program.cs` — OpenTelemetry condicional

```csharp
var aiConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
                       ?? builder.Configuration["ApplicationInsights:ConnectionString"];

if (!string.IsNullOrEmpty(aiConnectionString))
{
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor(o => o.ConnectionString = aiConnectionString)
        .WithTracing(tracing => tracing.AddSource(AppMeter.MeterName))
        .WithMetrics(metrics => metrics
            .AddMeter(AppMeter.MeterName)
            .AddRuntimeInstrumentation());
}
```

`UseAzureMonitor` configura automáticamente:

- Logs (incluyendo `ILogger` ya registrado).
- Traces de ASP.NET Core (request/response).
- Métricas de ASP.NET Core (RPS, duración, status codes).
- HttpClient instrumentation (dependency calls).

Solo añadimos el meter de aplicación (`AppMeter.MeterName`) y los metrics
de runtime (GC, threads, working set).

## Despliegue por Portal de Azure

> Pasos canónicos. Si prefieres escenificar todo por terminal, salta a
> [`Despliegue alternativo con scripts az`](#despliegue-alternativo-con-scripts-az).

### Paso 1 — Resource Group + plan **Standard** S1 + Web App

`Portal → Resource groups → Create` → `rg-curso-m02-s25`.

Plan Linux S1, web app .NET 10 (igual que en submódulos anteriores).

### Paso 2 — Log Analytics workspace

`Portal → Log Analytics workspaces → Create`:

| Campo | Valor |
| --- | --- |
| Name | `law-curso-m02-s25-<iniciales>` |
| Region | igual que el RG |
| Pricing tier | Pay-As-You-Go (default) |

### Paso 3 — Application Insights (workspace-based)

`Portal → Application Insights → Create`:

| Campo | Valor |
| --- | --- |
| Name | `ai-curso-m02-s25-<iniciales>` |
| Resource Mode | **Workspace-based** |
| Log Analytics Workspace | el que acabas de crear |

Cuando esté listo, copia su **Connection String** (no la legacy "Instrumentation Key").

### Paso 4 — Conectar la web app

`tu Web App → Configuration → Application settings → New application setting`:

| Name | Value |
| --- | --- |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | `InstrumentationKey=...;IngestionEndpoint=https://...` |

`Save`. La app reinicia y `Program.cs` activa OpenTelemetry porque ahora
detecta la connection string.

### Paso 5 — Action Group (slide 26)

`Portal → Monitor → Alerts → Action groups → Create`:

| Campo | Valor |
| --- | --- |
| Action group name | `ag-curso-m02-s25` |
| Display name | `demo-s25` |
| Notifications → Email | tu correo |

### Paso 6 — Crear alertas (slides 12, 27)

`tu Web App → Monitoring → Alerts → New alert rule`. Tres reglas:

| Nombre | Condición | Severity |
| --- | --- | --- |
| `<app>-http5xx` | `Http5xx total > 5` en 5 min, evaluación cada 1 min | 1 (Critical) |
| `<app>-latencia-alta` | `AverageResponseTime avg > 3000` en 10 min, evaluación cada 5 min | 2 (Warning) |
| `<plan>-cpu-alta` | sobre el **plan**: `CpuPercentage avg > 80` en 15 min | 2 (Warning) |

Action group: el que acabas de crear.

### Paso 7 — Availability test (slide 19)

`tu Application Insights → Availability → Add Standard test`:

| Campo | Valor |
| --- | --- |
| Test name | `health-<app>` |
| URL | `https://<app>.azurewebsites.net/health` |
| Frequency | 5 minutes |
| Test locations | tres regiones (Europa + Américas + Asia) |
| Success criteria | HTTP 200, response < 30 s |

Conecta también este test al action group anterior.

### Paso 8 — Generar tráfico

```bash
URL=https://<app>.azurewebsites.net
for i in $(seq 1 200); do
  curl -s -o /dev/null "$URL/" &
  curl -s -o /dev/null -X POST "$URL/demo/orders" \
    -H "Content-Type: application/json" \
    -d '{"sku":"SKU-A","quantity":2,"unitPrice":12.5}' &
  if (( i % 10 == 0 )); then
    curl -s -o /dev/null "$URL/demo/error?type=500" &
  fi
  wait
done
```

### Paso 9 — Verificar en el Portal

- **Live Metrics** (`Application Insights → Live Metrics`): verás requests/s,
  failures/s, dependencies/s en tiempo real mientras corre el tráfico.
- **Application Map**: aparece automáticamente con la app y sus dependencias.
- **Custom metrics**: `Application Insights → Metrics → Metric Namespace →
  custom → demo.orders.created`.
- **Logs (KQL)**: `Application Insights → Logs`. Pega las queries de
  [`scripts/08-show-kql-queries.sh`](scripts/08-show-kql-queries.sh).

### Paso 10 — Limpieza

`Portal → Resource groups → rg-curso-m02-s25 → Delete`.

## Despliegue alternativo con scripts `az`

```bash
cd scripts
cp .env.demo.example .env.demo
# editar SUBSCRIPTION_ID, APP, LAW, AI, NOTIFY_EMAIL únicos

bash 01-provision.sh
bash 02-deploy.sh
bash 03-configure-app-insights.sh
bash 04-create-action-group.sh
bash 05-create-alerts.sh
bash 06-create-availability-test.sh
bash 07-generate-traffic.sh 5      # 5 minutos de tráfico variado
bash 08-show-kql-queries.sh        # imprime KQL para copiar al portal
bash 09-cleanup.sh                 # cuando termines
```

`bash demo.sh` para el menú interactivo.

## Nota técnica: NU1902 suprimida

`Azure.Monitor.OpenTelemetry.AspNetCore 1.4.0` (la última estable) trae
transitivamente `OpenTelemetry.Api 1.13.x`, que tiene una advisory
[GHSA-g94r-2vxg-569j](https://github.com/advisories/GHSA-g94r-2vxg-569j) sin
patch upstream al cierre de este ejemplo. La regla `TreatWarningsAsErrors=true`
del `Directory.Build.props` raíz convierte la advertencia en error de build,
así que se suprime sólo en este ejemplo con `<NoWarn>NU1902</NoWarn>` en los
csproj. Cuando Microsoft publique la versión parcheada, retirar el supress.

## Cierre del módulo M02

Con S2.5 cierras App Services:

- **S2.1** te enseñó a publicar la primera versión.
- **S2.2** a desplegar sin downtime con slots.
- **S2.3** a escalar bajo demanda.
- **S2.4** a guardar config y secretos de forma segura.
- **S2.5** a ver qué pasa cuando ya está en producción.

El siguiente módulo (M03 — Azure Functions I) cambia de paradigma a serverless
y usa otro tipo de proyecto (Functions Worker), pero los principios de
configuración, monitorización y despliegue son los mismos.
