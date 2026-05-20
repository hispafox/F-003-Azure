# S2.4 — Variables, connection strings y configuración segura

> **Submódulo de referencia:** [M02-S2.4](../../../doc/M02-App-Services/v4-actual/M02-S2.4-variables-conexion-config-segura-v4.md)
> **TFM:** `net10.0` · **Tipo:** Minimal API · **Tier:** Standard S1 (igual que S2.2/S2.3)

> ℹ️ El submódulo está redactado sobre **.NET 8**, código en **.NET 10**.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el mayordomo y el cofre, Key Vault references como salida estándar para secretos, scrubbing por nombre de clave, fingerprints sin filtrar y la disciplina de tres capas (User Secrets / App Settings / Key Vault).

## Objetivo

Construir sobre la API del [submódulo S2.3](../S2.3-escalado-automatico-planes) y
añadir las piezas de configuración segura del submódulo:

- **Options pattern con validación al arrancar** (slides 5, 18, 22) — `AppOptions`
  con `[Required]`, `[Range]`, `[Url]` + un **`AppOptionsValidator`** custom
  (`IValidateOptions<T>`) que rechaza connection strings con `Password=` sin
  `Encrypt=true`, `ApiKey` < 8 chars y referencias `@Microsoft.KeyVault(...)` no
  resueltas.
- **`/config`** que devuelve toda la configuración con **scrubbing por nombre de
  clave** (slide 28). Cualquier clave que contenga `password`, `secret`, `key`,
  `token`, `connectionstring`, `credential` se devuelve como `***REDACTED***`.
- **`/connection`** que extrae solo los campos seguros de la connection string
  (Server, Database, Encrypt) sin filtrar la password (slide 7). Detecta también
  si el valor es una **KV reference no resuelta**.
- **`/features/new-ui`** con `IFeatureManager` (Microsoft.FeatureManagement) —
  cambia el payload según `FeatureManagement:NewUI` (slides 11, 16).
- **`/secrets/api-key/check`** — NUNCA devuelve el secreto, solo metadatos
  verificables: longitud, fingerprint SHA-256 truncado y origen detectado
  (`default-appsettings`, `key-vault-reference-unresolved` o `explicit`).
- **Scripts `az`** que escenifican el ciclo completo: provisión con Key Vault,
  Managed Identity con rol "Key Vault Secrets User", App Settings con
  **`@Microsoft.KeyVault(...)`** references, rotación de secret y export de
  config.

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| Application Settings + jerarquía con `__` | 3, 4, 6 | [`scripts/03-configure-app-settings.sh`](scripts/03-configure-app-settings.sh) |
| Connection Strings | 7 | [`Endpoints/ConfigEndpoints.cs`](src/AppService.Demo.Api/Endpoints/ConfigEndpoints.cs) (`/connection`) + [`Configuration/ConnectionStringInspector.cs`](src/AppService.Demo.Api/Configuration/ConnectionStringInspector.cs) |
| Riesgo de secretos en App Settings | 8 | README "Tour del código" |
| Key Vault References + MI + roles | 9, 25 | [`scripts/04-configure-keyvault.sh`](scripts/04-configure-keyvault.sh) + [`scripts/05-configure-keyvault-references.sh`](scripts/05-configure-keyvault-references.sh) |
| Configuración por entorno | 10 | `appsettings.Development.json` + Slot settings (S2.2) |
| Feature flags simples | 11, 16 | [`Endpoints/FeatureFlagEndpoints.cs`](src/AppService.Demo.Api/Endpoints/FeatureFlagEndpoints.cs) |
| General Settings | 12 | `01-provision.sh` (Always On + HTTPS Only + healthCheckPath) |
| Exportar / importar config | 13 | [`scripts/07-export-config.sh`](scripts/07-export-config.sh) |
| IConfiguration precedencia | 19, 27 | README "Tour del código" |
| User Secrets local | 20 | README → "Ejecución local" |
| IOptions vs IOptionsSnapshot vs IOptionsMonitor | 21 | README "Tour del código" |
| Validación de Options al arrancar | 22 | [`Configuration/AppOptionsValidator.cs`](src/AppService.Demo.Api/Configuration/AppOptionsValidator.cs) |
| Key Vault RBAC vs access policies | 25 | `04-configure-keyvault.sh` (`--enable-rbac-authorization`) |
| Rotación de secretos | 26 | [`scripts/06-rotate-secret.sh`](scripts/06-rotate-secret.sh) |
| Config scrubbing en logs / endpoints | 28 | [`Configuration/ConfigScrubber.cs`](src/AppService.Demo.Api/Configuration/ConfigScrubber.cs) + `/config` + `/info` |

