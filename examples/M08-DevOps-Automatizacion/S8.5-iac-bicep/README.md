# S8.5 — IaC con Bicep (con CAPA de integración)

> **Submódulo de referencia:** [M08-S8.5](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.5-iac-bicep-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (`bicep build` es local; `az deployment what-if` no aplica cambios)

> 🎓 **Primer submódulo de M08 con CAPA de integración real.**
> `bicep build` es una herramienta local idempotente sin Azure → un
> test `SkippableFact` (lección 2 del HANDOFF) invoca el CLI si está
> en PATH; si no, se omite y la suite queda verde.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Linter del archivo `.bicep` (slides 6, 11, 19) | [`BicepFileValidator.cs`](src/Iac.Bicep.Demo.Api/Iac/BicepFileValidator.cs) |
| Parser del output `az deployment what-if` (slides 5, 14) | [`WhatIfDiffParser.cs`](src/Iac.Bicep.Demo.Api/Iac/WhatIfDiffParser.cs) |
| Comparativa Bicep / ARM / Terraform + recomendación (slide 3) | [`ToolingComparison.cs`](src/Iac.Bicep.Demo.Api/Iac/ToolingComparison.cs) |
| Plan + checklist del entregable | [`IIacPlanner.cs`](src/Iac.Bicep.Demo.Api/Iac/IIacPlanner.cs) |
| API que expone la lógica (/iac/*) | [`IacEndpoints.cs`](src/Iac.Bicep.Demo.Api/Endpoints/IacEndpoints.cs) |
| Integración real: `bicep build` (SkippableFact) | [`Integration_BicepBuildTests.cs`](tests/Iac.Bicep.Demo.Api.Tests/Integration_BicepBuildTests.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| IaC vs manual: reproducible, versionable, testeable | 2 | `IIacPlanner.Checklist` |
| Bicep vs ARM vs Terraform | 3 | `ToolingComparison.Comparativa` + `Recomendar` |
| Primer Bicep + outputs | 4 | snippet en `Integration_BicepBuildTests` |
| `validate`, `what-if`, `create` | 5 | scripts `01-validate-bicep.sh` |
| Parámetros + @secure() + KV reference | 6 | `BicepFileValidator` (param sin @secure) |
| Módulos por dominio | 7 | `IIacPlanner.Checklist` |
| Loops/condiciones/variables | 10 | (linter respeta) |
| Secretos: existing KV + Managed Identity | 11 | `BicepFileValidator` (Password=, output con secreto) |
| Entornos: params dev/staging/prod | 12 | `IIacPlanner.Checklist` |
| VS Code Bicep extension | 13 | scripts `_lib.sh` (instala bicep) |
| What-if preview + análisis | 14 | `WhatIfDiffParser` + Delete stateful → riesgo alto |
| Testing de IaC en CI | 19 | `Integration_BicepBuildTests` |
| AVM (Azure Verified Modules) | 22 | `IIacPlanner.Checklist` |

## Estructura

```
S8.5-iac-bicep/
├── src/Iac.Bicep.Demo.Api/
│   ├── Iac/        BicepFileValidator, WhatIfDiffParser,
│   │               ToolingComparison
│   │               + IIacPlanner/IacPlanner
│   ├── Endpoints/  IacEndpoints (/health, /iac/*)
│   └── Program.cs  AddSingleton<IIacPlanner> + enums por nombre
├── tests/Iac.Bicep.Demo.Api.Tests/
│   ├── Unit_*                     lógica pura (validator, what-if, tooling)
│   ├── DiContainer_Tests          resuelve IIacPlanner (contenedor real)
│   ├── Api_IacTests               E2E vía WebApplicationFactory
│   └── Integration_BicepBuildTests  SkippableFact: invoca `bicep build`
└── scripts/        bicep build + az validate + az what-if (solo lectura)
```

## Tests

```bash
dotnet test     # 24 pass + 1 skip (bicep no en PATH) + 0 fail
# Con `az bicep install`: 25 pass + 0 skip.
```

- **CAPA 1 · Unit**: `BicepFileValidator` (Bicep correcto OK, secreto
  literal `Password=` error, param que parece secreto sin `@secure`
  error, `@secure` justo encima válido, sin `targetScope` aviso,
  `output` con nombre de secreto aviso); `WhatIfDiffParser` (parsea
  `+/~/-/=`, **Delete de Cosmos/Storage/SQL → riesgo alto**, Delete
  de App Service no es high-risk, ignora líneas sin marcador);
  `ToolingComparison` (solo Azure → Bicep; multi-cloud → Terraform;
  equipo ya en Terraform → mantener).
- **CAPA 0 · DI**: resuelve `IIacPlanner` del contenedor real
  (`Assert.Same` singleton) y compone herramienta + validación +
  what-if + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/iac/{comparativa,recomendar,validar,whatif/parsear,plan}`.
- **CAPA Integration** (SkippableFact, lección 2): si `bicep` está
  en PATH, escribe un Bicep válido al temp, ejecuta `bicep build`,
  parsea el ARM JSON resultante y comprueba que contiene un
  `Microsoft.Web/serverfarms`. Si `bicep` no está → **skip**.

> 🧠 **Primer M08 con CAPA de integración real**: a diferencia de
> S8.1–S8.4 (decisión pura), Bicep tiene una herramienta local
> idempotente sin Azure (`bicep build`) que vale la pena ejercitar.
> La integración usa `SkippableFact` para que la suite siga verde
> sin Bicep instalado.

## Ejecución local

```bash
dotnet run --project src/Iac.Bicep.Demo.Api
# http://localhost:5109  — usa src/Iac.Bicep.Demo.Api/api.http
```

`/iac/validar` lintea el .bicep pegado en el body; `/iac/whatif/parsear`
extrae los cambios del output de `az deployment what-if` y avisa de los
Delete de recursos stateful; `/iac/comparativa` muestra la tabla
canónica; `/iac/plan` compone todo + checklist.

## Validar Bicep real (scripts)

```bash
./scripts/demo.sh
# 1) 01-validate-bicep.sh → az bicep build + az validate + az what-if
#    contra el RG configurado. Nunca ejecuta create.
```

Necesita `az bicep` (lo instala automáticamente la primera vez). Solo
lectura: nunca aplica cambios.

## Despliegue por Portal (entregable)

1. **Repo de infra** (slide 7): `infrastructure/main.bicep` +
   `modules/` por dominio (app-service, cosmos, storage, keyvault…) +
   `params.{dev,staging,prod}.json`.
2. **Pipeline IaC** (slide 19): stage Validate (`bicep build` + `az
   deployment group validate`) → stage Preview (what-if obligatorio)
   → stage Deploy (con aprobación manual del environment).
3. **Secretos**: `@secure()` + Key Vault Reference en
   `params.prod.json` — el secreto nunca aparece en texto plano.
4. **What-if obligatorio**: si ves `Delete:` en un recurso stateful
   (Cosmos/Storage/SQL/KV) → **alto riesgo**, revisa backup antes
   (slide 14).
5. **Verificar** con `./scripts/demo.sh`: bicep build OK, validate OK,
   what-if sin sorpresas.

## Ideas centrales

> Bicep es **el lenguaje IaC oficial para Azure**: DSL legible, sin
> state file (Azure ES el state), módulos nativos, VS Code extension
> (slide 3/13). La idempotencia es gratis: el mismo Bicep ejecutado N
> veces converge al mismo estado. **What-if antes de apply** (slide 5/
> 14): un `Delete:` de un recurso stateful es la señal de alarma. Para
> secretos: **`@secure()` en params + Key Vault Reference** en el JSON
> de parámetros — nunca password literal en código (anti-pattern
> caught by `BicepFileValidator`). En pipelines: validate → what-if
> → approval → deploy.

## Próximo paso

[`S8.6 — Application Insights y monitoring`](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.6-app-insights-monitor-v3.md):
KQL queries, alertas, dashboards.
