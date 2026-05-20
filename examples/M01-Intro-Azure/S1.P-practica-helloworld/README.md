# S1.P — Práctica: Hello World desde VS Code a Azure

> **Práctica de referencia:** [M01-S1.P](../../../doc/M01-Intro-Azure/v5-actual/M01-S1.P-practica-helloworld-v5.md)
> **Tipo:** primera práctica del curso · **Duración estimada:** 60-75 min
> **TFM:** `net10.0` · **Tier en Azure:** F1 (gratuito)

> ℹ️ La práctica está redactada sobre **.NET 8** (`dotnet:8` en runtime). Aquí
> usamos **.NET 10 LTS** (`DOTNETCORE:10.0`) siguiendo la convención del repo.

> 📘 **¿Primera vez con esta práctica?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: por qué esta es la primera práctica del curso, el modelo mental Suscripción → RG → Plan → App, y la idea de App Settings sin redesplegar.

## Qué vas a hacer

Esta es la **primera práctica del curso** y cubre el ciclo completo end-to-end:

1. Provisionar Resource Group + App Service Plan **F1 (gratis)** + Web App.
2. Desplegar una API minimal de un solo `Program.cs` a Azure.
3. Verificar la URL pública con sus 5 endpoints.
4. Configurar App Settings sin redesplegar.
5. (Opcional) Conectar Application Insights.
6. (Opcional) Aplicar security defaults (HTTPS only, TLS 1.2, FTPS Disabled).
7. Limpiar (o conservar el RG para reutilizarlo en M02-S2.P).

> 🔁 **Importante**: el RG y la web app que crees aquí los **reutilizarás en
> M02-S2.P** (la práctica de slots y swap). Cuando termines, en el cleanup
> elige la opción "borrar solo web app y plan" si vas a continuar con M02.

## Mapeo a slides

| Concepto | Slides | Dónde |
| --- | --- | --- |
| Pre-flight (CLI, login, suscripción) | 3, 6, 8 | README → "Antes de empezar" |
| Convenciones de naming | 4, 18 | [`scripts/.env.demo.example`](scripts/.env.demo.example) |
| Mental model: RG > Plan > App | 5 | README → "Conceptos antes de empezar" |
| Crear Resource Group | 14 | [`scripts/01-provision.sh`](scripts/01-provision.sh) |
| Crear App Service Plan F1 | 22 | mismo script |
| Crear Web App .NET | 23 | mismo script |
| Anatomía del proyecto generado | 27, 28, 29 | [`src/hello-world/`](src/hello-world/) |
| Endpoint raíz con campos diagnósticos | 27 | [`Program.cs`](src/hello-world/Program.cs) |
| Endpoint /health | 50 | [`Program.cs`](src/hello-world/Program.cs) |
| Build → publish → deploy | 33, 46 | [`scripts/02-deploy.sh`](scripts/02-deploy.sh) |
| Verificación end-to-end | 49, 51, 60 | [`scripts/04-smoke-test.sh`](scripts/04-smoke-test.sh) |
| Log streaming | 52 | `demo.sh` opción 7 |
| Application Insights workspace-based | 55-58 | [`scripts/05-setup-app-insights.sh`](scripts/05-setup-app-insights.sh) |
| Security defaults | 59 | [`scripts/06-secure-defaults.sh`](scripts/06-secure-defaults.sh) |
| Reto 1: App Settings vía env vars | 69 | endpoint `/api/info` + [`scripts/03-app-settings.sh`](scripts/03-app-settings.sh) |
| Reto 2: /api/echo con validación | 70 | endpoint `/api/echo` |
| Reto 3: /api/version con Assembly | 71 | endpoint `/api/version` |
| Cleanup | 82 | [`scripts/07-cleanup.sh`](scripts/07-cleanup.sh) |

## Estructura

```
S1.P-practica-helloworld/
├── README.md
├── HelloWorld.slnx
├── Directory.Build.props
├── global.json
├── .gitattributes
├── src/hello-world/
│   ├── hello-world.csproj
│   ├── Program.cs                       /, /health, /api/info, /api/echo, /api/version
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Properties/launchSettings.json
├── tests/hello-world.Tests/             (10 tests)
└── scripts/
    ├── .env.demo.example
    ├── _lib.sh
    ├── 01-provision.sh                  RG + plan F1 + web app
    ├── 02-deploy.sh                     publish + zip + zip deploy
    ├── 03-app-settings.sh               Asistente + CURSO_*
    ├── 04-smoke-test.sh                 verifica los 5 endpoints
    ├── 05-setup-app-insights.sh         (opcional) Log Analytics + AI
    ├── 06-secure-defaults.sh            (opcional) HTTPS only + TLS 1.2 + FTPS Disabled
    ├── 07-cleanup.sh                    borra RG completo o solo app+plan
    └── demo.sh                          menú interactivo
```

## Antes de empezar (slide 3)

```bash
# .NET 10 SDK
dotnet --list-sdks                 # debe mostrar 10.x

# Azure CLI >= 2.65
az --version

# Login activo en la suscripción correcta
az account show --output table
```

