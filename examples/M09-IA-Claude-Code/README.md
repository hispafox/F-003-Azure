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
| S9.2 | Casos de uso (refactor, IaC, debugging) | — | ⏳ Pendiente |
| S9.3 | CC + infraestructura (Bicep, ARM, AVM) | — | ⏳ Pendiente |
| S9.4 | MCP y herramientas externas | — | ⏳ Pendiente |
| S9.5 | Buenas prácticas y limitaciones | — | ⏳ Pendiente |
| S9.P | Práctica — CC + MCP end-to-end | — | ⏳ Pendiente |
| S9.P2 | Práctica — primer comando con Claude Code | — | ⏳ Pendiente |

⏳ **Módulo M09 en construcción** (1/7).

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
