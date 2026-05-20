# S2.P — Práctica: deployment slots y swap

> **Práctica de referencia:** [M02-S2.P](../../../doc/M02-App-Services/v4-actual/M02-S2.P-practica-slots-swap-v4.md)
> **Tipo:** práctica del alumno · **Duración estimada:** 60-75 min
> **TFM:** `net10.0` · **Tier mínimo en Azure:** B1 (subimos a S1 a mitad de la práctica)

> ℹ️ La práctica está redactada sobre .NET 8, código en .NET 10 (LTS). Las APIs no han cambiado.

> 📘 **¿Primera vez con esta práctica?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el ensayo general, el reflejo del rollback, los smoke tests antes del swap y por qué practicar el rollback antes de necesitarlo es la lección más valiosa de M02.

## Qué vas a hacer

Recorrer el ciclo completo de despliegue con slots desde cero:

1. Provisionar una Web App en plan **B1** (sin slots todavía).
2. Desplegar la **versión 1** a producción.
3. **Subir el plan a S1**, crear el slot `staging` y configurar **sticky settings**.
4. Desplegar la **versión 2** al slot staging.
5. Pasar **smoke tests** sobre staging.
6. Hacer **swap** staging → production.
7. Verificar que producción tiene la v2 y staging la v1.
8. **Rollback** con un swap inverso.
9. Limpieza (eliminar slot, bajar plan).

> 🎓 **Simulación didáctica**: para que la práctica se centre en los slots y no
> en mantener dos códigos distintos, "v1" y "v2" se distinguen por dos App
> Settings (`Practica:Version` y `Practica:Novedad`). En un caso real **el
> código** sería distinto entre slots; aquí el código es uno solo y el "qué"
> cambia se inyecta por configuración. La metáfora pedagógica se mantiene
> intacta: lo que viaja con la "v" lo declaras como App Setting normal, lo
> que es propio del entorno como **Slot setting** sticky.

## Mapeo a slides

| Concepto | Slide(s) | Dónde |
| --- | --- | --- |
| Pre-flight checklist | 3 | README → "Antes de empezar" |
| Plan B1 → S1 | 4 | [`scripts/03-upgrade-plan-and-create-slot.sh`](scripts/03-upgrade-plan-and-create-slot.sh) |
| Crear slot staging | 5 | mismo script |
| Sticky settings (`--slot-settings`) | 6 | mismo script |
| v1 vs v2 | 7 | `Practica:Version` / `Practica:Novedad` configurables |
| Deploy a slot | 8 | [`scripts/04-deploy-v2-to-staging.sh`](scripts/04-deploy-v2-to-staging.sh) |
| Warmup (`/warmup` + `WEBSITE_SWAP_WARMUP_PING_PATH`) | 9 | endpoint `/warmup` en `Program.cs` + setting en script 03 |
| Swap | 10 | [`scripts/06-swap.sh`](scripts/06-swap.sh) |
| Smoke tests | 11 | [`scripts/05-smoke-test.sh`](scripts/05-smoke-test.sh) |
| Rollback | 12 | [`scripts/07-rollback.sh`](scripts/07-rollback.sh) |
| Cleanup | 13 | [`scripts/09-cleanup.sh`](scripts/09-cleanup.sh) |
| Slot diff | 14 | [`scripts/08-slot-diff.sh`](scripts/08-slot-diff.sh) |
| Pricing por SKU | 16 | README → "Antes de empezar" |
| Troubleshooting | 17 | README → "Troubleshooting" |
| Checklist | 18 | README → "Checklist final" |
| Retos opcionales | 19, 20, 21 | README → "Retos opcionales" |

## Estructura

