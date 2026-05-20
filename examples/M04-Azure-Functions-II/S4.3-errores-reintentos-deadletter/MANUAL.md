# Manual del alumno — S4.3 · Errores, reintentos y dead-letter

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, despliegue por Portal, scripts. Este manual va antes: te cuenta por qué no todos los errores se tratan igual, qué decisión hay detrás de cada `Complete/Abandon/DeadLetter`, y por qué la pieza más valiosa del ejemplo es una clase de 30 líneas (`ErrorClassifier`) que no toca Azure.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M04-S4.3](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.3-errores-reintentos-deadletter-v4.md). Cinco rutas dentro de una sola función `ProcesarPedido` más un `ProcesarDeadLetter` que vacía la DLQ con criterio.

*Creado: 2026-05-20 14:30 +0200*

---

## 1. La idea en una frase

Reintentar un JSON malformado es malgastar reintentos: nunca va a parsear bien. Mandar a dead-letter un timeout de red es perder trabajo que pudo haber salido a la segunda. **La estrategia correcta no es "reintentar todo" ni "rendirse rápido": es clasificar el error y actuar según su naturaleza.** Lo transitorio se reintenta con backoff; lo permanente va directo a dead-letter; lo desconocido se loguea como crítico y se reintenta por si acaso.

El ejemplo materializa esa idea con tres piezas que se prueban por separado: el clasificador de errores (`IErrorClassifier`), el cliente HTTP resiliente con Polly (`IResilientApiClient`) y el procesador de mensajes envenenados (`ProcesarDeadLetterFunction`). Las tres son lógica pura — los tests no tocan Azure ni Service Bus real.

---

## 2. El problema real que hay detrás

Un equipo de pagos tenía una función de procesamiento de pedidos que en producción se comportaba mal. La cola crecía sin razón aparente, la dead-letter queue se llenaba con mensajes que sí podrían haberse procesado, y de vez en cuando un mismo pedido se cobraba dos veces. Tres síntomas distintos, una sola raíz: la función trataba todos los errores como iguales.

Cuando un mensaje fallaba, hacían `throw` y dejaban que Service Bus reintentara hasta `maxDeliveryCount = 10`. Un JSON corrupto se reintentaba diez veces antes de morir — minutos perdidos por basura. Un timeout de la pasarela bancaria se reintentaba diez veces en un minuto y el banco les baneaba la IP, así que cuando volvía a estar bien el siguiente bloque de mensajes también fallaba. Y para colmo, como no había idempotencia, cuando Service Bus reentregaba un mensaje "por si acaso" (at-least-once), un pago ya cobrado se cobraba otra vez.

La reescritura aplicó tres cambios:

1. **Clasificar antes de decidir**: una clase de 30 líneas que mira el tipo de excepción y devuelve `Transitorio`, `Permanente` o `Desconocido`. Cada tipo dispara una acción distinta.
2. **Idempotencia explícita**: registrar el id del pedido tras procesarlo. Si vuelve a llegar, `Complete` sin tocar la pasarela bancaria. Cuesta una línea (`_idempotencia.YaProcesado(pedido.Id)`) y elimina los cobros duplicados.
3. **Circuit breaker delante de la pasarela**: cuando el banco lleva varios fallos seguidos, Polly abre el circuito y las siguientes llamadas fallan en microsegundos. Service Bus reintenta más tarde, cuando el circuito reabre. Se acabó el baneo por inundación.

Tras el cambio, la DLQ pasó de llenarse de mensajes legítimos a tener solo basura real (JSON corruptos, pedidos sin `id`). Eso es lo que cubre este ejemplo.

---

## 3. Por qué esto importa en tu stack

Cualquier consumidor de mensajería con efectos externos —pago, envío de correo, llamada a un servicio de terceros, escritura en una BD ajena— vive con la misma pregunta: "¿qué hago cuando el efecto falla?". Lo intuitivo es reintentar. Lo correcto es preguntarse primero "¿de qué naturaleza es el fallo?" y actuar en consecuencia.

Tres reglas prácticas:

