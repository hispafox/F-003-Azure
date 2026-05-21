# S9.4 — MCP: Model Context Protocol y herramientas externas

> **Submódulo de referencia:** [M09-S9.4](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.4-mcp-herramientas-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; no invoca servidores MCP reales)

> 🎓 **Submódulo conceptual** (lección 9 del HANDOFF). MCP conecta
> Claude Code con ADO, GitHub, bases de datos, Notion, Slack, etc.
> Aquí extraemos las heurísticas pedagógicas: parser del
> `claude_desktop_config.json`, recomendador de servers por escenario
> del equipo y security checker contra los riesgos del slide 9.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el cinturón de llaves del técnico de mantenimiento como analogía (cada MCP server es una llave etiquetada con su alcance), recomendador de servers por escenario del equipo con permisos mínimos escritos, security checker contra los tres anti-patterns del slide 9 (tokens hardcoded, `filesystem` en `/`, servers de Git sin rotación) y el config versionado como template con `${VAR}` por defecto.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Parser del `claude_desktop_config.json` (slide 3) | [`McpConfigParser.cs`](src/ClaudeCode.Mcp.Demo.Api/Mcp/McpConfigParser.cs) |
| Recomendador de servers MCP por escenario (slides 4-7, 11, 15) | [`McpServerRecommender.cs`](src/ClaudeCode.Mcp.Demo.Api/Mcp/McpServerRecommender.cs) |
| Security checker: credenciales hardcoded + paths amplios + rotación (slide 9) | [`McpSecurityChecker.cs`](src/ClaudeCode.Mcp.Demo.Api/Mcp/McpSecurityChecker.cs) |
| Plan + checklist del onboarding MCP | [`IMcpPlanner.cs`](src/ClaudeCode.Mcp.Demo.Api/Mcp/IMcpPlanner.cs) |
| API que expone la lógica (`/mcp/*`) | [`McpEndpoints.cs`](src/ClaudeCode.Mcp.Demo.Api/Endpoints/McpEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Qué es MCP y por qué importa | 2 | `Checklist` (intro) |
| Configurar `mcpServers` en el JSON | 3 | `McpConfigParser.Parsear` |
| MCP + Azure DevOps | 4 | `EscenarioMcp.UsaAzureDevOps` → server `azure-devops` |
| MCP + GitHub | 5 | `EscenarioMcp.UsaGitHub` → server `github` |
| MCP + bases de datos | 6 | `UsaCosmosDb`, `UsaSqlServer`, `UsaPostgres` |
| MCP + Notion / Slack / Email | 7 | `UsaNotionODocs`, `UsaSlackOTeams` |
| Crear MCP server custom | 8 | (no se modela — se cubre en práctica S9.P) |
| Seguridad: tokens hardcoded, scope amplio | 9 | `McpSecurityChecker.Comprobar` |
| Flujo completo de desarrollo | 10/12 | `Checklist` paso 7 |
| Servers disponibles 2026 (tabla) | 11/15 | `McpServerRecommender` cubre los principales |
| Dashboards / reportes con MCP | 13 | (combinación libre de servers) |
| Automatización con scripts headless | 14 | `Checklist` paso 8 |

## Estructura

```
S9.4-mcp-herramientas/
├── src/ClaudeCode.Mcp.Demo.Api/
│   ├── Mcp/        McpConfigParser, McpServerRecommender,
│   │              McpSecurityChecker
│   │              + IMcpPlanner/McpPlanner
│   ├── Endpoints/  McpEndpoints (/health, /mcp/*)
│   └── Program.cs  AddSingleton<IMcpPlanner> + enums por nombre
└── tests/ClaudeCode.Mcp.Demo.Api.Tests/
    ├── Unit_*                lógica pura (parser, recomendador, seguridad)
    ├── DiContainer_Tests     resuelve IMcpPlanner
    └── Api_McpTests          E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 33 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `McpConfigParser` (parsea N servers con `command`/`args`/`env`;
    sin clave `mcpServers` → aviso y `Servers=[]`; server sin
    `command` → aviso; JSON inválido devuelve aviso sin lanzar;
    server que no es objeto → aviso).
  - `McpServerRecommender` (`filesystem` siempre incluido; cada flag
    `UsaXxx` añade su server; cada server lleva permisos mínimos no
    vacíos y `Slide` para referencia; sin flags → solo filesystem).
  - `McpSecurityChecker` (token GitHub `ghp_…` plano → `Critico`;
    `${VAR}` o `$env:VAR` → no es crítico; `env` sensible vacío →
    `Alto`; `filesystem` con `/` → `Critico`; `filesystem` con path
    restringido → no crítico; server de Git siempre genera un aviso
    `Medio` para recordar rotación 90 días; config conforme →
    `Seguro=true` con `Criticos=0` y `Altos=0`).
- **CAPA 0 · DI**: resuelve `IMcpPlanner` del contenedor real
  (`Assert.Same` singleton) y compone recomendados + config parseada
  + seguridad + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/mcp/{config/parsear, recomendar, seguridad, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: arrancar MCP servers
> reales (ADO, GitHub, Cosmos) consume credenciales, segundos de
> startup y rate limits. El valor pedagógico está en **diseñar el
> config bien** y en **detectar los anti-patterns** antes de
> ejecutar — eso es lógica pura.

## Ejecución local

```bash
dotnet run --project src/ClaudeCode.Mcp.Demo.Api
# http://localhost:5116  — usa src/ClaudeCode.Mcp.Demo.Api/api.http
```

- `/mcp/config/parsear` mastica el JSON y devuelve la lista de
  servers detectados + avisos de estructura.
- `/mcp/recomendar` propone qué servers habilitar según las
  herramientas del equipo (ADO, GitHub, Cosmos, Notion, Slack, etc.).
- `/mcp/seguridad` analiza el config en busca de tokens hardcoded,
  paths demasiado amplios y servers de Git sin rotación.
- `/mcp/plan` compone todo + checklist de 8 puntos para el onboarding.

## Entregable

El entregable es el `claude_desktop_config.json` del equipo
versionado en git **como template** (sin credenciales):

1. Identifica qué herramientas usa el equipo → `/mcp/recomendar`.
2. Habilita los servers recomendados con `command`, `args` y `env`.
3. **Nunca** pongas credenciales en plano — usa `${VAR}` o
   `$env:VAR` y exporta el valor desde el entorno del usuario
   (`~/.bashrc`, `$PROFILE` de PowerShell o key vault local).
4. Tokens con permisos mínimos: **read-only por defecto**, write
   sólo cuando Claude tenga que crear PRs / issues.
5. `filesystem` server restringido **al directorio del proyecto**,
   nunca a `/` o `$HOME`.
6. Pasa el config por `/mcp/seguridad` antes de commitear — debe
   salir `Seguro=true` con `Criticos=0`.
7. Calendario de rotación en `docs/mcp-rotation.md` (90 días
   por defecto).

## Ideas centrales

> MCP convierte Claude Code en un **agente que vive en tu workflow
> real**: lee work items de ADO, crea PRs en GitHub, consulta
> Cosmos, actualiza Notion. El precio es que cada server tiene
> credenciales reales y acceso a sistemas reales — la **higiene de
> seguridad** (least privilege, env vars, paths restringidos,
> rotación) deja de ser opcional. **No commitees credenciales**: el
> JSON versionado lleva `${VAR}`, el valor real se exporta desde el
> entorno (slide 9). **El `filesystem` server siempre acotado** al
> directorio del proyecto — un `/` o `$HOME` es una bomba.

## Próximo paso

[`S9.5 — Buenas prácticas y limitaciones`](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.5-buenas-practicas-limitaciones-v3.md):
cuándo NO usar Claude Code, riesgos típicos, antipatterns y cómo
revisar lo que Claude propone.
