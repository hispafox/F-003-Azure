# S10.P2 — Práctica: mini-proyecto Notas

> **Submódulo de referencia:** [M10-S10.P2](../../../doc/M10-Proyecto-Integrador/v3-actual/M10-S10.P2-practica-mini-proyecto-notas-v1.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; la mini-app real la monta el alumno en su entorno Azure)

> 🎓 **Práctica conceptual** (lección 9 del HANDOFF). Versión recortada
> del proyecto integrador S10.1: el alumno construye una mini-app de
> notas (Web App F1 + Table Storage + 5 endpoints CRUD) en 60-75 min.
> Sin auth, sin Functions, sin SB, sin pipeline complejo.
>
> 🧱 **Cierra M10 (2/2)** y, con S10.1, **cierra el curso F-003-Azure**.

> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el cortometraje antes del largometraje como analogía (Azure real a escala reducida, 11 fases del rodaje = 11 pasos, desmontar el set = cleanup `az group delete`). Cubre la decisión de alcance Mini/Completo/EmpezarPorMini, las 5 features incluidas vs las 9 que cubre S10.1 y el camino de 7 pasos para escalar al proyecto integrador grande.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Preflight ligero (slide 3) | [`MiniNotasPreflight.cs`](src/Practica.MiniNotas.Demo.Api/MiniNotas/MiniNotasPreflight.cs) |
| Evaluador de los 11 pasos (slides 4-14) | [`PasoChecker.cs`](src/Practica.MiniNotas.Demo.Api/MiniNotas/PasoChecker.cs) |
| Comparador de alcance Mini vs Completo (slide 2) | [`AlcanceComparator.cs`](src/Practica.MiniNotas.Demo.Api/MiniNotas/AlcanceComparator.cs) |
| Plan + camino hacia S10.1 + checklist | [`IPracticaMiniNotasPlanner.cs`](src/Practica.MiniNotas.Demo.Api/MiniNotas/IPracticaMiniNotasPlanner.cs) |
| API que expone la lógica (`/mininotas/*`) | [`MiniNotasEndpoints.cs`](src/Practica.MiniNotas.Demo.Api/Endpoints/MiniNotasEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Qué construye la mini-práctica (11 pasos) | 2 | `IPracticaMiniNotasPlanner.Checklist` |
| Alcance (incluido vs no incluido) | 2 | `AlcanceComparator.IncluidasEnMini` / `NoIncluidasEnMini` |
| Preflight: .NET 8 SDK + az + curl/jq + git | 3 | `MiniNotasPreflight.Comprobar` |
| Paso 1: diseñar modelo `Note` | 4 | `Paso.DisenarModelo` |
| Paso 2: crear solución `.sln` + src/tests | 5 | `Paso.CrearSolucion` |
| Paso 3: implementar `Note : ITableEntity` | 6 | `Paso.ImplementarModelo` |
| Paso 4: `NotesRepository` con `TableClient` | 7 | `Paso.ImplementarRepositorio` |
| Paso 5: 5 endpoints REST minimal API | 8 | `Paso.EndpointsCrud` |
| Paso 6: tests unitarios del repositorio | 9 | `Paso.TestsUnitarios` |
| Paso 7: smoke tests con curl | 10 | `Paso.SmokeTests` |
| Paso 8: infra Azure (RG + Storage + plan F1 + Web App) | 11 | `Paso.CrearInfra` |
| Paso 9: deploy con `az webapp deploy --type zip` | 12 | `Paso.DesplegarApp` |
| Paso 10: validación end-to-end | 13 | `Paso.ValidarEndToEnd` |
| Paso 11: cleanup (`az group delete`) | 14 | `Paso.Limpiar` |
| Camino del mini al proyecto completo | 2 | `PracticaMiniNotasPlanner.CaminoHaciaS101Slide2` |

## Estructura

```
S10.P2-practica-mini-proyecto-notas/
├── src/Practica.MiniNotas.Demo.Api/
│   ├── MiniNotas/  MiniNotasPreflight, PasoChecker,
│   │              AlcanceComparator
│   │              + IPracticaMiniNotasPlanner/PracticaMiniNotasPlanner
│   ├── Endpoints/  MiniNotasEndpoints (/health, /mininotas/*)
│   └── Program.cs  AddSingleton<IPracticaMiniNotasPlanner> + enums por nombre
└── tests/Practica.MiniNotas.Demo.Api.Tests/
    ├── Unit_*                lógica pura (preflight, pasos, alcance)
    ├── DiContainer_Tests     resuelve el planner
    └── Api_MiniNotasTests    E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 42 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `MiniNotasPreflight` (todo OK → listo; sin `.NET 8 SDK` / `az` /
    `curl` son bloqueantes; sin `jq` es solo aviso; sin M01/M02/M05
    previos son avisos también).
  - `PasoChecker` (Pasa/Falla/Pendiente según comando+output; los 11
    pasos mapean a slides 4-14; sugerencias específicas: paso 2 →
    `dotnet new sln`, paso 5 → `/notes`, paso 11 → `az group delete`).
  - `AlcanceComparator` (end-to-end mínimo → Mini; auth/Functions/SB/
    pipeline/producción → Completo; sin señales → `EmpezarPorMini`;
    sin M01/M02/M05 previos añade aviso de repaso; incluidas cubren
    WebApp/Persistencia/Crud/Tests/Deploy; no incluidas cubren
    Auth/SB/Functions/Pipeline/AppInsights).
- **CAPA 0 · DI**: resuelve `IPracticaMiniNotasPlanner` del contenedor
  real (`Assert.Same` singleton) y compone preflight + pasos + alcance
  + camino-S101 + checklist.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/mininotas/{preflight, paso, alcance, camino-s101, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: la mini-app real se
> despliega en Azure con `az webapp deploy`. Aquí lo testeable son
> las **decisiones del alumno** sobre el orden de pasos, el alcance
> elegido (Mini vs Completo) y la autoevaluación de cada paso.

## Ejecución local

```bash
dotnet run --project src/Practica.MiniNotas.Demo.Api
# http://localhost:5121  — usa src/Practica.MiniNotas.Demo.Api/api.http
```

- `/mininotas/preflight` clasifica los requisitos en OK / Aviso /
  Bloqueante. Más ligero que el de S10.1 (no necesita Cosmos / Entra /
  Key Vault).
- `/mininotas/paso` evalúa cada uno de los 11 pasos del slide 2-14
  con `ComandoEjecutado` + `OutputEsperadoVisible`.
- `/mininotas/alcance` recomienda Mini / Completo / EmpezarPorMini
  según el objetivo declarado por el alumno.
- `/mininotas/camino-s101` lista los 7 pasos para escalar de la
  mini-práctica al proyecto integrador completo.
- `/mininotas/plan` compone todo + checklist de 11 puntos.

## Flujo del alumno

1. **Decide el alcance** → `/mininotas/alcance` con tu objetivo. Si
   sale `Completo`, ve a S10.1; si sale `Mini`, sigue aquí.
2. **Preflight** → `/mininotas/preflight` con tu setup. Sin `.NET 8` +
   `az` + `curl` no se puede arrancar.
3. **Sigue los 11 pasos** del slide en tu terminal (60-75 min).
4. **Reporta evidencia** → `/mininotas/paso` por cada paso. Si alguno
   cae en `Pendiente` o `Falla`, las sugerencias te dicen qué probar
   y a qué slide volver.
5. **Cuando termines**, mira `/mininotas/camino-s101`: los 7 pasos
   para evolucionar el mini-proyecto al integrador completo (S10.1).

## Ideas centrales

> Esta práctica es **la puerta de entrada al proyecto integrador**:
> el alumno hace un end-to-end real (Web App + persistencia + deploy)
> en 60-75 min, sin morir en el detalle. **El alcance recortado es
> consciente** (slide 2): saltarse auth, Functions, pipeline y App
> Insights para que el alumno vea las 3 capas básicas funcionando
> antes de añadir complejidad. **Cuando esto funcione**, el camino
> hacia S10.1 está documentado (`CaminoHaciaS101`) — añade auth,
> Functions, pipeline y monitoring uno a uno, no todo de golpe (esto
> es el anti-pattern #3 del S9.5: "todo de golpe").

## Cierre M10 y cierre del curso

Con S10.P2 cerramos **M10 (2/2)** y, con M01–M09 ya cerrados,
**cierra el curso F-003-Azure**. El alumno tiene:
- 9 módulos teóricos cubiertos (M01–M09) con sus prácticas.
- El proyecto integrador grande (S10.1) para el "examen final".
- Esta mini-práctica (S10.P2) para validar el end-to-end antes del
  proyecto completo.
- M11 (bonus Claude Code en Azure) si quieres profundizar en IA.

**Próximo: M11 — Bonus Claude Code en Azure** (opcional). Para quien
quiera llevar Claude Code a un proyecto real en Azure.
