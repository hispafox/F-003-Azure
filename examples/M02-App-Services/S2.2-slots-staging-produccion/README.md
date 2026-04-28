# S2.2 — Slots de despliegue: staging y producción

> **Submódulo de referencia:** [M02-S2.2](../../../doc/M02-App-Services/v4-actual/M02-S2.2-slots-staging-produccion-v4.md)
> **TFM:** `net10.0` · **Tipo:** Minimal API · **Tier mínimo en Azure:** Standard S1 (slide 4)

> ℹ️ El submódulo está redactado sobre **.NET 8**, pero el código está en **.NET 10**
> (LTS, noviembre 2025). Las APIs no han cambiado.

## Objetivo

Construir sobre la API del [submódulo S2.1](../S2.1-creacion-config-publicacion) y
añadir lo específico de slots:

- Distinguir settings que **viajan con el código** de las **sticky** (slot settings).
- Endpoint `/warmup` para que App Service caliente el slot antes del swap.
- Endpoint `/version` para verificar visualmente el resultado del swap.
- `/info` ampliado: ahora reporta `slotName` y separa `travelsWithCode` de
  `stickyToSlot` para que se vea el contraste tras un swap.
- Scripts `az` opcionales (`scripts/`) que automatizan toda la demo: provisión,
  configuración, despliegues, swap simple y multi-fase, traffic routing,
  IP restrictions y limpieza.

> Los pasos del Portal siguen siendo la **referencia canónica** del README. Los
> scripts `az` están pensados para escenificar la demo en clase (`bash demo.sh`)
> — no para sustituir la sección de Portal.

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| Tier mínimo Standard S1 para slots | 4 | [`scripts/01-provision.sh`](scripts/01-provision.sh) crea plan S1 |
| Crear slot `staging` | 5 | `01-provision.sh`, README → "Despliegue por Portal" |
| Deploy a slot | 6 | [`scripts/03-deploy.sh staging`](scripts/03-deploy.sh) |
| Settings normales vs sticky | 7, 8, 9 | [`Configuration/AppOptions.cs`](src/AppService.Demo.Api/Configuration/AppOptions.cs) + [`scripts/02-configure-settings.sh`](scripts/02-configure-settings.sh) |
| Cómo funciona el swap | 10 | README → "Verificación del swap" |
| Ejecutar swap | 11 | [`scripts/04-swap.sh`](scripts/04-swap.sh) |
| Multi-phase swap (preview / complete / reset) | 12 | [`scripts/05-swap-with-preview.sh`](scripts/05-swap-with-preview.sh) |
| Rollback (swap inverso) | 13 | `04-swap.sh` aplicado dos veces |
| Traffic routing / canary | 14 | [`scripts/06-traffic-routing.sh`](scripts/06-traffic-routing.sh) |
| Warmup personalizado | 16, 29 | [`Endpoints/WarmupEndpoints.cs`](src/AppService.Demo.Api/Endpoints/WarmupEndpoints.cs) + `WEBSITE_SWAP_WARMUP_PING_PATH=/warmup` en `02-configure-settings.sh` |
| Proteger staging | 17 | [`scripts/07-protect-staging.sh`](scripts/07-protect-staging.sh) |

## Estructura

```
S2.2-slots-staging-produccion/
├── README.md
├── AppService.Demo.Slots.slnx
├── Directory.Build.props
├── global.json
├── .gitattributes
├── src/AppService.Demo.Api/
│   ├── AppService.Demo.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Properties/launchSettings.json
│   ├── Configuration/AppOptions.cs        ← Version + EnvironmentLabel + DbConnectionLabel + AppInsightsLabel
│   ├── Endpoints/
│   │   ├── HelloEndpoints.cs              GET /
│   │   ├── HealthEndpoints.cs             GET /health
│   │   ├── InfoEndpoints.cs               GET /info  (slotName + travelsWithCode/stickyToSlot)
│   │   ├── WarmupEndpoints.cs             GET /warmup (pre-swap)
│   │   └── VersionEndpoints.cs            GET /version (verifica swap visualmente)
│   └── Services/
│       ├── ConfigurableHealthCheck.cs
│       ├── ExternalApiClient.cs
│       └── DependencyChecks.cs            ← simula checks de DB/cache/etc.
├── tests/AppService.Demo.Api.Tests/       (8 tests, 8 verdes)
└── scripts/
    ├── .env.demo.example                  ← copiar a .env.demo
    ├── _lib.sh
    ├── 01-provision.sh                    plan S1 + web app + slot staging
    ├── 02-configure-settings.sh           settings + slot-settings (sticky)
    ├── 03-deploy.sh production|staging
    ├── 04-swap.sh                         swap con confirmación
    ├── 05-swap-with-preview.sh preview|complete|reset
    ├── 06-traffic-routing.sh <%>
    ├── 07-protect-staging.sh <ip>|open
    ├── 08-cleanup.sh                      borra el RG entero
    └── demo.sh                            menú interactivo para clase
```

## Requisitos previos

