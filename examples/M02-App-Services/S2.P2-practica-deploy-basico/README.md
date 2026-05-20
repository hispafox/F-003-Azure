# S2.P2 — Práctica: deploy básico a Azure App Service

> **Práctica de referencia:** [M02-S2.P2](../../../doc/M02-App-Services/v4-actual/M02-S2.P2-practica-deploy-basico-v1.md)
> **Tipo:** práctica del alumno · **Duración estimada:** 60-75 min
> **TFM:** `net10.0` · **Tier:** F1 (gratuito) · **Coste real:** **0 €**

> ℹ️ La práctica está redactada sobre .NET 8 con plan F1; el código aquí está
> en **.NET 10** (LTS). Para Azure usaremos `runtime DOTNETCORE:10.0`. Es la
> regla del repo: TFM siempre en la última LTS.

> 📘 **¿Primera vez con esta práctica?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el ciclo mínimo viable (crear → desplegar → configurar sin redesplegar → ver logs → limpiar), cuándo elegir S2.P2 vs S1.P y los cuatro retos opcionales para consolidar.

## Qué vas a hacer

Tu **primer deploy** end-to-end a Azure App Service desde cero. Sin slots,
sin pipelines, sin secretos: solo **código local → app pública en Azure**.

```
1. Crear el proyecto .NET 10 (ya está en este ejemplo)
2. Probar en local que funciona
3. Crear Resource Group + plan F1 + Web App
4. Desplegar via zip
5. Verificar que la URL pública responde
6. Configurar App Settings y ver el cambio sin redesplegar
7. Ver logs en streaming
8. Smoke tests automatizados
9. Cleanup
```

> 🎯 **Por qué empezar por aquí**: dominar el deploy básico es prerrequisito
> para todo lo demás del curso. Si no controlas esto, slots y CI/CD parecen
> magia. Cuando termines esta práctica, la siguiente
> ([S2.P — slots y swap](../S2.P-practica-slots-swap)) reusa este flujo y
> añade slots encima.

## Mapeo a slides

| Concepto | Slide(s) | Dónde |
| --- | --- | --- |
| Pre-flight (CLI, .NET, login) | 3 | README → "Antes de empezar" |
| Proyecto .NET con minimal API | 4 | [`src/MiPrimeraWebApp/`](src/MiPrimeraWebApp/) ya creado |
| 3 endpoints (`/`, `/health`, `/saludo/{nombre}`) | 5 | [`Program.cs`](src/MiPrimeraWebApp/Program.cs) |
| Probar en local | 6 | `dotnet run` |
| Resource Group | 7 | [`scripts/01-provision.sh`](scripts/01-provision.sh) |
| App Service Plan F1 | 8 | mismo script |
| Crear Web App | 9 | mismo script |
| Zip deploy | 10 | [`scripts/02-deploy.sh`](scripts/02-deploy.sh) |
| Verificar | 11 | `curl` o navegador |
| Logs en streaming | 12 | `az webapp log tail` (opción 5 del menú `demo.sh`) |
| Logging estructurado | 13 | `logger.LogInformation("... {Param} ...", ...)` en Program.cs |
| App Settings | 14 | [`scripts/03-app-settings.sh`](scripts/03-app-settings.sh) |
| Smoke tests | 15 | [`scripts/04-smoke-test.sh`](scripts/04-smoke-test.sh) |
| Troubleshooting | 16 | README → "Troubleshooting" |
| Métricas básicas | 17 | README → "Métricas" |
| Cleanup | 19 | [`scripts/05-cleanup.sh`](scripts/05-cleanup.sh) |
| Reto 1 (POST /usuarios) | 21 | Ya implementado en `Program.cs` |

## Estructura

```
S2.P2-practica-deploy-basico/
├── README.md
├── MiPrimeraWebApp.slnx
├── Directory.Build.props
├── global.json
├── .gitattributes
├── src/MiPrimeraWebApp/
│   ├── MiPrimeraWebApp.csproj          (sin packages — proyecto minimal)
│   ├── Program.cs                       /, /health, /saludo/{nombre}, /usuarios
│   ├── Configuration/SaludoOptions.cs   Saludo:Base + Saludo:MaxLength
│   ├── Models/Usuario.cs                record para POST /usuarios
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Properties/launchSettings.json
├── tests/MiPrimeraWebApp.Tests/         (7 tests, todos verdes)
└── scripts/
    ├── .env.demo.example
    ├── _lib.sh
    ├── 01-provision.sh                  RG + plan F1 + web app + healthCheck
    ├── 02-deploy.sh                     publish + zip + zip deploy
    ├── 03-app-settings.sh               Saludo__Base, Saludo__MaxLength
    ├── 04-smoke-test.sh                 4 checks
    ├── 05-cleanup.sh                    borra el RG
    └── demo.sh                          menú interactivo
```

## Antes de empezar (slide 3)

```bash
# Azure CLI actualizado
az --version
az account show --output table

# .NET SDK 10 disponible
dotnet --list-sdks | grep '10\.'
```

