# S7.7 — Migración ClickOnce → MSIX

> **Submódulo de referencia:** [M07-S7.7](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.7-migracion-clickonce-msix-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (decisión + plan; el empaquetado/distribución reales viven en S7.5/S7.6)

> 🎓 **Submódulo conceptual.** La migración real toca el `.sln` y
> Visual Studio. El valor docente es la **lógica testeable**: mapper
> `.application` (ClickOnce) → `AppxManifest`, evaluación de
> compatibilidad contra los blockers de la slide 3, y **roadmap por
> fases con criterios de salida testeables** (solo se avanza si todos
> los criterios pasan).

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: la mudanza por fases como analogía, mapper ClickOnce → MSIX con normalización de versión, evaluador de compatibilidad (bloqueador/precaución/OK + PSF) y plan de coexistencia ClickOnce ≥ 4 semanas con rollback build+1.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Mapeo ClickOnce → AppxManifest (Identity, Publisher con CN=, Version 4 partes) (slides 6, 8) | [`ClickOnceManifestMapper.cs`](src/Migration.Demo.Api/Migration/ClickOnceManifestMapper.cs) |
| Roadmap por fases con criterios de salida (slides 2, 11) | [`MigrationRoadmap.cs`](src/Migration.Demo.Api/Migration/MigrationRoadmap.cs) |
| Compatibilidad con MSIX (blockers / PSF / OK) (slides 3, 12) | [`MigrationCompatibilityCheck.cs`](src/Migration.Demo.Api/Migration/MigrationCompatibilityCheck.cs) |
| Plan + checklist del entregable | [`IMigrationPlanner.cs`](src/Migration.Demo.Api/Migration/IMigrationPlanner.cs) |
| API que expone la lógica (/migracion/*) | [`MigrationEndpoints.cs`](src/Migration.Demo.Api/Endpoints/MigrationEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Estrategia por fases (4-6 semanas) | 2 | `MigrationRoadmap.Fases` |
| Pre-requisitos: comportamientos incompatibles | 3 | `MigrationCompatibilityCheck` |
| Método: WAP (recomendado) | 5 | `IMigrationPlanner.Checklist` |
| Configurar manifest (Identity / Publisher CN= / Version) | 6 | `ClickOnceManifestMapper.Mapear` |
| Migrar datos del usuario | 9, 14 | `IMigrationPlanner.Checklist` |
| Coexistencia ClickOnce + MSIX | 10, 15 | `IMigrationPlanner.Checklist` |
| Plan semana a semana | 11 | `MigrationRoadmap.SiguienteFase` |
| PSF para apps con HKLM / services | 12 | `MigrationCompatibilityCheck.RequierePsf` |
| Checklist completa | 13 | `IMigrationPlanner.Checklist` |
| Comunicación a usuarios | 16 | `IMigrationPlanner.Checklist` |
| Verificación post-migración | 17 | scripts `01-verify-migration.ps1` |
| Plan de rollback (ClickOnce activo ≥ 4 semanas) | 18 | `IMigrationPlanner.Checklist` |

## Estructura

```
S7.7-migracion-clickonce-msix/
├── src/Migration.Demo.Api/
│   ├── Migration/  ClickOnceManifestMapper, MigrationRoadmap,
│   │               MigrationCompatibilityCheck
│   │               + IMigrationPlanner/MigrationPlanner
│   ├── Endpoints/  MigrationEndpoints (/health, /migracion/*)
│   └── Program.cs  AddSingleton<IMigrationPlanner> + enums por nombre
├── tests/Migration.Demo.Api.Tests/
│   ├── Unit_*             lógica pura (mapper, roadmap, compatibility)
│   ├── DiContainer_Tests  resuelve IMigrationPlanner (contenedor real)
│   └── Api_MigrationTests E2E vía WebApplicationFactory
└── scripts/         PowerShell (verifica migración en este PC, slide 17)
```

## Tests

```bash
dotnet test     # 32 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `ClickOnceManifestMapper` (sanitiza Empresa+App,
  añade `CN=` si falta, **normaliza versión** completando ceros hasta
  4 partes, parsea `.application` con namespaces `asm.v2`,
  `runFullTrust` declarado en `rescap:`); `MigrationCompatibilityCheck`
  (WPF+filesystem+HTTP → OK; HKLM/Service → Precaución + PSF; kernel
  driver / writes a Program Files → Bloqueador; bloqueador gana sobre
  precaución); `MigrationRoadmap` (avanza solo si **todos** los
  criterios pasan; un criterio false bloquea; ModernizarDotNet8 es
  fase final).
- **CAPA 0 · DI**: resuelve `IMigrationPlanner` del contenedor real
  (`Assert.Same` singleton) y compone manifest + compatibilidad + fase
  + checklist. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/migracion/{mapear,parsear,compatibilidad,fase,siguiente-fase,plan}`.

> 🧠 **Sin CAPA de integración a propósito.** La migración real toca
> el `.sln`, Visual Studio, certificados y publicación a Azure Blob
> (todo lo del S7.5/S7.6). El valor docente aquí está en la decisión —
> qué cambia, qué bloquea, cuándo avanzar — y eso es lógica pura.

## Ejecución local

```bash
dotnet run --project src/Migration.Demo.Api
# http://localhost:5102  — usa src/Migration.Demo.Api/api.http
```

`/migracion/mapear` convierte un `assemblyIdentity` ClickOnce a un
manifest MSIX listo para WAP (Identity sanitizado, Publisher con CN=,
Version 4 partes); `/migracion/compatibilidad` clasifica el riesgo;
`/migracion/siguiente-fase` solo avanza si TODOS los criterios pasan;
`/migracion/plan` compone el plan + checklist.

## Verificación local (PowerShell, slide 17)

```powershell
pwsh -File scripts/demo.ps1
# 1) 01-verify-migration.ps1 -IdentityName MiEmpresa.VentasDesktop
#    → ¿está el MSIX instalado? ¿queda ClickOnce residual?
#    → ¿hay marker .clickonce-migrated en LocalState?
```

Solo lectura. No instala ni desinstala — el alumno desinstala ClickOnce
manualmente desde el Panel de control si el script lo detecta.

## Despliegue (no aplica — produce un plan)

S7.7 no despliega: produce el **plan de migración** y la decisión "¿se
puede empezar?" (¿hay bloqueadores?). El despliegue real se hace con
los entregables de **S7.5** (empaquetado) y **S7.6** (auto-update).

## Ideas centrales

> Migrar **no es big-bang**: 4-6 semanas en 4 fases (Empaquetado →
> Piloto → Rollout → opcional Modernizar a .NET 8). El proyecto WPF
> **no se toca**: solo se añade un WAP. **Identity.Name** debe ser
> `Empresa.AppName`; **Publisher** debe llevar `CN=` y coincidir con
> el Subject del certificado; **Version** se completa a 4 partes.
> Antes de empezar, comprobar bloqueadores (drivers de kernel,
> escrituras a `C:\Program Files`); HKLM y services se pueden con PSF
> pero añaden complejidad. **Coexistencia ClickOnce + MSIX** durante
> la transición. **No apagar ClickOnce** hasta tener todos los
> usuarios en MSIX y 1 semana sin incidencias.

## Próximo paso

[`S7.P — Práctica MSIX`](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.P-practica-msix-v3.md):
materializa el roadmap end-to-end en una app WPF de prueba.
