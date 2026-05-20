# Manual del alumno — S4.P2 · Práctica Durable Hello World

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, despliegue, scripts. Este manual va antes: te cuenta por qué esta práctica vale como "ejemplo canónico" pese a su brevedad, qué hace cada una de las tres piezas de Durable y por qué el ejemplo deliberadamente no tiene `Thread.Sleep`.

Tiempo de lectura: ~15 min. Submódulo de teoría: [M04-S4.P2](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.P2-practica-durable-hello-world-v1.md). Tres funciones, ocho tests, cero servicios externos, todo Durable.

*Creado: 2026-05-20 15:50 +0200*

---

## 1. La idea en una frase

Si S4.2 te enseñó las cinco patrones grandes de Durable (chaining, retry, human interaction, saga, fan-out/fan-in) con una saga compleja de procesamiento de pedidos, esta práctica los reduce al **mínimo absoluto**: un único patrón —fan-out/fan-in— sobre el dominio más trivial que se le puede ocurrir a nadie, "decir hola a una lista de nombres". El objetivo no es enseñar nada nuevo; es **fijar la mecánica** de las tres piezas (Starter, Orchestrator, Activity) con un ejemplo que cabe en la cabeza y se ejecuta en local sin emuladores.

---

## 2. El problema real que hay detrás

Cuando alguien aprende Durable por primera vez, la curva tiene dos picos. El primero es entender la **regla del determinismo**: el orquestador no puede usar `DateTime.Now`, ni `Random`, ni hacer I/O, porque se ejecuta muchas veces durante la vida del flujo (replay) y necesita producir exactamente las mismas decisiones cada vez. El segundo es entender el **modelo de las tres piezas**: el starter arranca, el orquestador coordina, las activities trabajan.

Las dos cosas se entienden mejor con un ejemplo en el que la lógica sea irrelevante, para que la atención vaya al andamiaje. Si el dominio es complejo, la gente se enreda en el dominio. Si es "saluda a una lista", la gente ve las tres piezas con claridad y se queda con la mecánica.

Por eso "Hello World" sigue siendo un ejemplo legítimo treinta años después de inventarse. Cuando el contenido importa menos que la forma, lo trivial es lo correcto.

---

## 3. Por qué esto importa en tu stack

Si has hecho S4.2, ya entiendes Durable a fondo. Esta práctica vale para tres cosas:

- **Refrescar la mecánica** cuando vuelves a Durable tras meses sin tocarlo. Las tres piezas y el patrón fan-out/fan-in caben en quince minutos de lectura.
- **Servir de plantilla** para empezar un proyecto nuevo. Copias estas tres funciones, sustituyes "saludar" por tu lógica real, y tienes un esqueleto funcional desde la primera tarde.
- **Enseñar a alguien**. Para un nuevo miembro del equipo que no haya tocado Durable, este ejemplo es mejor punto de partida que la saga de S4.2 — la primera idea queda fija antes de meter saga y compensaciones.

---

## 4. La analogía vertebradora: el director de coro

Imagina un coro de tres voces y un director:

- **El público** (un email, un endpoint, un Service Bus message) le entrega al director un papel: "Hoy cantamos `[Ana, Luis, Marta]`". Esto es el **Starter** — recibe el input y arranca el flujo.
- **El director del coro** mira el papel y dice "Ana, ataca tu solo. Luis, ataca tu solo. Marta, ataca tu solo. Cuando estén las tres voces, salimos a saludar al público con un único acorde". Esto es el **Orchestrator** — coordina, no canta.
- **Cada cantante** (Ana, Luis, Marta) ataca su solo en paralelo y devuelve su voz al director. Esto son las **Activities** — hacen el trabajo real.

Cuando los tres han terminado, el director **consolida** las tres voces en un acorde y se lo entrega al público como output final. Eso es el fan-in con `Task.WhenAll`.

Detalles importantes que la analogía deja claros:

- El director **nunca canta** (el orquestador nunca hace I/O ni cálculos sustantivos). Solo coordina.
- Los cantantes cantan a la vez (las activities corren en paralelo, en distintas instancias del Function App).
- El director recuerda perfectamente quién ya cantó y quién no, aunque le pongan a desensayar el mismo concierto cinco veces seguidas (replay determinista). Cada vez que vuelve a ensayar, cuando llega al punto "Ana atacó su solo y dijo X", lo sabe sin volver a preguntárselo — está en el historial.

Esa es la mecánica completa de Durable, en una imagen.

---

## 5. Recorrido por el código

### La activity (`SaludarActivity`)

Dos métodos:

