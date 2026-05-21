# S11.3 — Skills: capacidades especializadas para Azure (BONUS)

> **Submódulo de referencia:** [M11-S11.3](../../../doc/M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.3-skills-capacidades-especializadas.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; no carga skills reales)

> 🎓 **Submódulo conceptual** (lección 9 del HANDOFF). Modela el
> estándar abierto `SKILL.md`: el frontmatter, la `description` como
> campo que decide la carga (progressive disclosure) y los
> anti-patrones del slide 17. Lo testeable son las heurísticas que
> deciden si un skill está bien escrito **antes** de que Claude lo
> cargue.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Scorer de la `description` (slide 16/24) | [`SkillDescriptionScorer.cs`](src/Bonus.SkillsAzure.Demo.Api/Skills/SkillDescriptionScorer.cs) |
| Validador del frontmatter (slide 6) | [`SkillFrontmatterValidator.cs`](src/Bonus.SkillsAzure.Demo.Api/Skills/SkillFrontmatterValidator.cs) |
| Detector de anti-patrones (slide 17) | [`SkillAntiPatternDetector.cs`](src/Bonus.SkillsAzure.Demo.Api/Skills/SkillAntiPatternDetector.cs) |
| Plan + Microsoft skills (slide 18) + roadmap (slide 27) | [`ISkillLibraryPlanner.cs`](src/Bonus.SkillsAzure.Demo.Api/Skills/ISkillLibraryPlanner.cs) |
| API que expone la lógica (`/skills/*`) | [`SkillsEndpoints.cs`](src/Bonus.SkillsAzure.Demo.Api/Endpoints/SkillsEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Qué es un skill (carpeta + SKILL.md) | 2 | (README) |
| Estándar abierto SKILL.md (agentskills.io) | 3 | `SkillFrontmatterValidator.Validar` (parser) |
| Tipos de skill (built-in / proyecto / personal / plugin) | 4 | `SkillLibraryPlanner` checklist |
| Progressive disclosure (frontmatter → contenido) | 5 | `SkillDescriptionScorer` (la `description` decide) |
| Frontmatter: todos los campos | 6 | `SkillFrontmatter` + `SkillFrontmatterValidator` |
| `context: fork` → subagent | 14 | `SkillFrontmatterValidator` (fork sin agent → aviso) |
| Crear un skill (`/skill-creator`) | 15 | `Checklist` paso 2 |
| Descriptions efectivas | 16 | `SkillDescriptionScorer.Evaluar` |
| Anti-patterns (los 5 DON'Ts) | 17 | `SkillAntiPatternDetector.Detectar` |
| Los skills de Microsoft (azure-skills plugin) | 18 | `SkillLibraryPlanner.SkillsMicrosoftSlide18` |
| Skills recomendados del equipo | 9-13 | `SkillLibraryPlanner.SkillsRecomendadosEquipo` |
| Gobierno: proyecto (PR) vs personal | 22 | `Checklist` paso 7 |
| Testing de descriptions | 24 | `SkillDescriptionScorer` (fiable sí/no) |
| Roadmap de adopción | 27 | `SkillLibraryPlanner.RoadmapSlide27` |

## Estructura

```
S11.3-skills-capacidades-especializadas/
├── src/Bonus.SkillsAzure.Demo.Api/
│   ├── Skills/     SkillDescriptionScorer, SkillFrontmatterValidator,
│   │              SkillAntiPatternDetector
│   │              + ISkillLibraryPlanner/SkillLibraryPlanner
│   ├── Endpoints/  SkillsEndpoints (/health, /skills/*)
│   └── Program.cs  AddSingleton<ISkillLibraryPlanner> + enums por nombre
└── tests/Bonus.SkillsAzure.Demo.Api.Tests/
    ├── Unit_*                lógica pura (description, frontmatter, antipatrones)
    ├── DiContainer_Tests     resuelve el planner
    └── Api_SkillsTests       E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 35 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `SkillDescriptionScorer` (description específica con keywords →
    fiable; lenguaje vago `help`/`maybe`/`puede` penaliza 25 pts cada
    uno; verbo de acción inicial suma; puntuación acotada 0-100;
    sugerencias por vaguedad / falta de keywords / longitud).
  - `SkillFrontmatterValidator` (parsea el bloque `---...---`; falta
    `name`/`description` → Error; `context: fork` sin `agent` →
    Advertencia; sin `allowed-tools` → Advertencia de menor
    privilegio; limpia comentarios inline del valor).
  - `SkillAntiPatternDetector` (credencial literal → Error; tools
    `Bash(*)`/`Write(**)`/`Edit(**)` → Advertencia; skill > 500
    líneas → Advertencia de tamaño).
- **CAPA 0 · DI**: resuelve `ISkillLibraryPlanner` del contenedor
  real (`Assert.Same` singleton) y compone frontmatter + description +
  antipatrones + catálogo Microsoft + recomendados + roadmap +
  checklist; con y sin `SkillMd`.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/skills/{frontmatter, description, antipatterns, microsoft, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: los skills se cargan
> dinámicamente dentro de Claude Code. Aquí lo testeable es la
> **calidad del SKILL.md antes de cargarlo**: ¿el frontmatter está
> completo?, ¿la description hará que Claude lo active?, ¿tiene
> anti-patrones? Validar la carga real es ruido en una clase.

## Ejecución local

```bash
dotnet run --project src/Bonus.SkillsAzure.Demo.Api
# http://localhost:5124  — usa src/Bonus.SkillsAzure.Demo.Api/api.http
```

- `/skills/frontmatter` parsea el `SKILL.md` y valida campos
  obligatorios (`name`, `description`) + consistencia `context`/
  `agent` + `allowed-tools` (slide 6).
- `/skills/description` puntúa la `description` 0-100 y dice si Claude
  la cargará de forma fiable (slide 16).
- `/skills/antipatterns` detecta los DON'Ts del slide 17: credenciales,
  tools demasiado amplios, skill enorme.
- `/skills/microsoft` devuelve los 8 skills de Microsoft más usados
  (slide 18).
- `/skills/plan` compone todo + skills recomendados del equipo +
  roadmap de adopción + checklist de 8 puntos.

## Flujo del alumno

1. **Escribe la `description` primero** → `/skills/description`.
   Si no es "fiable", reescríbela con keywords concretas (servicio
   Azure + acción). Es lo único que decide si Claude carga el skill.
2. **Valida el frontmatter** → `/skills/frontmatter`. Sin `name` ni
   `description` el skill está roto. Si usas `context: fork`, declara
   el `agent`.
3. **Pasa el detector de anti-patrones** → `/skills/antipatterns`.
   Cero credenciales, `allowed-tools` al mínimo, < 500 líneas (si
   crece, usa archivos de apoyo).
4. **Instala el plugin oficial** →
   `/plugin install azure-skills@microsoft-azure` y mira los 8 skills
   más usados en `/skills/microsoft`.
5. **Construye tu biblioteca** siguiendo el roadmap del slide 27:
   empieza por `convenciones-equipo` y `deploy-checklist`, y crece
   hacia skills de dominio (`migrate-clickonce-msix`,
   `generate-azure-function`) y avanzados (context fork).

## Ideas centrales

> Un skill es **experiencia del equipo codificada** en algo que
> Claude usa automáticamente — no un "prompt guardado". El campo que
> manda es la `description` (slide 16): si es vaga, Claude nunca lo
> carga; si tiene keywords concretas, se activa cuando aplica
> (progressive disclosure, slide 5). El frontmatter define el
> contrato (slide 6) y los anti-patrones (slide 17) son las cinco
> formas de escribir un skill que estorba en vez de ayudar. Microsoft
> publica 20 skills oficiales (slide 18), pero los más valiosos son
> los vuestros.

## Próximo paso

[`S11.4 — Agentes y subagentes`](../../../doc/M11-Bonus-Claude-Code-Azure/v1-actual/M11-S11.4-agentes-subagentes-azure.md):
el siguiente nivel de especialización — agents custom, subagentes
con `context: fork`, y cómo orquestar varios agentes para Azure.
