# S2.1 — App Service: creación, configuración y publicación

> **Submódulo de referencia:** [M02-S2.1](../../../doc/M02-App-Services/v4-actual/M02-S2.1-creacion-configuracion-publicacion-v4.md)
> **TFM:** `net10.0` · **Tipo:** Minimal API · **Tests:** xUnit + `WebApplicationFactory<Program>`

> ℹ️ El submódulo está redactado sobre **.NET 8**, pero el código está en **.NET 10**
> (LTS, noviembre 2025). Las APIs de ASP.NET Core que usamos no han cambiado: los
> conceptos del material son idénticos.

## Objetivo

Materializar en un proyecto pequeño los conceptos clave del submódulo S2.1:

- App Service Plan + instancias (slide 4)
- Always On + Health Check (slide 13)
- Configuración tipada y App Settings (slides 12, 14)
- Logging visible en App Service (slide 26)
- CORS configurable (slide 27)
- HttpClient singleton para evitar SNAT exhaustion (slide 31)
- HTTPS forzado en producción (slide 21)
- Run from Package compatible (slide 17)

El alumno termina con una API que se puede ejecutar en local y desplegar a un
App Service Linux .NET 10 con plan B1 siguiendo los pasos del Portal.

## Mapeo a slides

| Concepto del slide | Slide(s) | Dónde está en el código |
| --- | --- | --- |
| Arquitectura: instancias, host, machine name | 4 | [`Endpoints/HelloEndpoints.cs`](src/AppService.Demo.Api/Endpoints/HelloEndpoints.cs) |
| Always On + Health Check (`/health`) | 13 | [`Endpoints/HealthEndpoints.cs`](src/AppService.Demo.Api/Endpoints/HealthEndpoints.cs) + [`Services/ConfigurableHealthCheck.cs`](src/AppService.Demo.Api/Services/ConfigurableHealthCheck.cs) |
| App Settings, env vars, Options pattern | 12, 14 | [`Configuration/AppOptions.cs`](src/AppService.Demo.Api/Configuration/AppOptions.cs) + [`Endpoints/InfoEndpoints.cs`](src/AppService.Demo.Api/Endpoints/InfoEndpoints.cs) |
| Logging hacia App Service | 26 | `Logging.AddAzureWebAppDiagnostics()` en [`Program.cs`](src/AppService.Demo.Api/Program.cs) |
| CORS controlado por configuración | 27 | `AddCors` en [`Program.cs`](src/AppService.Demo.Api/Program.cs) |
| HttpClient singleton (SNAT) | 31 | [`Services/ExternalApiClient.cs`](src/AppService.Demo.Api/Services/ExternalApiClient.cs) |
| HTTPS forzado fuera de Development | 21 | `UseHttpsRedirection`/`UseHsts` en [`Program.cs`](src/AppService.Demo.Api/Program.cs) |
| Reproducir un fallo de salud (slide 32) | 32 | toggle `AppOptions:Healthy=false` en `ConfigurableHealthCheck` |

## Estructura

```
S2.1-creacion-config-publicacion/
├── README.md                          ← este archivo
├── AppService.Demo.slnx               ← solución con API + tests
├── Directory.Build.props              ← TFM net10.0, Nullable, ImplicitUsings, warnings as errors
├── global.json                        ← pin de SDK
├── .gitattributes                     ← LF en código (evita CRLF warnings)
├── src/AppService.Demo.Api/
│   ├── AppService.Demo.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Properties/launchSettings.json
│   ├── Configuration/AppOptions.cs
│   ├── Endpoints/
│   │   ├── HelloEndpoints.cs          → GET /
│   │   ├── HealthEndpoints.cs         → GET /health
│   │   └── InfoEndpoints.cs           → GET /info
│   └── Services/
│       ├── ConfigurableHealthCheck.cs
│       └── ExternalApiClient.cs
└── tests/AppService.Demo.Api.Tests/
    ├── HealthEndpointTests.cs
    ├── HelloEndpointTests.cs
    ├── InfoEndpointTests.cs
    └── CorsConfigurationTests.cs
```

## Requisitos previos

- **.NET SDK 10** (`dotnet --list-sdks` debe mostrar `10.x`).
- **Suscripción de Azure** (cualquier plan, incluido el gratuito).
- **VS Code** con la extensión **Azure App Service** _o_ Visual Studio 2022+.
- (Opcional) **dev cert .NET** confiado para HTTPS en local:
  ```bash
  dotnet dev-certs https --trust
  ```