- .NET SDK 10
- Suscripción de Azure
- VS Code con la extensión **Azure App Service** _o_ Visual Studio 2022+
- Para los scripts: **Azure CLI** (`az`) en `bash` (Git Bash o WSL en Windows)

## Ejecución local

```bash
dotnet run --project src/AppService.Demo.Api --launch-profile http
# → http://localhost:5080
```

Endpoints:

| Verbo | Ruta | Qué devuelve |
| --- | --- | --- |
| GET | `/` | Saludo + `machineName` + `instanceId` |
| GET | `/health` | `Healthy` / `Unhealthy` |
| GET | `/info` | runtime + variables `WEBSITE_*` + `slotName` + `travelsWithCode` + `stickyToSlot` |
| GET | `/warmup` | `200 warm` (y la lista de checks) o `503 cold` |
| GET | `/version` | `version`, `slotName`, `environmentLabel` |

> Local todo es `slotName=local`. La diferencia se ve cuando despliegas a Azure
> con dos slots y comparas las URLs `…/info` y `…-staging/info`.

## Tests

```bash
dotnet test
```

8 tests:

- `HealthEndpointTests` (2): healthy por defecto, unhealthy con toggle.
- `HelloEndpointTests` (1): saludo + machineName + instanceId.
- `InfoEndpointTests` (1): ahora valida `slotName`, `travelsWithCode`, `stickyToSlot`.
- `VersionEndpointTests` (1): version inyectada vía configuración + slotName.
- `WarmupEndpointTests` (1): `/warmup` devuelve 200 con la lista de checks.
- `CorsConfigurationTests` (2): preflight permitido / denegado.

## Tour del código

### Settings: lo que viaja vs lo sticky ([`AppOptions.cs`](src/AppService.Demo.Api/Configuration/AppOptions.cs))

```csharp
// VIAJA con el código (no sticky en Azure)
public string Version { get; init; }              // → /version
public string Greeting { get; init; }             // saludo de la app
public string[] AllowedOrigins { get; init; }     // CORS

// STICKY (configurar como Slot setting en Azure)
public string EnvironmentLabel { get; init; }     // production / staging
public string DbConnectionLabel { get; init; }    // prod-db / staging-db
public string AppInsightsLabel { get; init; }     // prod-insights / staging-insights
```

Tras un swap:

- `Version` cambia (nueva versión sirviendo en producción) ✓
- `EnvironmentLabel` permanece `production` ✓ (era sticky)
- `DbConnectionLabel` permanece `prod-db` ✓

### `/warmup` ([`WarmupEndpoints.cs`](src/AppService.Demo.Api/Endpoints/WarmupEndpoints.cs))

Cuando configuras `WEBSITE_SWAP_WARMUP_PING_PATH=/warmup` y
`WEBSITE_SWAP_WARMUP_PING_STATUSES=200`, App Service llama a este endpoint **antes
de redirigir tráfico** durante el swap. Si responde 503, **aborta el swap**.

`DependencyChecks` aquí es un placeholder que siempre devuelve OK; en una app real,
es donde haces ping a Cosmos / Service Bus / Redis y precalentas conexiones pooled.

### `/version` ([`VersionEndpoints.cs`](src/AppService.Demo.Api/Endpoints/VersionEndpoints.cs))

Devuelve la `Version` (no sticky), el `slotName` (de `WEBSITE_SLOT_NAME`) y el
`environmentLabel` (sticky). En clase, el contraste entre los tres antes/después
de un swap es lo que hace que se entienda la diferencia.

## Despliegue por Portal de Azure

