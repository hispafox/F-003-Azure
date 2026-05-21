# M11 — Bonus Claude Code en Azure · ejemplos

Ejemplos de código que acompañan al
[Módulo 11 — Bonus Claude Code en Azure](../../doc/M11-Bonus-Claude-Code-Azure).

**Módulo bonus opcional.** No estaba en el temario AZ-204 original;
en 2026 cubre el ecosistema Claude para Azure: setup, skills de
Microsoft, agentes especializados, MCP con servicios Azure, Cowork
para knowledge workers y un recap de cómo CC acelera cada uno de los
módulos M01-M10 anteriores.

Los ejemplos son **conceptuales** (lección 9 del HANDOFF): el ecosistema
Claude se ejecuta en el terminal/desktop del alumno; aquí extraemos
las heurísticas pedagógicas (clasificadores, recomendadores, planners)
para que el alumno valide su entendimiento antes de tocar comandos
reales.

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| [S11.1](../../doc/M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.1-introduccion-ia-agentica-azure.md) | Intro IA agéntica (generación, CC vs Cowork, nivel madurez) | [`S11.1-introduccion-ia-agentica-azure/`](S11.1-introduccion-ia-agentica-azure/README.md) | ✅ Disponible |
| [S11.2](../../doc/M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.2-claude-code-setup-azure.md) | Claude Code: setup avanzado para Azure (`.claude/`, `CLAUDE.md`, `permissions`, skills) | [`S11.2-claude-code-setup-azure/`](S11.2-claude-code-setup-azure/README.md) | ✅ Disponible |
| S11.3 | Skills: capacidades especializadas | — | ⏳ Pendiente |
| S11.4 | Agentes y subagentes | — | ⏳ Pendiente |
| S11.5 | MCP con servicios Azure | — | ⏳ Pendiente |
| S11.6 | Claude Code en cada módulo (recap M1-M10) | — | ⏳ Pendiente |
| S11.7 | Claude Cowork para Azure | — | ⏳ Pendiente |
| S11.8 | Workflows avanzados | — | ⏳ Pendiente |
| S11.P | Práctica — solución end-to-end | — | ⏳ Pendiente |
| S11.P2 | Práctica — Claude Code + Azure light | — | ⏳ Pendiente |

⏳ **Módulo M11 en construcción** (2/10).

## Patrón de tests

- **CAPA 1 · Unit**: heurísticas puras (clasificador de generación,
  recomendador de herramienta, evaluador de nivel de madurez).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve el planner real.
- **CAPA E2E**: la API completa vía `WebApplicationFactory`.
- **Sin integración**: Claude Code / Cowork se ejecutan en el entorno
  del alumno; aquí sólo validamos el razonamiento detrás de las
  decisiones.

## Requisitos comunes

- .NET SDK 10
- Para tocar los ejemplos en clase con Claude Code real: Node 18+,
  cuenta Anthropic, az CLI.
