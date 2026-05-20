# S8.1 — Azure DevOps: Repos, Boards y Artifacts

> **Submódulo de referencia:** [M08-S8.1](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.1-azure-devops-repos-boards-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (Azure DevOps gratis para equipos < 5; 6 €/usuario/mes si más, solo si usas Pipelines o Test Plans)

> 🎓 **Primer submódulo de M08 — DevOps y Automatización.** Submódulo
> conceptual: el valor docente es la **decisión** (monorepo vs
> multi-repo, branch policies de `main`, Conventional Commits) más
> que el setup en sí.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Parser de Conventional Commits + vínculo a work items (slides 7, 12) | [`ConventionalCommitParser.cs`](src/Devops.Repos.Demo.Api/Repos/ConventionalCommitParser.cs) |
| Branch policies de `main`: mínimas + recomendadas + evaluar (slides 5, 20) | [`BranchPolicyAdvisor.cs`](src/Devops.Repos.Demo.Api/Repos/BranchPolicyAdvisor.cs) |
| Monorepo vs multi-repo (slide 3) | [`RepoStrategyAdvisor.cs`](src/Devops.Repos.Demo.Api/Repos/RepoStrategyAdvisor.cs) |
| Plan + checklist del entregable | [`IRepoBoardsPlanner.cs`](src/Devops.Repos.Demo.Api/Repos/IRepoBoardsPlanner.cs) |
| API que expone la lógica (/devops/*) | [`DevopsEndpoints.cs`](src/Devops.Repos.Demo.Api/Endpoints/DevopsEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Monorepo vs multi-repo | 3 | `RepoStrategyAdvisor.Recomendar` |
| Trunk-based development | 4 | `IRepoBoardsPlanner.Checklist` |
| Branch policies en `main` | 5 | `BranchPolicyAdvisor.Minimas` |
| Pull Request flow | 6 | `IRepoBoardsPlanner.Checklist` |
| Conventional Commits | 7 | `ConventionalCommitParser.Parsear` |
| Boards: jerarquía Epic→Feature→Story→Task | 9 | `IRepoBoardsPlanner.Checklist` |
| Sprints + velocity | 10 | `IRepoBoardsPlanner.Checklist` |
| Vincular work items con commits/PRs | 12 | `ConventionalCommitParser.WorkItems` |
| Artifacts: feed NuGet privado | 13 | scripts `01-inventory-devops.sh` |
| Seguridad: PAT + permissions mínimas | 15 | `IRepoBoardsPlanner.Checklist` |
| Branch Protection Rules avanzadas | 20 | `BranchPolicyAdvisor.Recomendadas` |

## Estructura

```
S8.1-azure-devops-repos-boards/
├── src/Devops.Repos.Demo.Api/
│   ├── Repos/      ConventionalCommitParser, BranchPolicyAdvisor,
│   │               RepoStrategyAdvisor
│   │               + IRepoBoardsPlanner/RepoBoardsPlanner
│   ├── Endpoints/  DevopsEndpoints (/health, /devops/*)
│   └── Program.cs  AddSingleton<IRepoBoardsPlanner> + enums por nombre
├── tests/Devops.Repos.Demo.Api.Tests/
│   ├── Unit_*             lógica pura (commit, branch policy, repo)
│   ├── DiContainer_Tests  resuelve IRepoBoardsPlanner (contenedor real)
│   └── Api_DevopsTests    E2E vía WebApplicationFactory
└── scripts/        az devops solo lectura (inventario del project)
```

## Tests

```bash
dotnet test     # 34 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `ConventionalCommitParser` (los 10 tipos de la
  slide 7, scope opcional, breaking change con `!`, work items
  `#NNNN` deduplicados y ordenados, solo se valida el encabezado);
  `BranchPolicyAdvisor` (mínimas vs recomendadas, faltantes
  detectadas); `RepoStrategyAdvisor` (7 personas + 5 servicios →
  MultiRepo; 3 personas + shared code → Monorepo; equipos
  independientes + CI/CD → MultiRepo; persona/servicio cero → throws).
- **CAPA 0 · DI**: resuelve `IRepoBoardsPlanner` del contenedor real
  (`Assert.Same` singleton) y compone estrategia + policies +
  checklist. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/devops/{commit/parsear,commit/tipos,branch-policy/*,repo/estrategia,plan}`.

> 🧠 **Sin CAPA de integración a propósito.** Azure DevOps no se
> emula localmente; la lógica de decisión y los parsers son lo que
> realmente se enseña. Las llamadas reales a `az devops` se ven en
> los scripts (lectura).

## Ejecución local

```bash
dotnet run --project src/Devops.Repos.Demo.Api
# http://localhost:5105  — usa src/Devops.Repos.Demo.Api/api.http
```

`/devops/commit/parsear` valida un commit y extrae tipo/scope/work
items; `/devops/branch-policy/evaluar` dice qué policies faltan;
`/devops/repo/estrategia` decide monorepo vs multi-repo;
`/devops/plan` compone el plan + checklist.

## Inventario del DevOps project (scripts `az`)

```bash
./scripts/demo.sh
# 1) 01-inventory-devops.sh → repos + branch policies en main +
#    work items del usuario + feeds de Artifacts
```

Necesita la extensión `azure-devops` de az CLI; el script la instala
automáticamente la primera vez (`az extension add --name azure-devops`).
Solo lectura, sin cleanup.

## Despliegue por Portal (entregable)

> ⚠️ **Coste:** Azure DevOps Basic es **gratis para los primeros 5
> usuarios** por organización; 6 €/usuario/mes a partir del 6.º.
> Repos/Boards/Artifacts gratis hasta 2 GB de Artifacts.

1. **Crear organización + project** en https://dev.azure.com.
2. **Crear repos** según la estrategia (slide 3): multi-repo para 5-10
   personas con varios servicios.
3. **Branch policies en `main`** (slide 5): RequiredReviewers ≥ 1,
   BuildExitoso, ResolucionDeComentarios, NoPushDirecto.
4. **Configurar Boards** con la jerarquía Epic → Feature → Story →
   Task / Bug (slide 9) y sprints de 2 semanas (slide 10).
5. **Conventional Commits** + vínculo a work items con `#NNNN`
   (slide 7/12).
6. **Artifacts feed privado** para librerías compartidas (slide 13).
7. **Verificar** (`./scripts/demo.sh`): repos + policies + work items
   + feeds.

## Ideas centrales

> Trunk-based con feature branches cortas, PR obligatorio con
> ≥1 reviewer, **Conventional Commits** (`feat/fix/docs/refactor/test/
> chore/perf/ci/build/style`) vinculados a work items con `#NNNN`.
> Para 5-10 personas: **multi-repo** (un repo por servicio + uno de
> infra + uno de shared); para equipos pequeños con mucho código
> compartido: monorepo. Branch policies de `main` no negociables:
> reviewers, build OK, resolución de comentarios, sin push directo.

## Próximo paso

[`S8.2 — Pipelines CI/CD YAML`](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.2-pipelines-cicd-yaml-v3.md):
pipelines de build y deploy como código.
