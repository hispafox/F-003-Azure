# S2.3 — Escalado automático y planes de servicio

> **Submódulo de referencia:** [M02-S2.3](../../../doc/M02-App-Services/v4-actual/M02-S2.3-escalado-automatico-planes-v4.md)
> **TFM:** `net10.0` · **Tipo:** Minimal API · **Tier mínimo en Azure:** Standard S1 (slide 5)

> ℹ️ El submódulo está redactado sobre **.NET 8**, pero el código está en **.NET 10**.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el restaurante con camareros que entran y salen, scale up vs scale out, la disciplina de configurar autoscale antes del pico y el detalle del `ShutdownTimeout=30s` que evita 502s en scale-in.

## Objetivo

Construir sobre la API del [submódulo S2.2](../S2.2-slots-staging-produccion) y
añadir las piezas necesarias para escenificar **scale up**, **scale out manual**
y **autoscale por métricas** en clase:

- `/load/cpu?ms=N` — endpoint que quema CPU real (busca primos en bucle).
  Bombardeándolo, la métrica `CpuPercentage` del plan supera el umbral del
  autoscale y Azure añade instancias.
- `/api/products?limit=N` y `/api/categorias` — respuestas idempotentes con
  `Cache-Control` para que un CDN o Azure Front Door cacheen en edge (slide 25).
  También sirven como `applicationInitialization` para precalentar conexiones
  durante el warmup (slide 29).
- `/health/details` — health check con response writer JSON detallado (slide 21).
  El `/health` simple se mantiene como endpoint que App Service consulta.
- **Graceful shutdown** de 30 s en `Program.cs` (slide 22) para que el scale-in
  no mate requests en vuelo.
- **Scripts `az`** que automatizan la demo: provision, deploy, scale-up,
  scale-out manual, autoscale por CPU, perfil horario, generador de carga y
  vigilancia de instancias en directo.

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| Scale up (vertical, cambiar SKU) | 3 | [`scripts/03-scale-up.sh`](scripts/03-scale-up.sh) |
| Scale out manual (N instancias) | 4 | [`scripts/04-scale-out-manual.sh`](scripts/04-scale-out-manual.sh) |
| Autoscale por métrica (CPU) | 5, 6, 7 | [`scripts/05-autoscale-cpu.sh`](scripts/05-autoscale-cpu.sh) |
| Autoscale por horario | 8, 23 | [`scripts/06-autoscale-schedule.sh`](scripts/06-autoscale-schedule.sh) |
| Load balancer + visualizar instancias | 4, 10 | [`Endpoints/HelloEndpoints.cs`](src/AppService.Demo.Api/Endpoints/HelloEndpoints.cs) + [`scripts/08-watch-instances.sh`](scripts/08-watch-instances.sh) |
| Health check enriquecido (JSON) | 21 | [`Endpoints/HealthEndpoints.cs`](src/AppService.Demo.Api/Endpoints/HealthEndpoints.cs) (`/health/details`) |
| Scale-in protection / graceful shutdown | 22 | [`Program.cs`](src/AppService.Demo.Api/Program.cs) → `ConfigureHostOptions` |
| Cache headers (CDN / Front Door) | 25 | [`Endpoints/StaticEndpoints.cs`](src/AppService.Demo.Api/Endpoints/StaticEndpoints.cs) |
| Multi-rule autoscale | 28 | El README explica cómo añadir reglas a `05-autoscale-cpu.sh` |
| Warmup en scale-out | 29 | `WEBSITE_WARMUP_PATH=/health` configurado en `01-provision.sh`; `/api/products?limit=1` documentado en el README como `initializationPages` |
| Generador de carga para demo | — | [`Services/CpuLoadGenerator.cs`](src/AppService.Demo.Api/Services/CpuLoadGenerator.cs) + [`scripts/07-load-test.sh`](scripts/07-load-test.sh) |

## Estructura

