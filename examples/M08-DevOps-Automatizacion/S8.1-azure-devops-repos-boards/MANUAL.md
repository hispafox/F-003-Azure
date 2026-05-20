# Manual del alumno — S8.1 · Azure DevOps: Repos, Boards y Artifacts

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta qué decisiones de arranque definen la salud de un proyecto en Azure DevOps a 12 meses vista, qué políticas de `main` no son negociables y por qué Conventional Commits es la convención que reduce más fricción en revisiones de código.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M08-S8.1](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.1-azure-devops-repos-boards-v3.md). Tres piezas de lógica pura (parser de Conventional Commits con detección de work items, evaluador de branch policies, advisor monorepo vs multi-repo) más un planificador que las une.

*Creado: 2026-05-20 22:40 +0200*

---

## 1. La idea en una frase

Azure DevOps es **la cadena de herramientas integrada de Microsoft** para gestionar un proyecto de software: repos Git, Boards (work items, sprints, jerarquía Epic→Feature→Story→Task), Pipelines (CI/CD), Artifacts (feeds NuGet privados) y Test Plans. La conversación de S8.1 no es "cómo usar la UI"; es **tomar tres decisiones de arranque** que condicionan todo lo que viene: ¿monorepo o multi-repo según el tamaño del equipo y los servicios?, ¿qué políticas de rama de `main` son no negociables?, ¿cómo nombramos los commits para que vincular a work items y generar release notes salga gratis?

El ejemplo materializa las tres decisiones como funciones puras testeables. La instancia de Azure DevOps real se monta en el portal (gratis para los primeros 5 usuarios) y se audita con scripts `az devops`.

---

## 2. El problema real que hay detrás

Tres situaciones que justifican el submódulo de "Repos y Boards" antes de hablar de pipelines:

**Caso 1 — el monorepo gigante que paralizó al equipo.** Una empresa con 12 desarrolladores y 8 servicios distintos decidió "todo en un monorepo, así es más fácil compartir código". El CI tardaba 25 minutos en cada PR porque corría todos los tests. La gente paralelizaba PRs y los conflicts de merge eran constantes. La migración correcta: **multi-repo** —un repo por servicio—, pipelines independientes, builds de 3-4 minutos. Lo hubieran sabido desde el día uno con el advisor del ejemplo: 7-10 personas + ≥4 servicios → MultiRepo.

**Caso 2 — el push directo a main de viernes por la tarde.** Un equipo configuró el repo con permisos abiertos, sin políticas de rama. Un viernes a las 16:55 un developer hizo `git push` directo a `main` con una "fix tonta". La fix tenía un bug. El despliegue automático mandó la versión rota a producción. Lunes a las 9, incidente. La política `NoPushDirecto + RequiredReviewers ≥ 1 + BuildExitoso` aplicada desde el día uno habría evitado el incidente: el push directo se rechaza, el PR requiere reviewer y CI verde.

**Caso 3 — los commit messages `wip`, `update`, `fix typo`.** Otro equipo tenía un repo limpio pero sin convención de commit messages. Cuando llegó la primera release, alguien tuvo que escribir el changelog **leyendo a mano** los últimos 200 commits para clasificarlos. Tres horas de trabajo. Con Conventional Commits (`feat:`, `fix:`, `docs:`...) el changelog se genera con un script en segundos: agrupar por tipo, listar los `feat:` y `fix:`, marcar los breakings con `!`. Tres horas vs tres minutos.

Los tres casos los resuelve el ejemplo: el advisor decide estrategia de repos, el evaluador de políticas detecta lo que falta, el parser de commits valida el formato y extrae vínculos a work items.

---

## 3. Por qué esto importa en tu stack

Si arrancas un proyecto nuevo en Azure DevOps, **estas tres decisiones se toman en la primera semana**. Si las tomas mal, las cambias en la primera crisis seis meses después. Las preguntas:

- **¿Cómo organizo los repos?** Para 5-10 personas con varios servicios, multi-repo es la opción. Para equipos pequeños con mucho código compartido, monorepo. El advisor del ejemplo te lo dice con criterios objetivos.
- **¿Qué políticas pongo en `main`?** Las cuatro mínimas: RequiredReviewers ≥ 1, BuildExitoso, ResolucionDeComentarios, NoPushDirecto. Las dos extra recomendadas: LimitarMergeTypes (squash) y LinkedWorkItems. Sin las cuatro mínimas tienes el escenario del caso 2.
- **¿Cómo escriben commits?** Conventional Commits desde el día uno. `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`, `perf:`, `ci:`, `build:`, `style:`. Diez tipos que cubren todo. Con `(scope)` opcional y `!` para breaking changes.

Las tres respuestas se aplican en menos de una hora en el portal de Azure DevOps. Y te ahorran horas todos los meses siguientes.

---

## 4. La analogía vertebradora: la oficina compartida y sus normas

Imagina una empresa que monta una oficina nueva para 8 personas que van a trabajar en varios productos. Antes de llegar la primera persona, alguien tiene que tomar tres decisiones:

**Decisión 1 — ¿Una sola sala grande o varias salas por equipo?**

- **Sala grande compartida (monorepo)**: todos en el mismo espacio. Hablan entre ellos sin barreras. Comparten material. Bueno cuando son pocos y trabajan en cosas relacionadas; pésimo cuando hay 8 personas en 4 proyectos distintos que se interrumpen entre sí.
- **Salas separadas por equipo (multi-repo)**: cada equipo en su sala. Reuniones de coordinación cuando hace falta. Bueno cuando los equipos trabajan en cosas independientes; cuesta más cuando hay que compartir herramientas.

**Decisión 2 — ¿Qué normas hay para entrar a la sala de equipo (`main`)?**

- **Norma mínima**: para entrar tienes que enseñar tu carnet (RequiredReviewers ≥ 1: alguien te aprueba), tu equipo de trabajo está limpio y operativo (BuildExitoso: CI pasa), no hay quejas pendientes sobre tu trabajo (ResolucionDeComentarios), y no se puede entrar por la ventana (NoPushDirecto).
- **Norma recomendada extra**: tu visita está vinculada a una agenda concreta (LinkedWorkItems: el PR cita el work item).

Sin las cuatro normas mínimas, la sala se convierte en un caos. Con ellas, se mantiene en orden.

**Decisión 3 — ¿Cómo etiquetamos lo que dejamos en el casillero (commits)?**

- **Sin convención**: cada uno escribe lo que quiere. "update", "wip", "fix typo". Cuando llega fin de mes y hay que hacer un informe de lo que entró, alguien lee a mano cada papel.
- **Con Conventional Commits**: una etiqueta estándar al principio. `feat:` (cosa nueva), `fix:` (arreglo), `docs:` (documentación)... Y si quieres, cita el ticket del que viene (`#1234`). Al final de mes, un script lee las etiquetas y genera el informe en segundos.

Las tres decisiones se toman antes de que la oficina abra. Cambiarlas después es caro pero posible; tomarlas mal el primer día es la causa más común de "este proyecto se nos fue de las manos".

Mantén la imagen: salas-norma-etiquetas. Cada decisión cuesta una hora; cada decisión te ahorra horas todos los meses.

---

## 5. Recorrido por el código

### `ConventionalCommitParser.Parsear` — la regex que vale oro

El parser:

```csharp
[GeneratedRegex(@"^(?<tipo>[a-z]+)(?:\((?<scope>[^)]+)\))?(?<break>!)?:\s*(?<desc>.+)$")]
private static partial Regex Encabezado();

[GeneratedRegex(@"#(?<id>\d+)")]
private static partial Regex WorkItemRef();
```

La primera regex acepta:

- `feat: añadir endpoint /pedidos`
- `feat(api): añadir endpoint /pedidos`
- `feat(api)!: añadir endpoint /pedidos` (con `!` = breaking change)
- `fix: corregir cálculo de IVA`

Y rechaza:

- `WIP: foo` (`WIP` en mayúsculas, no es un tipo válido).
- `feat añadir endpoint` (sin `:` ni `()`).
- `: descripción` (sin tipo).

La segunda regex busca **work items** con `#NNNN` en cualquier parte del mensaje (encabezado o cuerpo). Y los devuelve deduplicados y ordenados:

```csharp
var workItems = WorkItemRef()
    .Matches(mensaje)
    .Select(x => int.Parse(x.Groups["id"].Value))
    .Distinct()
    .OrderBy(x => x)
    .ToList();
```

