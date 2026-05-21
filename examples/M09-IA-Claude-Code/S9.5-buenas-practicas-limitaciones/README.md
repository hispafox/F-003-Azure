# S9.5 — Buenas prácticas y limitaciones de IA en desarrollo

> **Submódulo de referencia:** [M09-S9.5](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.5-buenas-practicas-limitaciones-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; no invoca Claude Code real)

> 🎓 **Submódulo conceptual** (lección 9 del HANDOFF). Cierra los 5
> submódulos teóricos de M09 con las **defensas pedagógicas**:
> detectar anti-patterns de uso, validar la estructura de 7 secciones
> del prompt (slide 12) y clasificar cuándo la IA acelera o frena.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el auditor financiero externo como analogía (cuaderno de 10 banderas rojas + plantilla de 7 capítulos del informe + catálogo de riesgos por tipo de operación), las 7 reglas de oro como ADN del equipo que adopta Claude Code bien, y los tres mecanismos voluntarios (detector quincenal, validador para la biblioteca de prompts, matriz Acelera/Frena en planning) para introducirlo sin paternalismo.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Detector de los 10 anti-patterns (slide 13) | [`AntiPatternDetector.cs`](src/ClaudeCode.Limites.Demo.Api/Limites/AntiPatternDetector.cs) |
| Validador del template de 7 secciones (slide 12) | [`PromptStructureValidator.cs`](src/ClaudeCode.Limites.Demo.Api/Limites/PromptStructureValidator.cs) |
| Clasificador acelera vs frena (slide 5) | [`AceleraOFrenaClassifier.cs`](src/ClaudeCode.Limites.Demo.Api/Limites/AceleraOFrenaClassifier.cs) |
| Plan + 7 reglas de oro (slide 2) + checklist | [`ILimitesPlanner.cs`](src/ClaudeCode.Limites.Demo.Api/Limites/ILimitesPlanner.cs) |
| API que expone la lógica (`/limites/*`) | [`LimitesEndpoints.cs`](src/ClaudeCode.Limites.Demo.Api/Endpoints/LimitesEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Las 7 reglas de oro del desarrollo asistido | 2 | `LimitesPlanner.ReglasDeOroSlide2` |
| Prompt engineering: malo vs bueno | 3 | `PromptStructureValidator` (4 ingredientes ya en S9.2; aquí 7 secciones) |
| Alucinaciones: APIs/métodos/configs inventadas | 4 | `Checklist` paso 5 |
| Cuándo IA acelera y cuándo frena | 5 | `AceleraOFrenaClassifier.Clasificar` |
| Privacidad y compliance | 6 | `AntiPattern.SecretosOPiiEnPrompt` |
| Métricas: medir el impacto de IA | 7/10/14 | (no se modela — varía por equipo) |
| El futuro: agentes autónomos | 8/11 | `Checklist` paso 10 |
| Biblioteca de prompts del equipo | 9 | `Checklist` (regla #7) |
| Anatomía del prompt efectivo (7 secciones) | 12 | `PromptStructureValidator.Validar` |
| Los 10 anti-patterns | 13 | `AntiPatternDetector.Detectar` |

## Estructura

```
S9.5-buenas-practicas-limitaciones/
├── src/ClaudeCode.Limites.Demo.Api/
│   ├── Limites/    AntiPatternDetector, PromptStructureValidator,
│   │              AceleraOFrenaClassifier
│   │              + ILimitesPlanner/LimitesPlanner
│   ├── Endpoints/  LimitesEndpoints (/health, /limites/*)
│   └── Program.cs  AddSingleton<ILimitesPlanner> + enums por nombre
└── tests/ClaudeCode.Limites.Demo.Api.Tests/
    ├── Unit_*                lógica pura (anti-patterns, estructura, acelera-frena)
    ├── DiContainer_Tests     resuelve ILimitesPlanner
    └── Api_LimitesTests      E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 44 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `AntiPatternDetector` (cada anti-pattern del slide 13 detecta su
    frase canónica; descripción con múltiples anti-patterns reporta
    todos; no duplica el mismo anti-pattern aunque aparezcan varias
    palabras-clave; descripción limpia → `Limpio=true` y `Hallazgos=[]`;
    cada hallazgo lleva causa y fix concretos del slide).
  - `PromptStructureValidator` (prompt con las 7 secciones llega a
    100; prompt vago tiene puntuación < 30 y faltan ≥ 5 secciones;
    contexto + objetivo detectados aunque falten los demás; sugerencias
    son 1:1 con secciones faltantes; constraints detectado por "no
    romper"; DoD detectado por "tests verdes").
  - `AceleraOFrenaClassifier` (boilerplate, transformación, IaC,
    docs, análisis logs, refactor mecánico → `Acelera`; lógica de
    negocio compleja, arquitectura, perf tuning, seguridad,
    race conditions → `Frena`; `Otro` → `Neutro`; todos llevan slide
    y razones; perf tuning recuerda "medir antes").
- **CAPA 0 · DI**: resuelve `ILimitesPlanner` del contenedor real
  (`Assert.Same` singleton) y compone anti-patterns + estructura +
  clasificación + reglas + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/limites/{reglas, antipatterns, estructura, acelera-o-frena/{tipo}, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: el valor pedagógico está
> en **las decisiones previas al uso de Claude Code** (qué tarea
> delegar, cómo escribir el prompt, qué anti-patterns evitar). Eso es
> lógica pura.

## Ejecución local

```bash
dotnet run --project src/ClaudeCode.Limites.Demo.Api
# http://localhost:5117  — usa src/ClaudeCode.Limites.Demo.Api/api.http
```

- `/limites/reglas` devuelve las 7 reglas de oro del slide 2.
- `/limites/antipatterns` analiza una descripción de cómo el equipo
  usa Claude Code y reporta cada anti-pattern detectado + su fix.
- `/limites/estructura` puntúa un prompt (0-100) según las 7 secciones
  del slide 12 y devuelve sugerencias para las faltantes.
- `/limites/acelera-o-frena/{tipo}` clasifica un tipo de tarea como
  `Acelera` / `Frena` / `Neutro` con razones del slide 5.
- `/limites/plan` compone todo + checklist defensiva de 10 puntos.

## Entregable

El entregable es **un onboarding al uso de Claude Code para el
equipo**:

1. **`.claude/REGLAS.md`** con las 7 reglas de oro (slide 2).
2. **`.claude/ANTIPATTERNS.md`** con los 10 anti-patterns + fix
   (slide 13) — usar `/limites/antipatterns` cada vez que el
   workflow cambie significativamente.
3. **`.claude/prompts/`** con los prompts validados del equipo —
   cada uno debe pasar `/limites/estructura` con ≥ 80.
4. **Política**: marcar las tareas del backlog como `[ia-acelera]` /
   `[ia-frena]` / `[ia-neutro]` usando `/limites/acelera-o-frena`. Las
   `[ia-frena]` van a un humano sin prompt.
5. **Métricas** del slide 7/10 trackeadas mensualmente (LoC, PRs,
   tiempo medio por feature, bugs / 1000 LoC).

## Ideas centrales

> Claude Code **no reemplaza el thinking, reemplaza el typing**
> (slide 13). Las defensas son tres: **revisar siempre**, **dar
> contexto en 7 secciones** (slide 12 amplía el de 4 del S9.2), y
> **saber cuándo no usarlo** (slide 5: lógica de negocio compleja,
> arquitectura, perf tuning fino, seguridad crítica, race conditions
> → mejor sin IA o con IA muy supervisada). Los 10 anti-patterns
> (slide 13) son patrones reales que se ven en equipos que adoptan IA
> sin proceso — detectarlos pronto evita la espiral del "código
> frankenstein".

## Próximo paso

[`S9.P — Práctica CC + MCP end-to-end`](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.P-practica-cc-mcp-v3.md):
montar un workflow completo con Claude Code + MCP a Azure DevOps /
GitHub.