```
S2.3-escalado-automatico-planes/
├── README.md
├── AppService.Demo.Scale.slnx
├── Directory.Build.props
├── global.json
├── .gitattributes
├── src/AppService.Demo.Api/
│   ├── AppService.Demo.Api.csproj
│   ├── Program.cs                       graceful shutdown + DI nuevos
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Properties/launchSettings.json
│   ├── Configuration/AppOptions.cs      (igual que S2.2)
│   ├── Endpoints/
│   │   ├── HelloEndpoints.cs            GET /
│   │   ├── HealthEndpoints.cs           GET /health  +  /health/details JSON
│   │   ├── InfoEndpoints.cs             GET /info  (slotName + sticky/travels)
│   │   ├── WarmupEndpoints.cs           GET /warmup
│   │   ├── VersionEndpoints.cs          GET /version
│   │   ├── LoadEndpoints.cs             GET /load/cpu?ms=N      ← NUEVO
│   │   └── StaticEndpoints.cs           GET /api/products + /api/categorias  ← NUEVO
│   └── Services/
│       ├── ConfigurableHealthCheck.cs
│       ├── ExternalApiClient.cs
│       ├── DependencyChecks.cs
│       └── CpuLoadGenerator.cs          ← NUEVO
├── tests/AppService.Demo.Api.Tests/     (16 tests, 16 verdes)
└── scripts/
    ├── .env.demo.example
    ├── _lib.sh
    ├── 01-provision.sh                  RG + plan S1 + web app
    ├── 02-deploy.sh                     publish + zip + deploy
    ├── 03-scale-up.sh <SKU>             cambiar SKU del plan
    ├── 04-scale-out-manual.sh <N>       fijar N instancias
    ├── 05-autoscale-cpu.sh              regla 1-5, 30-70%
    ├── 06-autoscale-schedule.sh         perfil L-V 09:00-19:00
    ├── 07-load-test.sh [min] [par] [ms] generador de carga
    ├── 08-watch-instances.sh            polling de /info en bucle
    ├── 09-cleanup.sh
    └── demo.sh                          menú interactivo para clase
```

## Requisitos previos

- .NET SDK 10
- Suscripción de Azure
- Para los scripts: Azure CLI (`az`) en `bash`. `curl` y `zip` también.

## Ejecución local

```bash
dotnet run --project src/AppService.Demo.Api --launch-profile http
# → http://localhost:5080
```

Endpoints disponibles:

| Verbo | Ruta | Notas |
| --- | --- | --- |
| GET | `/` | hello + machineName + instanceId |
| GET | `/health` | `Healthy` (200) o `Unhealthy` (503) |
| GET | `/health/details` | JSON detallado con `status`, `checks`, `totalDurationMs` |
| GET | `/info` | runtime + slotName + travelsWithCode + stickyToSlot |
| GET | `/warmup` | dependency checks (200/503) |
| GET | `/version` | versión + slotName + environmentLabel |
| GET | `/load/cpu?ms=N` | quema CPU N ms (1–60 000), 400 fuera de rango |
| GET | `/api/products?limit=N` | array de productos, `Cache-Control: public, max-age=60` |
| GET | `/api/categorias` | array de categorías, `Cache-Control: public, max-age=3600` |

## Tests

```bash
dotnet test
```

16 tests:

- `HealthEndpointTests` (2): healthy/unhealthy.
- `HealthDetailsTests` (1): `/health/details` devuelve JSON con `status` y `checks`.
- `HelloEndpointTests` (1).
- `InfoEndpointTests` (1).
- `VersionEndpointTests` (1).
- `WarmupEndpointTests` (1).
- `CorsConfigurationTests` (2).
- `LoadEndpointTests` (5): happy path + `[Theory]` con 4 valores fuera de rango → 400.
- `StaticEndpointsTests` (2): `/api/products` con `max-age=60`, `/api/categorias` con `max-age=3600`.

## Tour del código

### `CpuLoadGenerator` ([código](src/AppService.Demo.Api/Services/CpuLoadGenerator.cs))

Busca primos en bucle hasta agotar el tiempo solicitado. Crucial usar **CPU
real**, no `Thread.Sleep`: solo así sube la métrica `CpuPercentage` del plan, que
es la que dispara el autoscale.

### `/load/cpu` ([código](src/AppService.Demo.Api/Endpoints/LoadEndpoints.cs))

```
GET /load/cpu?ms=2000
→ 200 OK { "generatedMs": 2000, "primesFound": 12345, "instanceId": "..." }
```

Validación: `1 ≤ ms ≤ 60000`, fuera devuelve 400 (evita que un cliente despistado
te tire la instancia 24 h).

### `/api/products`, `/api/categorias` ([código](src/AppService.Demo.Api/Endpoints/StaticEndpoints.cs))

Setean `Cache-Control` distinto según la "frescura" de los datos:

| Endpoint | `Cache-Control` | Caso de uso |
| --- | --- | --- |
| `/api/products?limit=N` | `public, max-age=60` | datos que cambian poco |
| `/api/categorias` | `public, max-age=3600` | datos casi inmutables |

Útil cuando hay un Front Door o un CDN delante (slide 25).

### `/health/details` ([código](src/AppService.Demo.Api/Endpoints/HealthEndpoints.cs))

`MapHealthChecks` con un `ResponseWriter` que serializa el `HealthReport`
completo a JSON: cada check (`name`, `status`, `description`, `durationMs`),
`totalDurationMs` y status global. Pensado para dashboards y observabilidad,
no para que App Service lo pinche cada 30 s.

### Graceful shutdown ([Program.cs](src/AppService.Demo.Api/Program.cs))