¿Para qué sirve esto? Para tres cosas que tu pipeline va a hacer automáticamente:

1. **Vincular PRs a work items**: Azure DevOps detecta los `#NNNN` y crea el vínculo bidireccional automáticamente. En la PR ves los work items; en cada work item ves la PR.
2. **Validar el formato en pre-commit hook**: si el commit no cumple, el hook lo rechaza. Cero commits `wip` o `update`.
3. **Generar changelog automático**: agrupar commits por tipo, listar `feat:` y `fix:`, marcar breakings.

El parser valida los 10 tipos del slide 7:

```csharp
public static readonly HashSet<string> Validos = new(StringComparer.Ordinal)
{
    "feat", "fix", "docs", "refactor", "test",
    "chore", "perf", "ci", "build", "style",
};
```

Cualquier otro tipo es rechazado. Si tu equipo necesita uno más (`revert`, `release`), lo añades a la lista. **Nunca aceptes commits libres**; rompes la convención y vuelves al caos.

### `BranchPolicyAdvisor` — las cuatro mínimas

Dos listas y una función:

```csharp
public static IReadOnlyList<BranchPolicy> Minimas { get; } =
[
    BranchPolicy.RequiredReviewers,
    BranchPolicy.BuildExitoso,
    BranchPolicy.ResolucionDeComentarios,
    BranchPolicy.NoPushDirecto,
];

public static IReadOnlyList<BranchPolicy> Recomendadas { get; } =
[
    // las cuatro mínimas + ...
    BranchPolicy.LimitarMergeTypes,
    BranchPolicy.LinkedWorkItems,
];

public static EvaluacionPolicies Evaluar(IReadOnlyList<BranchPolicy> configuradas)
{
    var set = configuradas.ToHashSet();
    var faltantes = Minimas.Where(p => !set.Contains(p)).ToList();
    return new EvaluacionPolicies(faltantes, configuradas, faltantes.Count == 0);
}
```

Las cuatro mínimas con su porqué operativo:

1. **`RequiredReviewers`** (≥ 1): nadie mergea su propio código sin que otra persona lo mire. Captura bugs evidentes y reparte conocimiento.
2. **`BuildExitoso`**: CI tiene que pasar antes de mergear. Si los tests fallan, no se mergea. Sin esta política, "voy a saltarme el CI por esta vez" rompe `main`.
3. **`ResolucionDeComentarios`**: si el reviewer pide cambios, hay que resolverlos o argumentar por qué no. Sin esto, los reviews se convierten en formalidad.
4. **`NoPushDirecto`**: implícita en RequiredReviewers (no se puede mergear sin PR), pero merece la pena marcarla explícita por si hay roles especiales que podrían saltársela.

Las dos recomendadas extra:

- **`LimitarMergeTypes`** (squash recomendado): el historial de `main` queda limpio, un commit por feature en vez de cien `wip` intermedios.
- **`LinkedWorkItems`**: cada PR cita un work item. Trazabilidad bidireccional, fácil generar release notes filtradas por work item.

Cuando alguien te diga "vamos a relajar las políticas para acelerar", aplica el evaluador a su propuesta. Lo que falta es exactamente lo que va a romper algo en el próximo trimestre.

### `RepoStrategyAdvisor.Recomendar` — un repo o varios

La función:

```csharp
public static RecomendacionRepo Recomendar(EscenarioEquipo e)
{
    var aMonorepo = new List<string>();
    var aMultiRepo = new List<string>();

    if (e.MuchaSharedCode)
        aMonorepo.Add("Mucho código compartido entre proyectos → monorepo lo facilita.");
    if (e.Personas <= 4 && e.Servicios <= 3)
        aMonorepo.Add("Equipo pequeño con pocos servicios → setup más simple.");

    if (e.EquiposIndependientes)
        aMultiRepo.Add("Equipos independientes → multi-repo evita acoplamiento.");
    if (e.CiCdIndependiente)
        aMultiRepo.Add("CI/CD independiente por servicio → pipelines simples.");
    if (e.Servicios >= 4)
        aMultiRepo.Add($"{e.Servicios} servicios distintos → un repo por servicio.");
    if (e.Personas is >= 5 and <= 10)
        aMultiRepo.Add("Equipo 5-10 personas → multi-repo es la recomendación.");

    bool multi = aMultiRepo.Count > aMonorepo.Count;
    return multi
        ? new RecomendacionRepo(EstrategiaRepo.MultiRepo, aMultiRepo)
        : new RecomendacionRepo(EstrategiaRepo.Monorepo, ...);
}
```