## Ejecución local

> Pedro lanza las apps. Si quieres lanzarla tú:

```bash
# desde la carpeta del ejemplo
dotnet run --project src/AppService.Demo.Api --launch-profile http
# → http://localhost:5080

# o con HTTPS (requiere dev cert confiado)
dotnet run --project src/AppService.Demo.Api --launch-profile https
# → https://localhost:5081
```

Endpoints expuestos:

| Verbo | Ruta | Qué devuelve |
| --- | --- | --- |
| GET | `/` | Saludo + `machineName` + `instanceId` (slide 4) |
| GET | `/info` | Info de runtime + variables `WEBSITE_*` + `AppOptions` actuales |
| GET | `/health` | `Healthy` (200) o `Unhealthy` (503) según `AppOptions:Healthy` |

## Tests

```bash
dotnet test
```

Casos cubiertos:

- `HealthEndpointTests`
  - GET `/health` por defecto → `200 Healthy`.
  - Con `AppOptions:Healthy=false` → `503 Service Unavailable` (simula slide 32).
- `HelloEndpointTests`
  - GET `/` devuelve `greeting`, `machineName` e `instanceId` no vacíos.
- `InfoEndpointTests`
  - GET `/info` expone `machineName`, `dotnetVersion` y `appOptions.{greeting,healthy,allowedOrigins}`.
- `CorsConfigurationTests`
  - Preflight desde un origen permitido → header `Access-Control-Allow-Origin`.
  - Preflight desde un origen no permitido → sin ese header.

## Tour del código

### `Program.cs` (37 líneas operativas)

Pipeline mínimo en este orden:

1. **`AddAzureWebAppDiagnostics`** — habilita el provider que envía los `ILogger`
   al sistema de logs de App Service (slide 26).
2. **`AddOptions<AppOptions>().ValidateOnStart()`** — Options pattern con
   validación de Data Annotations al arranque (slide 12).
3. **`AddCors`** — política `DefaultPolicy` que sólo acepta los orígenes presentes
   en `AppOptions:AllowedOrigins`. Si la lista está vacía, **bloquea todo** (slide 27).
4. **`AddHttpClient<ExternalApiClient>`** — typed client; el `HttpMessageHandler`
   se reutiliza, evitando SNAT exhaustion (slide 31).
5. **`AddHealthChecks().AddCheck<ConfigurableHealthCheck>`** — el check personalizado
   permite reproducir un fallo desde configuración (slide 32).
6. **`UseHttpsRedirection` + `UseHsts`** — sólo fuera de Development; complementa
   el toggle "HTTPS Only" del Portal (slide 21).
7. `MapHello` / `MapInfo` / `MapHealth` — endpoints organizados como extensiones.

> El archivo termina con `public partial class Program;` para que
> `WebApplicationFactory<Program>` funcione en los tests.

### `Configuration/AppOptions.cs`

Pequeño POCO con tres settings:

| Setting | Tipo | Para qué |
| --- | --- | --- |
| `Greeting` | `string` (requerido) | El texto que devuelve `GET /` |
| `Healthy` | `bool` | Toggle para simular fallo de salud sin redeploy |
| `AllowedOrigins` | `string[]` | Lista blanca de orígenes para CORS |

En App Service estas settings se inyectan vía **Configuration → Application
settings**:

| Application setting (Portal) | Equivalente local |
| --- | --- |
| `AppOptions__Greeting` | `AppOptions:Greeting` |
| `AppOptions__Healthy` | `AppOptions:Healthy` |
| `AppOptions__AllowedOrigins__0` | `AppOptions:AllowedOrigins:0` |

## Despliegue por Portal de Azure

> Todos los pasos son del **Portal web** (no `az`). El único punto donde más
> adelante usaremos CLI será al crear el Service Principal para CI/CD en M08.

### Paso 1 — Resource Group

`Portal → Resource groups → Create`

| Campo | Valor |
| --- | --- |
| Subscription | _la tuya_ |
| Resource group | `rg-curso-m02-s21` |
| Region | `West Europe` (o la más cercana) |

### Paso 2 — App Service Plan

`Portal → App Service plans → Create`

| Campo | Valor | Por qué |
| --- | --- | --- |
| Name | `plan-curso-m02-s21` | |
| Operating System | **Linux** | Más barato (sin licencia OS) — slide 11 |
| Region | igual que el RG | |
| Pricing plan | **Basic B1** | Mínimo para Always On + custom domain — slide 8 |

