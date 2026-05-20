# S7.6 — MSIX: auto-update, canales y rollback

> **Submódulo de referencia:** [M07-S7.6](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.6-msix-auto-update-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (el `.appinstaller` lo sirve Azure Blob — slide 8 de S7.5)

> 🎓 **Submódulo conceptual.** El `.appinstaller` real lo sirve Azure
> Blob y lo aplica la API de Windows. El valor docente es la **lógica
> testeable**: builder/parser del XML, política de canary release y
> comparación de versiones (incluido rollback al estilo "republicar la
> previa con build+1", slide 8).

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Builder + parser del `.appinstaller` (slides 2-3, 13) | [`AppInstallerBuilder.cs`](src/AutoUpdate.Demo.Api/AutoUpdate/AppInstallerBuilder.cs) |
| Canary rollout: cohortes deterministas, etapas 5/25/50/100, canales (slides 10, 20, 25) | [`CanaryRolloutPolicy.cs`](src/AutoUpdate.Demo.Api/AutoUpdate/CanaryRolloutPolicy.cs) |
| Comparación de versiones, obligatoriedad, rollback (slides 7, 8, 13) | [`UpdateVersionAdvisor.cs`](src/AutoUpdate.Demo.Api/AutoUpdate/UpdateVersionAdvisor.cs) |
| Plan + checklist del entregable | [`IAutoUpdatePlanner.cs`](src/AutoUpdate.Demo.Api/AutoUpdate/IAutoUpdatePlanner.cs) |
| API que expone la lógica (/update/*) | [`AutoUpdateEndpoints.cs`](src/AutoUpdate.Demo.Api/Endpoints/AutoUpdateEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| AppInstaller XML + UpdateSettings | 2-3 | `AppInstallerBuilder.Construir` |
| Flujo de comprobación al abrir / background | 4 | endpoint `/update/comparar` |
| Versionado: nueva DEBE ser mayor | 7 | `UpdateVersionAdvisor.Comparar` |
| Rollback (republicar previa con build+1) | 8 | `UpdateVersionAdvisor.PlanificarRollback` |
| Canales dev / beta / stable | 10 | `CanaryRolloutPolicy.AppInstallerUri` |
| Telemetría de versión por usuario | 12 | scripts `02-installed-versions.ps1` |
| Forzar actualización obligatoria (`UpdateBlocksActivation`) | 13 | `IAutoUpdatePlanner.Planificar` |
| Health checks post-update | 17, 21 | `IAutoUpdatePlanner.Checklist` |
| Staged rollout 5% → 25% → 50% → 100% | 20 | `CanaryRolloutPolicy.SiguienteEtapa` |
| Anti-patterns (big-bang, sin rollback, sin telemetry) | 24 | `IAutoUpdatePlanner.Checklist` |
| AppInstaller deprecation roadmap 2026 | 18 | `IAutoUpdatePlanner.Checklist` |

## Estructura

```
S7.6-msix-auto-update/
├── src/AutoUpdate.Demo.Api/
│   ├── AutoUpdate/  AppInstallerBuilder, CanaryRolloutPolicy,
│   │                UpdateVersionAdvisor
│   │                + IAutoUpdatePlanner/AutoUpdatePlanner
│   ├── Endpoints/   AutoUpdateEndpoints (/health, /update/*)
│   └── Program.cs   AddSingleton<IAutoUpdatePlanner> + enums por nombre
├── tests/AutoUpdate.Demo.Api.Tests/
│   ├── Unit_*             lógica pura (builder, canary, version)
│   ├── DiContainer_Tests  resuelve IAutoUpdatePlanner (contenedor real)
│   └── Api_AutoUpdateTests E2E vía WebApplicationFactory
└── scripts/         PowerShell (inspeccionar .appinstaller + versiones)
```

## Tests

```bash
dotnet test     # 37 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `AppInstallerBuilder` (incluye identidad +
  UpdateSettings + `UpdateBlocksActivation`; round-trip construir →
  parsear es equivalente); `CanaryRolloutPolicy` (cohorte
  determinista por SHA-256; **monotónico**: si entras en el 5%, entras
  en el 25%; etapas 5/25/50/100, salud KO → mantener; URLs por canal);
  `UpdateVersionAdvisor` (mayor → actualizar, igual no, menor bloqueada
  sin `ForceUpdateFromAnyVersion`, plan de rollback `previa+build+1`,
  historial desordenado, version mal formada lanza `ArgumentException`).
- **CAPA 0 · DI**: resuelve `IAutoUpdatePlanner` del contenedor real
  (`Assert.Same` singleton) y compone canal + UpdateSettings críticas +
  etapas + checklist. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/update/{appinstaller,parsear,canal,canary,siguiente-etapa,comparar,obligatoria,rollback,plan}`.

> 🧠 **Sin CAPA de integración a propósito.** El `.appinstaller` real
> lo sirve Azure Blob y lo aplica la API de Windows (`PackageManager`);
> el round-trip Windows-side no es reproducible sin MSIX firmado +
> Windows. El valor docente es la lógica de decisión, que es pura.
> Mismo criterio que el resto de M07.

## Ejecución local

```bash
dotnet run --project src/AutoUpdate.Demo.Api
# http://localhost:5101  — usa src/AutoUpdate.Demo.Api/api.http
```

`/update/appinstaller` construye el XML; `/update/canary` asigna
cohortes (mismo userId → misma cohorte siempre); `/update/rollback`
calcula el plan slide-8 (publicar la previa con etiqueta `build+1`);
`/update/plan` compone el plan completo + checklist.

## Inspección remota (PowerShell)

```powershell
pwsh -File scripts/demo.ps1
# 1) 01-inspect-appinstaller.ps1 → descarga e inspecciona un .appinstaller
# 2) 02-installed-versions.ps1   → versiones MSIX instaladas por Identity
```

`01` descarga el XML, extrae versión + UpdateSettings y avisa si
`UpdateBlocksActivation=true` (slide 13 — solo para releases críticas).
`02` lista qué versiones del paquete tienes en este PC (slide 12 —
fleet telemetry manual). Solo lectura, sin cleanup.

## Despliegue por Portal (entregable)

> ⚠️ **Coste:** hosting del `.appinstaller` + `.msix` en Azure Blob
> (~0,02 €/GB/mes, ver S7.5).

1. **Blob Storage** con tres "contenedores virtuales" — `msix-stable/`,
   `msix-beta/`, `msix-dev/` — cada uno con su `.msix` y su
   `MiApp-{canal}.appinstaller` (slide 10).
2. **Pipeline CI/CD**: build → sign → upload `.msix` → actualizar el
   `.appinstaller` del canal con la nueva versión.
3. **Staged rollout** (slide 20): publicar primero al canal `beta`,
   observar 24 h, mover al `stable` con `5% → 25% → 50% → 100%`.
4. **Telemetría** (slide 12): App Insights con `Package.Current.Id.Version`
   en cada `AppStarted`.
5. **Health checks post-update** (slide 17/21): si `from != current`,
   ejecutar la checklist (backend connectivity, auth, data migration)
   y alertar si algo falla.
6. **Rollback** (slide 8): si la salud cae, **republicar la previa
   buena con build+1** y actualizar el `.appinstaller`.

## Ideas centrales

> El `.appinstaller` es el contrato del auto-update: dónde está el
> `.msix`, cada cuánto comprobar y si bloquea la activación. **Versiones
> incrementales** (Major.Minor.Build.Revision); el rollback más limpio
> (slide 8) es **republicar la previa con `build+1`** (la etiqueta sube
> pero el código es el bueno). **Canary determinista por SHA-256 del
> userId**: un usuario en el 5% sigue en el 25%/50%/100%. **Staged
> rollout**: no avanzar de etapa si la salud falla. **Mandatory updates
> con ≥ 24 h de aviso**, no big-bang. AppInstaller está en deprecation
> roadmap 2026 — empieza a planificar la migración a winget (slide 18).

## Próximo paso

[`S7.7 — Migración ClickOnce → MSIX`](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.7-migracion-clickonce-msix-v3.md):
plan paso a paso para mover una app legacy.