Cinco señales hacia multi-repo, dos hacia monorepo. La regla pragmática que emerge:

- **Equipo pequeño (≤ 4) + servicios pocos (≤ 3)**: monorepo. Setup más simple, sin overhead de varios repos.
- **Mucho código compartido entre productos**: monorepo. Editar una librería compartida y ver el efecto en todos los productos en el mismo commit.
- **Equipo 5-10 personas + servicios ≥ 4**: multi-repo. CI/CD por servicio, builds rápidos, equipos pueden trabajar en paralelo sin pisarse.
- **Equipos independientes**: multi-repo. Cada equipo dueño de su repo, su CI, su release cycle.

El "punto de inflexión" típico es 4-5 servicios y/o 6-7 personas. Por debajo, monorepo. Por encima, multi-repo. Hay casos especiales (Google, Facebook usan monorepos gigantes con tooling propio), pero para empresas normales con Azure DevOps, multi-repo gana en cuanto pasas de la fase artesanal.

### `RepoBoardsPlanner` — el plan + checklist

El servicio inyectable que une los anteriores. Recibe el contexto del equipo y devuelve:

- Estrategia de repos recomendada (con razones).
- Política mínima de `main` y faltantes si el equipo ya tiene algunas.
- Checklist completa del entregable (jerarquía Boards, sprints, Conventional Commits en pre-commit hook, feed Artifacts, PAT con permisos mínimos).

Es lo que el script `01-inventory-devops.sh` valida después contra tu organización real.

---

## 6. La jerarquía de Boards en una imagen

El submódulo lo menciona y vale la pena tener clara la jerarquía. Cuatro niveles:

```
Epic (objetivo de negocio grande, varios meses)
  └── Feature (funcionalidad concreta, 1-3 sprints)
        └── User Story (algo que entrega valor al usuario, < 1 sprint)
              └── Task / Bug (trabajo técnico, horas)
```

Ejemplo concreto:

- **Epic**: "Sistema de pedidos online" (Q1).
- **Feature**: "Carrito de compra persistente".
- **User Story**: "Como usuario quiero que mi carrito persista entre sesiones".
- **Tasks**: "Diseñar modelo de carrito", "Implementar repositorio Cosmos", "Endpoint POST /carrito", "Tests de integración"...
- **Bug**: "El carrito se vacía si la sesión caduca después de las 23:00".

Y luego el sprint (2 semanas típicamente): contiene User Stories y Tasks. La velocity del equipo se mide en story points completados por sprint. Esto NO está en el código del ejemplo (es configuración de Boards), pero el `Planner.Checklist` lo incluye como ítem del entregable.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/Devops.Repos.Demo.Api
# http://localhost:5105
```

Endpoints:

```http
### Parsear un commit
POST http://localhost:5105/devops/commit/parsear
Content-Type: application/json

"feat(api)!: añadir endpoint /pedidos\n\nCloses #1234"
# → { valido: true, tipo: "feat", scope: "api", breakingChange: true,
#     descripcion: "añadir endpoint /pedidos", workItems: [1234] }

### Listar los 10 tipos válidos
GET http://localhost:5105/devops/commit/tipos

### ¿Qué políticas mínimas tengo y cuáles me faltan?
POST http://localhost:5105/devops/branch-policy/evaluar
Content-Type: application/json

["RequiredReviewers", "BuildExitoso"]
# → { faltantes: ["ResolucionDeComentarios", "NoPushDirecto"], cumple: false }

### Monorepo o multi-repo
POST http://localhost:5105/devops/repo/estrategia
Content-Type: application/json

{
  "personas": 8,
  "servicios": 5,
  "muchaSharedCode": false,
  "ciCdIndependiente": true,
  "equiposIndependientes": false
}
# → MultiRepo con 4 razones

