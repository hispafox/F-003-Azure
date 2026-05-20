# Manual del alumno — S4.2 · Durable Functions: orquestación de flujos

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: estructura, pasos por Portal, scripts. Este manual va antes: te cuenta qué cambio mental hay que hacer para usar Durable, la regla del determinismo (la única que importa) y los cinco patrones que cubren el 95% de los flujos reales.

Tiempo de lectura: ~30 min. Submódulo de teoría: [M04-S4.2](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.2-durable-functions-v4.md). Una saga real de procesamiento de pedidos (chaining + retry + human interaction + compensación) más un fan-out/fan-in de facturas más una Durable Entity como contador persistente.

*Creado: 2026-05-20 13:07 +0200*

---

## 1. La idea en una frase

Hasta S4.1, cuando un flujo tenía varios pasos, la coordinación era manual: una función encola, otra consume, llama a la siguiente, cada una con su propio estado. El código termina disperso en colas, suscripciones y funciones distintas; sigue funcionando, pero entender "qué pasa con un pedido concreto" requiere rebuscar en tres logs distintos. **Durable Functions cambia el modelo**: la coordinación se escribe como **un solo método C# que parece síncrono** —espera resultados con `await`, decide con `if`, captura errores con `try/catch`— pero por detrás cada paso es una activity independiente con su propio retry y persistencia.

Lo que ves es código lineal:

```csharp
var reserva = await context.CallActivityAsync<Reserva>("ReservarInventario", pedido);
if (pedido.Total > 5000)
    await EsperarAprobacionAsync(context);
var pago = await context.CallActivityAsync<Pago>("ProcesarPago", reserva);
```

Lo que pasa por debajo son tres activities ejecutándose en distintas instancias de Functions, con un orquestador que se ejecuta **muchas veces** durante la vida del flujo (replay) reconstruyendo el estado desde un historial guardado en Storage. La magia operativa: el flujo puede durar **semanas** (esperando una aprobación humana) y sobrevivir reinicios, deploys y crashes. Tu código sigue siendo el método lineal.

---

## 2. El problema real que hay detrás

Una empresa tenía un flujo de aprobación de pedidos con cuatro pasos y una decisión humana en medio:

1. Validar el pedido contra reglas de negocio.
2. Reservar inventario en el almacén.
3. **Si el total supera 5.000 €, esperar aprobación del manager** (puede tardar horas o días).
4. Procesar el pago.
5. Enviar confirmación.

Si algo falla a mitad, hay que **deshacer lo anterior** — si el pago rebota, liberar la reserva de inventario; si la aprobación se rechaza, lo mismo. Es la receta clásica de **saga con compensación**.

La primera implementación: cinco funciones de Service Bus encadenadas con `[ServiceBusOutput]` entre ellas, una App Setting `EstadoActual` por pedido en Cosmos, lógica de "qué paso viene ahora" duplicada en cada función. Mantener el flujo era una pesadilla: añadir un nuevo paso significaba refactorizar tres funciones y migrar el estado almacenado. Y el peor caso era el "esperar aprobación del manager" — había que dejar el pedido en estado `pendiente_aprobacion` con un Timer que comprobaba cada 5 minutos si había llegado la decisión.

Con Durable Functions el código se reduce a un método de 40 líneas que se lee como pseudocódigo. La espera de la aprobación es `WaitForExternalEvent("AprobacionManager")` con un `Task.WhenAny` contra un timer de 72 horas. La compensación es un `catch` que llama a `CompensarPedido`. El estado lo gestiona Durable por debajo, en Storage. **No hay queue, no hay Cosmos para estado intermedio, no hay polling.**

Lo que entrega el ejemplo:

| Patrón | Para qué | Dónde |
| --- | --- | --- |
| **Chaining** (pasos secuenciales) | Encadenar activities con `await` lineal | [`ProcesarPedidoOrchestrator.cs`](src/AzureFunctions.Demo/Functions/ProcesarPedidoOrchestrator.cs) |
| **Retry policies** (backoff exponencial) | Reintentar activities ante fallos transitorios | `RetryActivities` (3 intentos, 5s/10s/20s) |
| **Human interaction** (esperar evento + timeout) | `WaitForExternalEvent` + `CreateTimer` + `Task.WhenAny` | `EsperarAprobacionAsync` |
| **Saga / compensación** | `try/catch` → llamar a activities de rollback | catch de `TaskFailedException` → `CompensarPedido` |
| **Fan-out / Fan-in** | Lanzar N activities en paralelo, esperar todas | [`ProcesarLoteFacturasOrchestrator.cs`](src/AzureFunctions.Demo/Functions/ProcesarLoteFacturasOrchestrator.cs) (chunks de 50) |
| **Durable Entity** (estado persistente) | "Objeto" con estado que vive entre orquestaciones | [`ContadorPedidosEntity.cs`](src/AzureFunctions.Demo/Functions/ContadorPedidosEntity.cs) |
| **Starter / status / raise event** | HTTP triggers para arrancar, consultar y mandar eventos externos | [`PedidoStarterFunctions.cs`](src/AzureFunctions.Demo/Functions/PedidoStarterFunctions.cs) |

---

## 3. Por qué esto importa en tu stack

Cualquier sistema que coordine pasos con estado se beneficia de Durable cuando el flujo es complejo. La pregunta práctica: **¿cuánto durará y cuántos pasos hay?**.

- **Flujo de < 1 minuto, sin estado intermedio que valga la pena guardar**: una función normal vale. Durable es overkill (el replay añade ~50-200ms).
- **Flujo simple sin coordinación entre pasos** (fan-out independiente, mensajes asíncronos): Service Bus Topic o Queue.
- **Flujo con coordinación, estado entre pasos, posible espera larga o compensación**: Durable. Es el sitio donde más brilla.
- **Eternal orchestrations** (procesos que viven indefinidamente con `ContinueAsNew`) y monitor pattern (polling con timers): Durable, casos avanzados.

El cambio mental: en lugar de pensar "función + queue + función + queue", piensas **"método lineal con `await` entre pasos"**. La queue desaparece, el estado intermedio desaparece, el polling de "ya ha pasado X" desaparece. Lo que queda es el código de negocio.

Y la limitación honesta: el modelo tiene **un precio** — el replay. El orquestador se ejecuta varias veces durante la vida del flujo, reconstruyendo su estado desde el historial. Por eso hay reglas estrictas sobre qué puede hacer (sección 4) y por eso no es para tareas que necesitan latencia ultra-baja.

---

## 4. El modelo mental: el director de orquesta y la partitura

Imagina una orquesta sinfónica. El **director** (orquestador) no toca ningún instrumento. Lleva la batuta y dice "ahora violines", "ahora trompetas", "ahora silencio durante cuatro compases", "ahora todos". Los **músicos** (activities) interpretan cuando el director les marca, cada uno con su instrumento (servicio). El director conoce la **partitura** (el código del orquestador) y la sigue exactamente — si en la partitura pone "tras la entrada de violines, esperar la respuesta del oboe", el director espera al oboe antes de la siguiente indicación.

Y un detalle crítico: si el director se duerme y otro director toma su lugar a media obra, **el nuevo director tiene que ser capaz de reconstruir exactamente dónde se quedaron**. Para eso lee la partitura desde el principio y mira qué entradas musicales ya se han producido (el historial). Si en la partitura ponía "el director decide qué tocar lanzando un dado", el segundo director **no puede reconstruir** la decisión del primero — tendría que tirar el dado otra vez y le saldría algo distinto. **Por eso la partitura no permite azar**.

```
                    POST /api/pedidos/procesar
                              │
                              ▼
              ProcesarPedido (Orchestrator)
              ┌─────────────────────────────┐
              │ var r = await Reservar(p)   │ ← marca de batuta 1
              │ if (p.Total > 5000) {       │ ← decisión determinista
              │   await EsperarAprobacion() │ ← marca de batuta 2 (puede tardar días)
              │ }                            │
              │ try {                        │
              │   await ProcesarPago(p)     │ ← marca de batuta 3
              │ } catch {                    │
              │   await Compensar(r)        │ ← rollback
              │ }                            │
              │ await Confirmar()           │ ← marca de batuta 4
              └─────────────────────────────┘
                     │  cada activity →
                     ▼
              ┌──────────────────┐
              │ Activities       │ ← músicos
              │ (adaptadores)    │
              └──────┬───────────┘
                     │
                     ▼
              ┌──────────────────┐
              │ Servicios C#     │ ← instrumentos (lógica real)
              └──────────────────┘
                     │
                     ▼
              ┌──────────────────┐
              │ Storage Tables   │ ← historial de la obra
              │ Storage Queues   │   (de dónde se reconstruye)
              └──────────────────┘
```