```
S2.P-practica-slots-swap/
├── README.md
├── AppService.Practica.Slots.slnx
├── Directory.Build.props
├── global.json
├── .gitattributes
├── src/AppService.Practica.Api/
│   ├── AppService.Practica.Api.csproj
│   ├── Program.cs                          /, /health, /warmup
│   ├── Configuration/PracticaOptions.cs    Version + Novedad + NotaEntorno
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Properties/launchSettings.json
├── tests/AppService.Practica.Api.Tests/    (4 tests)
└── scripts/
    ├── .env.demo.example
    ├── _lib.sh
    ├── 01-provision.sh
    ├── 02-deploy-as-v1.sh
    ├── 03-upgrade-plan-and-create-slot.sh
    ├── 04-deploy-v2-to-staging.sh
    ├── 05-smoke-test.sh                     production|staging [version]
    ├── 06-swap.sh
    ├── 07-rollback.sh
    ├── 08-slot-diff.sh
    ├── 09-cleanup.sh
    └── demo.sh                              menú interactivo
```

## Antes de empezar (slide 3)

```bash
# Azure CLI actualizado (>= 2.65.0)
az --version

# Login activo
az account show --output table

# Suscripción correcta
az account list --output table
az account set --subscription "<tu-sub>"

# .NET SDK 10 disponible
dotnet --list-sdks
```

**Errores típicos:**
- `No subscriptions found` → `az login --tenant <tenant-id>`
- `Forbidden` → pedir rol Contributor sobre el RG al instructor

**Coste**: la práctica usa B1 (~13 €/mes prorrateado) durante todo el flujo y
sube brevemente a S1 (~70 €/mes prorrateado) durante la parte de slots. Si
terminas en menos de un día, el coste real es de **menos de 1 €**. Limpia con
`./09-cleanup.sh` cuando acabes.

**Tiempo estimado: 60-75 minutos**, reservado en bloque continuo. Los swaps
tienen tiempos de propagación (1-2 min cada uno) que no se pueden acelerar.

## Ejecución local

```bash
dotnet run --project src/AppService.Practica.Api --launch-profile http
# → http://localhost:5080
```

Endpoints disponibles:

| Verbo | Ruta | Notas |
| --- | --- | --- |
| GET | `/` | JSON con `version`, `novedad`, `entorno`, `nota_entorno`, `slot`, `servidor`, `hora_utc` |
| GET | `/health` | `Healthy` (200) — App Service consulta este endpoint |
| GET | `/warmup` | `200 warm` — App Service llama a este antes del swap |

En local `slot=local` y `entorno=Development`. La práctica empieza al desplegar
a Azure.

## Tests

```bash
dotnet test
```

4 tests:

- `HomeEndpointTests` (2): refleja `Practica:Version` y `Practica:Novedad`
  inyectados desde configuración; defaults razonables sin config.
- `HealthEndpointTests` (1): `/health` responde 200 con `status: healthy`.
- `WarmupEndpointTests` (1): `/warmup` responde 200 con `status: warm`.

## Práctica paso a paso por Portal

> Pasos canónicos. Si prefieres terminal, salta a la siguiente sección.

### Paso 1 — Resource Group + plan **B1** + Web App

`Portal → Resource groups → Create` → `rg-curso-m02-sp`.

`Portal → App Service plans → Create`: Linux, **Basic B1**.

`Portal → App Services → Create → Web App`: runtime **.NET 10 (LTS)**, plan
`plan-curso-m02-sp`. Activa **Always On** y **HTTPS Only** en
`Configuration → General settings`. Health check path `/health`.

### Paso 2 — Deploy de la v1 a producción

`Configuration → Application settings`:

| Name | Value | Slot setting |
| --- | --- | --- |
| `Practica__Version` | `1.0` | ❌ |
| `Practica__Novedad` | `Hello World` | ❌ |
| `WEBSITE_RUN_FROM_PACKAGE` | `1` | (luego será sticky) |

VS Code → **Deploy to Web App…** apuntando a `src/AppService.Practica.Api`.

```bash
curl https://<app>.azurewebsites.net/
# → version: "1.0", novedad: "Hello World"
```

### Paso 3 — Upgrade del plan B1 → S1 (slide 4)

`tu Web App → Scale up (App Service plan) → Production → Standard S1 → Apply`.

