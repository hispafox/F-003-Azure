# S9.P — Práctica: Claude Code + MCP en acción

> **Submódulo de referencia:** [M09-S9.P](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.P-practica-cc-mcp-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; el alumno ejecuta los ejercicios reales en su terminal)

> 🎓 **Práctica conceptual** (lección 9 del HANDOFF). El alumno hace
> los 8 ejercicios en su terminal con Claude Code real. Aquí
> extraemos las heurísticas que validan que cada ejercicio se
> completó bien: preflight, evaluador de ejercicios y comparador de
> prompts (slide 12 con 3 niveles de detalle).

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el primer vuelo en solitario del aprendiz de piloto como analogía (preflight literal, 8 maniobras evaluadas, comunicación radio = comparativa de prompts), evaluador con tres niveles Pasa / Pendiente / Falla y acciones concretas por ejercicio, comparador del slide 12 con el cap a 25 puntos si el prompt mide menos de 40 caracteres.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Preflight de requisitos (slide 2/8) | [`PracticaPreflight.cs`](src/Practica.CcMcp.Demo.Api/Practica/PracticaPreflight.cs) |
| Evaluador de los 8 ejercicios (slides 3-7, 11-13) | [`EjercicioEvaluator.cs`](src/Practica.CcMcp.Demo.Api/Practica/EjercicioEvaluator.cs) |
| Comparador de prompts vago / medio / detallado (slide 12) | [`PromptComparison.cs`](src/Practica.CcMcp.Demo.Api/Practica/PromptComparison.cs) |
| Plan + checklist de la práctica (slide 8) | [`IPracticaCcMcpPlanner.cs`](src/Practica.CcMcp.Demo.Api/Practica/IPracticaCcMcpPlanner.cs) |
| API que expone la lógica (`/practica/*`) | [`PracticaEndpoints.cs`](src/Practica.CcMcp.Demo.Api/Endpoints/PracticaEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Qué se hace en la práctica | 2 | `Checklist` (intro) |
| Ejercicio 1: generar servicio completo + tests | 3 | `Ejercicio.GenerarServicioCompleto` |
| Ejercicio 2: generar Bicep + validate | 4 | `Ejercicio.GenerarBicep` |
| Ejercicio 3: MCP con Azure DevOps | 5 | `Ejercicio.McpConAzureDevOps` |
| Ejercicio 4: análisis de error de producción | 6 | `Ejercicio.AnalisisDeError` |
| Ejercicio 5: refactoring con IA | 7 | `Ejercicio.RefactoringConIa` |
| Checklist completa | 8 | `IPracticaCcMcpPlanner.Checklist` |
| Ejercicio 6: documentación completa | 11 | `Ejercicio.GenerarDocumentacion` |
| Ejercicio 7: comparativa de prompts (vago/medio/detallado) | 12 | `PromptComparison.Comparar` |
| Ejercicio 8: crear MCP server custom | 13 | `Ejercicio.McpServerCustom` |

## Estructura

```
S9.P-practica-cc-mcp/
├── src/Practica.CcMcp.Demo.Api/
│   ├── Practica/   PracticaPreflight, EjercicioEvaluator,
│   │              PromptComparison
│   │              + IPracticaCcMcpPlanner/PracticaCcMcpPlanner
│   ├── Endpoints/  PracticaEndpoints (/health, /practica/*)
│   └── Program.cs  AddSingleton<IPracticaCcMcpPlanner> + enums por nombre
└── tests/Practica.CcMcp.Demo.Api.Tests/
    ├── Unit_*                lógica pura (preflight, ejercicios, prompts)
    ├── DiContainer_Tests     resuelve IPracticaCcMcpPlanner
    └── Api_PracticaTests     E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 34 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `PracticaPreflight` (todo OK → `ListoParaArrancar=true`; sin Node
    18 → bloqueante; sin Claude autenticado → bloqueante; sin API key →
    bloqueante; sin repo local → bloqueante; sin az/gh CLI o sin
    CLAUDE.md → aviso, no bloquea).
  - `EjercicioEvaluator` (compila + tests + convenciones → `Pasa`; ni
    compila ni tests → `Falla`; compila pero validate falla →
    `Pendiente`; sin convenciones aporta sugerencia específica; cada
    `Ejercicio` mapea a su slide correcto; Bicep validate fallido
    sugiere pasar el output a Claude; MCP server custom sugiere
    `mcp-inspector`).
  - `PromptComparison` (vago < detallado en puntuación; delta positivo;
    detallado sin criterio éxito avisa en lecciones; detallado con los
    4 ingredientes detecta los 4; vago < 40 chars se capa a 25;
    lecciones incluyen `/100`).
- **CAPA 0 · DI**: resuelve `IPracticaCcMcpPlanner` del contenedor
  real (`Assert.Same` singleton) y compone preflight + ejercicios +
  comparativa + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/practica/{preflight, ejercicio, comparativa, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: la práctica se hace con
> Claude Code real en el terminal del alumno (consume tokens, requiere
> API key). El valor que aporta este código es validar **que el alumno
> sabe medir** si el ejercicio se completó bien.

## Ejecución local

```bash
dotnet run --project src/Practica.CcMcp.Demo.Api
# http://localhost:5118  — usa src/Practica.CcMcp.Demo.Api/api.http
```

- `/practica/preflight` clasifica los requisitos en OK / Aviso /
  Bloqueante y devuelve `listoParaArrancar`.
- `/practica/ejercicio` evalúa un ejercicio concreto con 3 flags
  (compila/lint, tests/validate, convenciones) y devuelve veredicto +
  acciones.
- `/practica/comparativa` puntúa 3 prompts (vago/medio/detallado),
  calcula el delta y emite lecciones del slide 12.
- `/practica/plan` compone todo + checklist de 10 puntos.

## Flujo del alumno

1. **Preflight** → `/practica/preflight` con tu setup real. Si está
   bloqueante, instala lo que falte antes de empezar.
2. **Ejecuta cada ejercicio** del slide 3-7 / 11-13 con Claude Code
   real en tu terminal.
3. **Reporta evidencia** → `/practica/ejercicio` por cada ejercicio.
   El veredicto + las acciones te guían si algo falla.
4. **Comparativa de prompts** (slide 12) → escribe los 3 niveles y
   pasa por `/practica/comparativa`. Internalizar el delta es la
   lección clave del módulo.
5. **Plan completo** → `/practica/plan` con todas las evidencias.
   Marca los 10 ítems de la checklist y guárdala como evidencia de la
   práctica.

## Ideas centrales

> Esta práctica **cierra los 5 submódulos teóricos** (S9.1-S9.5)
> aplicando todo en flujos reales: instalar Claude Code (S9.1),
> identificar casos de uso (S9.2), generar IaC (S9.3), conectar con
> ADO/MCP (S9.4) y respetar las defensas (S9.5). La **comparativa de
> prompts** del slide 12 es el "momento aha" — ver que un prompt
> detallado obtiene 75-100 puntos vs un prompt vago < 25 demuestra el
> ROI del 30-50% del slide 7. **MCP server custom** es la parte
> avanzada: pasar de "uso Claude Code" a "construyo herramientas para
> mi equipo".

## Próximo paso

[`S9.P2 — Práctica: primer comando con Claude Code`](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.P2-practica-claude-code-primer-comando-v1.md):
versión simplificada para alumnos que arrancan, con un solo comando
end-to-end.
