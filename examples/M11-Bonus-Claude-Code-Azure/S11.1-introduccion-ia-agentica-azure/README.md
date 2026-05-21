# S11.1 — Introducción: IA agéntica para Azure (BONUS)

> **Submódulo de referencia:** [M11-S11.1](../../../doc/M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.1-introduccion-ia-agentica-azure.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; no invoca Claude Code/Cowork reales)

> 🎓 **Submódulo conceptual** (lección 9 del HANDOFF). Apertura del
> módulo bonus M11. Heurísticas para que el alumno se sitúe ante el
> ecosistema Claude para Azure: en qué generación de IA está cada
> herramienta, cuándo usar Claude Code o Cowork, y en qué nivel de
> madurez (1/2/3) está el equipo en uso de IA agéntica.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Clasificador por generación de IA (slide 3) | [`GeneracionIaClassifier.cs`](src/Bonus.IntroIaAgentica.Demo.Api/Intro/GeneracionIaClassifier.cs) |
| Comparador Claude Code vs Cowork (slide 9) | [`CcVsCoworkRecommender.cs`](src/Bonus.IntroIaAgentica.Demo.Api/Intro/CcVsCoworkRecommender.cs) |
| Evaluador del nivel de madurez (slide 10 + 18) | [`NivelUsoEvaluator.cs`](src/Bonus.IntroIaAgentica.Demo.Api/Intro/NivelUsoEvaluator.cs) |
| Plan + objetivos M11 (slide 7) + checklist | [`IIntroIaAgenticaPlanner.cs`](src/Bonus.IntroIaAgentica.Demo.Api/Intro/IIntroIaAgenticaPlanner.cs) |
| API que expone la lógica (`/intro/*`) | [`IntroEndpoints.cs`](src/Bonus.IntroIaAgentica.Demo.Api/Endpoints/IntroEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Las 3 generaciones de IA para desarrollo | 3 | `GeneracionIaClassifier.Clasificar` |
| Qué es un agente (loop percibir/razonar/actuar) | 4 | (intro README) |
| Ecosistema Anthropic para Azure (abril 2026) | 5 | (intro README) |
| Filosofía: skills abiertos de Microsoft | 6 | (intro README) |
| Qué vais a construir en M11 | 7 | `IntroIaAgenticaPlanner.ObjetivosM11Slide7` |
| Precondiciones (Node + az + cuenta Anthropic) | 8 | `Checklist` |
| Claude Code vs Cowork | 9 | `CcVsCoworkRecommender` |
| Los 3 niveles de uso | 10 | `NivelUsoEvaluator` → `Nivel1/2/3` |
| ROI en datos reales | 11 | (intro README) |
| Privacidad y compliance + Claude en Foundry | 13/14 | `Checklist` paso 4 |
| Los 4 principios del equipo que adopta IA bien | 18 | `NivelUsoEvaluator.PrincipiosCumplidos` |

## Estructura

```
S11.1-introduccion-ia-agentica-azure/
├── src/Bonus.IntroIaAgentica.Demo.Api/
│   ├── Intro/      GeneracionIaClassifier, CcVsCoworkRecommender,
│   │              NivelUsoEvaluator
│   │              + IIntroIaAgenticaPlanner/IntroIaAgenticaPlanner
│   ├── Endpoints/  IntroEndpoints (/health, /intro/*)
│   └── Program.cs  AddSingleton<IIntroIaAgenticaPlanner> + enums por nombre
└── tests/Bonus.IntroIaAgentica.Demo.Api.Tests/
    ├── Unit_*                lógica pura (generación, comparador, nivel)
    ├── DiContainer_Tests     resuelve el planner
    └── Api_IntroTests        E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 33 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `GeneracionIaClassifier` (Claude Code → Gen3; Copilot inline →
    Gen1; ChatGPT → Gen2; sin palabras clave → `Desconocida`; cada
    generación lleva años + contexto + acción).
  - `CcVsCoworkRecommender` (tabla canónica con 12 filas; dev en
    terminal → CC; PM con informes → Cowork; equipo mixto → Ambas;
    sin señales → CC por defecto).
  - `NivelUsoEvaluator` (sólo prompts concretos → Nivel 1; skills +
    MCP → Nivel 2; agents + workflows → Nivel 3; los 4 principios del
    slide 18 se cuentan independientes; sugerencias adaptadas al
    nivel actual y a principios faltantes).
- **CAPA 0 · DI**: resuelve `IIntroIaAgenticaPlanner` del contenedor
  real (`Assert.Same` singleton) y compone clasificación + recomendación
  + nivel + objetivos M11 + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/intro/{generacion, comparativa, recomendar, nivel, objetivos, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: Claude Code y Cowork se
> instalan y corren en el entorno del alumno. Aquí lo testeable son
> las **heurísticas previas a usarlos**: qué herramienta elegir, en
> qué nivel de madurez está el equipo y qué principios faltan por
> aplicar.

## Ejecución local

```bash
dotnet run --project src/Bonus.IntroIaAgentica.Demo.Api
# http://localhost:5122  — usa src/Bonus.IntroIaAgentica.Demo.Api/api.http
```

- `/intro/generacion` clasifica una descripción de uso en Gen1
  (autocompletado), Gen2 (chat) o Gen3 (agente).
- `/intro/comparativa` devuelve la tabla canónica de 12 filas del
  slide 9 (Claude Code vs Cowork).
- `/intro/recomendar` decide CC / Cowork / Ambas según el escenario
  del equipo.
- `/intro/nivel` evalúa madurez 1/2/3 + cuántos de los 4 principios
  del slide 18 cumple el equipo + próximos pasos.
- `/intro/objetivos` lista los 7 objetivos del M11 (slide 7).
- `/intro/plan` compone todo + checklist de 7 puntos de arranque.

## Flujo del alumno

1. **Sitúate en el ecosistema** → `/intro/generacion` con la
   herramienta que ya uses (Copilot, ChatGPT, Claude Code…). Si te
   sale Gen1/Gen2, M11 te abre Gen3.
2. **Decide tu herramienta principal** → `/intro/recomendar` con tu
   rol (dev / PM / mixto).
3. **Evalúa la madurez actual del equipo** → `/intro/nivel`. Si estás
   en Nivel 1, M11 te lleva a Nivel 2 (skills + MCP) y luego a Nivel
   3 (agents + workflows).
4. **Aplica los 4 principios desde el día 1**: skills en Git +
   permisos mínimos + humano en loop + auditar uso (slide 18).
5. Pasa al **S11.2 (setup)** cuando tengas claro qué herramienta usar
   y en qué nivel quieres aterrizar.

## Ideas centrales

> Estamos en la **generación 3 de IA para desarrollo** (slide 3): los
> agentes ejecutan tareas multipaso (leer, escribir, ejecutar,
> verificar), no solo completan texto. **Para Azure hay un ecosistema
> entero** (slide 5): Claude Code + Cowork + Skills oficiales de
> Microsoft + MCP servers (Azure, Bicep, ADO). **CC y Cowork no se
> solapan** (slide 9): CC para devs en terminal, Cowork para
> knowledge workers; equipos mixtos pueden usar ambas. **Los 3
> niveles de uso** (slide 10) son una escalera realista: empieza por
> Nivel 1, sube a Nivel 2 cuando tengas skills + MCP, y a Nivel 3
> cuando tengas agents propios y workflows. **Los 4 principios del
> slide 18** son las barandillas que evitan que la productividad
> cobre como auditoría/seguridad.

## Próximo paso

[`S11.2 — Claude Code: setup para Azure`](../../../doc/M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.2-claude-code-setup-azure.md):
instalar Claude Code, configurar `azure-skills` plugin, conectar
Azure MCP + Bicep MCP + ADO MCP.
