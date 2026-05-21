# S10.1 — Proyecto Integrador: diseño y arquitectura

> **Submódulo de referencia:** [M10-S10.1](../../../doc/M10-Proyecto-Integrador/v3-actual/M10-S10.1-diseno-arquitectura-v3.md)
> **TFM:** `net10.0` · **Tipo:** ASP.NET Minimal API · **Coste:** 0 € (lógica pura; el sistema real lo construye el alumno en Azure)

> 🎓 **Submódulo conceptual** (lección 9 del HANDOFF). El alumno
> construye el sistema completo (API + Functions + Cosmos + SB +
> Entra + KV + MI + App Insights + Bicep + Pipeline) siguiendo los
> 4 bloques A-D del slide 5. Aquí extraemos las heurísticas:
> checklist de los 10 componentes, recomendador del bloque
> siguiente y evaluador de la entrega con los 8 criterios pesados
> del slide 11.

## Objetivo

| Concepto | Dónde |
| --- | --- |
| Checklist de los 10 componentes (slide 3/4) | [`ArquitecturaChecklist.cs`](src/ProyectoIntegrador.Diseno.Demo.Api/Diseno/ArquitecturaChecklist.cs) |
| Recomendador de bloque siguiente A-D (slide 5) | [`BloqueRecommender.cs`](src/ProyectoIntegrador.Diseno.Demo.Api/Diseno/BloqueRecommender.cs) |
| Evaluador de entrega con 8 criterios pesados (slide 11) | [`EntregaEvaluator.cs`](src/ProyectoIntegrador.Diseno.Demo.Api/Diseno/EntregaEvaluator.cs) |
| Plan + retos opcionales (slide 12) | [`IProyectoIntegradorPlanner.cs`](src/ProyectoIntegrador.Diseno.Demo.Api/Diseno/IProyectoIntegradorPlanner.cs) |
| API que expone la lógica (`/diseno/*`) | [`DisenoEndpoints.cs`](src/ProyectoIntegrador.Diseno.Demo.Api/Endpoints/DisenoEndpoints.cs) |

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Objetivo: gestión de pedidos end-to-end | 2 | (intro del README) |
| Arquitectura con todos los componentes Azure | 3 | `ArquitecturaChecklist.Inventariar` |
| 10 componentes con su función | 4 | `Componente` enum + descripciones |
| Plan de trabajo: bloques A/B/C/D (3h) | 5 | `BloqueRecommender.Recomendar` |
| Bloque A: Bicep modular | 6 | `Bloque.A_Infraestructura` |
| Bloque B: API + Cosmos + Auth | 7 | `Bloque.B_ApiYAuth` |
| Bloque C: Functions + SB + Change Feed | 8 | `Bloque.C_FunctionsYSb` |
| Bloque D: Pipeline + Monitoring | 9 | `Bloque.D_PipelineYMonitor` |
| Alertas mínimas (5xx, latencia) | 10 | (en las tareas del bloque D) |
| Checklist de entrega con pesos | 11 | `EntregaEvaluator.Evaluar` |
| Retos opcionales (bonus) | 12 | `ProyectoIntegradorPlanner.RetosOpcionales` |

## Estructura

```
S10.1-diseno-arquitectura/
├── src/ProyectoIntegrador.Diseno.Demo.Api/
│   ├── Diseno/     ArquitecturaChecklist, BloqueRecommender,
│   │              EntregaEvaluator
│   │              + IProyectoIntegradorPlanner/ProyectoIntegradorPlanner
│   ├── Endpoints/  DisenoEndpoints (/health, /diseno/*)
│   └── Program.cs  AddSingleton<IProyectoIntegradorPlanner> + enums por nombre
└── tests/ProyectoIntegrador.Diseno.Demo.Api.Tests/
    ├── Unit_*                lógica pura (checklist, bloques, entrega)
    ├── DiContainer_Tests     resuelve el planner
    └── Api_DisenoTests       E2E vía WebApplicationFactory
```

## Tests

```bash
dotnet test     # 27 pass + 0 fail + 0 warn
```