Tres frases para fijar el modelo:

- **El orquestador no hace I/O, no usa azar, no consulta el reloj**. Solo decide y delega. Toda la "magia" la hacen las activities y los servicios. Esta regla es absoluta — su violación rompe el modelo silenciosamente.
- **Las activities son adaptadores finos**. Llaman a un servicio inyectado y devuelven el resultado. La lógica de negocio vive en los servicios, no en las activities. Esa separación es lo que hace los tests **rápidos y sin runtime de Durable**.
- **El historial es la memoria persistente del flujo**. Si el host se cae a mitad de un flujo, cuando vuelve a arrancar lee el historial desde Storage Tables y reconstruye dónde estaba. El flujo de "esperar 72 horas la aprobación del manager" sobrevive reinicios sin problema.

---

## 5. La regla del determinismo (la única que importa de verdad)

El orquestador es **determinista por contrato**. Si lo violas, Durable funciona en pruebas pequeñas y rompe en producción con errores confusos. Las violaciones más comunes:

| Violación | Por qué es problema | Alternativa correcta |
| --- | --- | --- |
| `DateTime.UtcNow` | Da valor distinto en cada replay | `context.CurrentUtcDateTime` |
| `Guid.NewGuid()` | Aleatorio, distinto en cada replay | `context.NewGuid()` |
| `new Random().Next()` | Aleatorio | extraer azar a una activity |
| `await httpClient.GetAsync(...)` | I/O con resultado posiblemente distinto | extraer a activity |
| `await dbContext.SaveChangesAsync()` | I/O + efecto secundario | extraer a activity |
| `Task.Delay(timespan)` | Bloquea el thread; replay se vuelve infinito | `context.CreateTimer(...)` |
| `Thread.Sleep` | Lo mismo y peor | `context.CreateTimer(...)` |
| Llamada a APIs externas | I/O no determinista | extraer a activity |

La regla mental simple: **el orquestador solo puede hacer dos cosas — `context.*` y `await context.CallActivityAsync`**. Cualquier otra cosa (incluyendo "let me just check this one thing", "es solo un log", "necesito el timestamp actual") rompe el determinismo. **El logging dentro del orquestador tiene que usar `context.CreateReplaySafeLogger<T>()`**, no un `ILogger` directo — el replay-safe logger silencia las invocaciones de replay para que los logs no se dupliquen N veces.

> 🧠 **El bug más cruel del determinismo**: el orquestador se ejecuta hasta el primer `await context.CallActivityAsync` la primera vez. Cuando la activity termina, Durable guarda su resultado en el historial y **vuelve a ejecutar el orquestador desde el principio**. Pero esta vez, cuando llega al primer `await`, ya hay un resultado en el historial — lo lee y sigue. Después llega al segundo `await`, espera, y otra vez replay. **Si tu código tenía un `DateTime.UtcNow` antes del primer `await`, cada replay produce un timestamp distinto** — y el control de flujo basado en ese timestamp (un `if (DateTime.UtcNow > deadline)`) puede dar resultados incoherentes entre invocaciones. La excepción que verás eventualmente es `NonDeterministicOrchestrationException`, pero el síntoma confuso es "el flujo se comporta de forma rara y no entiendo por qué".

---

## 6. Los cinco patrones que cubren el 95% de los casos

### Chaining: el más simple, secuencia con `await`

```csharp
var resultado1 = await context.CallActivityAsync<R1>("Activity1", input);
var resultado2 = await context.CallActivityAsync<R2>("Activity2", resultado1);
var resultado3 = await context.CallActivityAsync<R3>("Activity3", resultado2);
return resultado3;
```

Tres activities en serie. Cada una espera a la anterior. Es el patrón básico de "primero esto, luego esto, luego esto". El equivalente con Service Bus sería tres queues encadenadas — aquí cabe en seis líneas.

### Retry con backoff exponencial

```csharp
var options = new TaskOptions(new RetryPolicy(
    maxNumberOfAttempts: 3,
    firstRetryInterval: TimeSpan.FromSeconds(5),
    backoffCoefficient: 2));
await context.CallActivityAsync("ProcesarPago", input, options);
```

