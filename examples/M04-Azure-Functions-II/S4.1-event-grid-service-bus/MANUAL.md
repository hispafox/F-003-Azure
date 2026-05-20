# Manual del alumno — S4.1 · Integración con Event Grid y Service Bus

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: comandos por Portal, scripts `az`, lista exacta de App Settings y suscripciones de Event Grid. Este manual va antes: te cuenta por qué este es el primer ejemplo "serio" del módulo, el cambio mental respecto a M03 y la decisión que más importa — Event Grid, Service Bus Queue o Service Bus Topic.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M04-S4.1](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.1-event-grid-service-bus-v4.md). Pasamos de **triggers aislados** (M03) a un **sistema asíncrono real**: HTTP responde 202, el trabajo viaja por Queue para un consumidor exclusivo y por Topic para fan-out a varios suscriptores. Event Grid orquesta el clasificador de archivos con peek-lock real.

*Creado: 2026-05-20 13:07 +0200*

---

## 1. La idea en una frase

En M03 las funciones eran reactivas pero **aisladas**: cada una respondía a su evento sin coordinar con las demás. En M04 las conectas. El patrón cambia: un endpoint HTTP ya no responde con el resultado del procesado, responde con **202 Accepted** y mete el trabajo en una cola. Otro proceso lo recoge, lo procesa con su propio ritmo, gestiona retries y dead-letter. Y para notificar a sistemas que están escuchando — sin acoplarse a ellos — usas Event Grid o Service Bus Topic.

Esa separación entre **"recibir la petición"** y **"hacer el trabajo"** es lo que permite que tu API sea rápida (responde en milisegundos), resiliente (si el procesado falla, el mensaje se reintenta) y escalable (puedes paralelizar consumidores). Es el patrón estándar de cualquier sistema serio con tráfico real, y este ejemplo te lo enseña con el dominio de pedidos que arrastras desde M03.

> ⚠️ **Coste primero**: este es el **primer ejemplo del curso con tarifa fija mensual**. Service Bus Standard cuesta ~10 €/mes aunque no envíes mensajes — no hay versión gratuita real. El coste prorrateado de una demo de un día es ~0,30 €, pero hay que recordar borrar el RG al acabar.

---

## 2. El problema real que hay detrás

Una pequeña tienda online tenía un endpoint `POST /api/pedidos` que hacía cuatro cosas síncronamente: guardar el pedido en la BD, mandar un email de confirmación al cliente, generar la factura PDF y notificar al almacén. Todo dentro de la misma petición HTTP. Tres consecuencias previsibles:

- **Latencia**: el cliente esperaba 4-8 segundos a recibir el `200 OK`. Si el servicio de email tenía un blip, los 8 segundos eran 30.
- **Fragilidad**: si la BD aceptaba el pedido pero el email fallaba, el handler lanzaba excepción y el cliente recibía un `500`. El pedido **sí estaba guardado**, pero el cliente pensaba que no.
- **Acoplamiento**: añadir un nuevo paso (notificar a Slack, mandar a analytics) requería modificar el handler. Cada cambio era riesgo para los demás pasos.

La refactorización con mensajería: el handler hace una cosa — encolar un mensaje en Service Bus Queue. Devuelve `202 Accepted` en milisegundos. Un consumidor de cola procesa los pasos pesados con su propia velocidad, retries, dead-letter. Y para los pasos que son "notificación a terceros" (Slack, analytics), un Service Bus Topic los publica una vez y N suscriptores reaccionan. Cliente feliz (rápido), sistema resiliente (retries), arquitectura desacoplada (nuevo consumidor = nueva subscription).

Esa transición es lo que entrena el ejemplo. Tres patrones de mensajería en un solo proyecto:

| Pieza | Para qué | Dónde |
| --- | --- | --- |
| **`CrearPedidoFunction`** | HTTP → SB Queue + SB Topic con MultiResponse (202 + outputs nullables) | [`CrearPedidoFunction.cs`](src/AzureFunctions.Demo/Functions/CrearPedidoFunction.cs) |
| **`ProcesarPedidoFunction`** | SB Queue trigger con peek-lock real: Complete / Abandon / DeadLetter | [`ProcesarPedidoFunction.cs`](src/AzureFunctions.Demo/Functions/ProcesarPedidoFunction.cs) |
| **`NotificarPedidoCreadoFunction`** | SB Topic + Subscription (fan-out) | [`NotificarPedidoCreadoFunction.cs`](src/AzureFunctions.Demo/Functions/NotificarPedidoCreadoFunction.cs) |
| **`ClasificarArchivoFunction`** | Event Grid sobre `BlobCreated` → fan-out a queues distintas según extensión | [`ClasificarArchivoFunction.cs`](src/AzureFunctions.Demo/Functions/ClasificarArchivoFunction.cs) |
| **`/api/estado`** | Inspección consolidada (counters de los cuatro flujos) | [`EstadoFunction.cs`](src/AzureFunctions.Demo/Functions/EstadoFunction.cs) |

---

## 3. Por qué esto importa en tu stack

Cuando una API hace algo más que "leer un dato y devolverlo", la pregunta correcta deja de ser "¿este endpoint funciona?" y pasa a ser "**¿qué pasa si una de las dependencias está lenta o caída?**". Hay tres respuestas posibles:

- **Síncrono y bloqueante** (lo que tenía la tienda al principio): el endpoint espera a que todo termine. Latencia alta, fragilidad alta, acoplamiento alto. Funciona en sistemas pequeños sin tráfico real.
- **Asíncrono con cola** (lo que enseña este ejemplo con Service Bus Queue): el endpoint encola y se va. El consumidor procesa con su ritmo. Si el procesado falla, el mensaje vuelve a la cola y se reintenta. Tras N reintentos, dead-letter.
- **Asíncrono con eventos** (Event Grid o SB Topic): el sistema publica "pasó X" y todos los interesados reaccionan independientemente. Cada uno con su propio retry y aislamiento de fallos.

La elección entre Service Bus Queue, Service Bus Topic y Event Grid es **la decisión de arquitectura más importante de este módulo**. Tiene una regla mental que conviene memorizar (sección 5).

---

## 4. El modelo mental: la oficina postal con apartado

Imagina un edificio con tres formas distintas de hacer llegar comunicación.

**Servicio postal con apartado individual.** Tú llevas la carta al apartado del destinatario. Él la recoge cuando puede. Si está de vacaciones, la carta se queda esperando. Si nadie la recoge en 30 días, va a la oficina central de no entregadas. **Eso es Service Bus Queue**: un destinatario, peek-lock, dead-letter automático tras N intentos.

**Boletín de la asociación de vecinos.** Tú escribes el boletín una vez y se reparte a todas las suscripciones activas. Cada vecino lo recibe en su propio apartado. Si añades un vecino nuevo a la lista, recibe los próximos boletines sin que tengas que cambiar nada. Si un vecino se da de baja, deja de recibir sin que el resto se entere. **Eso es Service Bus Topic + Subscriptions**: N destinatarios, fan-out controlado, cada uno con su propio buzón y filtros.

**Sistema de megafonía del barrio.** El ayuntamiento anuncia "ha llegado el camión de la basura". Quien lo oye decide qué hacer — sacar la bolsa, ignorar, llamar al vecino. No hay buzón, no hay destinatario explícito, no hay garantía de que todos lo escuchen exactamente igual. **Eso es Event Grid**: publicación de eventos, suscriptores se enganchan según les interese, modelo "fire and forget" con retry pero sin peek-lock.

```
                 POST /api/pedidos
                       │
                       ▼
          ┌─────────────────────────┐
          │ CrearPedidoFunction     │
          │ MultiResponse:          │
          │   202 Accepted          │
          │   + SB Queue            │
          │   + SB Topic            │
          └────┬───────────────┬────┘
               │               │
   ┌───────────┴───┐       ┌───┴──────────────────┐
   │ SB Queue       │      │ SB Topic              │
   │ "pedidos-      │      │ "pedidos-eventos"     │
   │  procesar"     │      │   ├── sub-notif       │
   │                │      │   └── (futuras subs)  │
   └────┬───────────┘      └────┬──────────────────┘
        │                       │
        ▼                       ▼
  ProcesarPedido          NotificarPedidoCreado
  (peek-lock real)        (handler por sub)


  Blob ".pdf"|".csv" en uploads/
        │
        ▼  (Event Grid → BlobCreated)
   ClasificarArchivoFunction
        ├── pdf → SB Queue "facturas-procesar"
        └── csv → SB Queue "imports-procesar"
```