- **Lo que nunca va a funcionar a la segunda no se reintenta**: JSON malformado, validación que falla por un campo obligatorio ausente, regla de negocio violada, 404 en un recurso que no existe. Eso va a dead-letter inmediato. Reintentar sería tirar de cuota.
- **Lo que pudo haber sido un pico se reintenta**: timeout, 429, 502/503/504, error de conexión. Pero con backoff exponencial y jitter — si 100 instancias reintentan en el mismo segundo, el servicio que se estaba recuperando se cae otra vez.
- **Lo desconocido se trata como transitorio pero se loguea como crítico**: si no sabes qué es, no descartes — pero alerta para que alguien lo investigue y mañana sí esté catalogado.

Si tu sistema solo tiene "intenta diez veces y rendíate", probablemente estás haciendo lo correcto el 70% del tiempo y mal el 30% restante. Este patrón te lleva al 95%.

---

## 4. La analogía vertebradora: el triaje de urgencias

Imagina la sala de triaje de un hospital. Llega gente continuamente, y un enfermero tiene treinta segundos para clasificar a cada paciente:

- **Código verde**: catarro, contusión leve, dolor menor. *Vuelva mañana al médico de familia.* En nuestra función, eso es un `Complete` exitoso: el trabajo se hizo, se acabó.
- **Código amarillo**: dolor agudo pero estable, fiebre alta sin signos de alarma. *Espere en observación, le veremos en cuanto haya un médico libre.* Es el `Abandon` para un error transitorio — el mensaje vuelve a la cola, Service Bus lo reintentará en unos segundos con backoff exponencial.
- **Código rojo**: traumatismo grave, parada cardíaca. *UCI directamente, ahora.* Es el `DeadLetter` para un error permanente — no tiene sentido tenerlo esperando en la sala general; va a una unidad dedicada (la dead-letter queue) donde otro equipo decidirá qué hacer.
- **Código negro**: pacientes que no sobreviven. Pasan a la unidad forense. Es la `ProcesarDeadLetterFunction` — un equipo separado que procesa lo que llegó a la DLQ y decide si descartar, poner en cuarentena o reencolar con criterio.

La función central del ejemplo es el enfermero de triaje. Hace lo mismo en su `switch`:

```csharp
switch (tipo)
{
    case TipoError.Transitorio: await actions.AbandonMessageAsync(mensaje); break;   // amarillo
    case TipoError.Permanente:  await actions.DeadLetterMessageAsync(...);   break;  // rojo
    default:                    await actions.AbandonMessageAsync(mensaje); break;   // desconocido → amarillo cauteloso
}
```

Y antes de clasificar, hace dos chequeos rápidos que te ahorran trabajo:

- Si el paciente trae **historial de hoy** ("este id ya lo procesé"), no le vuelvas a hacer pruebas: `Complete` y a casa. Eso es la idempotencia.
- Si el paciente llega con **el informe ilegible** (JSON malformado), no le mandes a observación: nunca va a tener un diagnóstico válido. Directo a la unidad forense. Eso es el dead-letter inmediato por parseo.

Mantén esta imagen mientras lees el resto. Toda la complejidad del ejemplo —Polly, circuit breaker, idempotency store, dos funciones distintas— sirve a una sola pregunta: ¿qué código de triaje pongo a este mensaje?

---

## 5. Recorrido por el código

### El clasificador de errores (`ErrorClassifier.cs`)

Es la pieza más simple y la más importante. Un `switch` sobre el tipo de excepción:

```csharp
public TipoError Clasificar(Exception ex) => ex switch
{
    JsonException             => TipoError.Permanente,   // payload roto
    ArgumentException         => TipoError.Permanente,   // validación
    InvalidOperationException => TipoError.Permanente,   // regla de negocio
    TimeoutException          => TipoError.Transitorio,  // pico de latencia
    TaskCanceledException     => TipoError.Transitorio,  // timeout de HttpClient
    HttpRequestException http when EsTransitorioHttp(http) => TipoError.Transitorio,
    CircuitoAbiertoException  => TipoError.Transitorio,  // el circuito reabrirá
    _ => TipoError.Desconocido,
};
```

`EsTransitorioHttp` mira el `StatusCode` y devuelve true para 408, 429, 502, 503, 504 — y también para `null`, que en `HttpRequestException` significa que ni siquiera llegó a haber respuesta (fallo de conexión, DNS, TLS). Todos esos son cosas que el siguiente intento podría resolver.

