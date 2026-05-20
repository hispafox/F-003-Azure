# S7.P2 — Práctica: empaquetar MSIX con el wizard de Visual Studio

> **Submódulo de referencia:** [M07-S7.P2](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.P2-practica-msix-wizard-v1.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (todo local; el wizard de VS hace el trabajo)

> 🎓 **Práctica que cierra M07.** Versión "wizard" de la práctica
> principal (S7.P): cero CLI, todo desde la UI de Visual Studio, en
> ~30-45 min. El valor docente — y lo que aquí se materializa — es:
> (1) **qué comandos CLI ejecuta el wizard por debajo** (slide 15),
> (2) **catálogo de errores comunes** con código + causa + fix (slide 16),
> (3) **decisión Wizard vs CLI** según el contexto (slide 17).

> 📘 **¿Primera vez con esta práctica?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el cambio automático vs manual como analogía, los cuatro comandos CLI que el wizard ejecuta por debajo, el catálogo de los 6 errores típicos (con `0x80073CFD` a la cabeza) y la decisión Wizard vs CLI según el escenario.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Lo que el wizard hace por debajo: makeappx + signtool + Import-Certificate + Add-AppPackage (slide 15) | [`WizardComandosExpander.cs`](src/WizardMsix.Demo.Api/Wizard/WizardComandosExpander.cs) |
| Catálogo de los 6 errores típicos (slide 16) | [`MsixErrorTroubleshooter.cs`](src/WizardMsix.Demo.Api/Wizard/MsixErrorTroubleshooter.cs) |
| Decisión Wizard vs CLI + limitaciones del wizard (slides 15, 17) | [`WizardVsCliAdvisor.cs`](src/WizardMsix.Demo.Api/Wizard/WizardVsCliAdvisor.cs) |
| Plan + checklist del entregable (slide 19) | [`IPracticaMsixWizardPlanner.cs`](src/WizardMsix.Demo.Api/Wizard/IPracticaMsixWizardPlanner.cs) |
| API que expone la lógica (/wizard/*) | [`WizardEndpoints.cs`](src/WizardMsix.Demo.Api/Endpoints/WizardEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Qué construyes (WPF + WAP) | 2 | `PlanWizard.Checklist` |
| Pre-flight: VS 2022 + workloads + sideloading | 3 | scripts `01-check-vs-components.ps1` |
| Paso 1-7 del wizard (UI) | 4-10 | `IPracticaMsixWizardPlanner.Checklist` |
| Smoke tests automatizados | 11 | `IPracticaMsixWizardPlanner.Checklist` |
| Cambio + re-empaquetar v1.0.1.0 | 12 | `IPracticaMsixWizardPlanner.Checklist` |
| Cleanup: Remove-AppPackage + cert | 14 | scripts `02-cleanup.ps1` |
| Comparativa Wizard vs CLI | 15 | `WizardVsCliAdvisor.Recomendar` |
| 6 errores comunes con código/fix | 16 | `MsixErrorTroubleshooter.Diagnosticar` |
| Limitaciones del wizard | 17 | `WizardVsCliAdvisor.LimitacionesWizard` |
| Checklist 11-item | 19 | `IPracticaMsixWizardPlanner.Checklist` |

## Estructura

```
S7.P2-practica-msix-wizard/
├── src/WizardMsix.Demo.Api/
│   ├── Wizard/     WizardComandosExpander, MsixErrorTroubleshooter,
│   │               WizardVsCliAdvisor
│   │               + IPracticaMsixWizardPlanner/PracticaMsixWizardPlanner
│   ├── Endpoints/  WizardEndpoints (/health, /wizard/*)
│   └── Program.cs  AddSingleton<IPracticaMsixWizardPlanner>
├── tests/WizardMsix.Demo.Api.Tests/
│   ├── Unit_*             lógica pura (comandos, troubleshooter, decisión)
│   ├── DiContainer_Tests  resuelve IPracticaMsixWizardPlanner (real)
│   └── Api_WizardTests    E2E vía WebApplicationFactory
└── scripts/         PowerShell (preflight VS + cleanup interactivo)
```

## Tests

```bash
dotnet test     # 31 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `WizardComandosExpander` (4 herramientas en orden,
  `/fd SHA256` en signtool, `.cer` derivado del `.pfx`, `TrustedPeople`
  en Import-Certificate); `MsixErrorTroubleshooter` (lookup exacto y por
  contención: `0x80073CFD: ...` → entrada `0x80073CFD`; código
  desconocido → null; las 6 entradas tienen causa+diagnóstico+fix);
  `WizardVsCliAdvisor` (cualquier factor "senior" — CI/CD, Key Vault,
  multi-arch, equipo grande, distrib corporativa — empuja a CLI;
  aprendizaje + simple → Wizard; sin señales → Wizard por defecto).
- **CAPA 0 · DI**: resuelve `IPracticaMsixWizardPlanner` del contenedor
  real (`Assert.Same` singleton); dos casos: aprendizaje → Wizard;
  CI/CD + Key Vault → CLI. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/wizard/{expandir,elegir,limitaciones,troubleshoot,errores,plan}`.
  El troubleshoot con código desconocido devuelve **404**.

> 🧠 **Sin CAPA de integración a propósito.** El wizard real lo ejecuta
> Visual Studio (UI). Aquí modelamos lo que esa UI esconde para que el
> alumno pueda diagnosticar errores y decidir cuándo pasarse a CLI.

## Ejecución local

```bash
dotnet run --project src/WizardMsix.Demo.Api
# http://localhost:5104  — usa src/WizardMsix.Demo.Api/api.http
```

`/wizard/expandir` muestra los 4 comandos CLI que el wizard ejecuta por
ti; `/wizard/troubleshoot?codigoOMensaje=0x80073CFD` da el fix para el
error #1 de la práctica; `/wizard/elegir` decide Wizard vs CLI;
`/wizard/plan` compone el plan + checklist.

## Pre-flight y cleanup (PowerShell)

```powershell
pwsh -File scripts/demo.ps1
# 1) 01-check-vs-components.ps1 → SOLO LECTURA, verifica VS 2022 +
#    "Windows Application Packaging Tools" + workload .NET desktop (slide 3)
# 2) 02-cleanup.ps1 -PackageName MiPrimeraMSIX.Package
#       -CertSubjectContiene MsixDemo
#    → INTERACTIVO con confirmación: Remove-AppPackage + borra cert (slide 14)
```

> ⚠️ `02-cleanup.ps1` es la **única excepción de M07** a la regla
> "scripts solo lectura": necesita borrar el cert y el paquete del PC
> del alumno al cerrar la práctica. Por eso pide confirmación antes
> de cada borrado y no es automático.

## Despliegue por Portal (entregable: el wizard de VS)

> ⚠️ **Coste:** 0 € — todo es local. Si después quieres subir el
> `.msix` a Azure Blob, ver S7.5/S7.6.

1. **Crear WPF mínima** en VS 2022 (slide 4).
2. **Add → New Project → Windows Application Packaging Project**
   (slide 5). Set as Startup.
3. **Inspeccionar `Package.appxmanifest`** (slide 6) — el wizard lo
   genera con `Empresa.App` Identity, `CN=` Publisher de prueba.
4. **Right-click Packaging → Publish → Create App Packages**:
   Sideloading → Generate self-signed → Build (slide 7).
5. **Instalar el cert** generado en `Cert:\LocalMachine\TrustedPeople`
   (slide 8) y **Add-AppPackage** el `.msix` (slide 9).
6. **App en Start Menu** → arranca → muestra versión (slide 10).
7. **Cambio + v1.0.1.0** → rebuild → reinstalar → in-place (slide 12).
8. **Cleanup** con `02-cleanup.ps1` (slide 14).

## Ideas centrales

> El wizard hace **lo mismo que el CLI** (`makeappx` + `signtool` +
> `Import-Certificate` + `Add-AppPackage`) pero desde la UI de VS — 0
> CLI, 0 conocimiento previo de manifests. Ideal para **aprendizaje
> inicial y apps simples**. **Sus límites** (slide 17): cert solo
> self-signed o de cert store (no Key Vault), un `.msix` por arch (no
> bundle), sin AppInstaller. Cuando aparezca alguno de los **6
> errores comunes** (slide 16) — `0x80073CFD`, `MSB3325`, `NotSigned`,
> etc. — el troubleshooter dice exactamente qué hacer. Para CI/CD,
> Key Vault o multi-arch: pasa a CLI (S7.P principal).

## Cierre de M07 ✅

Con S7.P2 se cierra el módulo M07 (Integración y MSIX) completo:

- **S7.1** Service Bus / Event Grid avanzado
- **S7.2** Diseño event-driven
- **S7.3** Azure API Management
- **S7.4** ClickOnce vs MSIX
- **S7.5** MSIX empaquetado y distribución
- **S7.6** MSIX auto-update
- **S7.7** Migración ClickOnce → MSIX
- **S7.P** Práctica MSIX end-to-end (CLI manual)
- **S7.P2** Práctica MSIX wizard (este)

**Siguiente módulo:** M08 — DevOps y Automatización (Azure DevOps,
pipelines YAML, IaC con Bicep, Application Insights).