Tres frases para fijar el modelo:

- **Service Bus Queue es para "un destinatario específico hará el trabajo"**. Pedido → procesado por **una** función que descuenta stock y envía email. El mensaje se entrega exactamente a un consumidor.
- **Service Bus Topic es para "varios suscriptores reaccionarán de forma independiente"**. Pedido creado → suscripción de email + suscripción de analytics + suscripción de warehouse. Cada una procesa su copia, falla independientemente, retry independiente.
- **Event Grid es para "publico que pasó algo en mi sistema"**, sin acoplarme a quién lo consumirá. Storage publica "BlobCreated" y tu función se entera porque suscribió a ese evento. Si mañana añades otra función que también reaccione, suscribes y listo. El productor (Storage) no sabe ni quién consume.

---

## 5. La decisión: ¿Queue, Topic o Event Grid?

La regla mental que separa los tres:

| Pregunta | Respuesta correcta |
| --- | --- |
| ¿Quiero **encolar trabajo** para un consumidor específico? | Service Bus **Queue** |
| ¿Quiero **notificar** a múltiples consumidores que harán cosas distintas? | Service Bus **Topic** + Subscriptions |
| ¿Quiero **publicar eventos del sistema** (un blob se creó, un usuario se registró) que cualquiera puede consumir? | **Event Grid** |

Diferencias operativas que conviene tener claras:

| | Service Bus Queue/Topic | Event Grid |
| --- | --- | --- |
| **Modelo** | Mensajería con peek-lock | Eventos fire-and-forget con retry |
| **Garantías** | At-least-once con DLQ automática tras N intentos | At-least-once con retry exponencial 24h |
| **Tamaño del mensaje** | Hasta 256 KB (Standard) | Hasta 1 MB con CloudEvents v1.0 |
| **Filtros** | SQL filters en subscripciones | Filtros por subject prefix/suffix, advanced filters |
| **Coste base** | **~10 €/mes** Service Bus Standard | **Scale-to-zero** (~0,60 €/M eventos) |
| **Latencia típica** | ~50-100ms | ~500ms-2s |
| **Cuándo usar** | Procesado de trabajo crítico, FIFO, sessions, transacciones | Reacción a cambios de plataforma (Storage, Resource Manager, etc.) |

Para tu próximo proyecto: si vas a encolar trabajo con garantías estrictas (cada mensaje se procesa **exactamente cuando tu consumidor está listo** y los fallos van a DLQ para revisión humana), Service Bus. Si vas a reaccionar a eventos de plataforma o publicar eventos "sucedió X" donde la entrega "buen esfuerzo" basta, Event Grid.

> 🧠 **El error más común**: usar Event Grid para encolar trabajo "porque es scale-to-zero". Funciona en demo, pero el día que tu consumidor cae durante una hora, Event Grid sigue intentando entregar y al cabo de 24h declara el evento perdido — sin dead-letter por defecto, sin peek-lock manual. Service Bus está diseñado para "trabajo que tiene que completarse"; Event Grid para "evento que se publica". No los intercambies.

---

## 6. El peek-lock real en `ProcesarPedidoFunction`

Mira `ProcesarPedidoFunction.cs`. El trigger es el que se ve en muchos tutoriales pero con un detalle importante:

```csharp
[Function(nameof(ProcesarPedido))]
public async Task ProcesarAsync(
    [ServiceBusTrigger("pedidos-procesar", Connection = "ServiceBusConnection")]
    ServiceBusReceivedMessage message,
    ServiceBusMessageActions actions,
    FunctionContext context,
    CancellationToken ct)
{
    // ... validar el mensaje ...
    if (esMalformado)
    {
        await actions.DeadLetterMessageAsync(message,
            deadLetterReason: "MalformedJson",
            deadLetterErrorDescription: ex.Message);
        return;
    }
    if (esTransitorio)
    {
        await actions.AbandonMessageAsync(message);
        return;
    }
    // ... procesar ...
    await actions.CompleteMessageAsync(message);
}
```

El parámetro `ServiceBusMessageActions` es la clave. En lugar del modelo simplificado "el método retorna sin excepción = ack automático", aquí **declaras explícitamente** qué hacer con cada mensaje:

- **`CompleteMessageAsync`**: el procesado fue OK. El mensaje sale de la cola, no se vuelve a entregar.
- **`AbandonMessageAsync`**: error transitorio (red lenta, BD bajo presión). El mensaje vuelve a la cola y se reintenta. Incrementa `DeliveryCount`.
- **`DeadLetterMessageAsync`**: error permanente (JSON malformado, datos inválidos imposibles de recuperar). El mensaje va a la dead-letter queue para revisión humana, no se vuelve a procesar.

Esa tres distinciones son lo que hace el sistema robusto. Sin ellas, todo error es retry → eventual DLQ tras 10 intentos. Con ellas, los errores permanentes van directo a DLQ desde el primer intento (no consumes 10 ejecuciones reprocesando un mensaje malo), y los transitorios sí se reintentan correctamente.

> 🧠 **La regla práctica**: **`DeadLetterMessageAsync` directo** para errores que no van a desaparecer con retry (JSON corrupto, validación de schema, IDs inexistentes). **`AbandonMessageAsync`** para errores que pueden resolverse al esperar (BD caída, API externa lenta, throttling). **`CompleteMessageAsync`** para el happy path. Distinguir es lo que separa una DLQ con tres mensajes "que merecen atención humana" de una DLQ con cientos de "JSON malformados que reciclaron 10 veces".

---

## 7. El patrón MultiResponse aplicado a 202 + dos outputs

`CrearPedidoFunction` reutiliza el MultiResponse que viste en S3.6 — pero aquí los outputs son a Service Bus:

```csharp
public sealed class CrearPedidoResult
{
    [HttpResult]
    public IActionResult HttpResponse { get; init; } = null!;

    [ServiceBusOutput("pedidos-procesar", Connection = "ServiceBusConnection")]
    public string? MensajeQueue { get; init; }

    [ServiceBusOutput("pedidos-eventos", Connection = "ServiceBusConnection",
        EntityType = ServiceBusEntityType.Topic)]
    public string? EventoTopic { get; init; }
}
```

Tres propiedades, tres efectos. Si la validación falla y dejas `MensajeQueue = null` y `EventoTopic = null`, **ni se encola ni se publica nada**. La fail-safe del null vuelve a jugar. El cliente recibe `400 Bad Request` con detalle del error y el sistema queda limpio (no hay un mensaje huérfano en la cola que ningún consumidor sabe interpretar).

El happy path: validación OK, los tres se llenan. El cliente recibe `202 Accepted` con el `pedidoId`, **el mensaje va a la queue** (procesado), **el evento va al topic** (notificaciones). Y aquí hay un matiz importante: **los dos outputs son independientes**. Si Service Bus acepta el mensaje a la queue pero falla al publicar al topic, te quedas con un pedido encolado para procesar **pero sin notificación**. Functions no hace rollback. Para transaccionalidad estricta entre Cosmos + outputs, el patrón es outbox via Change Feed (S3.5).

> 🧠 **`EntityType.Topic` es la línea que cambia entre Queue y Topic**. La misma sintaxis `[ServiceBusOutput]` cubre los dos modelos — solo el `EntityType` decide a cuál apunta. Es un detalle que cuesta encontrar la primera vez (la documentación no es obvia) pero, una vez visto, simétrico y limpio.

---

## 8. Event Grid + fan-out por extensión

`ClasificarArchivoFunction` es la pieza distinta del módulo: recibe eventos de Event Grid (no de Service Bus) y hace **fan-out** a queues distintas según el tipo de archivo:

```csharp
[Function(nameof(ClasificarArchivo))]
public ClasificarArchivoResult Clasificar(
    [EventGridTrigger] EventGridEvent eventGridEvent,
    ILogger<ClasificarArchivoFunction> logger)
{
    // ... parsear el evento BlobCreated ...
    var extension = Path.GetExtension(blobName).ToLowerInvariant();

    return extension switch
    {
        ".pdf" => new ClasificarArchivoResult { ColaFacturas = blobName },
        ".csv" => new ClasificarArchivoResult { ColaImports = blobName },
        _ => new ClasificarArchivoResult()  // ignora (los dos outputs son null)
    };
}
```

El patrón resultante: cuando alguien sube un blob al container `uploads/`, Storage publica un evento `BlobCreated` a Event Grid, Event Grid llama a esta función, la función mira la extensión y **envía el nombre del blob a una queue distinta** según sea PDF o CSV. Otros consumidores (no en este ejemplo) leerían esas queues para procesar facturas y imports respectivamente.