Pasos canónicos (cumplen la regla "Azure = Portal" del proyecto). Si prefieres
escenificar todo por terminal, salta a la sección [`Despliegue alternativo con
scripts az`](#despliegue-alternativo-con-scripts-az).

### Paso 1 — Resource Group y plan **Standard** S1

`Portal → Resource groups → Create` → `rg-curso-m02-s22`.

`Portal → App Service plans → Create`:

| Campo | Valor |
| --- | --- |
| Name | `plan-curso-m02-s22` |
| OS | Linux |
| SKU | **Standard S1** (los slots requieren Standard+ — slide 4) |

### Paso 2 — Web App .NET 10

`Portal → App Services → Create → Web App`. Runtime stack **.NET 10 (LTS)**, plan
`plan-curso-m02-s22`.

### Paso 3 — Crear slot `staging`

`Portal → tu Web App → Deployment slots → Add slot`:

| Campo | Valor |
| --- | --- |
| Name | `staging` |
| Clone settings from | tu web app principal |

URL resultante: `https://<app>-staging.azurewebsites.net`.

### Paso 4 — Application settings y **Slot settings**

`Configuration → Application settings`. Para cada uno marca o no la columna
**"Deployment slot setting"**:

#### Slot principal (producción)

| Name | Value | Slot setting |
| --- | --- | --- |
| `AppOptions__Greeting` | `Hola desde producción` | ❌ |
| `AppOptions__Version` | `1.0.0` | ❌ |
| `AppOptions__EnvironmentLabel` | `production` | ✅ |
| `AppOptions__DbConnectionLabel` | `prod-db` | ✅ |
| `AppOptions__AppInsightsLabel` | `prod-insights` | ✅ |
| `WEBSITE_RUN_FROM_PACKAGE` | `1` | ✅ |
| `WEBSITE_SWAP_WARMUP_PING_PATH` | `/warmup` | ✅ |
| `WEBSITE_SWAP_WARMUP_PING_STATUSES` | `200` | ✅ |

#### Slot `staging`

| Name | Value | Slot setting |
| --- | --- | --- |
| `AppOptions__Greeting` | `Hola desde staging` | ❌ |
| `AppOptions__Version` | `1.1.0` | ❌ (versión nueva que vais a probar) |
| `AppOptions__EnvironmentLabel` | `staging` | ✅ |
| `AppOptions__DbConnectionLabel` | `staging-db` | ✅ |
| `AppOptions__AppInsightsLabel` | `staging-insights` | ✅ |
| `WEBSITE_RUN_FROM_PACKAGE` | `1` | ✅ |

#### Configuration → General settings (en **ambos** slots)

| Toggle | Valor | Slide |
| --- | --- | --- |
| Always On | On | 13 (S2.1) |
| HTTPS Only | On | 21 (S2.1) |
| Health check path | `/health` | 13 (S2.1) |

### Paso 5 — Deploy

1. **A producción** primero (versión 1.0.0): VS Code → Deploy to Web App… →
   selecciona el slot **principal**.
2. **A staging** después (versión 1.1.0): VS Code → Deploy to Web App… →
   selecciona el slot **staging**.

> Para que `Version` sea distinta entre slots aunque el código sea idéntico,
> basta con cambiar `AppOptions__Version` en App Settings de cada slot.

### Paso 6 — Verificación del swap (el momento de la verdad)

```bash
# antes del swap
curl https://<app>.azurewebsites.net/version
# { "version": "1.0.0", "slotName": "Production", "environmentLabel": "production" }

curl https://<app>-staging.azurewebsites.net/version
# { "version": "1.1.0", "slotName": "staging", "environmentLabel": "staging" }
```

Hacer swap: `Portal → tu Web App → Deployment slots → Swap`. Source: `staging`,
Target: `production`. Click **Swap**.

```bash
# después del swap
curl https://<app>.azurewebsites.net/version
# { "version": "1.1.0", "slotName": "Production", "environmentLabel": "production" }
#                                                  ↑ sigue siendo production
#               ↑ la versión nueva ya está sirviendo en prod, ZERO downtime

curl https://<app>-staging.azurewebsites.net/version
# { "version": "1.0.0", "slotName": "staging", "environmentLabel": "staging" }
#               ↑ la versión vieja queda en staging — perfecto para rollback
```

Si la nueva versión falla → vuelve a `Deployment slots → Swap`. Mismo botón,
otra vez. La versión vieja vuelve a producción en segundos.

### Paso 7 — Otras pruebas

- **Multi-phase swap** (slide 12): `Deployment slots → Swap → Perform swap with
  preview`. Aplica la config de producción a staging sin redirigir tráfico.
  Cuando estés satisfecho, `Complete swap`. Si algo falla, `Cancel swap`.
- **Traffic routing / canary** (slide 14): `Deployment slots → Slots traffic` →
  pon `staging = 10%`. Una de cada 10 peticiones a la URL principal va al slot
  staging. Para forzar siempre el principal: añade `?x-ms-routing-name=self`.
- **Proteger staging** (slide 17): `staging slot → Networking → Access
  restrictions`. Añade tu IP en Allow y cambia el default action a Deny.

### Paso 8 — Limpieza

`Portal → Resource groups → rg-curso-m02-s22 → Delete`.

## Despliegue alternativo con scripts `az`

> Sigue la misma secuencia que el Portal. Pensado para clase: `bash demo.sh` y
> vas eligiendo opciones. Útil también para reproducir la demo varias veces.

```bash
cd scripts
cp .env.demo.example .env.demo
# editar .env.demo con tu SUBSCRIPTION_ID y APP único

bash 01-provision.sh
bash 02-configure-settings.sh
bash 03-deploy.sh production    # versión 1.0.0
# editar AppOptions__Version a 1.1.0 en .env / appsettings y republish
bash 03-deploy.sh staging
bash 04-swap.sh                 # swap directo
# o el flujo seguro:
bash 05-swap-with-preview.sh preview
bash 05-swap-with-preview.sh complete
# canary:
bash 06-traffic-routing.sh 10
# proteger staging:
bash 07-protect-staging.sh 203.0.113.50/32
# limpieza:
bash 08-cleanup.sh
```

O directamente el menú interactivo:

```bash
bash scripts/demo.sh
```

## Siguiente paso

[`S2.3 — Escalado automático`](../../../doc/M02-App-Services/v4-actual/M02-S2.3-escalado-automatico-planes-v4.md)
añade reglas de autoscale al plan S1 que ya tienes desplegado, y enseña a medir
el impacto sin romper la app de producción ni el slot staging.
