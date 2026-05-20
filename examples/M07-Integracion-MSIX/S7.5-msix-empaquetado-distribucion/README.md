# S7.5 — MSIX: empaquetado, firma y distribución

> **Submódulo de referencia:** [M07-S7.5](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.5-msix-empaquetado-distribucion-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (decisión + validación; Azure Blob/CDN ~0,02 €/GB si despliegas)

> 🎓 **Submódulo conceptual.** El empaquetado real exige Windows SDK +
> signtool + clave privada (no portable). El valor docente es la
> **lógica testeable**: validar el `Package.appxmanifest`, calcular el
> nombre final del `.msix`, elegir el canal de distribución y la
> política de auto-update.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el pasaporte y el embarque como analogía, el manifest como identidad legal del paquete, naming determinista del `.msix`, los cuatro canales (Store/AppInstaller/Intune/winget) y la clave privada viviendo en Key Vault con `AzureSignTool`.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Validador del `Package.appxmanifest` (slides 3, 15, 28) | [`AppxManifestValidator.cs`](src/Msix.Demo.Api/Msix/AppxManifestValidator.cs) |
| Nombre del paquete + versionado de pipeline (slides 3, 4, 10, 11) | [`PackageNamingResolver.cs`](src/Msix.Demo.Api/Msix/PackageNamingResolver.cs) |
| Canal de distribución + política AppInstaller (slides 7, 8, 9, 26, 27) | [`DistributionChannelAdvisor.cs`](src/Msix.Demo.Api/Msix/DistributionChannelAdvisor.cs) |
| Plan + checklist del entregable | [`IMsixPackagingPlanner.cs`](src/Msix.Demo.Api/Msix/IMsixPackagingPlanner.cs) |
| API que expone la lógica (/msix/*) | [`MsixEndpoints.cs`](src/Msix.Demo.Api/Endpoints/MsixEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Estructura del proyecto WAP + manifest | 2-3 | `AppxManifestValidator.Parsear` |
| Build `.msix` + nombre final | 4 | `PackageNamingResolver.NombreArchivo` |
| Firma (signtool / Azure Key Vault) | 5-6 | `IMsixPackagingPlanner.Checklist` |
| `.appinstaller` + UpdateSettings | 7 | `DistributionChannelAdvisor.PoliticaPorDefecto` |
| Hospedar en Azure Blob + CDN | 8 | `IMsixPackagingPlanner.Checklist` |
| Sideloading + cert trusted | 9 | `IMsixPackagingPlanner.Checklist` |
| `.msixbundle` multi-arquitectura | 10 | `PackageNamingResolver.NombreBundle` |
| Pipeline CI/CD + versionado de build | 11 | `PackageNamingResolver.SiguienteVersion` |
| Capabilities y `rescap:` | 3, 15 | `AppxManifestValidator.Validar` |
| Anti-patterns (HKLM, Program Files, no firmar dev, single-arch) | 28 | `IMsixPackagingPlanner.Checklist` |
| Distribución vía Intune / winget / Store | 26-27 | `DistributionChannelAdvisor.Recomendar` |

## Estructura

```
S7.5-msix-empaquetado-distribucion/
├── src/Msix.Demo.Api/
│   ├── Msix/       AppxManifestValidator (XML), PackageNamingResolver,
│   │               DistributionChannelAdvisor
│   │               + IMsixPackagingPlanner/MsixPackagingPlanner
│   ├── Endpoints/  MsixEndpoints (/health, /msix/*)
│   └── Program.cs  AddSingleton<IMsixPackagingPlanner> + enums por nombre
├── tests/Msix.Demo.Api.Tests/
│   ├── Unit_*             lógica pura (manifest, naming, distribution)
│   ├── DiContainer_Tests  resuelve IMsixPackagingPlanner (contenedor real)
│   └── Api_MsixTests      E2E vía WebApplicationFactory
└── scripts/        PowerShell (validar manifest local + tooling check)
```

## Tests

```bash
dotnet test     # 34 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `AppxManifestValidator` (Name/Publisher/Version/
  Arch/MinVersion/Capabilities — todas las reglas slide 3; parseo XML);
  `PackageNamingResolver` (nombre `_2.4.1.0_x64.msix`, bundle, siguiente
  versión desde `buildId`, incremental); `DistributionChannelAdvisor`
  (público→Store, +power users→Winget, corporativo+Intune→Intune,
  Blob+auto-update→AppInstaller, default→AppInstaller; política de
  auto-update por defecto).
- **CAPA 0 · DI**: resuelve `IMsixPackagingPlanner` del contenedor real
  (`Assert.Same` singleton) y compone manifest + nombre + canales +
  checklist. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/msix/{parsear,validar,nombre,version-siguiente,incremental,distribucion,politica-auto-update,plan}`.

> 🧠 **Sin CAPA de integración a propósito.** El empaquetado real
> (`msbuild .wapproj`, `signtool`, `MakeAppx bundle`) requiere Windows
> SDK + clave privada del certificado: no es reproducible en CI sin
> credenciales. El valor docente está en la lógica de validación y
> decisión, que es pura. Mismo criterio que M06 / S7.1–S7.4.

## Ejecución local

```bash
dotnet run --project src/Msix.Demo.Api
# http://localhost:5100  — usa src/Msix.Demo.Api/api.http
```

`/msix/parsear` lee un `Package.appxmanifest`; `/msix/validar` aplica
todas las reglas de la slide 3; `/msix/plan` compone el plan completo
con canales recomendados y checklist.

## Inventario local (PowerShell, no Azure)

```powershell
pwsh -File scripts/demo.ps1
# 1) 01-validate-manifest.ps1 → comprueba un Package.appxmanifest local
# 2) 02-tooling-check.ps1     → busca signtool, makeappx, AzureSignTool
```

Los scripts son **solo lectura**: no firman, no empaquetan, no instalan.
El entregable real (build + sign + upload) se hace en el pipeline
CI/CD descrito en la slide 11.

## Despliegue por Portal (entregable)

> ⚠️ **Coste:** el `.msix` y el `.appinstaller` se hospedan en **Azure
> Blob Storage** (~0,02 €/GB/mes + tráfico). Con CDN delante, el coste
> baja y la latencia mejora.

1. **Storage Account** (Standard) + contenedor `msix` con acceso
   `blob` (público de solo lectura, slide 8).
2. **Subir `MiApp_X.Y.Z_x64.msix`** (Content-Type `application/msix`)
   y **`MiApp.appinstaller`** (Content-Type `application/appinstaller`).
3. **Azure CDN** delante del Storage para distribución masiva.
4. **Certificado de firma** con la clave privada en **Azure Key
   Vault** (slide 6); el pipeline firma con `AzureSignTool` —
   la clave NUNCA sale del KV.
5. **Pipeline CI/CD** (slide 11): build → AzureSignTool sign → upload
   blob → actualizar `.appinstaller` con la nueva versión.
6. **Verificar** (scripts PowerShell): manifest válido, signtool y
   AzureSignTool disponibles localmente para el alumno.

## Ideas centrales

> El `Package.appxmanifest` es **la identidad del paquete**: `Identity
> Name = Empresa.NombreApp`, `Publisher = CN=…` (debe coincidir con el
> Subject del certificado), `Version = Major.Minor.Build.Revision`
> siempre incremental. Firma siempre (incluso en dev). Multi-arch en
> `.msixbundle` (x64 + arm64). Auto-update con `.appinstaller`. Clave
> privada en **Key Vault**, no en el repo. Anti-pattern más caro:
> escrituras a `HKLM` / `C:\Program Files` que fallan silenciosamente
> en el sandbox — usar `ApplicationData.Current.LocalFolder`.

## Próximo paso

[`S7.6 — MSIX auto-update`](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.6-msix-auto-update-v3.md):
políticas de auto-update, canary releases y rollback.