```csharp
[Function(nameof(Saludar))]
public string Saludar([ActivityTrigger] string nombre, FunctionContext ctx)
{
    var saludo = Construir(nombre);
    ctx.GetLogger(...).LogInformation("Saludo generado: {Saludo}", saludo);
    return saludo;
}

internal static string Construir(string? nombre)
{
    var limpio = string.IsNullOrWhiteSpace(nombre) ? "desconocido" : nombre.Trim();
    return $"¡Hola, {limpio}!";
}
```

La regla "la lógica pura va en un método separado y testeable" del módulo se ve aquí en pequeño. `Saludar` es el adaptador al runtime de Functions; `Construir` es la lógica que se puede testear con un `[Theory]` de cinco `[InlineData]` sin levantar nada.

Una decisión deliberada que merece mención: el guion del curso usa `Thread.Sleep(2000)` dentro de la activity "para ver el paralelismo" en clase. **El ejemplo lo deja fuera del código** y lo documenta así en el comentario:

```csharp
// El guion usa Thread.Sleep(2000) para "ver" el paralelismo en clase; lo
// dejamos FUERA del código: una activity con sleep no es testeable de
// forma determinista y Thread.Sleep nunca va en producción.
```

¿Por qué? Porque una activity con sleep introduce comportamiento temporal no determinista en los tests (no puedes assertar tiempos), y porque enseñar a meter sleeps en código de producción es enseñar una mala práctica que cuesta años de quitar. El paralelismo se ve perfectamente en los timestamps de los logs cuando despliegas a Azure; no hace falta forzar un retraso artificial.

### El orquestador (`SaludosOrchestrator`)

Diez líneas que contienen toda la mecánica fan-out/fan-in:

```csharp
var nombres = context.GetInput<List<string>>() ?? [];
if (nombres.Count == 0) return [];

var tareas = nombres
    .Select(n => context.CallActivityAsync<string>("Saludar", n))
    .ToList();

var saludos = await Task.WhenAll(tareas);
return [.. saludos];
```

El patrón en su forma más pura:

1. `nombres.Select(...).ToList()` — fan-out. Crea N tasks (una por nombre), pero **no las espera todavía**. Cada `CallActivityAsync` devuelve un `Task<string>` que representa una activity programada.
2. `await Task.WhenAll(tareas)` — fan-in. Espera a que todas las activities hayan terminado y obtén sus resultados como un array.
3. `return [..saludos]` — devuelve el resultado consolidado al starter.

Lo no obvio: cuando el orquestador ejecuta `Task.WhenAll`, el código no se bloquea esperando — el motor de Durable suspende el orquestador, espera a que las activities completen (pueden ser segundos o minutos), y reanuda el orquestador desde donde lo dejó. Por debajo es un "checkpoint y reanudar" que sobrevive a reinicios del Function App. Pero el código que tú escribes parece síncrono. Esa es la magia operativa de Durable.

Y `context.CreateReplaySafeLogger<T>()` es el truco para loguear sin romper el replay. Un `ILogger` normal logueado en el orquestador se ejecuta cada vez que el orquestador entra en replay — diez ejecuciones del mismo log line por cada paso, lo cual ensucia los logs y rompe métricas. El "replay-safe logger" detecta si estás en replay y solo emite el log la primera vez. Suena trivial; en producción te ahorra dolores de cabeza.

### El starter (`SaludosStarterFunctions`)

Dos endpoints HTTP:

**`POST /api/saludos`** — arranca el orquestador con el array de nombres como input:

```csharp
var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
    nameof(SaludosOrchestrator.SaludarATodos), nombres);

return new AcceptedResult(
    $"/api/saludos/{instanceId}",
    new { instanceId, estadoUrl = $"/api/saludos/{instanceId}" });
```

Devuelve `202 Accepted` con un `instanceId` único. Esa respuesta es la promesa "te he encolado el trabajo; consulta el estado en esta URL". El cliente NO espera al resultado — el orquestador puede tardar segundos o minutos en completar.

**`GET /api/saludos/{instanceId}`** — consulta el estado actual:

```csharp
var estado = await client.GetInstanceAsync(instanceId, getInputsAndOutputs: true);
return new OkObjectResult(new {
    instanceId,
    runtimeStatus = estado.RuntimeStatus.ToString(),
    createdAt = estado.CreatedAt,
    lastUpdatedAt = estado.LastUpdatedAt,
    output = estado.SerializedOutput,
});
```

El `runtimeStatus` te devuelve `Pending → Running → Completed` (o `Failed`, `Terminated`, etcétera). Cuando es `Completed`, `SerializedOutput` lleva el JSON con el array de saludos.