Lo que NO ves aquí: ningún `try/catch`, ningún acceso a Service Bus, ninguna dependencia de Azure. Es una función pura, `Exception -> TipoError`. Y por eso se prueba con ocho tests `[Theory]` que cubren cada rama en milisegundos.

Si mañana descubres que un determinado mensaje de error de tu proveedor también es transitorio, añades una rama al `switch` y un test. Cinco minutos. Cero despliegues para validar la idea.

### La función principal (`ProcesarPedidoFunction.cs`)

La estructura del método sigue exactamente al triaje:

1. **Parseo**. Si `JsonSerializer.Deserialize` lanza `JsonException`, vamos directos a dead-letter con `deadLetterReason: "MalformedJson"`. Nunca va a parsear bien, no malgastes reintentos.
2. **Validación mínima**. Si falta el `id`, dead-letter con `deadLetterReason: "BusinessRule"`. Mismo argumento.
3. **Idempotencia**. `if (_idempotencia.YaProcesado(pedido.Id))` → `Complete` y fuera. Service Bus garantiza at-least-once, así que esto es la diferencia entre cobrar una vez o dos.
4. **Trabajo real**, dentro de `_api.EjecutarAsync(...)` para que Polly se ocupe del retry+circuit breaker.
5. **Si lanza**: `_classifier.Clasificar(ex)` decide el siguiente paso del `switch`.

Una sutileza importante en el orden: registramos en el idempotency store **después** de que el trabajo termine bien. Si registráramos antes y el trabajo fallara, el siguiente intento se saltaría el mensaje creyendo que ya estaba hecho — y no estaría hecho.

### El cliente resiliente con Polly (`PollyResilientApiClient.cs`)

Dos estrategias en una `ResiliencePipeline`:

- **Retry**: hasta 3 intentos, backoff exponencial con jitter (`UseJitter = true`). El jitter no es opcional: si 100 funciones fallan a la misma hora y todas reintentan exactamente a los 200ms, 400ms, 800ms, el servicio que se estaba recuperando se hunde de nuevo. El jitter mete una variación aleatoria de hasta el 50% para desincronizar la avalancha.
- **Circuit breaker**: si fallan al menos el 50% de un mínimo de 4 llamadas en una ventana de 10s, abre el circuito durante 5s. Mientras está abierto, todas las llamadas fallan en microsegundos sin tocar el servicio externo.

El orden importa, y aquí está la sutileza: **el retry envuelve al breaker**, no al revés. Así, cuando el breaker está abierto y lanza `BrokenCircuitException`, esa excepción NO entra en la lista de cosas a reintentar — fallaría en otra microsegundo. Cuando ocurre, lo traducimos a `CircuitoAbiertoException` para que el `ErrorClassifier` la marque como `Transitorio` y haga `Abandon`: Service Bus lo reintentará más tarde, cuando el circuito ya haya reabierto.

### El procesador de la DLQ (`ProcesarDeadLetterFunction.cs`)

Esta es la sorpresa pedagógica del submódulo: una `Function` que **triggerea sobre la propia dead-letter queue**. La ruta es la cola principal con el sufijo `/$deadletterqueue`:

```csharp
[ServiceBusTrigger("pedidos-procesar/$deadletterqueue", Connection = "ServiceBusConnection")]
```

Lo que hace es leer cada mensaje muerto, mirar su `DeadLetterReason` y su `DeadLetterErrorDescription`, y decidir con `IPoisonClassifier`:

- `MalformedJson` → `Discard`. Es basura conocida, se descarta con log.
- Descripción que contiene `timeout` → `NotifyAndRetry`. Pudo ser un pico, avisa y reencola con delay.
- `MaxDeliveryCount` → `Quarantine`. Si agotó todos los reintentos del trigger, hay algo persistente mal; humano que mire.
- `BusinessRule` o cualquier otra cosa → `Quarantine`. Por seguridad, no descartar a ciegas.

En todos los casos hace `Complete` del mensaje DLQ. Si no, la DLQ crecería sin fin: el mensaje vuelve a sí misma, se vuelve a procesar, y entras en un bucle. Salir de la DLQ es el invariante absoluto de la función.

---

## 6. Idempotencia: la línea que ahorra el cobro duplicado

Mira esta línea de `InMemoryIdempotencyStore`:

```csharp
public bool TryRegistrar(string clave) => _procesados.TryAdd(clave, 0);
```