Si la activity falla por excepción, Durable la reintenta automáticamente con backoff exponencial (5s, 10s, 20s). Si los 3 intentos fallan, lanza `TaskFailedException` al orquestador — tu código puede capturarla y reaccionar (compensación). Sin este mecanismo, todo error era "retry manual con queue y polling".

### Human interaction: esperar evento + timeout

```csharp
private async Task<bool> EsperarAprobacionAsync(TaskOrchestrationContext context)
{
    var aprobacionTask = context.WaitForExternalEvent<bool>("AprobacionManager");
    var timeoutTask = context.CreateTimer(
        context.CurrentUtcDateTime.AddHours(72), CancellationToken.None);

    var ganador = await Task.WhenAny(aprobacionTask, timeoutTask);
    return ganador == aprobacionTask && aprobacionTask.Result;
}
```

`WaitForExternalEvent` bloquea hasta que alguien (vía HTTP, otra función) llame a `RaiseEventAsync(instanceId, "AprobacionManager", true)`. **`Task.WhenAny`** contra un `CreateTimer` añade timeout: si pasan 72 horas sin aprobación, el timer gana y la función retorna `false` → compensación. Durante esas 72 horas el flujo está suspendido sin consumir compute; el estado vive en Storage.

### Saga / compensación

```csharp
try
{
    await context.CallActivityAsync("ProcesarPago", input, retryOptions);
}
catch (TaskFailedException ex)
{
    // El pago falló tras 3 reintentos. Compensar.
    await context.CallActivityAsync("LiberarReserva", reserva);
    await context.CallActivityAsync("NotificarRechazo", pedido);
    return "compensado";
}
```

Si la activity falla definitivamente (tras agotar reintentos), `TaskFailedException` aparece en el `await`. Lo capturas con `try/catch` y llamas a activities de rollback. El patrón se llama **saga**: una secuencia de operaciones con sus correspondientes compensaciones por si una falla. Sin Durable, implementar esto en Service Bus requiere queues de compensación, estado persistente para saber qué deshacer, lógica dispersa. Aquí cabe en un `try/catch` lineal.

### Fan-out / Fan-in con control de paralelismo

```csharp
var facturas = input.Facturas;
const int chunkSize = 50;

for (int i = 0; i < facturas.Count; i += chunkSize)
{
    var chunk = facturas.Skip(i).Take(chunkSize);
    var tareas = chunk.Select(f =>
        context.CallActivityAsync<Resultado>("ProcesarFactura", f));
    var resultados = await Task.WhenAll(tareas);
    // ... agregar resultados ...
}
```

Lanzas N activities en paralelo con `Task.WhenAll`. Para no saturar el plan Consumption, las divides en **chunks de 50** y procesas chunk por chunk. Cada activity dentro del chunk corre en paralelo; entre chunks van secuencial. Es la forma de procesar 1000 facturas sin desbordar el sistema ni dejarlas todas en paralelo a la vez.

---

## 7. Las activities como adaptadores finos

Mira `PedidoActivities.cs`. Cada activity es **unas tres líneas**:

```csharp
[Function(nameof(ReservarInventario))]
public async Task<Reserva> ReservarInventarioAsync(
    [ActivityTrigger] Pedido pedido,
    IInventarioService inventario)
{
    return await inventario.ReservarAsync(pedido);
}
```

La activity llama a un servicio inyectado. **El servicio tiene toda la lógica**: `InMemoryInventarioService` valida stock, descuenta cantidades, persiste. La activity no contiene lógica de negocio — solo es el "punto de entrada" que Durable invoca, con DI funcionando normalmente.

¿Por qué tan finas? Por **testabilidad**. El servicio (`InMemoryInventarioService`) se testea con xUnit directamente, sin Durable, sin Functions, sin Storage. Una vez que confías en que el servicio hace lo correcto, la activity es trivial — solo enchufa. Los tests del orquestador (sección siguiente) tampoco tocan Durable real; mockean `TaskOrchestrationContext` para verificar que el orquestador llama a las activities en el orden correcto.

> 🧠 **La regla: orquestador → activity → servicio**. El orquestador decide qué activity llamar y en qué orden. La activity es el adaptador (cinco líneas, sin lógica). El servicio es donde está todo el código real de negocio. **Esa separación es lo que permite los 22 tests del ejemplo sin tocar Storage**.

