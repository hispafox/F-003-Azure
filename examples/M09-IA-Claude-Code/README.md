# M09 — IA Claude Code · ejemplos

Ejemplos de código que acompañan al
[Módulo 9 — IA Claude Code](../../doc/M09-IA-Claude-Code).

Cambio fuerte de dominio respecto a M01–M08: aquí **Claude Code es la
herramienta** que el alumno aprende a usar, no algo que se despliega a
Azure. Por eso los ejemplos son **100% conceptuales** (lección 9 del
HANDOFF): extraen heurísticas pedagógicas (recomendador de modo de
ejecución, comparativa Claude Code vs Copilot, builder de
`settings.json` del equipo, validador de hooks, plan de adopción MCP)
en clases puras testeables, sin lanzar realmente un CLI ni invocar
APIs externas.

## Submódulos cubiertos

| Submódulo | Tema | Ejemplo | Estado |
| --- | --- | --- | --- |
| [S9.1](../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.1-claude-code-intro-v3.md) | Claude Code intro (modo, features, settings.json) | [`S9.1-claude-code-intro/`](S9.1-claude-code-intro/README.md) | ✅ Disponible |
| [S9.2](../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.2-claude-code-casos-uso-v3.md) | Casos de uso (clasificador, templates, evaluador de prompts) | [`S9.2-claude-code-casos-uso/`](S9.2-claude-code-casos-uso/README.md) | ✅ Disponible |
| [S9.3](../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.3-cc-infraestructura-v3.md) | CC + infraestructura (parser requirements, prompts canónicos, audit) | [`S9.3-cc-infraestructura/`](S9.3-cc-infraestructura/README.md) | ✅ Disponible |
| [S9.4](../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.4-mcp-herramientas-v3.md) | MCP y herramientas externas (parser config, recomendador, seguridad) | [`S9.4-mcp-herramientas/`](S9.4-mcp-herramientas/README.md) | ✅ Disponible |
| [S9.5](../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.5-buenas-practicas-limitaciones-v3.md) | Buenas prácticas y limitaciones (anti-patterns, 7 secciones, acelera vs frena) | [`S9.5-buenas-practicas-limitaciones/`](S9.5-buenas-practicas-limitaciones/README.md) | ✅ Disponible |
| S9.P | Práctica — CC + MCP end-to-end | — | ⏳ Pendiente |
| S9.P2 | Práctica — primer comando con Claude Code | — | ⏳ Pendiente |

⏳ **Módulo M09 en construcción** (5/7).

## Patrón de tests

- **CAPA 1 · Unit**: heurísticas puras (qué modo CC para qué tarea, qué
  features complementarias, qué allowed tools, qué hooks recomendados).
- **CAPA 0 · DI**: `WebApplicationFactory` resuelve el planner real.
- **CAPA E2E**: la API completa vía `WebApplicationFactory`.
- **Sin integración**: Claude Code no se ejecuta desde los tests
  (consumiría tokens y necesitaría API key); el alumno lo invoca en
  clase siguiendo el README y los slides.

## Requisitos comunes

- .NET SDK 10
- (Para usar Claude Code en clase) Node.js 18+, `npm install -g
  @anthropic-ai/claude-code`, API key de console.anthropic.com.