Errores típicos:
- `No subscriptions found` → `az login --tenant <tenant-id>`
- `Forbidden` al crear → pedir Contributor sobre la suscripción

## Conceptos antes de empezar (slide 5)

```
Suscripción Azure (ya existe)
        │
        ▼
Resource Group "rg-curso-azure-<tu-nombre>"   (contenedor lógico, no cuesta)
        ├── App Service Plan F1                (el "hardware", gratis)
        │       └── Web App                     (tu hosting concreto)
        │              └── https://app-curso-<tu-nombre>.azurewebsites.net
        └── (opcional) Log Analytics + AI       (telemetría)
```

Borrar el Resource Group elimina todo lo que contiene. Es la opción "nuclear"
de cleanup.

## Ejecución local

```bash
dotnet run --project src/hello-world --launch-profile http
# → http://localhost:5000
```

Endpoints:

| Verbo | Ruta | Notas |
| --- | --- | --- |
| GET | `/` | JSON con `mensaje`, `asistente`, `entorno`, `servidor`, `hora_utc`, `runtime`, `os` |
| GET | `/health` | `{ status: "healthy" }` |
| GET | `/api/info` | (reto 1) lee `CURSO_MODULO`, `CURSO_SESION`, `CURSO_FECHA` |
| GET | `/api/echo?msg=...` | (reto 2) eco con validación; 400 si falta `msg` |
| GET | `/api/version` | (reto 3) `version`, `assembly`, `framework`, `buildTime` |

En local `entorno=Development`. Tras desplegar a Azure será `Production` —
la prueba más simple de que el deploy funcionó.

## Tests

```bash
dotnet test
```

10 tests:

- `RootEndpointTests` (2): los 7 campos del JSON están presentes; sin `Asistente` configurado, fallback al placeholder.
- `HealthEndpointTests` (1): `/health` responde 200 con `status: healthy`.
- `ApiInfoTests` (2): lee env vars correctamente; defaults cuando no están.
- `ApiEchoTests` (4): happy path + `[Theory]` con 3 valores inválidos (`/api/echo`, `?msg=`, `?msg=%20`) → 400.
- `ApiVersionTests` (1): metadatos del assembly.

## Tour del código

`Program.cs` está intencionalmente plano (sin Options pattern, sin DI custom)
— es la primera práctica y queremos que se lea como en el material lectivo.
La única abstracción es leer `Asistente` y `CURSO_*` desde `IConfiguration`
en lugar de `Environment.GetEnvironmentVariable` (recomendado en .NET
moderno: en local lee de `appsettings.json`, en Azure de App Settings sin
cambiar el código).

Para cada endpoint, el comentario sobre la cabecera apunta al número de
slide del material lectivo donde se explica.

## Práctica paso a paso por Portal

> Pasos canónicos. Si prefieres terminal completo, salta a la siguiente sección.

### Paso 1 — Resource Group (slide 14)

`Portal → Resource groups → Create`:

| Campo | Valor |
| --- | --- |
| Subscription | _la tuya_ |
| Resource group | `rg-curso-azure-<tu-nombre>` |
| Region | `West Europe` (slide 17) |

Tags (slide 20): `curso=AZ-204`, `sesion=M01`, `owner=<tu-email>`.

### Paso 2 — App Service Plan F1 (slide 22)

`Portal → App Service plans → Create`:

| Campo | Valor |
| --- | --- |
| Name | `plan-curso-<tu-nombre>` |
| OS | Linux |
| Pricing tier | **Free F1** (slide 16) |

### Paso 3 — Web App (slide 23)

`Portal → App Services → Create → Web App`:

| Campo | Valor |
| --- | --- |
| Name | `app-curso-<tu-nombre>` (debe ser único globalmente) |
| Runtime stack | **.NET 10 (LTS)** |
| OS | Linux |
| Plan | `plan-curso-<tu-nombre>` |

`Configuration → General settings → Health check path = /health`.

### Paso 4 — Deploy (slide 46)

VS Code → panel Azure → tu suscripción → App Services → tu app → botón derecho
→ **Deploy to Web App…** y selecciona la carpeta `src/hello-world`.

VS Code se encarga de `dotnet publish` + zip + subida a Kudu.

### Paso 5 — Verificar (slide 49)

```bash
curl https://<app>.azurewebsites.net/
```

Deberías ver:
- `entorno: "Production"` (vs `Development` en local)
- `servidor` con un nombre tipo `DW1SDWK0012DF` (no tu PC)
- `runtime: ".NET 10.0.x"`
- `os` con la plataforma de Azure (Linux en este caso)

### Paso 6 — App Settings (slide 69, opcional)

`Configuration → Application settings → New application setting`:

| Name | Value |
| --- | --- |
| `Asistente` | tu nombre |
| `CURSO_MODULO` | `1` |
| `CURSO_SESION` | `Introduccion` |
| `CURSO_FECHA` | la fecha de la práctica |

`Save`. La app reinicia (~30 s) y `/api/info` devuelve esos valores. **Cambiar
App Settings no requiere redesplegar** — es lo que ahorra tiempo en producción.