## Estructura

```
S2.4-variables-conexion-config-segura/
├── README.md
├── AppService.Demo.Config.slnx
├── Directory.Build.props
├── global.json
├── .gitattributes
├── src/AppService.Demo.Api/
│   ├── AppService.Demo.Api.csproj         (+ Microsoft.FeatureManagement)
│   ├── Program.cs
│   ├── appsettings.json                    (+ ConnectionStrings + FeatureManagement)
│   ├── appsettings.Development.json
│   ├── Properties/launchSettings.json
│   ├── Configuration/
│   │   ├── AppOptions.cs                   ← + ConnectionString, ApiKey, RequestTimeout, ExternalApiBaseUrl
│   │   ├── AppOptionsValidator.cs          ← NEW (slide 22)
│   │   ├── ConfigScrubber.cs               ← NEW (slide 28)
│   │   └── ConnectionStringInspector.cs    ← NEW (slide 7)
│   ├── Endpoints/
│   │   ├── (todos los anteriores)
│   │   ├── InfoEndpoints.cs                ← actualizado con scrubbed values
│   │   ├── ConfigEndpoints.cs              ← NEW: /config + /connection
│   │   ├── FeatureFlagEndpoints.cs         ← NEW: /features/new-ui
│   │   └── SecretsEndpoints.cs             ← NEW: /secrets/api-key/check
│   └── Services/                            (igual que S2.3)
├── tests/AppService.Demo.Api.Tests/         (41 tests, 41 verdes)
└── scripts/
    ├── .env.demo.example                    ← + KV
    ├── _lib.sh
    ├── 01-provision.sh                      RG + plan + app + KV (RBAC)
    ├── 02-deploy.sh
    ├── 03-configure-app-settings.sh         settings + slot-settings
    ├── 04-configure-keyvault.sh             MI + role + secrets
    ├── 05-configure-keyvault-references.sh  App Settings con @Microsoft.KeyVault
    ├── 06-rotate-secret.sh                  rota ApiKey y verifica refresh
    ├── 07-export-config.sh                  export a JSON
    ├── 08-cleanup.sh
    └── demo.sh                              menú interactivo
```

## Requisitos previos

- .NET SDK 10
- Suscripción de Azure
- Para los scripts: Azure CLI (`az`), `jq`, `openssl` (vienen en Git Bash) y
  permisos para asignar roles RBAC en la suscripción.

## Ejecución local

```bash
dotnet run --project src/AppService.Demo.Api --launch-profile http
# → http://localhost:5080
```

### User Secrets para el desarrollador (slide 20)

En lugar de poner el ApiKey de desarrollo en `appsettings.Development.json`,
úsalo así:

```bash
cd src/AppService.Demo.Api
dotnet user-secrets init     # añade <UserSecretsId> al csproj
dotnet user-secrets set "AppOptions:ApiKey" "mi-clave-de-dev-32-chars-min"
dotnet user-secrets list
```

Los User Secrets viven fuera del repo (`%APPDATA%\Microsoft\UserSecrets\<id>`) y
solo se cargan en `Development`. En Azure los ignora — usará la KV reference.

### Endpoints

| Verbo | Ruta | Notas |
| --- | --- | --- |
| GET | `/config` | toda la config con valores sensibles redactados |
| GET | `/connection` | solo Server/Database/Encrypt; flag `isKeyVaultReferenceLiteral` si la KV ref no se resolvió |
| GET | `/features/new-ui` | payload `v1` o `v2` según `FeatureManagement:NewUI` |
| GET | `/secrets/api-key/check` | metadatos del secreto (longitud, fingerprint, origen) — nunca el valor |
| GET | `/info` | igual que en S2.3 + `connectionString` y `apiKey` ya redactados |

## Tests

```bash
dotnet test
```

41 tests:

- **Unit tests** (no necesitan host):
  - `ConfigScrubberTests` (10): keys sensibles redactadas, no-sensibles intactas, helper `ScrubAll` funciona, valores nulos/vacíos.
  - `ConnectionStringInspectorTests` (3): extrae Server/Database, ignora Password/User, soporta `Data Source`/`Initial Catalog`.
  - `AppOptionsValidatorTests` (4): éxito con baseline válido, fallo por ApiKey corto, fallo por Password sin Encrypt, fallo por KV reference no resuelta.