### Paso 3 — Web App

`Portal → App Services → Create → Web App`

**Pestaña "Basics":**

| Campo | Valor |
| --- | --- |
| Resource group | `rg-curso-m02-s21` |
| Name | `app-curso-m02-s21-<tus-iniciales>` (debe ser único) |
| Publish | **Code** |
| Runtime stack | **.NET 10 (LTS)** |
| Operating System | Linux |
| Region | igual que el plan |
| Linux Plan | `plan-curso-m02-s21` |
| Pricing plan | Basic B1 (heredado del plan) |

**Pestaña "Monitoring + secure":** desactiva Application Insights por ahora
(lo veremos en M08); deja el resto por defecto.

`Review + create → Create`.

### Paso 4 — Configuración esencial

Cuando la web app esté creada, vete a la sección **Configuration**:

#### General settings

| Toggle | Valor | Slide |
| --- | --- | --- |
| Always On | **On** | 13 |
| HTTP version | 2.0 | — |
| HTTPS Only | **On** | 21 |
| Minimum TLS Version | 1.2 | — |

#### Application settings

`Configuration → Application settings → New application setting` (uno por fila):

| Name | Value |
| --- | --- |
| `AppOptions__Greeting` | `Hola desde App Service en Azure` |
| `AppOptions__Healthy` | `true` |
| `AppOptions__AllowedOrigins__0` | `https://tu-frontend.com` |
| `WEBSITE_RUN_FROM_PACKAGE` | `1` |

`Save` y deja que la app se reinicie.

#### Health check

`Monitoring → Health check`:

| Campo | Valor |
| --- | --- |
| Status | **Enable** |
| Path | `/health` |
| Load balancing | 2 minutes |

### Paso 5 — Despliegue del ZIP

#### Opción A — Desde VS Code (recomendada para desarrollo)

1. Abre la carpeta del ejemplo en VS Code.
2. Panel lateral → Azure → expande tu suscripción → App Services.
3. Botón derecho sobre `app-curso-m02-s21-<iniciales>` → **Deploy to Web App…**
4. Selecciona la carpeta del proyecto `src/AppService.Demo.Api`.
5. Acepta el aviso de "publish" — VS Code corre `dotnet publish -c Release` y
   sube el ZIP resultante por Kudu (`/api/zipdeploy`).

#### Opción B — Deployment Center (Portal)

1. En la web app: `Deployment → Deployment Center`.
2. Source: **Local Git** o **External Git** según prefieras.
3. Sigue las credenciales que muestre el Portal y haz `git push azure main`.

#### Opción C — Manual con un ZIP

```bash
# 1. publica
dotnet publish src/AppService.Demo.Api -c Release -o out

# 2. zip
cd out && zip -r ../app.zip . && cd ..

# 3. sube vía Kudu (Portal → Advanced Tools / SCM → Tools → Zip Push Deploy)
#    arrastras app.zip y se despliega
```

### Paso 6 — Verificación

```bash
# salud
curl https://app-curso-m02-s21-<iniciales>.azurewebsites.net/health
# Healthy

# info
curl https://app-curso-m02-s21-<iniciales>.azurewebsites.net/info
# JSON con instanceId, siteName, resourceGroup, appOptions...

# saludo
curl https://app-curso-m02-s21-<iniciales>.azurewebsites.net/
```

En el Portal, **Log stream** debe mostrar las líneas `Hello endpoint hit on
instance ...` cada vez que llamas a `/`.

**Reproducir slide 32** (auto-restart de App Service):

1. `Configuration → Application settings`.
2. Cambia `AppOptions__Healthy` a `false` → Save.
3. Espera ~2 minutos (intervalo del health check).
4. App Service registra "Health check failed" y reinicia la instancia.
5. Vuelve a poner `true` para que se quede sana.

## Limpieza

`Portal → Resource groups → rg-curso-m02-s21 → Delete resource group`. Confirma
escribiendo el nombre. Esto borra plan + web app + todas sus settings (no hay
recursos compartidos con otros submódulos).

## Siguiente paso

[`S2.2 — Slots de despliegue`](../../../doc/M02-App-Services/v4-actual/M02-S2.2-slots-staging-produccion-v4.md)
añade un slot `staging` a esta misma app y enseña cómo hacer el `swap` sin
downtime. El ejemplo S2.2 reutilizará esta API y la promocionará entre slots.