### Plan completo
POST http://localhost:5105/devops/plan
```

Los 34 tests cubren los 10 tipos de commit, scopes con paréntesis, breaking change con `!`, work items deduplicados y ordenados, las cuatro políticas mínimas detectadas como faltantes en varias combinaciones, y los escenarios típicos de monorepo vs multi-repo.

Para auditar tu organización real:

```bash
./scripts/demo.sh
# 1) 01-inventory-devops.sh → repos + branch policies en main +
#    work items del usuario + feeds de Artifacts
```

El script usa `az devops` (instala la extensión `azure-devops` la primera vez). Solo lectura. Requiere PAT con permisos `Read & Write` a `Code` y `Work Items`.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. Por qué Conventional Commits es la convención que merece adoptar

Hay otras convenciones de commits, pero Conventional Commits ha ganado la conversación por tres razones:

1. **Especificación clara y corta**: 10 tipos, formato fijo, fácil de explicar en una página.
2. **Tooling extenso**: `commitlint` para validar en pre-commit, `semantic-release` para generar versiones, `conventional-changelog` para changelogs, herramientas de Azure DevOps que detectan `#NNNN` y vinculan.
3. **Mapping a SemVer automático**: `feat:` = bump minor, `fix:` = bump patch, `feat!:` o `fix!:` = bump major. Tu pipeline puede calcular la siguiente versión sin que nadie la decida a mano.

El precio: un par de horas formando al equipo y un pre-commit hook que rechaza commits mal formados. Beneficio: changelogs automáticos, versionado SemVer automático, trazabilidad bidireccional con work items.

---

## 9. Los anti-patterns del slide 31 (que se cuelan en el checklist)

Aunque el slide no se cita explícitamente, hay cinco anti-patterns que el checklist del planner detecta:

1. **PAT con permisos `Full Access`**: cualquiera con el token puede borrar repos. Usa permisos mínimos: `Code: Read & Write`, `Work Items: Read & Write`, lo que necesites y nada más.
2. **Repos públicos por defecto en organizaciones privadas**: asegúrate de que la organización tenga visibility `Private` por defecto. Los repos públicos accidentales son fuga de IP.
3. **Sin Artifacts feed privado**: subir paquetes NuGet a public feed o pasar `.nupkg` por correo es un riesgo. Crea un feed privado para tu organización (gratis hasta 2 GB).
4. **Sprints de 4 semanas**: dos semanas es el sweet spot. Cuatro semanas pierde foco a la mitad; una semana es overhead constante.
5. **Boards sin Sprints**: solo Kanban abierto. Está bien para soporte/operations, no para producto. Sin sprint, no hay sentido de "lo que cabe esta iteración".

---

## 10. Glosario breve

- **Azure DevOps**: la suite SaaS de Microsoft para DevOps (Repos, Boards, Pipelines, Test Plans, Artifacts).
- **Repos**: Git hospedado por Microsoft, integrado con el resto de la suite.
- **Boards**: gestión de work items con jerarquía Epic→Feature→Story→Task/Bug y sprints.
- **Artifacts**: feeds NuGet/npm/Maven privados.
- **Pipelines**: CI/CD como código (YAML). Se ve en S8.2 y siguientes.
- **PAT** (Personal Access Token): credencial para invocar APIs de Azure DevOps desde scripts.
- **Branch policy**: regla que aplica a una rama (típicamente `main`). Bloquea push directo, exige reviewers, exige CI verde.
- **Conventional Commits**: spec de formato de commits (`tipo(scope)!: descripción`) con 10 tipos canónicos.
- **Monorepo**: todo el código de la organización en un solo repo.
- **Multi-repo**: un repo por servicio/proyecto/equipo.
- **Trunk-based development**: branches cortas (horas a días) que mergean a `main` rápidamente, no `release` branches largas.
- **Squash merge**: forma de merge que comprime todos los commits del PR en uno. Limpia el historial.

---

## 11. Cierre

S8.1 te da las tres decisiones de arranque de un proyecto en Azure DevOps que vas a tomar la primera semana: estrategia de repos, políticas de `main`, convención de commits. Si las eliges bien, el resto del proyecto va sobre raíles. Si las eliges mal o las pospones, las pagas en el primer trimestre.

Lo siguiente es [`S8.2 — Pipelines CI/CD YAML`](../S8.2-pipelines-cicd-yaml/MANUAL.md), donde la conversación se mueve de gestión del repo a automatizar build y deploy: pipelines como código, triggers, validación de YAML.