¿Por qué esto en lugar de "que cada consumidor se suscriba a Event Grid"? Porque el **clasificador** centraliza la lógica del "qué hacer con cada tipo de archivo" en un solo sitio. Si mañana añades un tercer tipo (`.xml` para órdenes), modificas solo la función clasificadora. Sin el clasificador, cada nuevo tipo requeriría modificar la suscripción de Event Grid o cada consumidor tendría que filtrar a mano lo que le interesa.

> 🧠 **El fan-out por código es más flexible que el fan-out por subscription**. Event Grid permite filtros por subject prefix/suffix, pero la lógica "si la extensión es X, ir a la queue A; si es Y, ir a la B" es difícil de expresar con filtros nativos. Un clasificador en código se lee mejor, se testea mejor y se mantiene mejor. Reserva los filtros de Event Grid para casos triviales ("solo eventos del container `uploads/`"); para lógica de routing real, una función clasificadora.

---

## 9. Recorrido guiado

Lanza la app en local (sección 11) y prueba los cuatro caminos. La parte interesante de la mensajería es en Azure (no hay emulador local de Service Bus); en local sólo verás los HTTP triggers.

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `POST /api/pedidos` con body válido | `202 Accepted` con `pedidoId`. En Azure: cola `pedidos-procesar` tiene un mensaje, topic `pedidos-eventos` también | MultiResponse a Queue + Topic (sección 7). |
| 2 | Espera ~30s tras el paso 1 | el mensaje de queue desaparece (consumido por `ProcesarPedido`), la subscripción `sub-notificaciones` también consumió | Peek-lock real con Complete (sección 6). |
| 3 | `GET /api/estado` | counters: `encolados=1, procesados=1, notificaciones=1` | Inspección consolidada. |
| 4 | `POST /api/pedidos` con `total: -5` | `400 Bad Request` con detalle; **ni queue ni topic reciben nada** | Fail-safe del null en MultiResponse. |
| 5 | Manda un JSON malformado directamente a la queue `pedidos-procesar` desde el portal | log "DeadLetter: MalformedJson"; **el mensaje aparece en la DLQ** `pedidos-procesar/$DeadLetterQueue` | DeadLetter directo (no retry inútil). |
| 6 | Sube `factura.pdf` a `uploads/` | Event Grid → log "Clasificado factura.pdf → facturas-procesar" | Event Grid fan-out (sección 8). |
| 7 | Sube `productos.csv` a `uploads/` | Event Grid → log "Clasificado productos.csv → imports-procesar" | El clasificador decide queue por extensión. |
| 8 | Sube `imagen.jpg` a `uploads/` | Event Grid se dispara pero el clasificador ignora (extensión no relevante) | Filtrado en código, los outputs quedan null. |

Un experimento muy útil para entender peek-lock: en el paso 5, **mira la DLQ tras 30 segundos**. El mensaje malformado **está ahí**, no en la cola principal. Si el código hubiera usado `AbandonMessageAsync` en vez de `DeadLetterMessageAsync` directo, el mensaje habría rebotado entre 1-10 veces antes de acabar en DLQ — diez ejecuciones consumidas para nada. Con `DeadLetter` directo, una sola ejecución.

Y otro experimento que vale: para Service Bus desde el portal: namespace → queue `pedidos-procesar` → **Service Bus Explorer** te deja ver mensajes pendientes, peek-lock, completar a mano. Es la herramienta operativa más útil cuando algo va mal en producción.

---

## 10. Tests y la pieza del Fake

32 tests sin tocar SB, Event Grid ni Azure. La pieza didácticamente más valiosa está en `ProcesarPedidoFunctionTests`:

```csharp
public sealed class FakeServiceBusMessageActions : ServiceBusMessageActions
{
    public bool CompleteCalled { get; private set; }
    public bool AbandonCalled { get; private set; }
    public string? DeadLetterReason { get; private set; }

    public override Task CompleteMessageAsync(...) { CompleteCalled = true; return Task.CompletedTask; }
    public override Task AbandonMessageAsync(...) { AbandonCalled = true; return Task.CompletedTask; }
    public override Task DeadLetterMessageAsync(...
        string deadLetterReason, ...)
    {
        DeadLetterReason = deadLetterReason;
        return Task.CompletedTask;
    }
}
```