Sin downtime, instantáneo.

### Paso 4 — Crear slot staging (slide 5)

`tu Web App → Deployment slots → Add slot`:

| Campo | Valor |
| --- | --- |
| Name | `staging` |
| Clone settings from | `<tu-app>` |

URL resultante: `https://<app>-staging.azurewebsites.net`.

### Paso 5 — Sticky settings (slide 6)

En el **slot principal** (producción), edita `Configuration → Application
settings` y marca como **Slot setting** estos valores:

| Name | Value |
| --- | --- |
| `Practica__NotaEntorno` | `Entorno de producción` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `WEBSITE_SWAP_WARMUP_PING_PATH` | `/warmup` |
| `WEBSITE_SWAP_WARMUP_PING_STATUSES` | `200` |
| `WEBSITE_RUN_FROM_PACKAGE` | `1` |

En el **slot staging** crea estos como Slot setting:

| Name | Value |
| --- | --- |
| `Practica__NotaEntorno` | `Entorno de staging — solo QA` |
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `WEBSITE_RUN_FROM_PACKAGE` | `1` |

### Paso 6 — Deploy de la v2 al slot staging (slide 8)

En el slot **staging**, edita Application settings y añade (no sticky):

| Name | Value |
| --- | --- |
| `Practica__Version` | `2.0` |
| `Practica__Novedad` | `Slots de despliegue funcionando` |

VS Code → **Deploy to Web App…** y selecciona explícitamente el slot
`staging` (no el principal).

```bash
curl https://<app>-staging.azurewebsites.net/
# → version: "2.0", novedad: "Slots...", nota_entorno: "Entorno de staging..."

curl https://<app>.azurewebsites.net/
# → version: "1.0" (sin cambios)
```

### Paso 7 — Smoke tests (slide 11)

```bash
bash scripts/05-smoke-test.sh staging 2.0
```

Deberías ver los 4 checks en verde. Si no, **no hagas swap**.

### Paso 8 — Swap staging → production (slide 10)

`tu Web App → Deployment slots → Swap`:
- Source: `staging`
- Target: `production`

Click **Swap**. App Service ejecuta el warmup ping primero (a
`/warmup` del slot staging) y solo redirige tráfico cuando responde 200.

```bash
curl https://<app>.azurewebsites.net/
# → version: "2.0"
#   novedad: "Slots de despliegue funcionando"
#   nota_entorno: "Entorno de producción"   ← sticky se quedó
#   entorno: "Production"                    ← sticky se quedó

curl https://<app>-staging.azurewebsites.net/
# → version: "1.0"                            ← código viejo en staging
#   nota_entorno: "Entorno de staging..."     ← sticky se quedó
```

### Paso 9 — Rollback (slide 12)

Si la v2 tiene un bug, `Deployment slots → Swap → Source: staging,
Target: production` otra vez. Mismo botón. La v1 vuelve a producción en
segundos porque sigue viva en el slot staging.

### Paso 10 — Cleanup (slide 13)

`tu Web App → Deployment slots → ⋯ → Delete` para borrar el slot. Luego
`Scale up → Basic B1 → Apply` para bajar el plan.

Si quieres limpieza total: `Resource groups → rg-curso-m02-sp → Delete`.

## Práctica paso a paso por scripts

```bash
cd scripts
cp .env.demo.example .env.demo
# editar .env.demo con tu SUBSCRIPTION_ID y APP único

bash 01-provision.sh                       # B1 + app + healthCheck
bash 02-deploy-as-v1.sh                    # version=1.0 a producción
bash 03-upgrade-plan-and-create-slot.sh    # B1 -> S1 + slot + sticky + warmup
bash 04-deploy-v2-to-staging.sh            # version=2.0 al slot
bash 05-smoke-test.sh staging 2.0          # validación pre-swap
bash 06-swap.sh                            # swap con confirmación
bash 05-smoke-test.sh production 2.0       # validación post-swap
bash 07-rollback.sh                        # opcional: swap inverso
bash 08-slot-diff.sh                       # ver diferencias entre slots
bash 09-cleanup.sh                         # borrar slot + bajar plan
```

