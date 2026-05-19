# S7.4 — ClickOnce vs MSIX: por qué migrar y qué cambia

> **Submódulo de referencia:** [M07-S7.4](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.4-clickonce-vs-msix-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (decisión pura; ningún recurso Azure ni instalador)

> 🎓 **Submódulo conceptual — inicio del bloque de distribución
> desktop Windows.** El valor docente es la **decisión de migración**:
> comparativa de formatos (ClickOnce / MSIX / MSI / winget), factores
> de migración (slide 18) y elección de certificado de firma (slide 8).
> Lógica pura, sin instaladores reales — el empaquetado real va en S7.5.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Matriz de características por formato (slides 4, 11, 26) | [`DistributionFormatComparator.cs`](src/Distribution.Demo.Api/Distribution/DistributionFormatComparator.cs) |
| Decisión de migración + escenario A/B/C (slides 12, 18) | [`MigrationDecisionAdvisor.cs`](src/Distribution.Demo.Api/Distribution/MigrationDecisionAdvisor.cs) |
| Certificado de firma por escenario (slide 8) | [`SigningCertAdvisor.cs`](src/Distribution.Demo.Api/Distribution/SigningCertAdvisor.cs) |
| Plan + checklist del entregable | [`IDistributionPlanner.cs`](src/Distribution.Demo.Api/Distribution/IDistributionPlanner.cs) |
| API que expone la decisión (/distribution/*) | [`DistributionEndpoints.cs`](src/Distribution.Demo.Api/Endpoints/DistributionEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| ClickOnce: cómo funciona y dónde duele | 3 | `DistributionFormatComparator.Matriz[ClickOnce]` |
| MSIX vs ClickOnce: tabla feature-by-feature | 4 | `DistributionFormatComparator.VentajasMsixSobreClickOnce` |
| Instalación en contenedor + desinstalación limpia | 5 | scripts `01-inventory-msix.ps1` |
| Sideloading con AppInstaller | 6 | `IDistributionPlanner.Checklist` |
| Certificado de firma por escenario | 8 | `SigningCertAdvisor.Recomendar` |
| Roadmap Microsoft (ClickOnce sin futuro) | 11 | `DistributionFormatComparator` (FuturoMicrosoft) |
| Escenarios de migración A/B/C | 12 | `MigrationDecisionAdvisor.RecomendarEscenario` |
| Decisión "¿migrar ahora o esperar?" | 18 | `MigrationDecisionAdvisor.DebeMigrar` |
| Matriz final ClickOnce/MSIX/MSI/winget | 26 | `DistributionFormatComparator.Soporta` |

## Estructura

```
S7.4-clickonce-vs-msix/
├── src/Distribution.Demo.Api/
│   ├── Distribution/  DistributionFormatComparator,
│   │                  MigrationDecisionAdvisor, SigningCertAdvisor
│   │                  + IDistributionPlanner/DistributionPlanner
│   ├── Endpoints/     DistributionEndpoints (/health, /distribution/*)
│   └── Program.cs     AddSingleton<IDistributionPlanner> + enums por nombre
├── tests/Distribution.Demo.Api.Tests/
│   ├── Unit_*             lógica pura (comparator, migration, cert)
│   ├── DiContainer_Tests  resuelve IDistributionPlanner (contenedor real)
│   └── Api_DistributionTests E2E vía WebApplicationFactory
└── scripts/         PowerShell (inventario local — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 30 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `DistributionFormatComparator` (ClickOnce no
  sandbox, MSIX sí; MSI requiere admin; winget hereda de MSIX; ≥7
  ventajas MSIX sobre ClickOnce); `MigrationDecisionAdvisor` (Intune +
  problemas → migrar; .NET 8 + cert caduca → migrar; indecisión cuando
  un solo driver compite con "funciona bien"; escenario A/B/C por
  inputs); `SigningCertAdvisor` (self-signed / Enterprise CA / Public
  CA / Microsoft Store por escenario, coste y SmartScreen).
- **CAPA 0 · DI**: resuelve `IDistributionPlanner` del contenedor real
  (`Assert.Same` singleton) y compone migración + escenario + cert +
  ventajas + checklist. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/distribution/{soporta,comparar,migrar,escenario,cert,plan}`.

> 🧠 **Sin CAPA de integración a propósito.** S7.4 es decisión: no hay
> ningún servicio Azure ni instalador real involucrado. Mismo criterio
> que M06 / S7.1–S7.3. El empaquetado real (firma + .msix + .appinstaller)
> se materializa en S7.5.

## Ejecución local

```bash
dotnet run --project src/Distribution.Demo.Api
# http://localhost:5099  — usa src/Distribution.Demo.Api/api.http
```

`/distribution/comparar?a=ClickOnce&b=Msix` lista feature por feature
quién gana; `/distribution/migrar` aplica los factores de la slide 18;
`/distribution/plan` compone el plan + checklist.

## Inventario local (entregable: ¿qué tengo en mi PC?)

> Distinto al resto de M07: aquí no hay Azure que verificar. Los
> scripts son **PowerShell** (`demo.ps1`) e inventarían lo que ya está
> instalado en este Windows.

```powershell
pwsh -File scripts/demo.ps1
# 1) Get-AppxPackage → MSIX/AppX instalados (slide 5/14)
# 2) %LocalAppData%\Apps\2.0\*.application → ClickOnce (slide 3)
```

Los dos inventarios son la base real de la decisión de migración: si
hay ClickOnce activo, escenario A (empaquetar tal cual) o B (.NET 8 +
MSIX). Si no hay ClickOnce, escenario C (app nueva directamente MSIX).

## Despliegue (no aplica)

S7.4 no despliega nada. La decisión de migración produce un **plan**
(ver `/distribution/plan` o `IDistributionPlanner.Checklist`); el
empaquetado y la firma se aplican en S7.5–S7.7 y en las prácticas
S7.P/S7.P2.

## Ideas centrales

> ClickOnce funcionó, pero **Microsoft ya no lo evoluciona** (slide 11):
> sin .NET 8+, sin Intune, sin sandbox, sin firma moderna. **MSIX** es
> el formato estándar de Windows (contenedor, identidad, desinstalación
> limpia, AppInstaller, winget, Microsoft Store, Intune). La pregunta
> no es *si* migrar, sino *cuándo y cómo*: **empezar por apps nuevas
> directamente en MSIX** (escenario C) y migrar las existentes
> empaquetándolas tal cual (A) o modernizándolas a .NET 8+ (B). El
> certificado: **Enterprise CA** para distribución corporativa interna.

## Próximo paso

[`S7.5 — MSIX: empaquetado y distribución`](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.5-msix-empaquetado-distribucion-v3.md):
empaquetar realmente (manifest, capabilities, signing, AppInstaller).