Si tu material lectivo dice .NET 8, no pasa nada — este ejemplo usa .NET 10
y el `Directory.Build.props` lo aplica al proyecto y los tests
automáticamente. Las APIs que toca esta práctica no han cambiado.

## Ejecución local

```bash
dotnet run --project src/MiPrimeraWebApp --launch-profile http
# → http://localhost:5080
```

Endpoints:

| Verbo | Ruta | Notas |
| --- | --- | --- |
| GET | `/` | JSON con `aplicacion`, `version`, `entorno`, `hora_servidor`, `mensaje` |
| GET | `/health` | `Healthy` (200) — App Service lo usará vía `healthCheckPath=/health` |
| GET | `/saludo/{nombre}` | Mensaje compuesto con `Saludo:Base`. 400 si excede `Saludo:MaxLength` |
| POST | `/usuarios` | body `{ nombre, email }` → 201 si email contiene `@`; 400 si no |

## Tests

```bash
dotnet test
```

**7 tests verdes**:

- `RootEndpointTests` (1): GET `/` devuelve `aplicacion`, `version`, `entorno`, `hora_servidor`.
- `HealthEndpointTests` (1): GET `/health` → 200 con `status: healthy`.
- `SaludoEndpointTests` (2): respeta `Saludo:Base` desde configuración; 400 si supera `Saludo:MaxLength`.
- `UsuariosEndpointTests` (3): POST con email válido → 201 con `id`; `[Theory]` con email vacío y sin `@` → 400.

## Práctica paso a paso por Portal

> Pasos canónicos. Si prefieres terminal, salta a "Práctica con scripts".

### Paso 1 — Resource Group

`Portal → Resource groups → Create`:

| Campo | Valor |
| --- | --- |
| Name | `rg-curso-m02-sp2` |
| Region | West Europe |

### Paso 2 — App Service Plan F1

`Portal → App Service plans → Create`:

| Campo | Valor |
| --- | --- |
| Name | `plan-curso-m02-sp2` |
| OS | Linux |
| Region | West Europe |
| Pricing tier | **Free F1** |

### Paso 3 — Web App

`Portal → App Services → Create → Web App`:

| Campo | Valor |
| --- | --- |
| Resource group | `rg-curso-m02-sp2` |
| Name | `webapp-curso-m02-sp2-<iniciales>` (único globalmente) |
| Runtime stack | **.NET 10 (LTS)** |
| OS | Linux |
| Region | West Europe |
| Plan | `plan-curso-m02-sp2` |

`Configuration → General settings → Health check path` = `/health`.

> ⚠️ Always On no se puede activar en F1 (slide 8). La app se duerme tras
> 20 min sin tráfico. **Es lo esperado**.

### Paso 4 — Deploy desde VS Code

1. VS Code → panel Azure → expandir tu suscripción → App Services.
2. Click derecho sobre `webapp-curso-m02-sp2-<iniciales>` → **Deploy to
   Web App…**
3. Selecciona la carpeta `src/MiPrimeraWebApp`.
4. Confirma el aviso de "publish".

### Paso 5 — Verificar (slide 11)

```bash
URL=https://webapp-curso-m02-sp2-<iniciales>.azurewebsites.net

curl "$URL/"
# → { "aplicacion":"Mi Primera Web App", "version":"1.0",
#     "entorno":"Production", "hora_servidor":"...", "mensaje":"Hola desde Azure" }

curl "$URL/health"
# → { "status":"healthy", "timestamp":"..." }

curl "$URL/saludo/Madrid"
# → { "mensaje":"Hola, Madrid", "hora":"..." }
```

> 💡 **Cold start del F1**: el primer `curl` puede tardar 5-30 s si la app
> estaba dormida. El segundo es rápido. Es normal en el plan gratuito.

### Paso 6 — App Settings sin redesplegar (slide 14)

`Configuration → Application settings → New application setting`:

| Name | Value |
| --- | --- |
| `Saludo__Base` | `Hola desde Azure App Service,` |
| `Saludo__MaxLength` | `80` |

`Save`. La app reinicia sola en ~10-30 s.

```bash
curl "$URL/saludo/Pedro"
# → { "mensaje":"Hola desde Azure App Service, Pedro", "hora":"..." }
#                ↑ cambió SIN redeploy del código
```

### Paso 7 — Logs en streaming (slide 12)

`tu Web App → Monitoring → Log stream`. Mientras dejas la pestaña abierta:

```bash
curl "$URL/saludo/test"
curl "$URL/health"
curl "$URL/inexistente"   # 404
```

Verás cada request aparecer en directo. La línea `Saludando a {Nombre}`
demuestra el logging estructurado (slide 13).

### Paso 8 — Smoke tests (slide 15)

Con la URL pública configurada en `.env.demo`:

```bash
bash scripts/04-smoke-test.sh
```

4 checks: raíz, health, saludo válido, latencia media.

### Paso 9 — Cleanup (slide 19)

`Portal → Resource groups → rg-curso-m02-sp2 → Delete`. Confirma escribiendo
el nombre del RG.

## Práctica con scripts

Equivalente a los pasos de Portal, todo desde terminal:

```bash
cd scripts
cp .env.demo.example .env.demo
# edita SUBSCRIPTION_ID y APP único

bash 01-provision.sh        # RG + plan F1 + web app + healthCheck
bash 02-deploy.sh           # publish + zip + zip deploy
bash 03-app-settings.sh     # Saludo__Base + Saludo__MaxLength
bash 04-smoke-test.sh       # 4 checks
bash 05-cleanup.sh          # borra el RG entero
```

`bash demo.sh` para el menú interactivo (incluye opción de log stream).

## Checklist final (slide 20)

| # | Paso | OK |
| --- | --- | --- |
| 1 | Pre-flight (az, .NET 10, login) | ☐ |
| 2 | `dotnet build` y `dotnet test` en local sin errores | ☐ |
| 3 | `dotnet run` local responde en los 3 endpoints | ☐ |
| 4 | Resource Group creado | ☐ |
| 5 | App Service Plan F1 creado | ☐ |
| 6 | Web App creada con runtime .NET 10 | ☐ |
| 7 | Código publicado a Azure | ☐ |
| 8 | URL pública responde con `entorno: "Production"` | ☐ |
| 9 | `App Settings` configurados y reflejados sin redesplegar | ☐ |
| 10 | Logs visibles en `log stream` o `az webapp log tail` | ☐ |
| 11 | Smoke tests script en verde | ☐ |
| 12 | Cleanup completado | ☐ |

## Retos opcionales (slide 21)

### Reto 1 — `POST /usuarios` con validación

Ya implementado en `Program.cs` y cubierto por `UsuariosEndpointTests`.
Pruébalo desde tu app desplegada:

```bash
curl -X POST "$URL/usuarios" \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Pedro","email":"pedro@example.com"}'
# → 201 Created con id GUID

curl -X POST "$URL/usuarios" \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Pedro","email":"sin-arroba"}'
# → 400 Bad Request
```

### Reto 2 — Custom error handling

Añade `app.UseExceptionHandler(...)` o un middleware que devuelva siempre
JSON estructurado en lugar de la página HTML por defecto. El M08 cubrirá
patrones más completos.

### Reto 3 — Health check más elaborado

Cambia `/health` para que verifique uptime, working set y devuelva
`status: degraded` si algo no es óptimo. Pista: el ejemplo del
[S2.5](../S2.5-monitorizacion-diagnostico) tiene un `/health/details`
con response writer JSON que puedes adaptar.

### Reto 4 — Deploy con `az webapp up` (slide 21)

```bash
cd src/MiPrimeraWebApp
az webapp up --runtime "DOTNETCORE:10.0" --name <tu-app> --resource-group $RG
```

Compara con el flujo de zip deploy. Para iteración rápida durante
desarrollo, `az webapp up` ahorra tiempo; para producción, el zip explícito
es más predecible.

### Reto avanzado — GitHub Actions

Configura un workflow simple que despliegue al hacer push a `main`. El M08
del curso lo cubre en profundidad; si quieres adelantarte, mira el ejemplo
de pipeline del slide 22 de la práctica S2.P.

## Troubleshooting (slide 16)

| Síntoma | Causa probable | Fix |
| --- | --- | --- |
| 502 / 503 al primer request | Cold start del F1 | Esperar 30 s y reintentar |
| 503 sostenido tras varios deploys | App no arrancó (excepción al inicio) | `az webapp log tail` para ver el error |
| Cambios de App Settings no se reflejan | Reinicio aún en curso | `az webapp restart` para forzarlo, esperar ~30 s |
| 503 sin razón aparente tras horas de uso | Cuota CPU diaria del F1 agotada (60 min/día) | Esperar al día siguiente o subir a B1 |
| Deploy "OK" pero código viejo sigue corriendo | Caché de App Service | `az webapp restart` y vuelve a probar |
| `AuthorizationFailed` en `az` | Suscripción incorrecta seleccionada | `az account list -o table` y `az account set --subscription <correcta>` |

**Cuando nada funciona** abre Kudu (consola web administrativa):

```
https://<app>.scm.azurewebsites.net
```

Permite explorar archivos desplegados, ver variables de entorno reales y
hacer SSH a la instancia (Linux).

## Métricas (slide 17)

`tu Web App → Monitoring → Metrics` y crea un gráfico con:

- `Http2xx`, `Http4xx`, `Http5xx` (éxito y errores)
- `AverageResponseTime` (latencia)
- `CpuTime` (importante en F1: hay 60 min/día)
- `MemoryWorkingSet` (RAM, hay 1 GB en F1)

Para esta práctica no configuramos alertas — eso lo cubre
[S2.5](../S2.5-monitorizacion-diagnostico) con Application Insights.

## Hand-off

Cuando termines esta práctica:

- Si quieres seguir el flujo: la práctica de
  [`S2.P — slots y swap`](../S2.P-practica-slots-swap) usa el mismo patrón
  y añade slots, sticky settings y swap. Mantén tu Resource Group si vas
  directo allí; solo subirás el plan F1 → S1 cuando toque.
- Si quieres profundizar en monitorización, salta al ejemplo del
  [`S2.5 — monitorización`](../S2.5-monitorizacion-diagnostico) con
  Application Insights y OpenTelemetry.