```csharp
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

Cuando el autoscale hace **scale-in** (quitar una instancia), el host deja de
aceptar nuevas peticiones y **espera hasta 30 s** a que las en vuelo terminen
antes de matar el proceso. Sin esto, el scale-in genera errores 502 visibles
para los usuarios.

## Despliegue por Portal de Azure

> Pasos canónicos. Si prefieres escenificar todo por terminal, salta a
> [`Despliegue alternativo con scripts az`](#despliegue-alternativo-con-scripts-az).

### Paso 1 — Resource Group y plan **Standard** S1

`Portal → Resource groups → Create` → `rg-curso-m02-s23`.

`Portal → App Service plans → Create`:

| Campo | Valor |
| --- | --- |
| Name | `plan-curso-m02-s23` |
| OS | Linux |
| SKU | **Standard S1** (autoscale requiere Standard+ — slide 5) |

### Paso 2 — Web App .NET 10

`Portal → App Services → Create → Web App`:

| Campo | Valor |
| --- | --- |
| Runtime stack | **.NET 10 (LTS)** |
| OS | Linux |
| Plan | `plan-curso-m02-s23` |

### Paso 3 — Configuración base

`Configuration → General settings`:

| Toggle | Valor |
| --- | --- |
| Always On | On |
| HTTPS Only | On |
| Health check path | `/health` |

`Configuration → Application settings`:

| Name | Value |
| --- | --- |
| `WEBSITE_RUN_FROM_PACKAGE` | `1` |
| `WEBSITE_WARMUP_PATH` | `/health` |

### Paso 4 — Deploy

VS Code → Azure → App Services → tu app → **Deploy to Web App…** apuntando a
`src/AppService.Demo.Api`.

Verifica:
```bash
curl https://<app>.azurewebsites.net/health
curl https://<app>.azurewebsites.net/health/details | jq
curl 'https://<app>.azurewebsites.net/load/cpu?ms=500'
```

### Paso 5 — Scale up (slide 3)

`tu Web App → Scale up (App Service plan)` → eligir SKU. **Zero downtime,
instantáneo**. Útil para subir a P1v3 cuando necesitas más RAM o zone redundancy.

### Paso 6 — Scale out manual (slide 4)

`tu Web App → Scale out (App Service plan) → Manual scale → Instance count → 3`.

Espera 1-2 minutos y comprueba con `curl /info` repetidamente: `instanceId`
empieza a rotar entre las tres instancias.

### Paso 7 — Autoscale por CPU (slides 5, 6, 7)

`tu Web App → Scale out (App Service plan) → Custom autoscale`:

- Default profile:
  - `Minimum 1, Maximum 5, Default 1`
  - Rule 1: `When CpuPercentage > 70 average over 5 minutes → Scale out 1 (cooldown 5 min)`
  - Rule 2: `When CpuPercentage < 30 average over 10 minutes → Scale in 1 (cooldown 10 min)`

`Save`.

### Paso 8 — Disparar el autoscale

```bash
# Bombardea /load/cpu durante 7 minutos (10 requests en paralelo de 2 segundos cada una)
for i in $(seq 1 1000); do
  for _ in $(seq 1 10); do
    curl -s -o /dev/null 'https://<app>.azurewebsites.net/load/cpu?ms=2000' &
  done
  wait
done
```

Mientras corre, abre `Portal → tu plan → Metrics → CpuPercentage` y `tu Web App
→ Scale out → Run history`: a los ~5-7 minutos verás cómo Azure añade instancias.

### Paso 9 — Perfil horario (slides 8, 23)

`Custom autoscale → Add a scale condition`. Recurrence Mon-Fri 09:00-19:00,
min 2 / max 8 / default 3.

### Paso 10 — Limpieza

`Portal → Resource groups → rg-curso-m02-s23 → Delete`.

## Despliegue alternativo con scripts `az`

```bash
cd scripts
cp .env.demo.example .env.demo
# editar .env.demo con tu SUBSCRIPTION_ID y APP único

bash 01-provision.sh
bash 02-deploy.sh
bash 05-autoscale-cpu.sh
bash 06-autoscale-schedule.sh   # opcional

# en una terminal aparte:
bash 08-watch-instances.sh

# en la terminal principal:
bash 07-load-test.sh 7 10 2000  # 7 min · 10 paralelos · 2000 ms por request
```

Mientras corre el load test, `08-watch-instances.sh` mostrará cómo cambia
`instanceId` cuando Azure añade instancias.

`bash demo.sh` para el menú interactivo.

## Siguiente paso

[`S2.4 — Variables de conexión y configuración segura`](../../../doc/M02-App-Services/v4-actual/M02-S2.4-variables-conexion-config-segura-v4.md)
añadirá Key Vault references a las settings y demostrará cómo proteger las
connection strings.