Y un detalle de DI: las activities pueden recibir servicios por constructor o por parámetro (`IInventarioService inventario` en el ejemplo). Functions Worker SDK 2.x resuelve los parámetros desde el contenedor DI cuando invoca la activity. La regla del cruce manual de constructores contra `Program.cs` (HANDOFF) aplica también aquí: **si una activity necesita un servicio nuevo, regístralo en `Program.cs` o la activity revienta en runtime**.

---

## 8. Tests de Durable con NSubstitute

Aquí está la pieza didácticamente más interesante del módulo. La superficie de `TaskOrchestrationContext` (≈20 métodos virtuales: `CallActivityAsync`, `WaitForExternalEvent`, `CreateTimer`, `CurrentUtcDateTime`, etc.) es demasiado grande para un fake manual como el `FakeServiceBusMessageActions` de S4.1. La solución es **mockear con NSubstitute**:

```csharp
var ctx = Substitute.For<TaskOrchestrationContext>();
ctx.CreateReplaySafeLogger<ProcesarPedidoOrchestrator>()
   .Returns(NullLogger<ProcesarPedidoOrchestrator>.Instance);
ctx.GetInput<Pedido>().Returns(pedido);
ctx.CallActivityAsync<Reserva>(nameof(ReservarInventario), pedido, Arg.Any<TaskOptions>())
   .Returns(new Reserva { /* ... */ });

await orchestrator.ProcesarPedido(ctx);

await ctx.Received().CallActivityAsync(nameof(EnviarConfirmacion), ...);
```

Los tests verifican que **el orquestador llama a las activities en el orden y con los argumentos correctos** en cada escenario:

- **Camino feliz** (`PedidoServicesTests` + el orquestador con total bajo): chaining completo, sin pedir aprobación.
- **Pago falla** (forzando `TaskFailedException` en el mock de `ProcesarPago`): captura, llama a `LiberarReserva` y `NotificarRechazo`, estado final `compensado`.
- **Total > 5000 con aprobación**: lanza el evento `AprobacionManager` con `true`, sigue hasta `completado`.
- **Total > 5000 con rechazo**: lanza el evento con `false`, compensa.
- **Total > 5000 con timeout**: deja pasar el `CreateTimer`, el timer gana en `Task.WhenAny`, compensa.

> 🧠 **Tres trucos críticos** del HANDOFF para mockear `TaskOrchestrationContext` con NSubstitute:
>
> **(1)** `ctx.CreateReplaySafeLogger<T>()` devuelve `null` por defecto en el mock → hay que configurarlo a `NullLogger<T>.Instance` o el orquestador peta al loguear. Es la primera trampa.
>
> **(2)** `TaskFailedException` tiene **constructor público**: `new TaskFailedException(taskName, taskId, innerException)`. Usa eso para simular el fallo de una activity tras agotar reintentos.
>
> **(3)** `ServiceBusModelFactory` (usado en S4.1 con SB) no aplica aquí, pero el patrón equivalente es: usar **`Substitute.For<TaskOrchestrationContext>()`** para el contexto del orquestador y configurar cada método que el código llama (`GetInput`, `CallActivityAsync`, `WaitForExternalEvent`, `CreateTimer`, `CurrentUtcDateTime`).

Esta capacidad de mockear el contexto del orquestador es lo que hace los 22 tests del ejemplo **rápidos y sin Storage**. Sin esto, los tests serían integración real con runtime de Durable, ~10x más lentos, frágiles, dependientes de Azurite. Con esto, son tests unitarios normales en milisegundos.

---

## 9. La Durable Entity: estado persistente con dispatch

`ContadorPedidosEntity.cs` es una pieza distinta del modelo: una **entidad durable**, conceptualmente un objeto con estado que vive entre orquestaciones.

```csharp
public sealed class ContadorPedidosState
{
    public int Total { get; set; }
    public int Completados { get; set; }
    public int Compensados { get; set; }
}

[Function(nameof(ContadorPedidos))]
public Task ContadorPedidos([EntityTrigger] TaskEntityDispatcher dispatcher)
    => dispatcher.DispatchAsync<ContadorPedidosState>();
```

Cualquier código puede mandarle "operaciones" como `Incrementar`, `MarcarCompletado`, `Reset`. Durable persiste el estado en Storage y serializa las operaciones para que **dos clientes no escriban a la vez**. Es una alternativa a "tener una tabla en BD con contadores" — más simple, integrada con Durable, sin necesidad de gestionar conexiones.