Lo importante no es el `ConcurrentDictionary` ni el `TryAdd`. Es entender **por qué `TryAdd` y no `GetOrAdd(clave, _ => ProcesarPedido())`**. La diferencia se ve cuando dos instancias de la función reciben el mismo mensaje a la vez (Service Bus puede entregar el mismo `MessageId` a dos consumidores en ventanas de fallo extraño):

- Con `TryAdd`: ambas llaman a `_procesados.TryAdd("ped-1", 0)`. Exactamente una devuelve true, la otra false. Solo una procesa el pedido. **Esto es lo que quieres.**
- Con `GetOrAdd(clave, _ => ProcesarPedido())`: el `valueFactory` se invoca múltiples veces bajo contención, aunque solo uno gane la inserción. **El cobro se hace dos veces aunque el dictionary muestre una sola entrada.** Es contraintuitivo y está documentado por Microsoft, pero en code review se cuela siempre.

Los tests del repo validan esto con un `Parallel.For(0, 200, ...)` lanzando 200 hilos sobre la misma clave: exactamente 1 gana, los 199 restantes ven `YaProcesado = true`. Si te encuentras alguna vez con un sistema que duplica cobros bajo carga, esta línea es lo primero que debes mirar.

En producción, este `ConcurrentDictionary` sería Cosmos DB, Azure Table Storage o Redis con TTL (el dictionary en memoria se pierde al reiniciar). Pero la lógica de "TryAdd atómico, no GetOrAdd con factory" es la misma sea cual sea el backend.

---

## 7. La pista falsa del slide 5

El submódulo presenta el atributo `[ExponentialBackoffRetry]` como mecanismo de reintento por función. Lo que descubres al codificarlo es que **sobre un `ServiceBusTrigger` no compila** en el isolated worker: el analyzer `AZFW0012` lo rechaza con un error claro.

La razón: ese atributo solo tiene sentido sobre triggers que **no traen retry propio** (Timer, Event Hub). Service Bus ya tiene su propio mecanismo —el `maxDeliveryCount` de la cola más el `Abandon` explícito que hace tu código— y combinarlos llevaría a un comportamiento confuso (¿qué retry gana?, ¿cuál cuenta hacia el dead-letter?, ¿cuál ignora a cuál?).

Si estás trabajando con un alumno que viene del modelo in-process, este es el momento de decirle: "Sí, lo del slide existe, pero no para Service Bus. Mira el `host.json` y el `maxDeliveryCount` de la cola: ese es tu retry. Y el `Abandon` que pones tú es lo que cuenta hacia ese contador". Está documentado en el comentario del código y en el README; tiene su sitio aquí por si alguien intenta replicar el slide al pie de la letra y no entiende por qué no compila.

---

## 8. Cómo probarlo en local

El ejemplo es de los que **sí necesita Azure** para verse de verdad: el flujo de dead-letter exige una cola Service Bus real con dead-lettering habilitado, y el emulador local no lo soporta tan bien como el servicio. Pero los tests cubren toda la lógica sin Azure, así que la mecánica diaria es:

1. Editar la clasificación o la función.
2. `dotnet test` — 44 tests verdes incluyen los siete del `switch` completo de la función principal.
3. Desplegar a Azure solo para validar el camino end-to-end.

Para reproducir end-to-end (Portal):

- Resource Group `rg-curso-m04-s43`.
- Storage Account para el runtime.
- **Service Bus tier Standard** (no Basic — Basic no soporta topics ni dead-lettering por suscripción). Cuesta unos 10 €/mes; bórralo al acabar.
- Cola `pedidos-procesar` con `max-delivery-count = 5` y `Enable dead lettering` activo.
- Function App con `ServiceBusConnection` apuntando al namespace.

Una vez desplegado, abre el **Service Bus Explorer** en el Portal y manda tres mensajes:

```jsonc
// OK
{"id":"ped-1","clienteId":"c","clienteEmail":"a@b.c","total":100}

// Duplicado: el MISMO mensaje otra vez
{"id":"ped-1","clienteId":"c","clienteEmail":"a@b.c","total":100}

// JSON corrupto
{ broken json
```

Espera diez segundos y consulta `/api/estado`. Verás `procesados=1`, `duplicadosSaltados=1`, `enviadosADeadLetter=1`, `poisonProcesados=1`. El último contador confirma que el `ProcesarDeadLetterFunction` se ha activado solo y ha drenado la DLQ.

