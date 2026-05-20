# M08 — DevOps y Automatización · ejemplos

Ejemplos de código que acompañan al
[Módulo 8 — DevOps y Automatización](../../doc/M08-DevOps-Automatizacion).

Cambia el dominio respecto a M07 (que era distribución desktop): aquí
volvemos a Azure y la nube — **Azure DevOps Repos/Boards/Artifacts,
pipelines YAML CI/CD, IaC con Bicep y Application Insights**. El
patrón M07 (conceptual + lección 9) sigue siendo la base, pero algunos
submódulos pueden tener integración real cuando hay algo emulable que
aporta valor (Bicep `what-if` local, parser de KQL, validación de
YAML de pipeline) — se reevalúa por submódulo.

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| [S8.1](../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.1-azure-devops-repos-boards-v3.md) | Azure DevOps: Repos, Boards, Artifacts | [`S8.1-azure-devops-repos-boards/`](S8.1-azure-devops-repos-boards/README.md) | ✅ Disponible |
| [S8.2](../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.2-pipelines-cicd-yaml-v3.md) | Pipelines CI/CD YAML (parser, validador, triggers) | [`S8.2-pipelines-cicd-yaml/`](S8.2-pipelines-cicd-yaml/README.md) | ✅ Disponible |
| [S8.3](../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.3-despliegue-automatizado-v3.md) | Despliegue automatizado (estrategia, health, rollback) | [`S8.3-despliegue-automatizado/`](S8.3-despliegue-automatizado/README.md) | ✅ Disponible |
| [S8.4](../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.4-ado-vs-github-actions-v3.md) | ADO vs GitHub Actions (decisión, equivalencias YAML, coste) | [`S8.4-ado-vs-github-actions/`](S8.4-ado-vs-github-actions/README.md) | ✅ Disponible |
| [S8.5](../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.5-iac-bicep-v3.md) | IaC con Bicep (linter, what-if, integración `bicep build`) | [`S8.5-iac-bicep/`](S8.5-iac-bicep/README.md) | ✅ Disponible |
| [S8.6](../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.6-app-insights-monitor-v3.md) | Application Insights + monitoring (KQL, alertas, parser de respuesta) | [`S8.6-app-insights-monitor/`](S8.6-app-insights-monitor/README.md) | ✅ Disponible |
| [S8.P](../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.P-practica-pipeline-cicd-v3.md) | Práctica — Pipeline CI/CD (preflight, stages, smoke + auto-rollback) | [`S8.P-practica-pipeline-cicd/`](S8.P-practica-pipeline-cicd/README.md) | ✅ Disponible |
| [S8.P2](../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.P2-practica-github-actions-publish-profile-v1.md) | Práctica — GitHub Actions + publish profile (parser, workflow, auth) | [`S8.P2-practica-github-actions-publish-profile/`](S8.P2-practica-github-actions-publish-profile/README.md) | ✅ Disponible |

✅ **Módulo M08 completo** (6 submódulos + 2 prácticas, 8/8).

> 📘 **Manuales del alumno disponibles** (S8.1–S8.5):
> [S8.1](S8.1-azure-devops-repos-boards/MANUAL.md) ·
> [S8.2](S8.2-pipelines-cicd-yaml/MANUAL.md) ·
> [S8.3](S8.3-despliegue-automatizado/MANUAL.md) ·
> [S8.4](S8.4-ado-vs-github-actions/MANUAL.md) ·
> [S8.5](S8.5-iac-bicep/MANUAL.md).
> S8.6, S8.P y S8.P2 pendientes. Cada `MANUAL.md` complementa al `README.md` técnico del ejemplo explicando el *para qué*, las decisiones y la puesta en marcha guiada para el alumno.

## Patrón de tests

- **CAPA 1 · Unit**: la lógica de decisión / parsing como funciones
  puras (commit parsing, branch policies, repo strategy, validación
  de YAML/Bicep, KQL parser).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve el grafo real
  (cubre la [lección DI de M03-S3.4](../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md))
  — corre sin Docker.
- **CAPA E2E**: la API completa vía `WebApplicationFactory`.
- **Integración**: solo donde haya algo emulable que aporte
  (`bicep build`/`what-if`, validadores de YAML, etc.); en submódulos
  cuyo valor son **decisiones** (estrategia ADO vs GitHub Actions,
  estructura de pipelines) **no** se fuerza una CAPA de integración.

## Requisitos comunes

- .NET SDK 10
- (Para despliegues) suscripción de Azure + Portal
- (Para algunos submódulos) Azure DevOps organization o cuenta GitHub
