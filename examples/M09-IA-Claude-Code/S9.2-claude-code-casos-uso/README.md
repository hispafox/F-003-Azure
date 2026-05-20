# S9.2 — Claude Code: casos de uso avanzados

> **Submódulo de referencia:** [M09-S9.2](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.2-claude-code-casos-uso-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; no invoca Claude Code real)

> 🎓 **Submódulo conceptual** (lección 9 del HANDOFF). Los 15 casos de
> uso del submódulo son **ejemplos pedagógicos** de cómo escribir
> prompts a Claude Code para tareas reales (migración, code review,
> IaC, debugging, optimización, expand-contract…). Aquí extraemos las
> heurísticas: clasificador de caso, generador de template y
> evaluador de calidad del prompt del alumno.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Clasificador de caso de uso (15 casos, slides 2-16) | [`CaseClassifier.cs`](src/ClaudeCode.CasosUso.Demo.Api/CasosUso/CaseClassifier.cs) |
| Generador de template canónico por caso | [`PromptTemplateBuilder.cs`](src/ClaudeCode.CasosUso.Demo.Api/CasosUso/PromptTemplateBuilder.cs) |
| Evaluador de calidad del prompt del alumno (4 ingredientes) | [`PromptQualityEvaluator.cs`](src/ClaudeCode.CasosUso.Demo.Api/CasosUso/PromptQualityEvaluator.cs) |
| Plan + checklist del flujo "tarea → prompt" | [`ICasosUsoPlanner.cs`](src/ClaudeCode.CasosUso.Demo.Api/CasosUso/ICasosUsoPlanner.cs) |
| API que expone la lógica (`/casos/*`) | [`CasosUsoEndpoints.cs`](src/ClaudeCode.CasosUso.Demo.Api/Endpoints/CasosUsoEndpoints.cs) |

## Mapeo a slides

| Caso | Slide | Detectado por |
| --- | --- | --- |
| Migrar de .NET Framework a .NET 10 | 2 | `.net framework`, `webclient`, `configurationmanager` |
| Documentación desde código | 3 | `documentación`, `api-reference`, `documenta los endpoints` |
| Code review | 4 | `code review`, `revisa los últimos`, `review` |
| Generar datos de prueba | 5 | `datos de prueba`, `seed data`, `datos sintéticos`, `fake data` |
| Troubleshooting con logs | 6 | `logs`, `stack trace`, `error en producción`, `troubleshoot` |
| Pipeline CI/CD | 7 | `azure-pipelines`, `github actions`, `pipeline ci/cd`, `pipeline` |
| Bicep desde infraestructura | 8 | `bicep`, `infraestructura`, `az group export` |
| Pair programming | 9 | `vamos a implementar`, `paso a paso`, `iterativamente` |
| API completa desde OpenAPI | 10 | `openapi`, `especificación openapi`, `genera api completa` |
| Migración esquema BD | 11 | `schema migration`, `migración de schema`, `renombrar campo/columna` |
| Tests de integración E2E | 12 | `integration tests`, `tests de integración`, `e2e`, `webapplicationfactory` |
| Optimización de rendimiento | 13 | `optimiza`, `rendimiento`, `latency`, `p95`, `p99`, `performance` |
| Documentación técnica | 14 | `readme.md`, `architecture.md`, `adr`, `documentación técnica` |
| Análisis de coste Azure | 15 | `coste mensual`, `coste azure`, `estima el coste` |
| Expand-contract refactor | 16 | `expand-contract`, `rename column`, `sin downtime`, `zero-downtime` |

## Estructura

