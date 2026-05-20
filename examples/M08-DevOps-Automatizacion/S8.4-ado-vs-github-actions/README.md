# S8.4 — Azure DevOps vs GitHub Actions

> **Submódulo de referencia:** [M08-S8.4](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.4-ado-vs-github-actions-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € en local; ambas plataformas tienen tier gratuito para equipos pequeños

> 🎓 **Submódulo conceptual de DECISIÓN.** No hay un servicio que
> desplegar: lo que se enseña es **qué plataforma elegir**, qué
> equivale a qué en YAML, y cuánto cuesta cada una.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: dos restaurantes del mismo dueño como analogía, las tres salidas posibles (ADO, GitHub, híbrido), la tabla de 16 equivalencias YAML, los costes reales (Test Plans = 52 €/u, GHAS = 49 €/u en ambas) y la lección 20: "antes de migrar, define qué ganas".

## Objetivo

| Concepto | Dónde |
| --- | --- |
| ADO / GitHub / Híbrido por escenario (slides 4, 5, 8, 11, 19) | [`PlatformAdvisor.cs`](src/Plataforma.Demo.Api/Plataforma/PlatformAdvisor.cs) |
| Equivalencias YAML ADO ↔ GitHub Actions (slide 6) | [`SyntaxEquivalenceMapper.cs`](src/Plataforma.Demo.Api/Plataforma/SyntaxEquivalenceMapper.cs) |
| Coste comparado (slides 12, 17) | [`MigrationCostEstimator.cs`](src/Plataforma.Demo.Api/Plataforma/MigrationCostEstimator.cs) |
| Plan + checklist del entregable | [`IPlatformPlanner.cs`](src/Plataforma.Demo.Api/Plataforma/IPlatformPlanner.cs) |
| API que expone la lógica (/plataforma/*) | [`PlataformaEndpoints.cs`](src/Plataforma.Demo.Api/Endpoints/PlataformaEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Comparativa completa de features | 3 | `IPlatformPlanner.Checklist` |
| Cuándo elegir Azure DevOps | 4 | `PlatformAdvisor` (yaUsasAdo, Boards, TestPlans, OnPremises) |
| Cuándo elegir GitHub Actions | 5 | `PlatformAdvisor` (OpenSource, CodeQL/Dependabot) |
| Equivalencias YAML | 6 | `SyntaxEquivalenceMapper.Todas` |
| Migración ADO → GitHub | 7 | `IPlatformPlanner.Checklist` |
| Híbrido ADO + GitHub | 8 | `PlatformAdvisor` (señales en ambos lados → Hybrid) |
| Security features GitHub | 9 | `IPlatformPlanner.Checklist` |
| Ventajas ADO para vuestro equipo | 11 | `PlatformAdvisor` (Boards completos) |
| Coste real ADO vs GitHub | 12 | `MigrationCostEstimator.Comparar` |
| GHAS for AzDO | 13 | `IPlatformPlanner.Checklist` |
| Recommendation matrix por equipo | 19 | `PlatformAdvisor` fallback |
| Lessons learned migraciones | 20 | `IPlatformPlanner.Checklist` |

## Estructura

```
S8.4-ado-vs-github-actions/
├── src/Plataforma.Demo.Api/
│   ├── Plataforma/   PlatformAdvisor (enum TipoPlataforma),
│   │                 SyntaxEquivalenceMapper, MigrationCostEstimator
│   │                 + IPlatformPlanner/PlatformPlanner
│   ├── Endpoints/    PlataformaEndpoints (/health, /plataforma/*)
│   └── Program.cs    AddSingleton<IPlatformPlanner> + enums por nombre
├── tests/Plataforma.Demo.Api.Tests/
│   ├── Unit_*             lógica pura (advisor, equivalencia, coste)
│   ├── DiContainer_Tests  resuelve IPlatformPlanner (contenedor real)
│   └── Api_PlataformaTests E2E vía WebApplicationFactory
└── scripts/        preflight: ¿tengo az+devops y gh listos?
```

> **Nota de naming**: el enum se llama `TipoPlataforma` (no
> `Plataforma`) para no colisionar con el segmento `Plataforma` del
> namespace `Plataforma.Demo.Api.Plataforma`. El compilador resuelve
> el identificador como namespace primero y rompe la expresión
> `Plataforma.AzureDevOps`.

## Tests

```bash
dotnet test     # 28 pass + 0 skip + 0 fail
```

- **CAPA 1 · Unit**: `PlatformAdvisor` (yaUsasAdo+Boards→ADO;
  OpenSource+CodeQL→GitHub; señales en ambos lados→Hybrid;
  OnPremises→ADO; sin señales→ADO por defecto; personas≤0 lanza);
  `SyntaxEquivalenceMapper` (≥15 equivalencias, conceptos clave con
  sintaxis GitHub, búsqueda por contención, concepto inexistente→null,
  `$(var)` → `${{ var }}`); `MigrationCostEstimator` (5 usuarios sin
  addons: ADO=0 vs GitHub=20; 10 usuarios: ADO=30 vs GitHub=40;
  TestPlans solo cuenta en ADO; GHAS cuenta en ambas).
- **CAPA 0 · DI**: resuelve `IPlatformPlanner` del contenedor real
  (`Assert.Same` singleton) y compone recomendación + coste +
  equivalencias clave + checklist. Cubre la
  [lección DI de M03-S3.4](../../M04-Azure-Functions-II/S4.5-testing-depuracion/README.md).
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/plataforma/{elegir,equivalencias,equivalencia,coste,plan}`.
  `equivalencia` con concepto inexistente devuelve **404**.

> 🧠 **Sin CAPA de integración a propósito.** La decisión y las
> equivalencias son lógica pura. Las APIs reales de ADO y GitHub no
> se necesitan para enseñar esto.

## Ejecución local

```bash
dotnet run --project src/Plataforma.Demo.Api
# http://localhost:5108  — usa src/Plataforma.Demo.Api/api.http
```

`/plataforma/elegir` aplica la tabla de decisión (slide 4/5/8);
`/plataforma/equivalencias` lista las 16 conversiones de sintaxis
clave (slide 6); `/plataforma/coste` calcula la factura mensual de
las dos plataformas para tu equipo (slide 12); `/plataforma/plan`
compone todo + checklist.

## Preflight de plataformas (scripts)

```bash
./scripts/demo.sh
# 1) 01-preflight-platforms.sh → verifica az+azure-devops y gh CLI
```

Solo lectura: no crea ni modifica nada. Si tienes ambas instaladas y
autenticadas, el **híbrido** (slide 8) es viable.

## Despliegue por Portal (entregable)

S8.4 no despliega: produce **una decisión** (qué plataforma usar) y
el **plan de migración** si toca cambiar. El despliegue real depende
de la decisión y se materializa en los submódulos S8.2 (pipelines) y
S8.3 (despliegue).

## Ideas centrales

> Microsoft es dueña de ambas plataformas y están convergiendo
> (slide 2). La pregunta no es "cuál es mejor" sino **cuál encaja con
> tu equipo**. Para equipos 6-10 personas con sprints: **ADO Boards
> sigue siendo significativamente mejor que GitHub Projects** (slide
> 11). Para open source + seguridad nativa (Dependabot, CodeQL):
> GitHub (slide 5/9). **Si necesitas ambas cosas, el híbrido funciona**:
> repos en GitHub + Pipelines y Boards en ADO (slide 8). Mismo YAML,
> sintaxis distinta: `stages/jobs/steps` ↔ `jobs/steps`, `task:` ↔
> `uses:`, `$(var)` ↔ `${{ var }}`, `dependsOn:` ↔ `needs:` (slide 6).
> **Antes de migrar "por modernizar", define qué beneficio concreto y
> medible obtienes** — slide 20 lessons learned.

## Próximo paso

[`S8.5 — IaC con Bicep`](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.5-iac-bicep-v3.md):
infraestructura como código + what-if + deployment stacks.