¿Cuándo merece? Para **estados pequeños que necesitan operaciones atómicas** sin abrir BD aparte. Contadores agregados, configuración compartida, estado de un agente. Para datos grandes o consultables, sigue siendo Cosmos/SQL.

---

## 10. Recorrido guiado

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `POST /api/pedidos/procesar` con `total: 1200` | `202 Accepted` con `instanceId`; tras ~5s `runtimeStatus: Completed`, output `"completado"` | Chaining feliz: Validar → Reservar → Pago → Confirmar (sección 6). |
| 2 | `POST /api/pedidos/procesar` con `total: 99.99` | Tras ~10s `output: "compensado"` | El servicio de pago rechaza `.99` → `TaskFailedException` → `try/catch` → `LiberarReserva` → `NotificarRechazo` (sección 6 saga). |
| 3 | `POST /api/pedidos/procesar` con `total: 8500` | `instanceId` devuelto; `customStatus: "esperando-aprobacion"` indefinidamente | El umbral > 5000 entra en `EsperarAprobacionAsync` (sección 6 human interaction). |
| 4 | `POST /api/pedidos/{id3}/aprobar` con `aprobado: true` | Tras ~5s `output: "completado"` | `RaiseEventAsync("AprobacionManager", true)` despierta el `WaitForExternalEvent`. |
| 5 | Repite paso 3 y **no mandes aprobación**. Espera 72 horas (o reduce el timer a 5 segundos en código para demo) | output `"rechazado"` por timeout | El `CreateTimer` gana en `Task.WhenAny` cuando nadie aprueba. |
| 6 | `POST /api/facturas/lote` con 120 facturas | Tras ~30s output con 3 chunks consolidados | Fan-out/fan-in con chunks de 50 (sección 6). |
| 7 | Llama 5 veces a `ContadorPedidosEntity.Incrementar` desde el orquestador | el estado persiste entre orquestaciones | Durable Entity (sección 9). |

Un experimento muy útil para entender el determinismo: en el orquestador (a propósito) reemplaza `context.CurrentUtcDateTime` por `DateTime.UtcNow` y ejecuta el paso 1. **Aparentemente funciona la primera vez**. Pero si haces el paso 2 o el paso 3 (donde hay más replay porque el flujo dura más), eventualmente verás `NonDeterministicOrchestrationException`. Es la primera vez que la regla se hace tangible — y queda grabada.

---

## 11. Tests del proyecto

22 tests sin Storage, sin runtime de Durable, sin Functions:

- **`PedidoServicesTests`** (12) — los servicios inyectados (validador, inventario, pago, facturación). Tests puros de C#. Aquí está el grueso de la lógica de negocio testeada con xUnit clásico.
- **`ProcesarPedidoOrchestratorTests`** (5) — el orquestador con `Substitute.For<TaskOrchestrationContext>()`. Cinco escenarios: feliz, pago falla, total > 5000 con aprobación, con rechazo, con timeout.
- **`ProcesarLoteFacturasOrchestratorTests`** (3) — fan-out/fan-in: consolidación correcta, lote vacío, 120 facturas en chunks.
- **`ContadorPedidosStateTests`** (2) — la lógica del State POCO, el dispatcher es trivial y no requiere test.

NSubstitute solo en el proyecto de tests; el runtime no lo lleva.

---

## 12. Puesta en marcha, ejecución y pruebas

### 12.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y testear | Sí |
| Azure Functions Core Tools | `func start` local | Recomendado |
| Azurite | emular Storage local (Durable lo necesita) | Sí |

### 12.2 Compilar y arrancar en local

```bash
cd examples/M04-Azure-Functions-II/S4.2-durable-functions
dotnet build AzureFunctions.Demo.slnx       # 0 errores

cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json

azurite --silent            # otra terminal

cd src/AzureFunctions.Demo
func start
```

### 12.3 Pasar los tests

```bash
dotnet test
```

Resultado: **22 pass · 0 fail**. Sin Azure, sin runtime de Durable.

### 12.4 Desplegar a Azure (resumen)

Patrón estándar: RG + Storage + Function App Consumption Linux .NET 10 isolated. Durable usa el Storage Account asociado al runtime (`AzureWebJobsStorage`) para sus tables (historial) y queues (control queue). **No hay coste fijo** — Storage cobra céntimos por las pocas filas/mensajes que Durable usa.