```
S9.2-claude-code-casos-uso/
├── src/ClaudeCode.CasosUso.Demo.Api/
│   ├── CasosUso/   CaseClassifier, PromptTemplateBuilder,
│   │              PromptQualityEvaluator
│   │              + ICasosUsoPlanner/CasosUsoPlanner
│   ├── Endpoints/  CasosUsoEndpoints (/health, /casos/*)
│   └── Program.cs  AddSingleton<ICasosUsoPlanner> + enums por nombre
└── tests/ClaudeCode.CasosUso.Demo.Api.Tests/
    ├── Unit_*                lógica pura (clasificación, template, calidad)
    ├── DiContainer_Tests     resuelve ICasosUsoPlanner
    └── Api_CasosUsoTests     E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 38 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `CaseClassifier` (15 `[Theory]` con descripción típica → caso
    esperado; sin palabras clave → `Otro`; expand-contract gana a
    schema migration por estar antes en las reglas; coste antes que
    bicep cuando la descripción tiene "estima el coste mensual de la
    infraestructura"; detecta múltiples palabras clave pero devuelve
    el primer caso; descripción vacía → `ArgumentException`).
  - `PromptTemplateBuilder` (migración tiene placeholders archivo +
    versiones; code review pide output JSON con severidad;
    optimización pide P50/P95/P99 + objetivo; pair programming menciona
    modo interactive + pasos; caso `Otro` devuelve template genérico
    con los 4 ingredientes; todos los templates tienen slide y texto;
    expand-contract menciona las 4 fases).
  - `PromptQualityEvaluator` (prompt vago y corto → `Pobre`; con los 4
    ingredientes → `Excelente`; solo contexto+formato → `Aceptable`;
    las sugerencias cubren los ingredientes faltantes; prompt < 40
    chars se penaliza aunque tenga marcadores).
- **CAPA 0 · DI**: resuelve `ICasosUsoPlanner` del contenedor real
  (`Assert.Same` singleton) y compone clasificación + template +
  evaluación + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/casos/{clasificar, template/{caso}, evaluar, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: invocar Claude Code en un
> test consume tokens, requiere API key y es no determinístico. El
> valor pedagógico está en las **decisiones previas al prompt** —
> clasificar el caso y construir un prompt bueno — y eso es lógica
> pura.

## Ejecución local

```bash
dotnet run --project src/ClaudeCode.CasosUso.Demo.Api
# http://localhost:5114  — usa src/ClaudeCode.CasosUso.Demo.Api/api.http
```

- `/casos/clasificar` mapea una descripción al caso canónico + el
  número de slide donde se cubre.
- `/casos/template/{caso}` devuelve el template con placeholders
  listos para sustituir.
- `/casos/evaluar` puntúa un prompt del alumno (0-100) según los 4
  ingredientes (contexto, constraints, formato salida, criterio éxito)
  + sugerencias concretas.
- `/casos/plan` compone todo + checklist de 9 puntos.

## Entregable

El entregable es **una colección de prompts versionados** en
`.claude/templates/` (uno por caso de uso que use el equipo):

1. Coge el template del caso (`/casos/template/{caso}` te lo da).
2. Rellena los placeholders con datos concretos (archivos, versiones,
   métricas, IDs).
3. Pásalo por `/casos/evaluar` antes de usarlo — debe puntuar ≥ 70.
4. Si puntúa menos, añade los ingredientes que falten (la respuesta
   de `/evaluar` te dice cuáles).
5. Versiona el prompt como Markdown en `.claude/templates/<caso>.md`
   para reuso del equipo.

## Ideas centrales

> **Un prompt bueno tiene 4 ingredientes**: contexto (qué proyecto,
> stack, módulo), constraints (qué NO debe romper), formato de salida
> (JSON, Markdown, archivos), criterio de éxito (tests verdes, build
> limpio, métrica objetivo). Si falta uno, Claude lo inventa o pide
> aclaración → más turnos → más tokens. **Empezar por el template del
> caso** evita escribir desde cero y garantiza que los 4 ingredientes
> aparecen. **Para tareas largas, modo interactive + pair programming**
> (slide 9) — no quieras hacerlo todo en un solo turno. **Para casos
> recurrentes, conviértelo en skill** (`.claude/skills/`) — el alumno
> lo aprende en S9.1.

## Próximo paso

[`S9.3 — Claude Code + infraestructura (Bicep, ARM, AVM)`](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.3-cc-infraestructura-v3.md):
generar Bicep desde descripciones, validar con what-if, usar AVM como
building blocks.