- **CAPA 1 · Unit**:
  - `ArquitecturaChecklist` (inventariar devuelve los 10 componentes;
    todo pendiente → 0%; todo desplegado → 100%; `EnProgreso` no
    cuenta; 5 de 10 → 50%; cada componente lleva descripción).
  - `BloqueRecommender` (sin Bicep → A; con Bicep pero sin API → B;
    A y B completos → C; A B y C completos → D; todo desplegado →
    `Terminado`; cada recomendación lleva justificación).
  - `EntregaEvaluator` (sin evidencias → 0%; todo cumplido → 100%
    aprobada; solo Bicep + API → 30%; 70% es el umbral de aprobado;
    los 8 criterios suman exactamente 100% de peso).
- **CAPA 0 · DI**: resuelve `IProyectoIntegradorPlanner` del
  contenedor real (`Assert.Same` singleton) y compone arquitectura +
  porcentaje + bloque + entrega + retos.
- **CAPA E2E**: la API completa vía `WebApplicationFactory` —
  `/diseno/{arquitectura, arquitectura/porcentaje, bloque-siguiente,
  entrega, retos, plan}`.

> 🧠 **Por qué no hay CAPA de integración**: el sistema real se
> construye en Azure con CLI/Portal/Pipeline. Aquí lo testeable son
> las **decisiones del alumno** sobre el orden de bloques y la
> autoevaluación contra los criterios de entrega.

## Ejecución local

```bash
dotnet run --project src/ProyectoIntegrador.Diseno.Demo.Api
# http://localhost:5120  — usa src/ProyectoIntegrador.Diseno.Demo.Api/api.http
```

- `/diseno/arquitectura` devuelve el estado de los 10 componentes
  con su descripción.
- `/diseno/arquitectura/porcentaje` calcula qué porcentaje está
  desplegado (sólo cuenta `Desplegado`, no `EnProgreso`).
- `/diseno/bloque-siguiente` recomienda el siguiente bloque A/B/C/D
  con sus tareas concretas y la justificación.
- `/diseno/entrega` evalúa los 8 criterios del slide 11 con sus
  pesos (15/15/10/10/15/10/15/10) y devuelve el porcentaje + si
  aprueba (umbral 70%).
- `/diseno/retos` lista los 5 retos opcionales del slide 12.
- `/diseno/plan` compone todo.

## Flujo del alumno

1. **Mira el estado actual** del sistema → `/diseno/arquitectura` con
   los componentes ya desplegados.
2. **Pide el siguiente paso** → `/diseno/bloque-siguiente`. Empieza
   por A si Bicep no está, sigue por B (API + Cosmos + Auth + KV +
   MI), luego C (Functions + SB) y termina por D (Pipeline + App
   Insights).
3. **Sigue las tareas** del bloque en clase usando los slides 6-10
   como referencia paso a paso.
4. **Antes de entregar**, evalúa con `/diseno/entrega` aportando las
   evidencias reales (Bicep desplegado, API con 2xx, JWT validado,
   etc.). Si < 70%, refuerza lo que falte.
5. **Cuando estés en 80%+**, considera los retos del `/diseno/retos`
   (slide 12) para subir nota.

## Ideas centrales

> El proyecto integrador es el **examen final** del curso: junta los
> 9 módulos anteriores en un sistema real desplegado. El orden de
> bloques **no es opcional** (slide 5): A primero porque sin
> infraestructura no hay donde meter nada; D último porque el
> pipeline necesita la app funcionando para hacer smoke tests.
> **Managed Identity es la regla** (slide 11): cero connection
> strings con password — si la entrega tiene `Server=...Password=...`
> en algún sitio, no aprueba. **Las 2 alertas mínimas** de
> Application Insights (5xx y latencia) son innegociables — un
> sistema sin alertas no es "de producción", es un experimento.

## Próximo paso

[`S10.P2 — Práctica mini-proyecto notas`](../../../doc/M10-Proyecto-Integrador/v3-actual/M10-S10.P2-practica-mini-proyecto-notas-v1.md):
versión reducida del proyecto integrador (notas en Cosmos + API
mínima + pipeline simple) para alumnos que quieran practicar antes
del proyecto completo.