Para ver las orquestaciones en el portal: tu Function App → **Durable Functions** o **Functions** → cualquier orquestador → **Monitor**. Verás cada `instanceId` con su estado (Running / Completed / Failed) y el historial paso a paso.

### 12.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `NonDeterministicOrchestrationException` | algo no determinista en el orquestador | revisa cualquier `DateTime.UtcNow`, `Guid.NewGuid()`, I/O, `Random` — sección 5 |
| El test del orquestador peta con NullReferenceException en logger | `CreateReplaySafeLogger` devuelve null por defecto en el mock | configura `ctx.CreateReplaySafeLogger<T>().Returns(NullLogger<T>.Instance)` |
| `TaskFailedException` no se simula | constructor mal usado | usa `new TaskFailedException(taskName, taskId, innerException)` — es público |
| Orquestador no avanza tras `WaitForExternalEvent` | nadie llamó a `RaiseEventAsync` | usa el endpoint `POST /api/pedidos/{id}/aprobar` o cualquier mecanismo equivalente |
| Test del orquestador necesita configurar `GetInput<T>()` | el mock no devuelve el input automáticamente | añade `ctx.GetInput<Pedido>().Returns(pedido);` |
| El flujo dura más de lo esperado en local | el polling de Durable en local es lento por defecto | normal; en Azure es más rápido |

### 12.6 Limpieza

`Portal → Resource groups → rg-curso-m04-s42 → Delete`. Sin coste fijo, pero limpia igual.

---

## 13. Ideas para llevarte

Lo más útil de Durable es **el cambio mental**: cuando un flujo tiene varios pasos con estado y posibles esperas, ya no piensas "queue → función → queue → función". Piensas **"método lineal con `await` entre pasos"**. Esa simplificación cambia cómo diseñas sistemas distribuidos.

Sobre la **regla del determinismo**: respétala religiosamente. No es "buena práctica", es la condición sin la cual Durable funciona. Cualquier I/O, cualquier azar, cualquier reloj → activity. El orquestador solo decide y delega.

Sobre la **separación orquestador → activity → servicio**: aplícala desde el primer Durable que escribas. Es lo que hace los tests rápidos y sin runtime de Durable. La activity es un adaptador de cinco líneas; la lógica vive en los servicios; el orquestador coordina. Cuando un orquestador empieza a tener "lógica de negocio dentro", refactoriza.

Y sobre **cuándo no usar Durable**: si tu flujo cabe en una sola función, no Durable. Si son dos funciones acopladas vía cola sin estado entre pasos, Service Bus. Durable empieza a brillar cuando hay coordinación, estado intermedio o esperas largas. La regla práctica: **¿hace falta el contexto de algo anterior para decidir el siguiente paso?**. Si la respuesta es sí, Durable. Si no, mensajería normal.

---

## 14. Comprueba que lo has entendido

1. ¿Por qué el orquestador no puede usar `DateTime.UtcNow` y qué tiene que usar en su lugar? *(secciones 4, 5)*
2. Tu orquestador llama a una activity de "procesar pago" con retry de 3 intentos. Los tres fallan. ¿Qué excepción ves en el orquestador y cómo la manejas para hacer compensación? *(sección 6 saga)*
3. Tienes que esperar la aprobación de un manager durante hasta 72 horas. ¿Cómo lo escribes y qué pasa con el compute durante esas horas? *(secciones 1, 6 human interaction)*
4. Tu activity necesita una llamada HTTP a una API externa. ¿La pones en el orquestador o en la activity? ¿Por qué? *(sección 4, sección 7)*
5. Tu test mockea `TaskOrchestrationContext` con NSubstitute y el orquestador peta con NullReferenceException en `LogInformation`. ¿Qué te falta? *(sección 8)*
6. Tienes que procesar 500 facturas en paralelo. ¿`Task.WhenAll` con las 500 a la vez o chunks? ¿Por qué? *(sección 6 fan-out/fan-in)*

<details>
<summary>Respuestas</summary>