Este patrón —starter devuelve URL de estado, cliente pollea el estado— es la versión más simple del **async API pattern** que el submódulo S4.2 desarrolla en profundidad.

---

## 6. La regla del determinismo, en una imagen

El orquestador del ejemplo cumple la regla del determinismo sin esfuerzo, porque no hace nada que pudiera romperla. Pero cuando empieces a escribir orquestadores reales, vas a tener la tentación de meter cosas que **no debes**:

| ❌ Esto rompe Durable | ✅ Hazlo así |
| --- | --- |
| `DateTime.Now` | `context.CurrentUtcDateTime` |
| `new Random().Next()` | `context.NewGuid()` o pasa el random como input |
| `await httpClient.GetAsync(...)` | `await context.CallActivityAsync(...)` (la activity hace el HTTP) |
| `Thread.Sleep(1000)` | `await context.CreateTimer(TimeSpan.FromSeconds(1), CancellationToken.None)` |
| Leer una variable de entorno | Pasarla como input al orquestador |
| `Guid.NewGuid()` | `context.NewGuid()` |

¿Por qué? Porque el motor de Durable ejecuta el método del orquestador **muchas veces durante la vida del flujo**, reconstruyendo el estado desde el historial. Si en ese replay `DateTime.Now` devuelve algo distinto, el árbol de decisiones diverge del original y el flujo se corrompe.

Las versiones "correctas" funcionan porque el motor de Durable las controla: `CurrentUtcDateTime` devuelve siempre lo mismo durante el replay (el valor de la primera ejecución, guardado en el historial); `CreateTimer` se delega al motor; `NewGuid` se genera deterministamente a partir de un seed conocido.

No memorices la tabla. Recuerda la idea: **el orquestador no puede tener efectos**; todos los efectos van a activities. Si te encuentras escribiendo algo "raro" en un orquestador, la pregunta es "¿puedo poner esto en una activity?". La respuesta es casi siempre que sí.

---

## 7. Cómo probarlo en local

El punto donde la práctica brilla: **NO necesita Cosmos emulator ni Service Bus**. Solo Azurite:

```bash
azurite --silent --location ./azurite-data &
cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json
func start --csharp
```

Mandar un POST:

```bash
R=$(curl -s -X POST http://localhost:7071/api/saludos \
     -H "Content-Type: application/json" \
     -d '["Ana","Luis","Marta"]')
echo $R
# { "instanceId": "abc123...", "estadoUrl": "/api/saludos/abc123..." }
```

Consultar el estado un par de segundos después:

```bash
ID=$(echo $R | jq -r .instanceId)
curl http://localhost:7071/api/saludos/$ID | jq
# {
#   "instanceId": "abc123...",
#   "runtimeStatus": "Completed",
#   "output": "[\"¡Hola, Ana!\",\"¡Hola, Luis!\",\"¡Hola, Marta!\"]"
# }
```

Mira los logs del `func start`: deberías ver tres líneas `Saludo generado: ¡Hola, X!` en milisegundos de diferencia (es el fan-out paralelo) y luego la línea de `Completados 3 saludos` del orquestador.

Si quieres ver el historial paso a paso del orquestador, en producción tienes el **panel de Durable Functions** en el Portal de Azure: te muestra cada paso del orquestador con su input, output y duración. En local también puedes inspeccionar las tablas de Azurite que crea Durable (`AzureStorageTaskhubHistoryHello*`, `AzureStorageTaskhubInstancesHello*`) con Azure Storage Explorer, aunque es menos cómodo que la UI del Portal.

> Yo no lanzo apps. Tú haces `func start --csharp` y `dotnet test`.

---

## 8. Los tests son la documentación viva

Solo ocho, pero cubren bien las dos piezas testeables:

**`SaludarActivityTests`** (5, todos `[Theory]`) — la lógica pura `Construir(string?)`:
- `"Ana"` → `"¡Hola, Ana!"`
- `"  Luis  "` → `"¡Hola, Luis!"` (trim)
- `""` → `"¡Hola, desconocido!"` (fallback)
- `null` → `"¡Hola, desconocido!"` (fallback null-safe)
- `"   "` → `"¡Hola, desconocido!"` (string solo whitespace)

**`SaludosOrchestratorTests`** (3) — el orquestador completo, con `TaskOrchestrationContext` mockeado por NSubstitute:
- Con `["Ana", "Luis"]` como input, se llama a `CallActivityAsync("Saludar", "Ana")` y a `CallActivityAsync("Saludar", "Luis")`, y se devuelve un array de dos elementos.
- Con `null` como input, devuelve `[]` sin invocar activities.
- Con `[]` como input, devuelve `[]` sin invocar activities.

