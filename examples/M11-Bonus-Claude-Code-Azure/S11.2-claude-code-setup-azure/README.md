# S11.2 — Claude Code: setup avanzado para Azure (BONUS)

> **Submódulo de referencia:** [M11-S11.2](../../../doc/M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.2-claude-code-setup-azure.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; no invoca Claude Code real)

> 🎓 **Submódulo conceptual** (lección 9 del HANDOFF). Aterriza el
> setup serio de Claude Code para un proyecto Azure: estructura del
> directorio `.claude/`, `CLAUDE.md` con DOs/DON'Ts, `settings.json`
> con `permissions` afinado, Azure Skills Plugin y MCP servers. Lo
> testeable son los validadores que reaccionan a los riesgos típicos
> (allow demasiado amplio, secretos en `CLAUDE.md`, falta de deny de
> comandos destructivos).

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Estructura del directorio `.claude/` (slide 4) | [`CarpetaClaudeStructurer.cs`](src/Bonus.SetupAzure.Demo.Api/Setup/CarpetaClaudeStructurer.cs) |
| Validador de `permissions` (slide 7/9) | [`SettingsPermissionsValidator.cs`](src/Bonus.SetupAzure.Demo.Api/Setup/SettingsPermissionsValidator.cs) |
| Evaluador de calidad del `CLAUDE.md` (slide 5/6) | [`ClaudeMdQualityEvaluator.cs`](src/Bonus.SetupAzure.Demo.Api/Setup/ClaudeMdQualityEvaluator.cs) |
| Plan + Azure Skills (slide 16) + checklist | [`ISetupAzurePlanner.cs`](src/Bonus.SetupAzure.Demo.Api/Setup/ISetupAzurePlanner.cs) |
| API que expone la lógica (`/setup/*`) | [`SetupEndpoints.cs`](src/Bonus.SetupAzure.Demo.Api/Endpoints/SetupEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Precondiciones (Node + auth) | 2/3 | `Checklist` pasos 1-2 |
| Estructura recomendada `.claude/` | 4 | `CarpetaClaudeStructurer.Inventariar` |
| `CLAUDE.md` con 6 secciones (Stack, Convenciones, Comandos, Arquitectura, Glosario, ZonasFragiles) | 5 | `SeccionClaudeMd` enum |
| DOs/DON'Ts del `CLAUDE.md` (no secretos, no docs de features) | 6 | `ClaudeMdQualityEvaluator.AntiPatrones` |
| `settings.json` (model + allow/deny + hooks) | 7 | `SettingsPermissionsValidator` |
| `.gitignore` del `settings.local.json` | 8 | `Checklist` paso 5 |
| Permissions afinadas — deny destructivos + exclude sensibles | 9 | `DenyImprescindibles` / `LecturaProhibida` |
| Heurística "¿analizas este proyecto?" | 11 | `Checklist` paso 9 |
| `.mcp.json` con azure + bicep + azure-devops | 14 | `Checklist` paso 7 |
| `/plugin install azure-skills@microsoft-azure` | 15 | `Checklist` paso 6 |
| Los 20 skills del Azure Skills Plugin | 16 | `SetupAzurePlanner.AzureSkillsSlide16` |
| Hook `PreToolUse` para política y logging | 18 | `Checklist` paso 8 |

## Estructura

```
S11.2-claude-code-setup-azure/
├── src/Bonus.SetupAzure.Demo.Api/
│   ├── Setup/      CarpetaClaudeStructurer, SettingsPermissionsValidator,
│   │              ClaudeMdQualityEvaluator
│   │              + ISetupAzurePlanner/SetupAzurePlanner
│   ├── Endpoints/  SetupEndpoints (/health, /setup/*)
│   └── Program.cs  AddSingleton<ISetupAzurePlanner> + enums por nombre
└── tests/Bonus.SetupAzure.Demo.Api.Tests/
    ├── Unit_*                lógica pura (estructura, settings, claudemd)
    ├── DiContainer_Tests     resuelve el planner
    └── Api_SetupTests        E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 43 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `CarpetaClaudeStructurer` (CLAUDE.md y `.claude/settings.json`
    siempre `Obligatorio`; `agents/`, `skills/`, `.mcp.json` se
    añaden cuando el equipo los pide; avisos por hooks ausentes,
    skills sin MCP, agents en trabajo individual).
  - `SettingsPermissionsValidator` (allow `Bash(*)` o `Write(**)` →
    `Critico`; falta de deny de `rm -rf`, `az group delete`,
    `az resource delete`, `drop database` → `Alto`; falta exclude
    de `*.env`, `*.pfx`, `*.key`, `local.settings.json` → `Alto`;
    sin `model` → `Medio` sin romper "seguro").
  - `ClaudeMdQualityEvaluator` (puntuación 0-100 ponderada por
    secciones; Stack/Convenciones/ZonasFragiles pesan 70%; detecta
    `password=`, connection strings, `sk-ant-`, placeholders
    `xxxxxxxx`; avisa si > 80 líneas).
- **CAPA 0 · DI**: resuelve `ISetupAzurePlanner` del contenedor
  real (`Assert.Same` singleton) y compone estructura + settings +
  claudeMd + 20 skills + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/setup/{estructura, settings, claudemd, azure-skills, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: Claude Code se instala
> en el entorno del alumno. Aquí lo testeable son las **políticas
> aplicadas al setup**: estructura recomendada, permisos sanos,
> CLAUDE.md sin secretos. Validar contra Claude Code real es ruido
> en una clase.

## Ejecución local

```bash
dotnet run --project src/Bonus.SetupAzure.Demo.Api
# http://localhost:5123  — usa src/Bonus.SetupAzure.Demo.Api/api.http
```

- `/setup/estructura` lista qué archivos y carpetas del `.claude/`
  necesita tu equipo (slide 4) según flags (agents custom, skills
  propios, MCP, hooks, slash commands, trabajo individual).
- `/setup/settings` valida tu `permissions` (slide 7/9). Marca
  `Critico` los allow demasiado amplios, `Alto` la falta de deny
  destructivos / exclude sensibles, `Medio` la falta de `model`.
- `/setup/claudemd` puntúa tu `CLAUDE.md` (slide 5/6) y avisa de
  secretos literales, connection strings y placeholders.
- `/setup/azure-skills` devuelve los 20 skills del Azure Skills
  Plugin oficial (slide 16) — los 17 skills + 3 MCP servers
  incluidos.
- `/setup/plan` compone todo + checklist de 9 puntos de arranque.

## Flujo del alumno

1. **Decide la estructura** → `/setup/estructura` con los flags de
   tu equipo. Si trabajas solo, valora poner `agents/` en
   `~/.claude/` global; si hay skills, añade `.mcp.json`.
2. **Pon el `permissions` sano** → `/setup/settings` con tu allow/
   deny actual. No empieces hasta que el informe diga "seguro"
   (cero `Critico`/`Alto`).
3. **Aterriza el `CLAUDE.md`** → `/setup/claudemd` con tu borrador.
   Apunta a Stack + Convenciones + Comandos + Arquitectura + Glosario
   + ZonasFragiles (los 6 marcadores del slide 5). Apunta a `≥ 70`
   antes de instalar nada.
4. **Instala Azure Skills Plugin** →
   `/plugin install azure-skills@microsoft-azure`. Ya tienes los 20
   skills (azure-prepare, azure-validate, azure-deploy, …) que
   verás en `/setup/azure-skills`.
5. **Configura `.mcp.json`** con `azure`, `bicep` y `azure-devops`
   (slide 14) y conecta lo que necesites. Comparte por Git para que
   el equipo entero tenga lo mismo.
6. **Activa el hook `PreToolUse`** (slide 18) para auditar y
   bloquear acciones por política. Tu primer `> Analiza este
   proyecto. Dime el stack y los riesgos` (slide 11) debería
   funcionar con esto en pie.

## Ideas centrales

> Claude Code se vuelve útil cuando le pones **contexto en
> `CLAUDE.md`** (slide 5), le pones **barandillas en `settings.json`**
> (slide 7/9) y le abres **herramientas vía MCP/skills** (slide
> 14/15). Los tres validadores de este ejemplo son exactamente las
> tres reglas de oro del setup: estructura completa, permissions sin
> agujeros, CLAUDE.md sin secretos. Si los tres están verdes, ya
> estás en la **Nivel 2** del módulo S11.1.

## Próximo paso

[`S11.3 — Skills: capacidades especializadas`](../../../doc/M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.3-skills-capacidades-especializadas.md):
qué son los skills (oficiales + propios), cómo combinarlos con el
Azure Skills Plugin y cómo escribir un skill propio para tu equipo.