### Paso 7 — Application Insights (slides 55-58, opcional)

`Portal → Log Analytics workspaces → Create` → `law-curso-<tu-nombre>`.

`Portal → Application Insights → Create`:
- Resource Mode: **Workspace-based**
- Workspace: el que acabas de crear

Copia su Connection String y añádelo como App Setting:
- `APPLICATIONINSIGHTS_CONNECTION_STRING` = `InstrumentationKey=...;IngestionEndpoint=...`
- `ApplicationInsightsAgent_EXTENSION_VERSION` = `~3`

`Save`, espera 2-3 min y abre `Application Insights → Live Metrics`.

### Paso 8 — Security defaults (slide 59, opcional pero recomendado)

`Configuration → General settings`:

| Toggle | Valor |
| --- | --- |
| HTTPS Only | **On** |
| Minimum TLS Version | **1.2** |
| FTP state | **Disabled** |

### Paso 9 — Cleanup (slide 82)

Si **vas a continuar con M02-S2.P** (slots y swap), conserva el RG y borra
solo la web app y el plan:
1. `tu Web App → Delete`.
2. `tu plan → Delete`.

Si terminaste el curso o no vas a continuar, borra todo:
`Portal → Resource groups → rg-curso-azure-<tu-nombre> → Delete`.

## Práctica alternativa con scripts

```bash
cd scripts
cp .env.demo.example .env.demo
# editar .env.demo con tu SUBSCRIPTION_ID, tus iniciales y ASISTENTE

bash 01-provision.sh                 # RG + plan F1 + web app
bash 02-deploy.sh                    # publish + zip + deploy
bash 03-app-settings.sh              # Asistente + CURSO_*
bash 04-smoke-test.sh                # verifica los 5 endpoints

# Opcionales:
bash 05-setup-app-insights.sh        # Log Analytics + AI workspace-based
bash 06-secure-defaults.sh           # HTTPS Only + TLS 1.2 + FTPS Disabled

# Cuando termines:
bash 07-cleanup.sh                   # te pregunta si borrar RG entero o solo app+plan
```

`bash demo.sh` para el menú interactivo.

## Verificación final (slide 60)

| # | Verificación | Cómo |
| --- | --- | --- |
| 1 | App existe | `az webapp show -n $APP -g $RG --query state -o tsv` → `Running` |
| 2 | URL responde 200 | `curl -o /dev/null -w "%{http_code}" https://$APP.azurewebsites.net/` |
| 3 | JSON con tu nombre | `curl -s https://$APP.azurewebsites.net/ \| jq .asistente` |
| 4 | `entorno = "Production"` | en el JSON anterior |
| 5 | `/health` responde 200 | `curl https://$APP.azurewebsites.net/health` |
| 6 | Logs streaming funciona | `az webapp log tail -n $APP -g $RG` |
| 7 | App Settings reflejados | `/api/info` muestra tus valores |
| 8 | Smoke tests pasan | `bash scripts/04-smoke-test.sh` |

## Troubleshooting (slide 61)

| Síntoma | Causa típica | Fix |
| --- | --- | --- |
| `The webapp name is already taken` | Otro alumno ya tiene ese nombre | Sufijo numérico: `app-curso-pedro-2` |
| 503 después del deploy (1-2 min) | Cold start del F1 | Esperar 30-60 s más, refrescar |
| 503 persistente >3 min | App crasheó al arrancar | `az webapp log tail -n $APP -g $RG`, leer la excepción |
| `entorno: "Development"` en Azure | Variable hardcoded o sobrescrita | Comprobar App Settings; Azure setea Production por defecto |
| 403 aleatorios después de mucho uso | F1 superó 60 min CPU/día | Esperar al reset UTC 00:00 o subir a B1 |
| Deploy con `az webapp deploy` cancelado | Timeout | `--timeout 300` o ZIP más pequeño |
| Cambios no aparecen tras deploy | Cache del navegador | `curl` directo (bypass cache) o `Ctrl+Shift+R` |

Si nada funciona, **abre Kudu** (slide 53):
`https://<app>.scm.azurewebsites.net` → Debug Console → `D:\home\site\wwwroot`
para ver qué archivos se desplegaron realmente.

## Hand-off al siguiente módulo

Lo que tienes ahora (slide 26):

- Web app en F1 con tu primer deploy real a Azure.
- App Settings configurables sin redeploy.
- (Opcional) Application Insights conectado.

[`M02-S2.P — Slots y swap`](../../../doc/M02-App-Services/v4-actual/M02-S2.P-practica-slots-swap-v4.md)
**reutiliza estos mismos recursos** y los mejora: subir el plan a S1, crear
slot staging, configurar sticky settings, hacer swap. Por eso el cleanup de
esta práctica te pregunta si quieres conservar el RG.

[`M02-S2.P2 — Deploy básico`](../../M02-App-Services/S2.P2-practica-deploy-basico/README.md)
es la versión "concentrada" de esta práctica (sin pre-flight tan extenso ni
retos opcionales) — útil como referencia rápida cuando ya tienes claros los
fundamentos.