- **Integration tests** (con `WebApplicationFactory<Program>`):
  - `HealthEndpointTests`, `HealthDetailsTests`, `HelloEndpointTests`,
    `WarmupEndpointTests`, `VersionEndpointTests`, `LoadEndpointTests` (5),
    `StaticEndpointsTests` (2), `CorsConfigurationTests` (2).
  - `InfoEndpointTests`: verifica que `connectionString` y `apiKey` se devuelven `***REDACTED***`.
  - `ConfigEndpointTests` (3): `/config` redacta sensibles, `/connection` muestra Server/Database sin password, `/connection` detecta KV ref literal.
  - `FeatureFlagEndpointTests` (2): payload `v1` con feature OFF, `v2` con feature ON.
  - `SecretsEndpointTests` (1): `/secrets/api-key/check` devuelve metadatos pero NUNCA el valor.

## Tour del código

### `AppOptions` — config tipada con DataAnnotations

```csharp
[Required(AllowEmptyStrings = false)] public string Greeting { get; init; }
[Required] public string ConnectionString { get; init; }
[Required] public string ApiKey { get; init; }
[Range(1, 60)] public int RequestTimeoutSeconds { get; init; }
[Url] public string ExternalApiBaseUrl { get; init; }
```

`Program.cs` registra el binding con `ValidateDataAnnotations()` y
`ValidateOnStart()` — la app **falla al arrancar** si la config es inválida.

### `AppOptionsValidator` — validación cross-field (slide 22)

`IValidateOptions<AppOptions>` con tres reglas que DataAnnotations no puede
expresar:

1. `ApiKey.Length < 8` → fallo.
2. ConnectionString contiene `Password=` pero NO `Encrypt=true` → fallo.
3. `ApiKey` empieza por `@Microsoft.KeyVault` → la KV reference no se resolvió
   (probablemente al MI le falta el rol o el secret no existe). Mensaje claro
   apuntando a la causa.

### `ConfigScrubber` — slide 28

Lista de tokens sensibles (`password`, `secret`, `key`, `token`,
`connectionstring`, `credential`). Cualquier clave que los contenga se redacta.
Funciona como utilidad estática, sin estado:

```csharp
ConfigScrubber.Scrub("AppOptions:ApiKey", "real-value")
// → "***REDACTED***"

ConfigScrubber.Scrub("Greeting", "hola")
// → "hola"
```

### `/connection` — slide 7

Extrae solo los campos "seguros" de la connection string: `Server`,
`Data Source`, `Database`, `Initial Catalog`, `Encrypt`,
`TrustServerCertificate`, `MultipleActiveResultSets`. Password/User se
descartan. Detecta también si el valor literal empieza por `@Microsoft.KeyVault`
(KV ref no resuelta).

### `IOptions` vs `IOptionsSnapshot` vs `IOptionsMonitor` (slide 21)

Este ejemplo usa `IOptions<AppOptions>` deliberadamente: la config se lee al
arrancar y no cambia durante la vida de la app, que es lo que tiene sentido
para connection strings y feature flags básicos. Para una app que necesite
recargar config en runtime (slide 23), se usaría `IOptionsMonitor` con
Azure App Configuration.

### Precedencia de configuración (slides 19, 27)

`Program.cs` no toca el orden por defecto. La precedencia que aplica es:

```
appsettings.json
  ↓ override
appsettings.Development.json   (solo en Development)
  ↓ override
User Secrets                    (solo en Development)
  ↓ override
Environment Variables           (App Settings de App Service llegan aquí)
  ↓ resolución
Key Vault References            (resueltas por App Service via MI)
```

## Despliegue por Portal de Azure

> Pasos canónicos. Si prefieres escenificar todo por terminal, usa los scripts
> de `scripts/` o el menú `bash demo.sh`.

### Paso 1 — Resource Group + plan **Standard** S1

`Portal → Resource groups → Create` → `rg-curso-m02-s24`.

`Portal → App Service plans → Create`: Linux, **Standard S1**, mismo RG.

### Paso 2 — Web App .NET 10

`Portal → App Services → Create → Web App`: Runtime **.NET 10 (LTS)**, Linux.

### Paso 3 — Configuración base

`Configuration → General settings`: Always On `On`, HTTPS Only `On`, Health
check path `/health`.

### Paso 4 — Key Vault con RBAC (slide 25)

`Portal → Key vaults → Create`:

| Campo | Valor |
| --- | --- |
| Name | `kv-curso-m02-s24-<iniciales>` (único globalmente) |
| Region | igual que el RG |
| Pricing tier | Standard |
| Permission model | **Azure role-based access control** |

### Paso 5 — Managed Identity y rol (slide 9)