`bash demo.sh` para el menú interactivo.

## Checklist final (slide 18)

| # | Paso | OK |
| --- | --- | --- |
| 1 | Plan subido a S1 | ☐ |
| 2 | Slot staging creado | ☐ |
| 3 | Sticky settings configurados (NotaEntorno + ASPNETCORE_ENVIRONMENT) | ☐ |
| 4 | Warmup configurado (`WEBSITE_SWAP_WARMUP_PING_PATH=/warmup`) | ☐ |
| 5 | v1 desplegada y verificada en producción | ☐ |
| 6 | v2 desplegada y verificada en staging | ☐ |
| 7 | Smoke tests pasados sobre staging antes del swap | ☐ |
| 8 | Swap ejecutado sin downtime aparente | ☐ |
| 9 | Verificado: `nota_entorno` y `ASPNETCORE_ENVIRONMENT` no viajaron | ☐ |
| 10 | Verificado: `Practica__Version` y `Practica__Novedad` sí viajaron | ☐ |
| 11 | Rollback ejecutado y producción volvió a v1 | ☐ |
| 12 | Slot eliminado y plan bajado a B1 | ☐ |

## Retos opcionales

### Reto 1 — Traffic routing / canary (slides 19, 21)

Antes del swap completo, manda solo el 10 % del tráfico a staging:

```bash
az webapp traffic-routing set --name "$APP" -g "$RG" --distribution staging=10
```

Bombardea `/` desde el navegador en modo incógnito (cookies limpias para que
no quede pegado al mismo slot). Aproximadamente 1 de cada 10 respuestas
debería decir `version: "2.0"`. Cuando estés satisfecho, sube a 50 y luego
haz el swap. Limpia con:

```bash
az webapp traffic-routing clear --name "$APP" -g "$RG"
```

### Reto 2 — Swap con preview (slide 19)

Multi-phase swap: aplica la config de producción al slot staging sin
redirigir tráfico. Permite verificar que la v2 funciona con la config real
de producción antes de comprometerse.

```bash
# Fase 1
az webapp deployment slot swap --name "$APP" -g "$RG" \
  --slot staging --target-slot production --action preview
# Verifica: curl https://<app>-staging.azurewebsites.net/info

# Fase 2 (si todo OK)
az webapp deployment slot swap --name "$APP" -g "$RG" \
  --slot staging --target-slot production --action swap

# O cancelar
az webapp deployment slot swap --name "$APP" -g "$RG" \
  --slot staging --target-slot production --action reset
```

### Reto 3 — Tres slots (slide 20)

Crea un tercer slot `dev` y prueba el flujo `dev → staging → production`.
Cada slot necesita sus settings sticky (NotaEntorno, ASPNETCORE_ENVIRONMENT).

## Troubleshooting (slide 17)

| Error | Causa típica | Fix |
| --- | --- | --- |
| `Operation 'Slot' is not supported` | Plan en F1 / D1 / B1 | Subir a S1 (`./03-upgrade-plan-and-create-slot.sh`) |
| Swap colgado > 2 min | Warmup ping no responde 200 | `curl https://<app>-staging.azurewebsites.net/warmup` para diagnosticar |
| Sticky setting "desaparece" tras swap | El setting estaba como **Application setting**, no como **Slot setting** | Re-marcarlo como Slot setting con `--slot-settings` |
| Después del swap, `version` sigue siendo 1.0 | El warmup falló y el swap se abortó | Revisa Activity Log del swap |
| `version` post-swap es la esperada pero `nota_entorno` cambió | NotaEntorno no estaba marcado como sticky | Revisa con `./08-slot-diff.sh` |

## Hand-off al siguiente módulo

Esta práctica cierra la parte aplicada del M02. La segunda práctica del
módulo (S2.P2 — deploy básico) introduce GitHub Actions y hace el puente
hacia el módulo M08 (DevOps).
