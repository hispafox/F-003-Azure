# S8.P — Práctica Pipeline CI/CD completo

> **Submódulo de referencia:** [M08-S8.P](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.P-practica-pipeline-cicd-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; los scripts solo leen el plan/slot/deploys existentes)

> 🎓 **Práctica conceptual** (lección 9 del HANDOFF). El pipeline real
> se monta en ADO o GitHub Actions — aquí extraemos las **3 piezas
> testeables** que sustentan la práctica: preflight, esqueleto de
> stages, y evaluador de smoke test con auto-rollback.

> 📘 **¿Primera vez con esta práctica?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: la entrega de un producto en una tienda como analogía, las tres puertas del pipeline (build verde, smoke staging, aprobación humana), preflight con 10 comprobaciones (bloqueantes vs avisos), OIDC vs Service Principal con secret, smoke con tres umbrales y auto-rollback con `condition: failed()`.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Preflight de requisitos antes de empezar (slide 3) | [`PreflightChecker.cs`](src/Practica.Pipeline.Demo.Api/Pipeline/PreflightChecker.cs) |
| Esqueleto del pipeline canónico (Build → Staging → Swap, slides 4-6, 10, 17, 18) | [`PipelineStageBuilder.cs`](src/Practica.Pipeline.Demo.Api/Pipeline/PipelineStageBuilder.cs) |
| Evaluador de smoke test → continuar / rollback (slide 5/6/10) | [`SmokeTestEvaluator.cs`](src/Practica.Pipeline.Demo.Api/Pipeline/SmokeTestEvaluator.cs) |
| Plan + checklist de la práctica (slide 11) | [`IPracticaPipelinePlanner.cs`](src/Practica.Pipeline.Demo.Api/Pipeline/IPracticaPipelinePlanner.cs) |
| API que expone la lógica (`/pipeline/*`) | [`PipelineEndpoints.cs`](src/Practica.Pipeline.Demo.Api/Endpoints/PipelineEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Qué construye la práctica (8 pasos del pipeline) | 2 | `IPracticaPipelinePlanner.Checklist` |
| Preflight (OIDC, plan S1+, slot, accesos) | 3 | `PreflightChecker.Comprobar` |
| Stage Build + Test + Publish artifact | 4 | `PipelineStageBuilder` → etapa "Build" |
| Stage Deploy Staging + smoke test | 5 | `PipelineStageBuilder` → "DeployStaging" + `SmokeTestEvaluator` |
| Stage Swap a Producción con aprobación | 6 | `PipelineStageBuilder` → "SwapProduction" `RequiereAprobacion=true` |
| Configurar pipeline + environments en ADO | 7 | `Checklist` |
| Pipeline observability + métricas DORA | 9 | (mapeado en `Checklist`) |
| Auto-rollback si el smoke test falla | 10 | `SmokeTestEvaluator.Decision == RollbackNecesario` y "Auto-rollback" en el stage |
| Checklist final de la práctica | 11 | `IPracticaPipelinePlanner.Checklist` |
| Security scanning (`dotnet list package --vulnerable`) | 15 | `OpcionesPipeline.EscanearVulnerables` añade stage "SecurityScan" |
| Pipeline OIDC federado sin passwords | 17 | `OpcionesPipeline.UsarOidc` cambia el tipo de tarea de deploy |
| Equivalente GitHub Actions (mismo deploy) | 18 | `OpcionesPipeline.Plataforma = GitHubActions` |

## Estructura

```
S8.P-practica-pipeline-cicd/
├── src/Practica.Pipeline.Demo.Api/
│   ├── Pipeline/   PreflightChecker, PipelineStageBuilder,
│   │              SmokeTestEvaluator
│   │              + IPracticaPipelinePlanner/PracticaPipelinePlanner
│   ├── Endpoints/  PipelineEndpoints (/health, /pipeline/*)
│   └── Program.cs  AddSingleton<IPracticaPipelinePlanner> + enums por nombre
├── tests/Practica.Pipeline.Demo.Api.Tests/
│   ├── Unit_*                lógica pura (preflight, stages, smoke)
│   ├── DiContainer_Tests     resuelve IPracticaPipelinePlanner (contenedor real)
│   └── Api_PipelineTests     E2E vía WebApplicationFactory
└── scripts/        preflight + smoke contra slot real (SOLO LECTURA, az CLI)
```

## Tests

```bash
dotnet test     # 32 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `PreflightChecker` (sin slot/sin plan S1 son bloqueantes; sin
    OIDC/sin az CLI son avisos; reporte agrega 3 bloqueantes si
    faltan org+push+sub).
  - `PipelineStageBuilder` (Build incluye restore+build+test+publish;
    SwapProduction `RequiereAprobacion=true` por defecto; auto-rollback
    añade paso de swap inverso; `EscanearVulnerables` inserta
    SecurityScan ANTES del deploy; ADO usa `AzureWebApp@1`, GHA usa
    `actions/setup-dotnet`+`actions/upload-artifact`; OIDC menciona
    Workload Identity Federation).
  - `SmokeTestEvaluator` (200+latencia baja+0% errores → Continuar;
    503 → RollbackNecesario; latencia > umbral → Rollback; error rate
    > 1% → Rollback; umbrales custom permiten latencia más alta;
    múltiples fallos reportan múltiples razones).
- **CAPA 0 · DI**: resuelve `IPracticaPipelinePlanner` del contenedor
  real (`Assert.Same` singleton) y compone preflight + esqueleto +
  smoke + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/pipeline/{preflight, etapas, smoke, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: el pipeline real corre en
> ADO/GHA; no se puede invocar `az pipelines run` en el test (necesita
> token y consume minutos). Lo testeable son las **piezas
> decisorias** — esas se cubren con CAPA 1 + E2E. El alumno valida el
> pipeline manualmente siguiendo `Checklist` y los scripts `az` solo
> leen el estado del slot.

## Ejecución local

```bash
dotnet run --project src/Practica.Pipeline.Demo.Api
# http://localhost:5111  — usa src/Practica.Pipeline.Demo.Api/api.http
```

- `/pipeline/preflight` clasifica los requisitos en OK / Aviso /
  Bloqueante y devuelve `listoParaArrancar`.
- `/pipeline/etapas` devuelve la secuencia canónica de stages (con
  pasos clave) para ADO o GitHub Actions, con knobs (OIDC, aprobación,
  auto-rollback, security scan, Teams).
- `/pipeline/smoke` decide deploy OK / rollback con umbrales
  configurables.
- `/pipeline/plan` compone todo + checklist de 12 puntos (slide 11
  enriquecido con OIDC, auto-rollback y security scanning).

## Verificar prerequisitos en Azure real (scripts)

```bash
./scripts/demo.sh
# 1) 01-preflight.sh           → comprueba plan S1+, slot staging, deploys recientes
# 2) 02-smoke-test.sh staging  → health 200 + latencia < 2s sobre 10 reqs
# 3) 02-smoke-test.sh production → smoke contra slot prod (post-swap)
```

**Solo lectura**: nunca crea ni modifica recursos. El smoke test
emula el step 5 del slide 5 (el del pipeline real), no un canary
agresivo — fallar significa rollback inmediato.

## Despliegue por Portal (entregable, 8 pasos del slide 2)

1. **Crear `azure-pipelines.yml`** en la raíz del repo (slide 4).
2. **Pipeline en ADO** → New Pipeline → Existing YAML (slide 7).
3. **Service Connection con OIDC**: Project Settings → Service
   connections → New → Azure RM → Workload Identity federation (slide
   3/17). Sin passwords, sin rotación.
4. **Environment `staging`** sin aprobación (slide 5/7).
5. **Environment `production`** con required reviewers (tech lead +
   on-call) (slide 6/7).
6. **Push a main** → el pipeline arranca solo (slide 8).
7. **Aprobar** el swap a producción cuando el smoke test del slot
   esté en verde (slide 7).
8. **Verificar** `/health == 200` post-swap; si falla, auto-rollback
   ejecuta swap inverso (slide 10).

## Ideas centrales

> Un pipeline CI/CD bien hecho tiene **tres puertas**: build verde
> (slide 4) — sin tests pasando no se publica; smoke test del slot
> staging (slide 5) — sin /health 200 no hay swap; **aprobación
> humana** + post-swap health (slide 6/7) — el último click es de una
> persona. **OIDC > Service Principal con secret** (slide 17): los
> tokens duran 1 hora, se renuevan solos y no hay nada que rotar.
> **Auto-rollback es la diferencia** entre "deploy con miedo" y
> "deploy con confianza" (slide 10): si el smoke test post-swap falla,
> el pipeline ejecuta el swap inverso automáticamente — MTTR pasa de
> 30-60 min a <2 min.

## Próximo paso

[`S8.P2 — Práctica GitHub Actions + publish profile`](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.P2-practica-github-actions-publish-profile-v1.md):
el mismo pipeline pero en GitHub Actions con publish profile (sin
OIDC, para repos personales o forks).