1. `tu Web App → Identity → System assigned → Status On → Save`.
2. Copia el **Object (principal) ID** que aparece tras guardar.
3. `tu Key Vault → Access control (IAM) → Add → Add role assignment`:
   - Role: **Key Vault Secrets User**
   - Members: pega el principal ID de la web app (busca por ese GUID)
   - `Review + assign`

### Paso 6 — Crear secrets en Key Vault

`tu Key Vault → Secrets → Generate/Import`:

| Name | Value |
| --- | --- |
| `ApiKey` | una cadena ≥ 8 chars (no `local-api-key-...`) |
| `ConnectionString` | `Server=tcp:demo-sql.database.windows.net,1433;Database=demo;User ID=admin;Password=DemoPass1!;Encrypt=true` |

### Paso 7 — App Settings normales

`Configuration → Application settings → New application setting`:

| Name | Value | Slot setting |
| --- | --- | --- |
| `AppOptions__Greeting` | `Hola desde Azure` | ❌ |
| `AppOptions__Version` | `1.0.0` | ❌ |
| `AppOptions__ExternalApiBaseUrl` | `https://api.github.com` | ❌ |
| `AppOptions__RequestTimeoutSeconds` | `30` | ❌ |
| `FeatureManagement__NewUI` | `false` | ❌ |
| `AppOptions__EnvironmentLabel` | `production` | ✅ |
| `AppOptions__DbConnectionLabel` | `prod-db` | ✅ |
| `AppOptions__AppInsightsLabel` | `prod-insights` | ✅ |
| `WEBSITE_RUN_FROM_PACKAGE` | `1` | ✅ |

### Paso 8 — Key Vault references (slide 9)

Mismo panel, dos settings más:

| Name | Value | Slot setting |
| --- | --- | --- |
| `AppOptions__ApiKey` | `@Microsoft.KeyVault(VaultName=<kv>;SecretName=ApiKey)` | ✅ |
| `AppOptions__ConnectionString` | `@Microsoft.KeyVault(VaultName=<kv>;SecretName=ConnectionString)` | ✅ |

`Save`. La app reinicia y resuelve los secretos via MI. En la columna "Source"
deberías ver "Key Vault Reference (Healthy)".

### Paso 9 — Deploy y verificación

VS Code → Deploy to Web App.

```bash
# /config: todas las claves sensibles redactadas
curl https://<app>.azurewebsites.net/config | jq '.["AppOptions:ApiKey"]'
# "***REDACTED***"

# /connection: muestra Server/Database, NO password
curl https://<app>.azurewebsites.net/connection | jq

# /secrets/api-key/check: metadatos del secret real, sin filtrarlo
curl https://<app>.azurewebsites.net/secrets/api-key/check | jq
# {
#   "isPresent": true,
#   "length": 32,
#   "fingerprint": "a4f2c8...",
#   "source": "explicit"
# }

# /features/new-ui: payload v1 o v2 según FeatureManagement:NewUI
curl https://<app>.azurewebsites.net/features/new-ui | jq
```

### Paso 10 — Rotar un secret (slide 26)

`Key Vault → Secrets → ApiKey → New Version`. Pon un valor nuevo. Vuelve a
llamar a `/secrets/api-key/check` — el `fingerprint` cambia (puede tardar
5-10 minutos por la cache, o reinicia la app desde el Portal para forzar
refresh inmediato).

### Paso 11 — Limpieza

`Portal → Resource groups → rg-curso-m02-s24 → Delete`. Si más adelante quieres
recrear el KV con el mismo nombre, hace falta `purge`:
`Portal → Key vaults → Manage deleted vaults → Purge`.

## Despliegue alternativo con scripts `az`

```bash
cd scripts
cp .env.demo.example .env.demo
# editar .env.demo: SUBSCRIPTION_ID, APP único, KV único

bash 01-provision.sh
bash 02-deploy.sh                       # la app aún NO arranca: faltan secrets
bash 03-configure-app-settings.sh
bash 04-configure-keyvault.sh           # MI + rol + secrets en KV
bash 05-configure-keyvault-references.sh # ahora la app SÍ arranca

# Verificar
curl "https://$APP.azurewebsites.net/secrets/api-key/check" | jq
curl "https://$APP.azurewebsites.net/config" | jq

# Rotar
bash 06-rotate-secret.sh

# Limpiar
bash 08-cleanup.sh
```

O `bash demo.sh` para el menú interactivo.

## Siguiente paso

[`S2.5 — Monitorización y diagnóstico`](../../../doc/M02-App-Services/v4-actual/M02-S2.5-monitorizacion-diagnostico-v4.md)
añadirá Application Insights, alertas de Azure Monitor y debugging avanzado a
la app que tienes desplegada.