Si quieres ver el caso del circuit breaker abierto, hay que tirar de los tests: `PollyResilientApiClientTests.CircuitBreaker_LanzaCircuitoAbiertoException_TrasFallosSostenidos` lo ejecuta en milisegundos sin tocar Azure.

> Yo no lanzo apps. Tú haces `func start --csharp` y `dotnet test`.

---

## 9. Los tests son la documentación viva

Las 44 pruebas son la mejor especificación de "qué hace el sistema cuando le pasa X". Sin Azure, sin Service Bus, sin emuladores. Tres patrones que conviene reconocer:

**Patrón 1 — Tabla de clasificación con `[Theory]`.** Cada `[InlineData]` es una pareja `Exception -> TipoError` esperada. Cuando alguien añade un tipo de error nuevo, añade una fila. La cobertura de `ErrorClassifier` está en la tabla.

**Patrón 2 — `FakeServiceBusMessageActions` para `Complete/Abandon/DeadLetter`.** No hay Service Bus real; hay un doble que registra qué acción se invocó y con qué argumentos. Cuando un test dice "el JSON malformado debe ir a dead-letter con motivo `MalformedJson`", lo que comprueba es que el fake recibió ese argumento. Es así de directo.

**Patrón 3 — Concurrencia con `Parallel.For`.** Para validar el `TryAdd` de idempotencia: 200 hilos sobre la misma clave, exactamente 1 gana. Si alguien refactoriza el store y rompe la atomicidad, este test salta. Es la red de seguridad de la línea más importante del sistema.

Un detalle de carpintería que descubrí escribiendo los tests y que conviene tener apuntado: `ServiceBusModelFactory.ServiceBusReceivedMessage(...)` **no** tiene parámetros `deadLetterReason` ni `deadLetterErrorDescription`. Para simular un mensaje que ya está en la DLQ, las propiedades `DeadLetterReason` y `DeadLetterErrorDescription` se leen de `ApplicationProperties` con claves bien conocidas (`"DeadLetterReason"`, `"DeadLetterErrorDescription"`). Así que en los tests se pasan por el diccionario `properties:` del factory. Si esto no estuviera documentado, lo descubrirías a base de probar.

Y otro más: `Activator.CreateInstance(tipo, "mensaje")` **no** honra parámetros opcionales del constructor. Las excepciones custom con firma `(string mensaje, Exception? inner = null)` fallan con `MissingMethodException` si intentas instanciarlas así. Los `[Theory]` usan `TheoryData<Exception>` con instancias construidas explícitamente (`new ErrorTransitorioException("x")`), no reflection.

---

## 10. La frontera con S4.2

S4.2 (Durable) y S4.3 (errores) resuelven cosas distintas que la gente confunde al principio:

- **S4.2** se ocupa de la **coordinación de pasos** con estado. El error de un paso se gestiona dentro del orquestador con `try/catch` y compensación. Si un pago falla, ejecutas `LiberarReserva` y `CancelarPedido`. La saga es sobre el flujo.
- **S4.3** se ocupa de la **resiliencia de un consumidor de cola**. No hay flujo de múltiples pasos: hay un mensaje, una acción, y la decisión de qué hacer con el fallo. La estrategia de errores es sobre el mensaje individual.

Las dos cosas conviven. Las activities de un orquestador Durable también pueden fallar con errores transitorios, y aplicarles un `RetryOptions` desde el orquestador es la versión Durable de lo que aquí hace Polly. La diferencia operativa: en Durable la retry policy la decides desde el orquestador (`RetryOptions`); en un trigger normal la decides tú con clasificación + `Abandon/DeadLetter`. Mismo principio, distinto vehículo.

Si te toca diseñar un sistema desde cero, pregúntate qué tienes: ¿pasos con estado y duración larga? Durable con compensación. ¿Un consumer simple que llama a un servicio externo? Clasificador + Polly + idempotency, como aquí.

---

## 11. La trampa de los efectos no idempotentes

El ejemplo lo deja en blanco a propósito: el `EjecutarAsync` del cliente resiliente simula éxito sin llamar a nada real. En producción ahí dentro hay un cobro, un envío de email, una inserción en BD. Y cada uno tiene una pregunta que decide cuándo retirar el `Abandon`:

- **¿Es idempotente la llamada externa?** Si envías el mismo cobro dos veces y la pasarela tiene su propio idempotency key, eres feliz. Si no, tienes que mantener tu propio registro y verificarlo antes de cada intento — la idempotency store del ejemplo es justo eso.
- **¿En qué punto del trabajo está el efecto?** Si el cobro va al principio y el envío de email al final, un fallo después del cobro pero antes del email te deja con dinero cobrado y cliente sin notificar. La solución honesta es **outbox pattern** (slide 17 del submódulo, deliberadamente fuera de alcance en este ejemplo): la inserción en BD y la cola de "enviar email" comparten transacción, así que o ambas pasan o ninguna.

El ejemplo cubre la mitad del problema —cuándo reintentar, cuándo no, cómo no duplicar— y deja la otra mitad —cómo garantizar atomicidad transaccional entre el trabajo y la mensajería— para una conversación posterior que requiere BD. Cuando llegues a producción, tendrás que cerrar ese segundo frente.

---

## 12. Glosario breve

- **Dead-letter queue (DLQ)**: cola hermana de cada cola/suscripción de Service Bus donde acaban los mensajes que el consumidor marcó como `DeadLetter` o que agotaron su `maxDeliveryCount`. Se accede con la ruta `cola/$deadletterqueue`.
- **At-least-once**: garantía de que un mensaje se entrega al menos una vez, pero **puede entregarse más veces** ante fallos. Por eso la idempotencia no es opcional.
- **Peek-lock**: modo de consumo de Service Bus en el que el mensaje queda bloqueado mientras lo procesas; lo confirmas con `Complete`, lo devuelves con `Abandon`, o lo descartas con `DeadLetter`. Es lo que usa `ServiceBusTrigger` por defecto.
- **Backoff exponencial**: estrategia de retry donde el tiempo entre intentos crece (200ms, 400ms, 800ms...) en lugar de ser constante. Evita machacar a un servicio que está en problemas.
- **Jitter**: variación aleatoria sobre el backoff. Sin él, todas las instancias reintentan a los mismos milisegundos y el servicio se hunde de nuevo en el pico.
- **Circuit breaker**: patrón que "abre" tras varios fallos, falla rápido durante un tiempo de descanso y reabre intentando. Protege al servicio externo y a tu propio coste de retry.
- **Idempotency store**: registro persistente de "qué he procesado ya". Convierte un consumer at-least-once en exactly-once efectivo desde la perspectiva del efecto.

---

## 13. Para ir más allá del ejemplo

Una vez tengas el flujo funcionando, hay tres frentes que el ejemplo deja abiertos a propósito:

- **Alertas de Application Insights** (slide 12, documentadas y no ejecutadas). Una métrica como `customMetrics/dlqCount` con un umbral de "si pasa de 10 en 5 min, llama a oncall". Es trivial técnicamente y crítica operativamente.
- **Persistir el idempotency store** en Cosmos DB con TTL de 24h, en vez de en memoria. Si la function reinicia, el dictionary se pierde y al siguiente mensaje el sistema "olvida" que ya lo procesó.
- **Outbox pattern para el trabajo transaccional**, mencionado arriba. Solo cuando tengas un caso real con BD donde la atomicidad entre escritura y mensajería importe.

Los tres son ejercicios honestos para subir el nivel del proyecto cuando ya entiendas a fondo el clasificador, la idempotency store y el procesador de DLQ.

---

## 14. Cierre

Si te quedas con una sola cosa de este submódulo, que sea la línea diagonal de la `ProcesarPedidoFunction`:

```csharp
var tipo = _classifier.Clasificar(ex);
switch (tipo) { ... }
```

Esas dos líneas son la diferencia entre un consumer que "intenta diez veces y rinde" y uno que sabe qué hacer con cada fallo. El resto del ejemplo —Polly, idempotency store, procesador de DLQ— sirve a ese momento. Mantén pequeño el clasificador, llénalo de tests, y cuando descubras una excepción nueva, añade su rama y un test. Eso es todo.

Lo siguiente es [`S4.4 — Despliegue y versionado`](../S4.4-despliegue-versionado/MANUAL.md), donde se ven los slots de Functions, el versionado de la API y la mecánica de blue/green que te permite desplegar este sistema sin perder mensajes en vuelo.