`ServiceBusMessageActions` es una clase abstracta del binding. Como no se puede mockear con Moq fácilmente (tipos sellados, métodos no virtuales), se hace un `Fake` derivando de la abstracta que captura en propiedades booleanas qué se llamó. Los tests entonces son directos:

```csharp
var actions = new FakeServiceBusMessageActions();
await function.ProcesarAsync(messageMalformado, actions, ctx, ct);
Assert.Equal("MalformedJson", actions.DeadLetterReason);
```

Sin Moq, sin NSubstitute. Más simple, más rápido, más legible. Es el patrón estándar de **fakes manuales** cuando la dependencia es difícil de mockear con frameworks.

Y otra pieza interesante: `ServiceBusModelFactory.ServiceBusReceivedMessage(...)` fabrica mensajes "como si vinieran del wire". Sin abrir conexión, sin Service Bus real. Lo necesitas para pasarle al método un `ServiceBusReceivedMessage` con propiedades específicas (DeliveryCount, MessageId, body).

> 🧠 **Fakes manuales > mocks de framework** cuando la dependencia es una clase abstracta con métodos no virtuales. Moq y NSubstitute tienen limitaciones reales con tipos del SDK de Azure (muchos están sellados o tienen métodos abstractos no fáciles de simular). Un `Fake` derivado de la abstracta es 20 líneas, se entiende mejor y no falla cuando el SDK se actualiza con cambios en los tipos.

---

## 11. Puesta en marcha, ejecución y pruebas

### 11.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y testear | Sí |
| Azure Functions Core Tools | `func start` local | Recomendado |
| Suscripción Azure | desplegar y crear Service Bus Standard | Sí (si vas a desplegar) |
| Azure CLI (`az`) | scripts | Recomendado |

**Importante sobre tests locales**: Service Bus **no tiene emulador**. Los 32 tests no requieren conexión (usan Fakes), pero si quieres probar el flujo end-to-end en local tienes que apuntar a un Service Bus real.

### 11.2 Compilar y arrancar en local

```bash
cd examples/M04-Azure-Functions-II/S4.1-event-grid-service-bus
dotnet build AzureFunctions.Demo.slnx       # 0 errores

cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json
# Edita ServiceBusConnection con el connection string de tu SB Standard

azurite --silent                # otra terminal

cd src/AzureFunctions.Demo
func start
```

En local los HTTP triggers funcionan; los SB y Event Grid triggers solo arrancan si tienes los recursos creados y el connection string apunta a ellos.

### 11.3 Pasar los tests

```bash
dotnet test
```

Resultado: **32 pass · 0 fail**. Sin Azure, sin SB, sin Event Grid.

### 11.4 Desplegar a Azure (resumen)

El detalle por Portal está en el [`README.md`](README.md). Pasos clave:

1. **RG + Storage** (container `uploads/` adicional).
2. **Service Bus Standard** con queues `pedidos-procesar`, `facturas-procesar`, `imports-procesar` (marca "Enable dead lettering") + topic `pedidos-eventos` con subscription `sub-notificaciones`.
3. **Function App** Consumption Linux .NET 10 isolated.
4. **App Setting** `ServiceBusConnection` con la connection string de SB.
5. **Deploy** desde VS Code.
6. **Crear Event Subscription** en el Storage Account: Events → Add → Event Type `Blob Created`, Endpoint `Azure Function → ClasificarArchivo`, Subject Filter `containers/uploads/`.

### 11.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| Mensajes que no se procesan | `ServiceBusConnection` mal configurado o el nombre de queue no coincide | revisa App Settings y nombres exactos (case sensitive) |
| El cliente recibe 202 pero nada llega a queue | el output `MensajeQueue` quedó null por error de validación | revisa el código del handler — el null es deliberado, error si no esperabas |
| Mensajes acumulándose en DLQ | tu código manda a DLQ directo o se queda sin retries | revisa el flujo (sección 6); cada DLQ debería tener alerta operacional |
| Event Grid no dispara la función | falta la Event Subscription, o el filtro de subject no coincide | crear/revisar suscripción en Portal Storage → Events |
| Coste mayor del esperado | dejaste SB Standard varios días | `04-cleanup.sh` o borra el RG; ~10 €/mes prorrateado |

### 11.6 Limpieza

