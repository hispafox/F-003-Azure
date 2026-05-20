# S8.3 — Despliegue automatizado: estrategias + health + rollback

> **Submódulo de referencia:** [M08-S8.3](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.3-despliegue-automatizado-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € en local; el deploy real corre en Azure DevOps

> 🎓 **Submódulo conceptual.** El deploy real corre en Azure DevOps;
> aquí se modela la **lógica testeable**: elegir estrategia por tipo
> de app, evaluar el health check post-deploy con retry y planificar
> el rollback (slide 8) o la alternativa con feature flag (slide 10).

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el cambio de neumáticos como analogía, la secuencia deploy→health→swap→smoke→rollback con `condition: failed()`, el plan de rollback ANTES de cada deploy y feature flag como rollback en segundos sin redeploy.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Estrategia por tipo de app (slides 3, 4, 5, 6, 7) | [`DeployStrategyAdvisor.cs`](src/Deploy.Demo.Api/Deploy/DeployStrategyAdvisor.cs) |
| Health check con retry + smoke test (slide 9) | [`HealthCheckEvaluator.cs`](src/Deploy.Demo.Api/Deploy/HealthCheckEvaluator.cs) |
| Plan de rollback + alternativa con feature flag (slides 8, 10) | [`RollbackPlanner.cs`](src/Deploy.Demo.Api/Deploy/RollbackPlanner.cs) |
| Plan + checklist del entregable | [`IDeploymentPlanner.cs`](src/Deploy.Demo.Api/Deploy/IDeploymentPlanner.cs) |
| API que expone la lógica (/deploy/*) | [`DeployEndpoints.cs`](src/Deploy.Demo.Api/Endpoints/DeployEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Comparativa de estrategias | 3 | `DeployStrategyAdvisor.Recomendar` |
| App Service deploy completo (staging → swap) | 4 | `IDeploymentPlanner.Checklist` |
| Functions con validación | 5 | `DeployStrategyAdvisor` (Premium → SlotSwap) |
| MSIX → AppInstaller | 6 | `DeployStrategyAdvisor` (Msix) |
| Bicep IaC: what-if obligatorio | 7 | `DeployStrategyAdvisor` (Infra) |
| Rollback strategies por tipo | 8 | `RollbackPlanner.Planificar` |
| Smoke tests + auto-rollback en `failed()` | 9 | `HealthCheckEvaluator` + checklist |
| Feature flags como alternativa | 10 | `RollbackPlanner.PlanFeatureFlag` |
| Sticky settings | 14 | `IDeploymentPlanner.Checklist` |
| Warmup post-deploy | 15 | `IDeploymentPlanner.Checklist` |

## Estructura

```
S8.3-despliegue-automatizado/
├── src/Deploy.Demo.Api/
│   ├── Deploy/     DeployStrategyAdvisor, HealthCheckEvaluator,
│   │               RollbackPlanner
│   │               + IDeploymentPlanner/DeploymentPlanner
│   ├── Endpoints/  DeployEndpoints (/health, /deploy/*)
│   └── Program.cs  AddSingleton<IDeploymentPlanner> + enums por nombre
├── tests/Deploy.Demo.Api.Tests/
│   ├── Unit_*             lógica pura (strategy, health, rollback)
│   ├── DiContainer_Tests  resuelve IDeploymentPlanner (contenedor real)
│   └── Api_DeployTests    E2E vía WebApplicationFactory
└── scripts/        az solo lectura (slots + sticky + último deploy)
```

## Tests

```bash
dotnet test     # 29 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `DeployStrategyAdvisor` (AppService+slots→
  SlotSwap, sin slots→DirectDeploy; Functions Premium→SlotSwap,
  Consumption→DirectDeploy; MSIX→AppInstaller; Infra→WhatIfApprove);
  `HealthCheckEvaluator` (pasa al 1.º intento, pasa al 2.º tras
  503, falla tras 5 intentos, **procesa en orden aunque lleguen
  desordenados**, smoke test pasa si todos 2xx); `RollbackPlanner`
  (App Service con slots → swap ~5s, sin slots → redesplegar,
  MSIX → build+1, Infra → avisa de storage/restore, feature flag
  como alternativa sin redeploy).
- **CAPA 0 · DI**: resuelve `IDeploymentPlanner` del contenedor real
  (`Assert.Same` singleton) y compone estrategia + rollback +
  checklist. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/deploy/{estrategia,healthcheck,smoke,rollback,rollback/feature-flag,plan}`.

> 🧠 **Sin CAPA de integración a propósito.** Disparar un deploy real
> requiere un App Service con slots + service connection — no
> reproducible en CI. La lógica de decisión y la evaluación de
> health check son lo que se enseña.

## Ejecución local

```bash
dotnet run --project src/Deploy.Demo.Api
# http://localhost:5107  — usa src/Deploy.Demo.Api/api.http
```

`/deploy/estrategia` decide por tipo de app; `/deploy/healthcheck`
modela el bucle de retry de la slide 9 (5×10s); `/deploy/smoke` verifica
que todos los endpoints respondan 2xx; `/deploy/rollback` da el plan
por tipo + alternativa con feature flag; `/deploy/plan` compone el
plan + checklist completo.

## Inventario de deploy (scripts `az`)

```bash
./scripts/demo.sh
# 1) 01-inventory-deploy.sh → slots de la Web App + health check
#    configurado + últimos 3 deploys + sticky settings (slide 14)
```

Solo lectura. No hace swap ni rollback.

## Despliegue por Portal (entregable)

1. **App Service con slot `staging`** (slide 4).
2. **Pipeline** que despliega a `staging`, ejecuta smoke test
   (`curl /health` con retry), aprueba y swap a producción.
3. **Sticky settings** marcados en App Service → Configuration: las
   connection strings y app settings de slot **no swap** con el slot
   (slide 14).
4. **Auto-rollback** en el pipeline con `condition: failed()` que
   ejecuta el swap inverso si algún smoke test posterior cae (slide 9).
5. **Feature flag** para activar el código gradualmente sin redeploy
   (slide 10).
6. **Verificar** con `./scripts/demo.sh`: slots + health + sticky.

## Ideas centrales

> Para App Service y Functions Premium, **slot swap** es la
> respuesta: **zero downtime** + **rollback en ~5 segundos** con
> swap inverso. Para MSIX, "rollback" = **publicar la versión buena
> con `build+1`** (slide 8.opción 1, ya visto en S7.6). Para infra,
> **`what-if` es obligatorio** antes de aplicar; si ves `Delete:` algo
> va mal. El **smoke test** post-deploy con retry (5×10s) caza los
> cold-starts; un `condition: failed()` dispara el swap inverso
> automático. La mejor red de seguridad antes de necesitar rollback:
> **feature flag** — el código está desplegado pero la feature está
> off; si algo va mal, se desactiva sin redeploy (slide 10).

## Próximo paso

[`S8.4 — ADO vs GitHub Actions`](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.4-ado-vs-github-actions-v3.md):
cuándo y cómo elegir entre Azure DevOps y GitHub Actions.
