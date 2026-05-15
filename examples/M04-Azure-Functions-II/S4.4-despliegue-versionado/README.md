# S4.4 — Estrategias de despliegue y versionado

> **Submódulo de referencia:** [M04-S4.4](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.4-despliegue-versionado-v4.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption
> **Coste:** ~0 € (Consumption puro, sin Service Bus ni Cosmos)

## Objetivo

S4.4 es un submódulo **mayormente operacional** (métodos de deploy, slots,
blue/green, rollback, Bicep, Flex Consumption). Este ejemplo materializa
los patrones que **son código**: versionado de API, verificación
post-deploy y feature flags para despliegue seguro.

| Patrón | Slide | Implementación |
| --- | --- | --- |
| **Versionado de API por ruta** | 7 | `GET /api/v1/productos` vs `GET /api/v2/productos` — v2 es un breaking change (añade `moneda`, `stock`) |
| **Health check post-deploy** | 10, 17 | `GET /api/health` agrega checks → 200 / 503 |
| **Endpoint de versión** | 14 | `GET /api/version` → build vivo + flags activos |
| **Feature flags** | 16 | `ProcesarPedido` conmuta legacy↔nuevo según `FEATURE_NUEVO_PROCESAMIENTO` |
| **Run from Package** | 4 | `WEBSITE_RUN_FROM_PACKAGE=1` en provision |
| **Script post-deploy verification** | 14 | [`scripts/05-postdeploy-check.sh`](scripts/05-postdeploy-check.sh) |

> 🎯 **Idea clave**: versionar el contrato ≠ cambiar la lógica. v1 y v2
> proyectan el **mismo dominio** (`IProductoCatalogo`) a contratos
> distintos. Y un **feature flag** permite "rollback sin redeploy":
> si la lógica nueva falla en prod, apagas el App Setting y vuelve la
> legacy al instante — sin pipeline, sin esperar.

## Estructura

```
S4.4-despliegue-versionado/
├── src/AzureFunctions.Demo/
│   ├── Functions/
│   │   ├── ProductosVersionadasFunctions.cs   ← v1 + v2 (slide 7)
│   │   └── OperacionesFunctions.cs            ← health, version, feature flag
│   ├── Models/Producto.cs                      (dominio + ProductoV1 + ProductoV2)
│   ├── Services/
│   │   ├── IProductoCatalogo  (+ ProductoMappers ToV1/ToV2)
│   │   ├── IFeatureFlags / EnvFeatureFlags
│   │   ├── IProcesadorPedido  (legacy / nuevo / selector)
│   │   └── IHealthCheck / HealthAggregator
│   ├── Middleware/                             (heredado)
│   └── host.json
├── tests/AzureFunctions.Demo.Tests/            (15 tests)
└── scripts/
    ├── 01-provision.sh                         (RUN_FROM_PACKAGE + flag OFF)
    ├── 02-deploy.sh
    ├── 03-smoke-test.sh                        (v1/v2 + flag + health)
    ├── 05-postdeploy-check.sh                  (slide 14: estado/health/version)
    └── 04-cleanup.sh
```

## Tests

```bash
dotnet test
```

15 tests sin Azure:

- **`ProductosVersionadasTests`** (4) — v1 sin moneda/stock, v2 con ellos,
  ambas proyectan el mismo dominio (precio idéntico), 404 en las dos.
- **`OperacionesFunctionsTests`** (7) — feature flag off→legacy (total
  intacto) / on→nuevo (−5%), body inválido→400, health all-ok→200,
  un check falla→503, check que lanza cuenta como unhealthy (no 500),
  `/version` expone flags.
- **`HelloFunctionTests`** + **`PingFunctionTests`** (4) — heredados.

## Despliegue por Portal

1. **RG** `rg-curso-m04-s44` · **Storage** `stcursom04s44{iniciales}` (LRS).
2. **Function App** .NET 10 Isolated, Linux, Consumption, ese Storage.
3. **Configuration** → añade:
   - `WEBSITE_RUN_FROM_PACKAGE` = `1` (slide 4 — despliegue atómico)
   - `FEATURE_NUEVO_PROCESAMIENTO` = `false` (deploy seguro; se activa tras verificar)
4. **Deploy** desde VS Code.
5. **Verificación post-deploy** (slide 14):
   ```bash
   curl https://func-curso-m04-s44-{ini}.azurewebsites.net/api/health   # 200
   curl https://func-curso-m04-s44-{ini}.azurewebsites.net/api/version  # build + flags
   ```
6. **Activar la feature** cuando todo esté verde (rollout):
   Configuration → `FEATURE_NUEVO_PROCESAMIENTO` = `true` → Save.
   El `POST /api/pedidos/procesar` ahora responde `procesadoPor:"nuevo"`.
   ¿Algo va mal? Vuelve a `false` → **rollback instantáneo sin redeploy**.
7. **Cleanup**: borra el RG.

## Slots, blue/green y rollback (conceptual — no ejecutado aquí)

El ejemplo no provisiona slots porque **requieren plan Premium/Dedicated**
(coste). El submódulo los cubre conceptualmente y este README los resume:

- **Slots** (slide 5): `az functionapp deployment slot create … --slot staging`,
  desplegar a staging, `slot swap`. Connection strings de staging marcadas
  como *sticky* para que sus triggers no toquen datos de producción.
- **Blue/green sin slots** (slide 11): dos Function Apps (`-blue`/`-green`),
  desplegar a green, verificar, cambiar el backend en APIM/DNS.
- **Rollback** (slide 15): con slots = swap inverso (instantáneo); sin
  slots = redeploy del artefacto anterior (guárdalo siempre) o
  `git revert` + pipeline. **Mejor aún**: feature flag (slide 16, lo que
  hace este ejemplo) — apagar un setting es más rápido que cualquier rollback.

## Fuera de alcance (deliberado)

Slots reales (Premium), Docker deploy (slide 12), Bicep/IaC (slides 9/13 →
M08), Deployment Stacks (slide 19), pipeline CI/CD completo (slide 8 → M08),
Flex Consumption (slide 18 — mismo código, distinto `az functionapp create`).

## Próximo paso

[`S4.5 — Testing y depuración`](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.5-testing-depuracion-v4.md):
estrategias de test (unit/integration), depuración local y remota,
y observabilidad — cierra el bloque conceptual de M04 antes de las prácticas.
