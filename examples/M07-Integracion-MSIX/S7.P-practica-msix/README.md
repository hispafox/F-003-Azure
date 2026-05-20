# S7.P — Práctica: empaquetar y publicar un MSIX end-to-end

> **Submódulo de referencia:** [M07-S7.P](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.P-practica-msix-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (todo local: WPF + cert self-signed + Add-AppxPackage)

> 🎓 **Práctica que cierra M07.** Integra empaquetado (S7.5),
> auto-update (S7.6) y manifest mapping (S7.7) en un flujo de **8 pasos
> guiados** (25-30 min según la slide 2; ~75-90 min realistas según
> slide 3): crear WPF + WAP, configurar manifest, firmar, instalar,
> simular actualización y configurar AppInstaller. El proyecto WPF real
> se construye en Visual Studio; aquí modelamos la **máquina de
> pasos + el check Publisher↔Cert + los artefactos canónicos**.

> 📘 **¿Primera vez con esta práctica?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: la receta de cocina con tiempos como analogía, los 8 pasos con criterios de validación, el error #1 (Publisher↔Cert ordinal exacto), los artefactos canónicos para comparar y la checklist de 11 ítems del entregable.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Máquina de 8 pasos con criterios de validación (slides 4-11, 15) | [`PracticaSteps.cs`](src/PracticaMsix.Demo.Api/Practica/PracticaSteps.cs) |
| Publisher del manifest = Subject del cert (slide 7 — error #1) | [`PracticaCertCheck.cs`](src/PracticaMsix.Demo.Api/Practica/PracticaCertCheck.cs) |
| Manifest + .appinstaller canónicos para comparar (slides 6, 11) | [`PracticaArtefactosBuilder.cs`](src/PracticaMsix.Demo.Api/Practica/PracticaArtefactosBuilder.cs) |
| Plan + checklist del entregable (slide 15) | [`IPracticaMsixPlanner.cs`](src/PracticaMsix.Demo.Api/Practica/IPracticaMsixPlanner.cs) |
| API que expone la práctica (/practica/*) | [`PracticaEndpoints.cs`](src/PracticaMsix.Demo.Api/Endpoints/PracticaEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Qué vas a hacer (8 pasos) | 2 | `PracticaSteps.Pasos` |
| Pre-flight: Windows 10 1809+, sideloading, tooling | 3 | scripts `01-preflight.ps1` |
| Paso 1: crear solución WPF + WAP | 4 | `PasoPractica.CrearSolucion` |
| Paso 2: personalizar la app (versión visible) | 5 | `PasoPractica.PersonalizarApp` |
| Paso 3: configurar `Package.appxmanifest` | 6 | `PracticaArtefactosBuilder.ConstruirManifest` |
| Paso 4: cert + Publisher COINCIDE (error #1) | 7 | `PracticaCertCheck.PublisherCoincide` |
| Paso 5: build firmado Release/x64 | 8 | scripts `02-verify-msix.ps1` |
| Paso 6: instalar con `Add-AppxPackage` | 9 | scripts `02-verify-msix.ps1` |
| Paso 7: simular actualización 1.0.0.0 → 1.0.1.0 | 10 | `PasoPractica.SimularActualizacion` |
| Paso 8: AppInstaller con auto-update (reto) | 11 | `PracticaArtefactosBuilder.ConstruirAppInstaller` |
| Checklist de 11 ítems | 15 | `IPracticaMsixPlanner.Checklist` |

## Estructura

```
S7.P-practica-msix/
├── src/PracticaMsix.Demo.Api/
│   ├── Practica/   PracticaSteps, PracticaCertCheck,
│   │               PracticaArtefactosBuilder
│   │               + IPracticaMsixPlanner/PracticaMsixPlanner
│   ├── Endpoints/  PracticaEndpoints (/health, /practica/*)
│   └── Program.cs  AddSingleton<IPracticaMsixPlanner>
├── tests/PracticaMsix.Demo.Api.Tests/
│   ├── Unit_*             lógica pura (steps, cert check, artefactos)
│   ├── DiContainer_Tests  resuelve IPracticaMsixPlanner (real)
│   └── Api_PracticaTests  E2E vía WebApplicationFactory
└── scripts/         PowerShell (preflight + verify-msix — SOLO LECTURA)
```

## Tests

```bash
dotnet test     # 28 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `PracticaSteps` (8 pasos numerados 1..8, criterios
  no vacíos, avanza solo con todos OK, último paso → null);
  `PracticaCertCheck` (coincidencia exacta ordinal, espacios NO se
  normalizan, falta `CN=` falla, EKU Code Signing requerido);
  `PracticaArtefactosBuilder` (manifest con `Empresa.App`, `CN=`,
  `rescap:runFullTrust`; .appinstaller con `MainPackage` apuntando a
  `{Empresa.App}_{Version}_x64.msix`, `OnLaunch HoursBetweenUpdateChecks=0`).
- **CAPA 0 · DI**: resuelve `IPracticaMsixPlanner` del contenedor real
  (`Assert.Same` singleton) y compone los 8 pasos + cert check +
  artefactos + checklist. Detecta también el error de Publisher↔Cert
  desalineados. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/practica/{pasos,paso,avanzar,cert-coincide,cert-uso,
  artefactos/manifest,artefactos/appinstaller,plan}` (artefactos
  servidos como `application/xml`).

> 🧠 **Sin CAPA de integración a propósito.** La práctica real requiere
> Visual Studio + Windows SDK + un cert privado (no reproducible en
> CI). Mismo criterio que el resto de M07.

## Ejecución local

```bash
dotnet run --project src/PracticaMsix.Demo.Api
# http://localhost:5103  — usa src/PracticaMsix.Demo.Api/api.http
```

`/practica/pasos` lista los 8 pasos con criterios; `/practica/avanzar`
es la máquina de estados; `/practica/cert-coincide` valida el match
crítico Publisher↔Cert (slide 7); `/practica/artefactos/manifest` y
`/.../appinstaller` devuelven los artefactos canónicos en XML para que
el alumno los compare con los suyos.

## Pre-flight y verificación (PowerShell)

```powershell
pwsh -File scripts/demo.ps1
# 1) 01-preflight.ps1   → Windows 10 1809+, Developer Mode, signtool,
#                         makeappx, admin (slide 3)
# 2) 02-verify-msix.ps1 -MsixPath ./MiApp.msix
#    → Get-AuthenticodeSignature + extrae AppxManifest.xml del paquete
#      (System.IO.Compression) y compara Publisher con Subject del
#      cert firmante (slide 7/13). SOLO LECTURA — no instala nada.
```

## Despliegue por Portal (entregable)

> ⚠️ **Coste:** 0 € si el `.appinstaller` se queda local (`file:///`).
> Para producción se sube a Azure Blob (~0,02 €/GB).

1. **Crear solución** WPF + Packaging Project en Visual Studio (slide 4).
2. **Personalizar** `MainWindow` para mostrar `Package.Current.Id.Version`
   (slide 5).
3. **Configurar** `Package.appxmanifest`: Identity `Empresa.MsixDemo`,
   Publisher `CN=Empresa`, capabilities `internetClient` + `rescap:runFullTrust`
   (slide 6).
4. **Crear cert** self-signed con Subject `CN=Empresa` (DEBE
   coincidir con el Publisher — slide 7).
5. **Build** Release/x64 → Publish → Sideloading → genera `.msix` firmado
   (slide 8).
6. **Importar** el cert a `Cert:\LocalMachine\TrustedPeople` y `Add-AppxPackage`
   (slide 9).
7. **Subir versión** a `1.0.1.0`, rebuild, `Add-AppxPackage` → actualización
   in-place (slide 10).
8. **(Reto)** crear `.appinstaller` apuntando al `.msix` con `OnLaunch`
   inmediato (slide 11).

## Ideas centrales

> El **error #1** de la práctica está en la slide 7: si el **Subject del
> cert** y el **Publisher del manifest** no son **exactamente iguales**
> (ordinal, sin normalizar espacios), Windows rechaza el paquete con
> "package signature hash validation failed". Todo lo demás es
> mecánica: WPF intacto, WAP añadido, Versión `Major.Minor.Build.Revision`
> incremental, `runFullTrust` en `rescap:`, AppInstaller con
> `MainPackage` que apunta al `.msix`. Para producción, sustituye
> `file:///` por una URL de Azure Blob.

## Próximo paso

[`S7.P2 — Práctica: MSIX wizard`](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.P2-practica-msix-wizard-v1.md):
cierra M07 con un wizard guiado paso a paso.
