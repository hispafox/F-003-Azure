# S4.P2 — Práctica: Durable Hello World (fan-out/fan-in mínimo)

> **Submódulo de referencia:** [M04-S4.P2](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.P2-practica-durable-hello-world-v1.md)
> **TFM:** `net10.0` · **Tipo:** Azure Functions isolated worker · **Tier:** Consumption
> **Coste:** ~0 € (Durable usa el Storage del Function App; sin SB ni Cosmos)

> 📘 **¿Primera vez con esta práctica?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: el director de coro de tres voces, las tres piezas de Durable (Starter, Orchestrator, Activity), la regla del determinismo y por qué este ejemplo deliberadamente no lleva `Thread.Sleep`.

## Objetivo

La práctica **más corta de M04**: Durable Functions en su forma mínima.
Recibe una lista de nombres, saluda a cada uno **en paralelo** y consolida
el resultado. Fan-out/fan-in en 3 piezas.

```
POST /api/saludos  ["Ana","Luis","Marta"]
   │  (Starter — client function)
   ▼
SaludarATodos (Orchestrator)
   ├─ fan-out: 1 activity por nombre, todas a la vez
   └─ fan-in:  Task.WhenAll → ["¡Hola, Ana!", ...]
        ▲
   SaludarActivity (hace el trabajo: construir el saludo)

GET /api/saludos/{id} → runtimeStatus + output
```

> 🎯 **Las 3 piezas de Durable (slide 3)**: *Starter* (cualquier trigger,
> arranca la orquestación) · *Orchestrator* (define el flujo, **determinista**)
> · *Activity* (hace el trabajo real). Es la base mental del S4.P (flujo
> completo) y de S4.2 (saga).

## Mapeo a slides

| Concepto | Slide | Dónde |
| --- | --- | --- |
| Activity (trabajo real) | 5 | [`SaludarActivity.cs`](src/AzureFunctions.Demo/Functions/SaludarActivity.cs) |
| Orchestrator fan-out/fan-in | 6 | [`SaludosOrchestrator.cs`](src/AzureFunctions.Demo/Functions/SaludosOrchestrator.cs) |
| Starter HTTP + status URL | 7, 8 | [`SaludosStarterFunctions.cs`](src/AzureFunctions.Demo/Functions/SaludosStarterFunctions.cs) |
| Storage como backend de Durable | 14 | `host.json` → `extensions.durableTask` |

## Tests

```bash
dotnet test     # 8/8 — sin Storage ni runtime de Durable
```

- **`SaludarActivityTests`** (5, `[Theory]`) — lógica del saludo extraída
  a `Construir()`: trim, fallback a "desconocido" para vacío/null.
- **`SaludosOrchestratorTests`** (3) — `TaskOrchestrationContext`
  mockeado con NSubstitute: consolida 1 saludo por nombre (fan-out de N
  activities verificado), lista vacía/null → vacío sin llamar activities.

> 📦 Gotcha reutilizado de S4.2: `ctx.CreateReplaySafeLogger<T>()` devuelve
> `null` en el mock → configurarlo a `NullLogger<T>.Instance` o el
> orquestador peta al loguear.
>
> ⚠️ El guion usa `Thread.Sleep(2000)` en la activity "para ver el
> paralelismo". Lo dejamos **fuera del código**: un sleep no es testeable
> de forma determinista y nunca va en producción. El paralelismo se
> aprecia igual en los timestamps de los logs.
>
> ⚠️ **DI cruzado a mano** (lección S3.4): ninguna `[Function]` inyecta
> servicios de negocio → `Program.cs` no necesita `AddSingleton`. El
> `[DurableClient]` lo inyecta el binding, no el contenedor.

## Despliegue por Portal

1. RG `rg-curso-m04-s4p2` · Storage `stcursom04s4p2{ini}` (LRS).
   Durable lo necesita: persiste el historial del orchestrator en Table
   Storage y coordina por Queues (slide 14). No hay que crear nada a mano.
2. Function App .NET 10 Isolated / Linux / Consumption, ese Storage.
3. Deploy desde VS Code.
4. Probar:
   ```bash
   APP="https://func-curso-m04-s4p2-{ini}.azurewebsites.net/api"
   R=$(curl -s -X POST "$APP/saludos?code=KEY" \
        -H "Content-Type: application/json" -d '["Ana","Luis","Marta"]')
   ID=$(echo $R | jq -r .instanceId)
   sleep 10
   curl "$APP/saludos/$ID?code=KEY" | jq
   # runtimeStatus=Completed, output: ["¡Hola, Ana!","¡Hola, Luis!","¡Hola, Marta!"]
   ```
5. Portal → Function App → **Durable Functions**: ves la instancia y su
   historial paso a paso.
6. Borra el RG.

(También `scripts/demo.sh` para hacerlo por CLI — coste ~0.)

## Rúbrica de "done"

```
[x] Las 3 funciones (Starter / Orchestrator / Activity)
[x] POST /saludos arranca la orquestación y devuelve instanceId
[x] GET /saludos/{id} muestra Pending→Running→Completed
[x] El output es un saludo por nombre (fan-out/fan-in)
[x] Lista vacía → 400
[x] Tests obligatorios 8/8 + DI cruzado a mano
```

## Fin del Módulo 4

Con S4.P2 **M04 queda completo**: 5 submódulos (S4.1-S4.5) + 2 prácticas
(S4.P, S4.P2). Event Grid/Service Bus, Durable Functions, errores y
dead-letter, despliegue/versionado, testing, y las dos prácticas
integradoras. Siguiente módulo: **M05 — Almacenamiento y BBDD**.
