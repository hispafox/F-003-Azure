# S8.2 — Pipelines CI/CD en YAML (Azure DevOps)

> **Submódulo de referencia:** [M08-S8.2](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.2-pipelines-cicd-yaml-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € en local; Azure Pipelines da 1.800 min/mes gratis con MS-hosted

> 🎓 **Submódulo conceptual.** El pipeline real corre en Azure DevOps;
> aquí se modela la **lógica testeable** del azure-pipelines.yml:
> parseamos la jerarquía stages/jobs/steps, validamos su estructura
> (slide 5) y recomendamos los bloques de `trigger:` por escenario
> (slide 4). **Primera excepción a "sin packages" de M07:** este
> submódulo usa **YamlDotNet 16** porque el YAML es el centro de la
> lección y un parser hand-rolled sería ruido.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Parser del `azure-pipelines.yml` a DTO (slide 3, 5, 7, 8) | [`PipelineYamlParser.cs`](src/Pipelines.Demo.Api/Pipelines/PipelineYamlParser.cs) |
| Validación estructural: stages/jobs/steps, dependsOn, environment, tests (slides 5/6/7/8) | [`PipelineStructureValidator.cs`](src/Pipelines.Demo.Api/Pipelines/PipelineStructureValidator.cs) |
| Recomendador de `trigger:` por escenario (slide 4) | [`TriggerAdvisor.cs`](src/Pipelines.Demo.Api/Pipelines/TriggerAdvisor.cs) |
| Plan + checklist del entregable | [`IPipelinePlanner.cs`](src/Pipelines.Demo.Api/Pipelines/IPipelinePlanner.cs) |
| API que expone la lógica (/pipeline/*) | [`PipelinesEndpoints.cs`](src/Pipelines.Demo.Api/Endpoints/PipelinesEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Pipeline as Code (YAML vs Classic) | 2 | `IPipelinePlanner.Checklist` |
| Anatomía: trigger / pool / variables / stages | 3 | `PipelineYamlParser.Parsear` |
| Triggers: branch / PR / cron / `none` | 4 | `TriggerAdvisor.Recomendar` |
| Jerarquía stages → jobs → steps | 5 | `PipelineStructureValidator` |
| CI completo .NET (build + test + publish + coverage) | 6 | `PipelineStructureValidator.TieneStepDeTest` |
| CD: deploy a staging + swap | 7 | api.http / checklist |
| Environments + aprobaciones | 8 | `PipelineStructureValidator` (deployment + environment) |
| Variable Groups + Key Vault | 9 | `PipelineDef.VariableGroups` |
| Templates / conditions / outputs | 12-14 | `PipelineDef.Stages[].Condition` |
| Service Connections + OIDC | 15, 22 | `IPipelinePlanner.Checklist` |
| Caching | 16 | `IPipelinePlanner.Checklist` |

## Estructura

```
S8.2-pipelines-cicd-yaml/
├── src/Pipelines.Demo.Api/
│   ├── Pipelines/  PipelineDef (DTO), PipelineYamlParser (YamlDotNet),
│   │               PipelineStructureValidator, TriggerAdvisor
│   │               + IPipelinePlanner/PipelinePlanner
│   ├── Endpoints/  PipelinesEndpoints (/health, /pipeline/*)
│   └── Program.cs  AddSingleton<IPipelinePlanner> + enums por nombre
├── tests/Pipelines.Demo.Api.Tests/
│   ├── Unit_*             lógica pura (parser, validator, advisor)
│   ├── DiContainer_Tests  resuelve IPipelinePlanner (contenedor real)
│   └── Api_PipelinesTests E2E vía WebApplicationFactory
└── scripts/         az pipelines list solo lectura
```

## Tests

```bash
dotnet test     # 29 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `PipelineYamlParser` (trigger branches +
  vmImage, conteo de stages/jobs/steps, `trigger: none`, **deployment
  jobs con `strategy.runOnce.deploy.steps`**, dependsOn, variable
  groups, schedules cron, YAML inválido lanza `FormatException`);
  `PipelineStructureValidator` (pipeline correcto OK, sin stages
  falla, dependsOn a stage inexistente error, job sin steps error,
  deployment sin environment error, job normal con env "production"
  → aviso, falta step de tests → aviso); `TriggerAdvisor` (CI/PR/
  nightly/manual, recomendación estándar de 3 bloques).
- **CAPA 0 · DI**: resuelve `IPipelinePlanner` del contenedor real
  (`Assert.Same` singleton) y planifica desde YAML. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/pipeline/{parsear,validar,trigger/{recomendado,estandar},plan}`.

> 🧠 **Sin CAPA de integración a propósito.** Lanzar un pipeline real
> en Azure DevOps requiere organización + service connection. La
> lógica de validación y recomendación, que es lo que se enseña, es
> pura.

## Ejecución local

```bash
dotnet run --project src/Pipelines.Demo.Api
# http://localhost:5106  — usa src/Pipelines.Demo.Api/api.http
```

`/pipeline/parsear` te enseña la estructura interna; `/pipeline/validar`
caza los errores (dependsOn roto, deployment sin environment) y avisa
(falta test, environment de prod en job normal); `/pipeline/trigger/
estandar` genera los 3 bloques YAML típicos (CI + PR + nightly).

## Inventario del proyecto (scripts `az`)

```bash
./scripts/demo.sh
# 1) 01-inventory-pipelines.sh → pipelines + últimas 5 runs por
#    pipeline + environments. Solo lectura.
```

Igual que S8.1: instala la extensión `azure-devops` automáticamente
la primera vez. No lanza ni cancela runs.

## Despliegue por Portal (entregable)

1. **Crear `azure-pipelines.yml`** en la raíz del repo.
2. **Pipelines → New pipeline → Existing Azure Pipelines YAML file**.
3. **Triggers** (slide 4): `trigger:` para CI en main, `pr:` para
   validar PRs.
4. **CI** (slide 6): `dotnet restore/build/test/publish` + cobertura.
5. **CD** (slide 7-8): deploy a slot staging → environment de
   producción con aprobación manual → swap.
6. **Variable Groups** linked a Key Vault para secretos (slide 9).
7. **Service Connection** con OIDC/Federated Identity (slide 15/22).
8. **Verificar** con `./scripts/demo.sh`: pipelines + runs + envs.

## Ideas centrales

> El pipeline ES código: vive en el repo, se versiona, se revisa en
> PRs como el resto. La **jerarquía mínima** es stages → jobs → steps;
> los deploys usan jobs `deployment:` con `environment:` para que el
> entorno pueda aplicar approvals (slide 8). El **error #1**: stage
> de Build sin `dotnet test` (el validador avisa). El **error #2**:
> `dependsOn` apuntando a un stage que no existe (error). El
> **error #3**: job normal con `environment: production` — no podrá
> usar approvals (aviso). Para los secretos, **Variable Groups
> linked a Key Vault**; para la auth, **OIDC** (sin secretos
> persistentes — slide 22).

## Próximo paso

[`S8.3 — Despliegue automatizado`](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.3-despliegue-automatizado-v3.md):
estrategias de release (blue/green, canary, ring-based) y rollback.