**Importante**: borra el RG en cuanto acabes para evitar el coste fijo de Service Bus. `Portal → Resource groups → rg-curso-m04-s41 → Delete`.

---

## 12. Ideas para llevarte

Lo más útil de esta práctica es **interiorizar el patrón "endpoint rápido + cola + worker"**. Cuando veas un endpoint HTTP que hace varias cosas pesadas síncronas, refactorízalo en dos pasos: el HTTP encola y devuelve 202; un consumidor de cola procesa con su ritmo. Latencia baja del HTTP, resilencia automática (retries + DLQ), arquitectura desacoplada.

Sobre **la decisión Queue vs Topic vs Event Grid**: la regla de la sección 5 cabe en un post-it. Memorízala. El error más común es usar Event Grid para "encolar trabajo" porque parece más barato; el día que tu consumidor cae durante una hora, descubres que Event Grid no tiene peek-lock y los eventos no entregados desaparecen al cabo de 24h.

Sobre **peek-lock**: distinguir Complete/Abandon/DeadLetter desde el primer handler te ahorra el día que la DLQ se llene de "JSON malformados que se reprocesaron 10 veces". `DeadLetterMessageAsync` directo para errores permanentes; `AbandonMessageAsync` para transitorios. La distinción es responsabilidad tuya, no del runtime.

Y sobre el **coste fijo de SB Standard**: ~10 €/mes es razonable para una app pequeña que vale más que eso por ahorrar incidentes operativos. Pero para una demo o un proyecto de juguete, el cleanup disciplinado importa. Configura una alerta de presupuesto en tu suscripción de prácticas que te avise si gastas más de 5 € en un mes.

---

## 13. Comprueba que lo has entendido

1. Tu endpoint hace tres cosas pesadas. ¿Cómo lo refactorizas para que el cliente reciba respuesta en milisegundos sin perder ninguna de las tres operaciones? *(sección 2)*
2. Quieres notificar a tres sistemas distintos que se creó un pedido (email, analytics, warehouse). ¿Service Bus Queue, Service Bus Topic o Event Grid? ¿Por qué? *(sección 5)*
3. Tu consumidor recibe un mensaje con JSON malformado. ¿Qué le haces — Complete, Abandon o DeadLetter? ¿Y si recibe un mensaje válido pero la BD está temporalmente lenta? *(sección 6)*
4. En MultiResponse, la validación falla. Olvidas dejar `MensajeQueue = null`. ¿Qué pasa y por qué es un bug sutil? *(sección 7)*
5. Subes un `.jpg` al container `uploads/`. La `ClasificarArchivoFunction` se dispara pero no encola nada. ¿Por qué es comportamiento correcto y no un bug? *(sección 8)*
6. Tu app usa Event Grid para enviar trabajo a una función procesadora. Tu función está caída durante 2 horas. Cuando vuelve, ¿procesa los eventos perdidos? ¿Qué pasaría con Service Bus Queue en el mismo escenario? *(sección 5)*

<details>
<summary>Respuestas</summary>

