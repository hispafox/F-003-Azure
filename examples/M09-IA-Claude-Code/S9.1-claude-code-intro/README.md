# S9.1 — Claude Code: introducción y setup

> **Submódulo de referencia:** [M09-S9.1](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.1-claude-code-intro-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; no invoca Claude Code real)

> 🎓 **Submódulo conceptual** (lección 9 del HANDOFF). Claude Code es
> la **herramienta** que el alumno aprende a usar — no se "despliega".
> Lo testeable son las heurísticas pedagógicas: qué modo de ejecución
> usar para cada tarea, Claude Code vs Copilot, y el `settings.json`
> recomendado del equipo.
>
> 🧱 **Primer submódulo de M09**: cambio de dominio respecto a M01–M08.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Recomendador de modo + features (slides 4, 7-10, 12, 15, 16, 18, 19, 20) | [`FeatureRecommender.cs`](src/ClaudeCode.Intro.Demo.Api/ClaudeCode/FeatureRecommender.cs) |
| Comparativa Claude Code vs GitHub Copilot (slide 5) | [`ToolComparison.cs`](src/ClaudeCode.Intro.Demo.Api/ClaudeCode/ToolComparison.cs) |
| Builder del `settings.json` del equipo (slides 6, 11, 13, 19) | [`ProjectConfigBuilder.cs`](src/ClaudeCode.Intro.Demo.Api/ClaudeCode/ProjectConfigBuilder.cs) |
| Plan + checklist de onboarding | [`IClaudeCodePlanner.cs`](src/ClaudeCode.Intro.Demo.Api/ClaudeCode/IClaudeCodePlanner.cs) |
| API que expone la lógica (`/cc/*`) | [`ClaudeCodeEndpoints.cs`](src/ClaudeCode.Intro.Demo.Api/Endpoints/ClaudeCodeEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Instalar Claude Code (Node 18+, API key) | 3 | `Checklist` (paso 1-2) |
| Flujo básico del modo interactivo | 4 | `FeatureRecommender` → `Interactive` por defecto |
| Claude Code vs GitHub Copilot | 5 | `ToolComparison.Tabla` + `Recomendar` |
| `.claude/config.yml` con convenciones | 6 | `ProjectConfigBuilder.SystemPrompt` |
| Casos: generar código, refactor, debug, IaC | 7-10 | `TipoTarea` (8 valores) |
| Seguridad: exclude *.env, *.pfx, secrets | 11 | `ProjectConfigBuilder.ExcludePatternsBase` |
| Modos: interactive / one-shot / pipe / headless | 12 | `ModoEjecucion` |
| `settings.json` (model, maxTokens, system prompt) | 13 | `SettingsRecomendados` |
| Slash commands / Skills | 14/20 | `FeatureRecommender` sugiere skill si `EsRecurrente` |
| Extended thinking para arquitectura | 15 | `FeatureRecommender.UsarExtendedThinking` |
| CI/CD: `claude -p ... --no-interactive` | 16 | `EnPipelineCiCd → Headless` |
| Changelogs automáticos | 17 | `TipoTarea.ChangelogODocs → OneShot` |
| Subagents (code-reviewer, log-analyst, etc.) | 18 | `FeatureRecommender` los sugiere por tarea |
| Hooks (PreToolUse, PostToolUse, SessionStart) | 19 | `ProjectConfigBuilder.HooksRecomendados` |

## Estructura

```
S9.1-claude-code-intro/
├── src/ClaudeCode.Intro.Demo.Api/
│   ├── ClaudeCode/  FeatureRecommender, ToolComparison,
│   │               ProjectConfigBuilder
│   │               + IClaudeCodePlanner/ClaudeCodePlanner
│   ├── Endpoints/   ClaudeCodeEndpoints (/health, /cc/*)
│   └── Program.cs   AddSingleton<IClaudeCodePlanner> + enums por nombre
└── tests/ClaudeCode.Intro.Demo.Api.Tests/
    ├── Unit_*                lógica pura (feature, comparison, config)
    ├── DiContainer_Tests     resuelve IClaudeCodePlanner
    └── Api_ClaudeCodeTests   E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 32 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `FeatureRecommender` (CI/CD → `Headless` + hook `PreToolUse`;
    análisis de logs → `Pipe`; changelog → `OneShot`; arquitectura →
    extended thinking; refactor complejo también; code review sugiere
    `code-reviewer` subagent; tarea recurrente sugiere skill; contexto
    aislado activa subagent aunque no sea CodeReview).
  - `ToolComparison` (tabla con ≥ 6 filas; solo IDE → Copilot; agente o
    MCP sin IDE → Claude Code; señales de ambas → `Combinacion`).
  - `ProjectConfigBuilder` (allowed tools mínimas Read/Write/Edit/Grep;
    `TocaInfraestructura` añade `Bash`; exclude patterns cubren
    `*.env`/`*.pfx`/`local.settings.json`/`.secrets/*`; producción
    añade hook pre-commit; compliance añade hook block-secrets; system
    prompt menciona framework y lenguaje; auto-format está en
    `PostToolUse`).
- **CAPA 0 · DI**: resuelve `IClaudeCodePlanner` del contenedor real
  (`Assert.Same` singleton) y compone herramienta + feature + settings
  + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/cc/{comparativa, recomendar, feature, settings, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: Claude Code requiere
> Node.js + API key + tokens; invocarlo desde tests es lento,
> impredecible y consume euros. El valor pedagógico está en las
> **decisiones** (modo, feature, settings), y esas son lógica pura.

## Ejecución local

```bash
dotnet run --project src/ClaudeCode.Intro.Demo.Api
# http://localhost:5113  — usa src/ClaudeCode.Intro.Demo.Api/api.http
```

- `/cc/comparativa` devuelve la tabla canónica Claude Code vs Copilot.
- `/cc/recomendar` decide herramienta según el escenario del alumno.
- `/cc/feature` recomienda modo + extended thinking + subagent + skill
  + hook para un tipo de tarea concreto.
- `/cc/settings` genera el `settings.json` recomendado del equipo
  (allowed tools, exclude patterns, hooks).
- `/cc/plan` compone todo + checklist de 10 puntos para arrancar.

## Despliegue por Portal (entregable)

No hay deploy a Azure en S9.1. El entregable es **el `.claude/` del
equipo** versionado en git:

1. **`.claude/settings.json`** — basado en `/cc/settings`: `model`,
   `maxTokens`, `systemPrompt`, `allowedTools`, `excludePatterns`,
   referencias a hooks.
2. **`.claude/agents/`** — al menos un subagent (`code-reviewer.md`
   recomendado, slide 18).
3. **`.claude/skills/`** — al menos un skill recurrente del equipo
   (`new-service.md`, `bicep-bootstrap.md`, etc.) (slide 20).
4. **`.claude/hooks/`** o `scripts/` — al menos un hook `PreToolUse`
   para bloquear comandos destructivos y otro `PostToolUse` para
   auto-format (slide 19).
5. **API key** del equipo gestionada como **secret** (no en el repo);
   en CI/CD vía `ANTHROPIC_API_KEY` con scope mínimo.

## Ideas centrales

> Claude Code es **un agente en tu terminal** con acceso al filesystem
> + bash + MCP (slide 2). **No reemplaza al desarrollador, amplifica
> la capacidad**: una sesión bien planteada genera 5x más output sin
> más errores. **Claude Code vs Copilot no es excluyente** (slide 5):
> Copilot autocompleta mientras tecleas, Claude Code hace las tareas
> que duran 15-60 minutos (refactor, IaC, debugging cross-archivo).
> **Modo correcto para cada tarea** (slide 12): `Pipe` para logs,
> `OneShot` para changelogs, `Headless` en CI/CD, `Interactive` para
> diseño/arquitectura. **Hooks + skills + subagents** convierten
> heurísticas en automatización determinística (slides 18-20).

## Próximo paso

[`S9.2 — Casos de uso (refactor, IaC, debugging)`](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.2-claude-code-casos-uso-v3.md):
aplicar Claude Code a las tareas más comunes del día a día.
