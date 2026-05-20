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
| S8.2 | Pipelines YAML CI/CD | — | ⏳ Pendiente |
| S8.3 | Despliegue automatizado | — | ⏳ Pendiente |
| S8.4 | ADO vs GitHub Actions | — | ⏳ Pendiente |
| S8.5 | IaC con Bicep | — | ⏳ Pendiente |
| S8.6 | Application Insights + monitoring | — | ⏳ Pendiente |
| S8.P | Práctica — Pipeline CI/CD | — | ⏳ Pendiente |
| S8.P2 | Práctica — GitHub Actions + publish profile | — | ⏳ Pendiente |

⏳ **Módulo M08 en construcción** (1/8).

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