1. El endpoint hace **una sola cosa**: encola un mensaje en Service Bus Queue con el payload necesario para el trabajo. Devuelve `202 Accepted` con un `id` del trabajo (para que el cliente pueda consultar estado si quiere). Un consumidor con `[ServiceBusTrigger]` procesa el mensaje con su propia velocidad, retries automáticos en errores transitorios, dead-letter en errores permanentes. El cliente recibe respuesta en milisegundos. Si una de las tres operaciones falla, el mensaje vuelve a la cola y se reintenta; no afecta al cliente que ya está satisfecho con su 202. La factura: la complejidad operativa pasa del cliente al consumidor (DLQ que vigilar, alertas), pero la UX y la resiliencia mejoran enormemente.
2. **Service Bus Topic + Subscriptions**, no Queue ni Event Grid. Razón: necesitas que **varios consumidores reaccionen al mismo evento de forma independiente**. Cada uno tendría su propia subscription con su propio retry y dead-letter. Si email falla y agota retries, va a DLQ — pero analytics y warehouse siguen funcionando con su mismo evento. Queue no sirve porque un mensaje en una queue se consume por **un** consumidor; los otros dos no lo verían. Event Grid sí podría usarse, pero las garantías son menores (24h de retry total, sin DLQ por defecto) — para acciones críticas como facturación o notificaciones al cliente, Service Bus Topic da mejor robustez.
3. JSON malformado → **`DeadLetterMessageAsync`** directo con `deadLetterReason: "MalformedJson"`. El error es permanente — reintentar 10 veces no va a hacer que el JSON se parsee correctamente; cada retry consume una ejecución para nada. Mejor a DLQ desde el primer intento para revisión humana. BD temporalmente lenta → **`AbandonMessageAsync`**. El error es transitorio — esperar 10 minutos y reintentar tiene buena probabilidad de éxito. El mensaje vuelve a la cola, su `DeliveryCount` incrementa, y al cabo de N intentos (configurable, default 10) acaba en DLQ si la BD no se recupera. Distinguir entre los dos es lo que separa una DLQ "con 3 mensajes que merecen atención humana" de una DLQ "con 100 mensajes reciclados que el sistema no podía procesar nunca".
4. **El mensaje se encola igual** aunque la validación haya fallado y el HTTP haya respondido 400. Functions interpreta `MensajeQueue = "algo"` como "publica este mensaje", independientemente de lo que devuelva el HTTP. Es un bug sutil porque: (a) el cliente recibe 400 pensando que el pedido no entró, (b) el consumer de la queue recibe un mensaje con un pedido **inválido** que no debería procesar. El consumer probablemente fallará al validar y mandará a DLQ — pero ya gastaste retries y polución de DLQ por algo que nunca debió encolarse. La fail-safe del null solo aplica si la propiedad es **explícitamente null**. Defensa: usa POCO con un único path de retorno con todo a null por defecto, y rellena solo lo que aplica en el happy path.
5. **Comportamiento correcto, no bug**. La función Event Grid se dispara siempre que se cree un blob en `uploads/` (es lo que define la suscripción de Event Grid). Pero la **lógica de fan-out por extensión** vive en el código de la función, no en la suscripción. Cuando ve `.jpg`, el `switch` no encuentra match y devuelve `new ClasificarArchivoResult()` con las dos propiedades a `null`. Functions, al ver los dos outputs null, no encola en ninguna queue. Es el patrón limpio: la función se ejecuta (consumiendo unos milisegundos de Functions Consumption, casi gratis), pero no produce efectos secundarios. Alternativa sería filtrar la suscripción de Event Grid para que solo dispare con `.pdf` o `.csv`, pero como Event Grid tiene filtros limitados, el filtrado en código es más flexible y mantenible.
6. **Event Grid**: probablemente **se pierden eventos**. Event Grid hace retry exponencial hasta 24 horas; tras 2 horas la mayoría de eventos se han intentado entregar varias veces y, aunque algunos pueden estar en el rango de retry todavía, otros pueden haber agotado intentos. Sin dead-letter configurado (no es el default en Event Grid), los eventos perdidos **desaparecen**. **Service Bus Queue**: en el mismo escenario, **no se pierde nada**. Los mensajes siguen en la cola esperando a un consumidor activo. Cuando tu función vuelve, empieza a consumirlos en el orden en que estaban. La cola tiene retención configurable (1-14 días por defecto, hasta `MaxDeliveryCount` intentos por mensaje). La diferencia es enorme: Event Grid asume "evento publicado, alguien lo escuchará pronto"; Service Bus Queue garantiza "trabajo encolado, alguien lo hará cuando pueda". Para acciones críticas, siempre Service Bus.

</details>

---

## 14. Hasta aquí

Vuelve a la imagen de la oficina postal de la sección 4. Apartado individual (Queue), boletín de la asociación (Topic), megafonía del barrio (Event Grid). Esas tres formas de hacer llegar comunicación cubren el 95% de los patrones de mensajería en Azure. Saber cuál elegir para cada situación es la decisión de arquitectura más importante de M04.

Lo siguiente es [`S4.2 — Durable Functions`](../S4.2-durable-functions/MANUAL.md), que cambia de paradigma: en lugar de "función A → cola → función B → cola → función C", introduce **orquestadores stateful** que coordinan varias funciones (fan-out/fan-in, async APIs, human-in-the-loop, monitor). Es el reemplazo declarativo de las cadenas largas de Service Bus cuando el flujo es complejo. Verás que en muchos casos lo que hoy son tres queues y tres consumidores se reduce a un orquestador y tres activities.