1. Porque el orquestador **se ejecuta múltiples veces** durante la vida del flujo (replay), reconstruyendo su estado desde el historial de Storage. `DateTime.UtcNow` da un valor distinto en cada replay → el control de flujo deja de ser coherente y eventualmente lanza `NonDeterministicOrchestrationException`. En su lugar usas `context.CurrentUtcDateTime`, que está congelado al momento original de la decisión y devuelve el mismo valor en cada replay. La misma regla aplica a `Guid.NewGuid()` → `context.NewGuid()`, `Task.Delay` → `context.CreateTimer`, y cualquier I/O → mover a activity.
2. **`TaskFailedException`** en el `await`. La manejas con `try/catch` y llamas a las activities de compensación: `await context.CallActivityAsync("LiberarReserva", ...)`, `await context.CallActivityAsync("NotificarRechazo", ...)`. Devuelves un estado distinto (`"compensado"`). Ese patrón se llama **saga** y es uno de los grandes valores de Durable — sin él, implementar compensación con Service Bus requiere queues de "rollback", estado persistente para saber qué deshacer y lógica dispersa en varias funciones.
3. Con `WaitForExternalEvent("AprobacionManager")` esperando el evento + `CreateTimer(72 horas)` para el timeout + `Task.WhenAny` para que gane lo que llegue primero. Durante esas horas el orquestador **no consume compute** — su estado está guardado en Storage Tables, no hay proceso "esperando". Cuando alguien llama a `RaiseEventAsync(instanceId, "AprobacionManager", true)` (vía HTTP starter), Durable carga el orquestador, le da el evento, y sigue desde el `await`. Si pasan 72 horas sin evento, el timer dispara `Task.WhenAny`, ganas la rama del timeout y compensas. Es la diferencia entre "función esperando consumiendo recursos" y "estado persistido sin coste".
4. **En la activity, nunca en el orquestador**. Llamadas HTTP son I/O no determinista — dos invocaciones pueden dar respuestas distintas (timeout, status code, payload). En el orquestador rompería el replay. La activity vive en una invocación distinta de Functions, hace la llamada HTTP, devuelve el resultado, Durable guarda ese resultado en el historial. En replays siguientes, el orquestador lee el resultado del historial sin volver a llamar a la API. Esa garantía es lo que hace Durable resiliente a reinicios — la API externa no se vuelve a invocar tras un crash, solo se lee del historial.
5. **Configurar `ctx.CreateReplaySafeLogger<T>().Returns(NullLogger<T>.Instance)`** en el setup del test. NSubstitute por defecto hace que los métodos del mock devuelvan `null` para tipos de referencia. Cuando el orquestador hace `var logger = context.CreateReplaySafeLogger<T>();` y luego `logger.LogInformation(...)`, el logger es null y peta. La solución es configurar el método para que devuelva un `NullLogger<T>.Instance` o un `Substitute.For<ILogger<T>>()`. Es uno de los tres trucos del HANDOFF para tests con NSubstitute sobre `TaskOrchestrationContext`.
6. **Chunks de 50** (o el número que tu plan pueda manejar), no las 500 a la vez. Lanzar 500 `CallActivityAsync` en paralelo con `Task.WhenAll` saturaría el plan Consumption — Functions intentaría escalar a 500 instancias casi al instante, golpeando límites de scaling, throttling de Storage, y eventualmente fallos de las activities. Con chunks de 50, divides en N batches, cada batch corre en paralelo, entre batches va secuencial. El total dura un poco más pero es estable y controlado. La regla práctica: **piensa en el límite de paralelismo que tu sistema downstream puede tolerar** (BD, APIs externas, throttling de servicios) y dimensiona el chunk a ese límite.

</details>

---

## 15. Hasta aquí

Vuelve a la imagen del director de orquesta de la sección 4. Sin tocar ningún instrumento, coordina la música entera. La partitura no permite azar — si lo permitiera, el director sustituto al cambio de turno no podría continuar. Esa regla del determinismo es **la** regla de Durable: respétala y el modelo es elegante; viólala y el sistema funciona en demo y rompe en producción.

Lo siguiente es [`S4.3 — Errores, reintentos y dead-letter`](../S4.3-errores-reintentos-deadletter/MANUAL.md), que profundiza en el manejo de fallos transversal — políticas de retry, circuit breaker, DLQ y observabilidad de fallos. Es aplicable tanto a los triggers de M03 como a las sagas de este submódulo. Después S4.4 cubre deploy y versionado, y S4.5 cierra con testing avanzado e integration tests — incluyendo el patrón explícito para cubrir la "lección DI" del HANDOFF que aparece en todo M03/M04.