El mock del `TaskOrchestrationContext` se prepara con los tres trucos que aprendiste en S4.2:

```csharp
var ctx = Substitute.For<TaskOrchestrationContext>();
ctx.CreateReplaySafeLogger<SaludosOrchestrator>().Returns(NullLogger<SaludosOrchestrator>.Instance);
ctx.GetInput<List<string>>().Returns(["Ana", "Luis"]);
ctx.CallActivityAsync<string>("Saludar", "Ana", Arg.Any<TaskOptions>()).Returns("¡Hola, Ana!");
```

Si te saltas el `CreateReplaySafeLogger`, el orquestador lanza `NullReferenceException` al loguear porque NSubstitute devuelve null por defecto. Si te saltas el `GetInput`, recibes `default(List<string>) = null` y el flujo se va por la rama "lista vacía" sin avisar.

---

## 9. La pieza que el ejemplo NO necesita

Cuando alguien lee el `Program.cs` por primera vez espera ver registros de DI como en los otros ejemplos:

```csharp
builder.Services.AddSingleton<IFoo, Foo>();
builder.Services.AddSingleton<IBar, Bar>();
```

**Aquí no hay nada de eso.** Las tres funciones no inyectan servicios de negocio (el `[DurableClient]` lo provee el binding, no el contenedor; el `FunctionContext` y el `TaskOrchestrationContext` también vienen del runtime). Por eso `Program.cs` es prácticamente el esqueleto mínimo, sin un solo `Add*` adicional.

Esto refuerza la lección dura del módulo S4.5: **cruzar a mano cada constructor con `Program.cs` es una validación que importa cuando hay dependencias, pero también importa cuando no las hay**. Si añadieras un `IGreeterCustomizer` al constructor de `SaludarActivity` para personalizar el saludo, y olvidaras registrarlo en `Program.cs`, la app arrancaría pero al primer disparo de la activity reventaría con "Unable to resolve service". Lo trivial del ejemplo es exactamente lo que hace fácil ver el patrón en estado puro.

---

## 10. Glosario breve

- **Starter / Client function**: la función que arranca el orquestador. Puede tener cualquier tipo de trigger (HTTP, Service Bus, Timer...) y se distingue por inyectar `[DurableClient] DurableTaskClient`.
- **Orchestrator**: la función que define el flujo. Inyecta `[OrchestrationTrigger] TaskOrchestrationContext`. **Determinista por contrato.**
- **Activity**: la función que hace el trabajo real. Inyecta `[ActivityTrigger]` y el tipo del input. No hay restricción de determinismo aquí; puede hacer I/O, leer `DateTime.Now`, etcétera.
- **Replay**: el motor de Durable ejecuta el orquestador muchas veces a lo largo del flujo, reconstruyendo el estado desde el historial. Por eso el orquestador debe ser determinista.
- **Instance ID**: identificador único de una ejecución del orquestador. Lo genera el motor cuando llamas a `ScheduleNewOrchestrationInstanceAsync`. Con él puedes consultar el estado en cualquier momento.
- **`CreateReplaySafeLogger`**: helper de `TaskOrchestrationContext` que devuelve un `ILogger` que solo emite logs en la primera ejecución del orquestador, no en los replays.
- **Fan-out / Fan-in**: patrón donde lanzas N actividades en paralelo (fan-out) y esperas a todas para consolidar (fan-in, normalmente con `Task.WhenAll`).

---

## 11. Cierre

S4.P2 cierra el módulo con la versión más reducida posible de Durable: tres funciones, un patrón, ocho tests. Si te quedas con una sola cosa, que sea el patrón fan-out/fan-in con `Task.WhenAll` — es la base de cualquier flujo paralelo en Durable, y a partir de ahí los otros patrones (chaining, retry, human interaction, saga) son combinaciones y variaciones.

Cuando vuelvas a Durable dentro de unos meses para un proyecto real, lee este manual en quince minutos para recuperar el modelo mental, copia las tres funciones como esqueleto, y sustituye el saludo por tu lógica. Empezarás desde el momento correcto.

**Con S4.P2, M04 queda completo**: cinco submódulos teóricos (S4.1–S4.5) más las dos prácticas integradoras (S4.P, S4.P2). El siguiente bloque del curso es M05 — Almacenamiento y BBDD — donde el foco vuelve a la persistencia. Ahí ya tienes manuales escritos.
