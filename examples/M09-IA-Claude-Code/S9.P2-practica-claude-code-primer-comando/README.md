# S9.P2 — Práctica: primer comando con Claude Code

> **Submódulo de referencia:** [M09-S9.P2](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.P2-practica-claude-code-primer-comando-v1.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; el alumno ejecuta Claude Code real en su terminal)

> 🎓 **Práctica conceptual introductoria** (lección 9 del HANDOFF).
> Versión simplificada respecto a S9.P: sin MCP, sin subagents, sin
> hooks — solo los primeros 8 pasos con Claude Code en terminal.
>
> 🧱 **Cierra M09 (7/7)**: última práctica del módulo de IA.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Preflight ligero (slide 3) | [`PrimerComandoPreflight.cs`](src/Practica.PrimerComando.Demo.Api/PrimerComando/PrimerComandoPreflight.cs) |
| Evaluador de los 8 pasos (slides 4-11) | [`PasoEvaluator.cs`](src/Practica.PrimerComando.Demo.Api/PrimerComando/PasoEvaluator.cs) |
| Detector de patterns de prompt (slide 12) | [`PromptPatronDetector.cs`](src/Practica.PrimerComando.Demo.Api/PrimerComando/PromptPatronDetector.cs) |
| Plan + slash commands + checklist | [`IPracticaPrimerComandoPlanner.cs`](src/Practica.PrimerComando.Demo.Api/PrimerComando/IPracticaPrimerComandoPlanner.cs) |
| API que expone la lógica (`/primercomando/*`) | [`PrimerComandoEndpoints.cs`](src/Practica.PrimerComando.Demo.Api/Endpoints/PrimerComandoEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Qué se hace en la práctica (8 pasos) | 2 | `IPracticaPrimerComandoPlanner.Checklist` |
| Preflight: Node 18+, cuenta Anthropic, repo | 3 | `PrimerComandoPreflight.Comprobar` |
| Paso 1: instalación con npm | 4 | `Paso.InstalarCli` |
| Paso 2: login y primera sesión | 5 | `Paso.LoginYPrimeraSesion` |
| Paso 3: pedirle algo concreto sobre un archivo | 6 | `Paso.PedirAlgoMasConcreto` |
| Paso 4: ejecutar comandos con su aprobación | 7 | `Paso.EjecutarComandos` |
| Paso 5: permission modes (default / acceptEdits / plan) | 8 | `Paso.EntenderPermissionModes` |
| Paso 6: slash commands esenciales | 9 | `Paso.SlashCommands` + `SlashCommandsEsencialesSlide9` |
| Paso 7: `/init` y CLAUDE.md | 10 | `Paso.CrearClaudeMd` |
| Paso 8: pedirle un test xUnit trivial | 11 | `Paso.PedirUnTest` |
| Trucos: cómo escribir buenos prompts (anti-patterns) | 12 | `PromptPatronDetector.Analizar` |
| Auth: claude.ai login vs API key | 13 | `MetodoAuth` |

## Estructura

```
S9.P2-practica-claude-code-primer-comando/
├── src/Practica.PrimerComando.Demo.Api/
│   ├── PrimerComando/  PrimerComandoPreflight, PasoEvaluator,
│   │                  PromptPatronDetector
│   │                  + IPracticaPrimerComandoPlanner/PracticaPrimerComandoPlanner
│   ├── Endpoints/  PrimerComandoEndpoints (/health, /primercomando/*)
│   └── Program.cs  AddSingleton<IPracticaPrimerComandoPlanner> + enums por nombre
└── tests/Practica.PrimerComando.Demo.Api.Tests/
    ├── Unit_*                lógica pura (preflight, pasos, patterns)
    ├── DiContainer_Tests     resuelve el planner
    └── Api_PrimerComandoTests E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 39 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `PrimerComandoPreflight` (todo OK → listo; sin Node / sin auth /
    sin cuenta / sin repo son bloqueantes; sin terminal moderna o sin
    git son avisos; `MetodoAuth.ApiKey` sirve igual que `ClaudeAi`).
  - `PasoEvaluator` (comando ejecutado + output visible → `Pasa`; ni
    uno ni el otro → `Falla`; solo uno → `Pendiente`; cada paso mapea
    al slide correcto del 4-11; sugerencias específicas para instalar
    `npm install`, `/init` y test xUnit).
  - `PromptPatronDetector` (3 anti-patterns: "mejora el código",
    "arregla los bugs", "crea una API completa"; 2 patterns positivos:
    "antes de implementar", rubber duck; prompt neutro → 50 puntos; 2
    anti-patterns → 0; anti + positivo se compensan a 50; cada
    hallazgo lleva causa y fix).
- **CAPA 0 · DI**: resuelve `IPracticaPrimerComandoPlanner` del
  contenedor real (`Assert.Same` singleton) y compone preflight +
  pasos + análisis de prompt + 8 slash commands + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/primercomando/{preflight, paso, slash-commands, prompt, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: la práctica se hace con
> Claude Code real (consume tokens, requiere API key). El valor que
> aporta este código es validar **que el alumno reconoce los pasos
> bien hechos** y **detecta los anti-patterns del prompt** antes de
> caer en ellos.

## Ejecución local

```bash
dotnet run --project src/Practica.PrimerComando.Demo.Api
# http://localhost:5119  — usa src/Practica.PrimerComando.Demo.Api/api.http
```

- `/primercomando/preflight` clasifica los requisitos en OK / Aviso /
  Bloqueante. Pide menos cosas que el preflight de S9.P (no requiere
  az/gh/ADO).
- `/primercomando/paso` evalúa cada uno de los 8 pasos del slide 2
  con `ComandoEjecutado` + `OutputEsperadoVisible`.
- `/primercomando/slash-commands` devuelve los 8 slash commands
  esenciales (slide 9).
- `/primercomando/prompt` detecta los 3 anti-patterns y 2 patterns
  positivos del slide 12 y puntúa el prompt 0-100.
- `/primercomando/plan` compone todo + checklist de 11 puntos.

## Flujo del alumno

1. **Preflight** → `/primercomando/preflight`. Si bloquea, instala
   Node, crea cuenta Anthropic o clona un sample.
2. **Sigue los 8 pasos** del slide en tu terminal con Claude Code
   real.
3. **Reporta evidencia** → `/primercomando/paso` por cada paso. Si
   alguno cae en `Pendiente` o `Falla`, las sugerencias te dicen
   exactamente qué probar.
4. **Pasa tu prompt** por `/primercomando/prompt` antes de enviárselo
   a Claude. Si tiene anti-patterns, refínalo siguiendo el fix.
5. **Cierra sesión limpia** con `/exit` o `Ctrl+C`. Plan + checklist
   en `/primercomando/plan` con todas las evidencias.

## Ideas centrales

> Esta práctica es la **puerta de entrada a M09**: el alumno termina
> con Claude Code instalado, una sesión real completada, un
> `CLAUDE.md` generado y un test ejecutándose. **Los 3 anti-patterns
> del slide 12** son los que se ven en el 80% de los prompts de
> alumnos nuevos — detectarlos en 5 segundos vale más que toda la
> teoría del slide. **Permission modes** (slide 8) son la diferencia
> entre "Claude rompe cosas" y "Claude trabaja contigo": empezad en
> `default`, pasad a `acceptEdits` cuando el flujo iterativo cueste.

## Cierre M09

Con S9.P2 cerramos el módulo M09 (7/7): S9.1 intro + S9.2 casos de
uso + S9.3 infraestructura + S9.4 MCP + S9.5 buenas prácticas + S9.P
práctica avanzada + S9.P2 práctica intro. **Próximo: M10 — Proyecto
Integrador**.
