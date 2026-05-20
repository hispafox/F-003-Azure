# Manual del alumno — S7.1 · Service Bus y Event Grid: patrones avanzados

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta por qué la conversación de mensajería en M07 ya no son triggers, son **decisiones de arquitectura**, qué hace el broker antes de entregar un mensaje, y por qué un evaluador de filtros SQL escrito como función pura te ayuda a entender el comportamiento real del Service Bus en producción.

Tiempo de lectura: ~30 min. Submódulo de teoría: [M07-S7.1](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.1-service-bus-event-grid-avanzado-v3.md). Tres piezas de lógica pura densas (evaluador de filtros SQL completo, deduplicador con ventana, árbol de decisión de cuatro servicios) más un planificador que las une.

*Creado: 2026-05-20 19:30 +0200*

---

## 1. La idea en una frase

En M04 viste cómo consumir mensajes de una cola con un trigger. Aquí subes de capa: lo importante ya no es escribir `[ServiceBusTrigger("...")]` sino **decidir qué servicio de mensajería usar** (Storage Queue, Service Bus Queue, Service Bus Topic, Service Bus Premium, Event Grid o Event Hubs), **configurar bien el broker** (filtros SQL en las suscripciones, deduplicación por MessageId, sessions para FIFO, alertas en la DLQ) y **clasificar incidentes** cuando algo cae a la dead-letter queue. Las decisiones son lógica pura, se prueban en milisegundos, y son las que diferencian un sistema de mensajería de juguete de uno enterprise.

El ejemplo modela tres tablas críticas: la del filtro SQL (lo que evalúa el broker por ti), la de la ventana de deduplicación (la garantía at-least-once convertida en exactly-once-en-ventana), y la del árbol de decisión de servicio (con seis hojas y un coste mensual asociado a cada una).

---

## 2. El problema real que hay detrás

Tres situaciones reales que justifican subir de nivel respecto a M04:

**Caso 1 — la "cola" que era topic enmascarado.** Un equipo arrancó con Service Bus pensando en una cola simple punto a punto. Al cabo de un trimestre, otro equipo quiso "engancharse" para reaccionar al mismo evento. La solución cómoda fue añadir un consumer que copiara mensajes a otra cola. Tres meses después había cinco consumers haciendo lo mismo: un fan-out manual que se rompía cada vez que alguien metía la pata. La reescritura correcta fue migrar a **Topic** con cinco **suscripciones** independientes, cada una con su propio filtro y su propia DLQ. Cinco minutos de configuración en Portal por suscripción nueva; el código no se tocó.

**Caso 2 — el procesamiento doble silencioso.** Una integración con un pago externo. El consumer cobraba al cliente, marcaba el pedido como pagado, mandaba el correo de confirmación. Estaba todo correcto excepto un detalle: Service Bus es at-least-once. De vez en cuando, por un timeout del consumer o una reanudación de la session, **el mismo mensaje se entregaba dos veces**. Y cada vez se cobraba al cliente y se mandaba el correo. El cliente recibía dos correos idénticos en 30 segundos. La solución más limpia fue activar **Duplicate Detection** en la cola con ventana de 5 minutos: el broker ya sabe que un MessageId visto en los últimos 5 minutos es duplicado, descarta el segundo antes de entregarlo. Sin tocar código de consumer.

**Caso 3 — el filtro hecho en el consumer.** Un equipo tenía un topic con un suscriptor que recibía TODO y filtraba por código:

```csharp
if (mensaje.ApplicationProperties["pais"]?.ToString() != "ES") return;
```

Cuando el volumen subió a 500 msg/s, el coste de Service Bus se disparó: pagaban operaciones por cada mensaje **descartado**, no solo por los que importaban. Y el consumer hacía CPU innecesario. La solución: poner un **filtro SQL en la suscripción** (`pais = 'ES'`). El broker descarta los mensajes que no cumplen antes de entregarlos. Coste a la mitad, latencia mejorada. Cinco caracteres en el portal.

Los tres casos se previenen sabiendo: qué servicio se usa para qué (topic vs queue vs Event Grid), cómo configurar deduplicación, y cómo escribir filtros SQL para que el broker descarte los irrelevantes en su lado. Es lo que enseña este submódulo.

---

## 3. Por qué esto importa en tu stack

Cualquier sistema que comunique servicios entre sí —y todos los sistemas modernos lo hacen— vive con tres preguntas que el equipo nuevo no sabe responder:

- **¿Qué servicio uso para qué?** Hay cuatro que se confunden constantemente: Storage Queue (la barata), Service Bus (la fiable), Event Grid (la del push), Event Hubs (la del streaming). Cada una tiene un caso de uso óptimo y al menos dos donde es la opción equivocada.
- **¿Cómo evito procesar dos veces lo mismo?** Service Bus es at-least-once por diseño. La idempotencia desde la app (S4.3) es la red de seguridad; la deduplicación en el broker es la prevención. Las dos juntas son lo robusto.
- **¿Cómo no pago por mensajes que no me interesan?** Filtros SQL en suscripciones. Una línea de configuración del topic que el broker aplica antes de entregar.

Si tienes claras las respuestas, el coste mensual de tu mensajería será predecible. Sin ellas, los costes crecen sin control y los bugs de duplicación aparecen los días de carga alta.

---

## 4. La analogía vertebradora: la centralita de cartas certificadas

Imagina una empresa con varios departamentos que reciben correspondencia. Hay una sala de clasificación central donde un equipo de oficinistas decide qué hacer con cada sobre que llega:

- **Cada sobre lleva metadatos visibles por fuera**: remitente, destinatario, país, importe declarado, urgencia. Eso son las **ApplicationProperties** del mensaje en Service Bus — no el contenido (el `Body`), sino las etiquetas que el broker puede leer sin abrir el sobre.
- **Cada departamento tiene una bandeja de entrada** (suscripción) con una **regla de qué sobres acepta**: "solo los que vienen de España" (`pais = 'ES'`), "solo los de importe > 100" (`total > 100`). La regla la define el departamento, la aplica la sala central. El **filtro SQL** es esa regla escrita.
- **El oficinista de la centralita evalúa la regla mirando solo los metadatos**, sin abrir el sobre. Si cumple, deja una copia en la bandeja del departamento; si no, el sobre no entra en esa bandeja. Si ningún departamento lo quiere, va a una bandeja especial de "no reclamados" — la dead-letter queue del topic.
- **El oficinista tiene un registro de los últimos sobres entregados** (los últimos N minutos, configurable: ventana de deduplicación). Si llega un sobre con el mismo número de seguimiento que uno ya entregado en ese rato, lo tira sin volver a entregarlo. Es la **dedup por MessageId**.

Y luego está la centralita en sí. Hay cuatro tipos según el volumen y el tipo de tráfico:

- **Centralita de barrio** (Storage Queue): barata, sencilla, una bandeja sin filtros sofisticados. Para cartas simples y volumen bajo. Cuesta céntimos al año.
- **Centralita corporativa estándar** (Service Bus Standard): topics con varias bandejas, filtros, DLQ, deduplicación, sessions para mantener orden. Cuesta unos 10 € al mes solo por existir, vale para la mayoría de sistemas serios.
- **Centralita corporativa premium** (Service Bus Premium): hace lo mismo pero conectada a tu red privada (VNet), con paquetes grandes (hasta 100 MB) y replicación geográfica. Cuesta 600 € al mes, solo si la regulación o el volumen lo justifican.
- **Centralita de mensajeros expresos** (Event Grid): no almacena sobres; recoge un sobre y lo **empuja inmediatamente** a un buzón electrónico (webhook). Si el destinatario no contesta, lo reintenta unas horas; si sigue fallando, lo tira o lo guarda en otra bandeja.
- **Centralita de prensa** (Event Hubs): no es para correspondencia, es para teletipos. Recibe un flujo continuo de mensajes y permite que distintos lectores los lean **a su ritmo**, con replay. Para telemetría, IoT, click streams.

Mantén la imagen: dos preguntas obvias para cada nueva integración son "¿qué tipo de centralita?" y "¿qué reglas pongo en mi bandeja?". El submódulo te da las dos respuestas como código.

---

## 5. Recorrido por el código

### `SqlFilterEvaluator` — lo que el broker hace por ti

Es la pieza más densa del ejemplo y la que más enseña sobre Service Bus de verdad. Implementa un subconjunto del SQL-92 que entiende el broker para evaluar reglas de suscripción:

- Comparaciones: `=`, `<>`, `!=`, `>`, `>=`, `<`, `<=`, `LIKE`.
- Lógica: `AND`, `OR`, `NOT`, paréntesis.
- Nulos: `IS NULL`, `IS NOT NULL`.
- Lógica de tres valores: si una propiedad referenciada no existe, el resultado es `UNKNOWN` (`null` en C#) y **el broker NO entrega el mensaje** — exactamente el mismo comportamiento que SQL-92.

```csharp
public static bool Coincide(
    string filtroSql, IReadOnlyDictionary<string, object?> propiedades)
{
    var tokens = Tokenizar(filtroSql);
    var parser = new Parser(tokens);
    var resultado = parser.ParseOr().Evaluar(propiedades);
    parser.EsperarFin();
    // UNKNOWN (null) → no se entrega (regla de Service Bus).
    return resultado == true;
}
```

Tener este evaluador como código tuyo cambia la conversación: cuando alguien pregunta "¿este filtro va a coger los mensajes que quiero?", la respuesta no es "supongo, lo probamos en Azure y vemos". Es **escribir el filtro, pasar las propiedades por el evaluador, y leer el resultado en milisegundos**. Sin namespace de Service Bus, sin coste, sin esperar al broker. Cuando tengas claro el filtro, lo configuras en el portal y sabes que se va a comportar como esperas.

Tres detalles que diferencian filtros bien escritos:

- **`pais = 'ES'`** entrega solo si la propiedad `pais` existe y vale exactamente `'ES'`. Si no existe (mensaje sin esa property), el filtro evalúa a `UNKNOWN` y no entrega. Si quieres incluir los mensajes sin `pais`, escribe `pais = 'ES' OR pais IS NULL`.
- **`total > 100`** asume que `total` es un número. Si llega un mensaje con `total = '100'` (string), evalúa a `UNKNOWN` — no compara strings con números. Mantén consistencia de tipos en las properties.
- **`nombre LIKE 'P%'`** funciona con `%` como cualquier secuencia y `_` como un solo carácter, igual que en SQL. Si necesitas escapar un `%` literal, sintaxis estándar.

### `MessageDeduplicator` — la garantía de "una vez por ventana"

Service Bus es **at-least-once**: el broker garantiza que el mensaje se entrega al menos una vez. Cuando hay reintentos, fallos de red, sessions que se reanudan, **un mensaje puede entregarse dos o más veces**. La deduplicación por `MessageId` en el broker convierte esta garantía en "exactly-once dentro de una ventana de tiempo":

```csharp
public static readonly TimeSpan VentanaMinima    = TimeSpan.FromSeconds(20);
public static readonly TimeSpan VentanaMaxima    = TimeSpan.FromDays(7);
public static readonly TimeSpan VentanaPorDefecto = TimeSpan.FromSeconds(30);
```

La lógica:

```csharp
foreach (var m in mensajes.OrderBy(x => x.Encolado))
{
    if (ultimoEntregado.TryGetValue(m.MessageId, out var previo) &&
        m.Encolado - previo <= ventana)
    {
        descartados.Add(m.MessageId);          // duplicado en ventana
        continue;
    }
    entregados.Add(m.MessageId);
    ultimoEntregado[m.MessageId] = m.Encolado;
}
```

Cuatro decisiones que conviene entender:

1. **El responsable de poner el MessageId eres tú**, no Service Bus. Si tu productor manda mensajes sin MessageId, el broker pone uno aleatorio y la deduplicación es inútil (cada mensaje tiene un id único). Para que la dedup funcione, el productor pone un id determinista basado en la operación de negocio: por ejemplo `pedido-1234-cobro`. Si reintentas el cobro, el id es el mismo, y el broker descarta el duplicado.
2. **La ventana es configurable de 20 segundos a 7 días**. La elección depende del riesgo:
   - 30 segundos (default): protege contra reentregas inmediatas por timeout del consumer.
   - 1-5 minutos: típico cuando la lógica de retry puede tardar.
   - 1 día o más: cuando un productor puede reenviar voluntariamente por error humano o reinicio del sistema.
3. **El reloj del broker se basa en el momento de encolado**, no en el momento de procesamiento. Si reintentas a los 31 segundos con ventana de 30 segundos, el segundo mensaje entra como nuevo. Si reintentas a los 29, se descarta.
4. **La deduplicación NO sustituye a la idempotencia** del consumer (S4.3). Es defense in depth: la dedup del broker protege de reentregas inmediatas; la idempotencia del consumer protege de todos los otros casos (mensaje reprocessed por un humano desde el portal, mensajes que escapan la ventana, etcétera).

### `MessagingServiceAdvisor` — el árbol de decisión

Quizá la pieza más utilizada en proyectos reales. Recibe un escenario y devuelve qué servicio usar y por qué:

```csharp
public sealed record EscenarioMensajeria(
    TipoMensaje Tipo,
    bool RequiereFifo = false,
    int TamanoMensajeKb = 64,
    bool PushAWebhook = false,
    bool RequiereReplay = false,
    bool FanOutMultiplesSuscriptores = false,
    bool RequiereVNet = false,
    long OperacionesMes = 100_000);
```

El árbol se recorre en orden de prioridad:

1. **`Tipo == Streaming` o `RequiereReplay`** → Event Hubs. Es el único servicio diseñado para alto volumen y retention con replay (7-90 días).
2. **`PushAWebhook`** → Event Grid. El único servicio que empuja activamente a un webhook HTTP (los otros son pull).
3. **`RequiereVNet` o `TamanoMensajeKb > 256`** → Service Bus Premium. VNet integration solo en Premium; mensajes > 256 KB solo en Premium (o patrón Claim Check con Standard).
4. **`FanOutMultiplesSuscriptores`** → Service Bus Topic (si quieres garantías + DLQ por suscripción) o Event Grid (si solo notificación sin garantías estrictas).
5. **`RequiereFifo`** → Service Bus Queue con Sessions.
6. **`Tipo == Comando` y volumen bajo** → Storage Queue (la opción más barata).
7. **Default** → Service Bus Queue Standard.

Cada rama lleva además una **razón** que explica el porqué y un **coste aproximado**. Cuando hagas la review con el equipo y alguien proponga "¿y si usamos Event Grid para esto?", el árbol te da la respuesta con la slide concreta detrás (3, 16, 17, 23, 25, 26, 32 del submódulo).

### `MessagingServiceAdvisor.ClasificarDeadLetter` — qué hacer cuando algo cae

Cuando un mensaje acaba en la DLQ, Service Bus pone un `DeadLetterReason` que indica por qué. La función mapea cada motivo a una acción correctiva:

| `DeadLetterReason` contiene… | Acción correctiva |
| --- | --- |
| `MaxDeliveryCount` | Reintentos agotados. Corregir bug en el consumer y reenviar desde la DLQ. |
| `TTL` o `Expired` | El consumer no procesa a tiempo. Escalar consumers o ampliar el TTL. |
| `HeaderSize` | ApplicationProperties demasiado grandes. Mover datos al body o usar Claim Check. |
| `Filter` o `Rule` | Sintaxis del filtro SQL incorrecta. Revisar la regla. |
| Otros | Inspeccionar `DeadLetterErrorDescription` y resolver caso a caso. |

Es la pieza menos glamorosa pero la que más se usa en producción: cada DLQ con mensajes encolados es un incidente a investigar; la clasificación te orienta en segundos.

### `MessagingPlanner` — el plan completo

Combina los anteriores en un plan: dado un escenario, recomienda servicio, filtros aplicables, ventana de deduplicación adecuada, anti-patterns a evitar (no usar singletons que persistan estado entre invocaciones, no usar MI con scope incorrecto, no holdear locks dentro del handler, idempotencia obligatoria). Es lo que el script `01-verify-messaging.sh` aplica contra tu namespace real para certificar el entregable.

---

## 6. Los anti-patterns del slide 31

El submódulo tiene una slide específica con los cinco errores más caros que se ven una y otra vez:

**Anti-pattern 1 — Connection string con SAS embebido en código**. Es un secreto rotable. Si se filtra, hay que rotar. Y si lo metiste en cinco apps, tienes que rotar en cinco sitios. La forma correcta: **Managed Identity** + rol "Azure Service Bus Data Sender/Receiver". Cero secretos.

**Anti-pattern 2 — Singleton de `ServiceBusClient` mal gestionado**. `ServiceBusClient` es thread-safe y costoso de crear. Debe ser **singleton** en DI. Lo que NO debe ser singleton es el `ServiceBusSender` que crees a partir de él para una cola/topic concreto — esos sí se reaprovechan pero tienen su ciclo de vida.

**Anti-pattern 3 — Lock dentro del handler**. Si haces `lock (obj)` o `await semaphoreSlim.WaitAsync()` dentro de un message handler, estás serializando el procesamiento. Para algo que viene en lotes paralelos, eso es muerte por throughput. La forma correcta: el handler procesa un mensaje a la vez (la sección crítica vive en Service Bus), tu código no tiene que serializar.

**Anti-pattern 4 — No idempotencia**. Ya cubierto en S4.3. Cualquier consumer at-least-once que no sea idempotente acaba en incidente.

**Anti-pattern 5 — Ignorar la DLQ**. Sin alerta en DLQ count, los mensajes se acumulan y nadie se entera hasta que el sistema downstream falla por inactividad. **Alerta cuando DLQ count > 10** (slide 19) y revisar diariamente en el primer mes de un sistema nuevo.

Los cinco están en el checklist del `IMessagingPlanner` y se verifican con el script.

---

## 7. Cómo probarlo en local

Es un ejemplo offline al 100% para la parte de decisión:

```bash
dotnet run --project src/Messaging.Demo.Api
# http://localhost:5096
```

Endpoints:

```http
### Evaluar un filtro SQL contra propiedades
POST http://localhost:5096/messaging/filtro
Content-Type: application/json

{
  "filtro": "pais = 'ES' AND total > 100",
  "propiedades": { "pais": "ES", "total": 250 }
}
# → { coincide: true }

### Recomendar servicio para un escenario
POST http://localhost:5096/messaging/recomendar
Content-Type: application/json

{
  "tipo": "EventoNegocio",
  "fanOutMultiplesSuscriptores": true,
  "requiereFifo": true
}
# → ServiceBusTopic + razones

### Clasificar un mensaje DLQ
POST http://localhost:5096/messaging/dlq
Content-Type: application/json

"MaxDeliveryCountExceeded"
# → "Se agotaron los reintentos: corregir la lógica..."
```

Los 54 tests cubren cada rama del árbol y muchos casos límite del filtro SQL (paréntesis anidados, `IS NULL` con propiedad ausente, `LIKE` con escapes, sintaxis inválida que lanza `FormatException`).

Para validar contra un namespace real:

- **Service Bus Standard** desde el portal (~10 €/mes, **bórralo al acabar**).
- Topic `pedidos-eventos` + dos suscripciones con filtros SQL distintos.
- Una cola `pedidos-dedup` con Duplicate Detection on y ventana 1 día.
- App Service con Managed Identity y rol "Azure Service Bus Data Sender/Receiver".

El script `01-verify-messaging.sh` inventaría tu namespace: SKU, topics, suscripciones, filtros, dedup, contadores de DLQ. No crea recursos; solo audita.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. Por qué este submódulo tampoco tiene CAPA de integración

El emulador oficial de Service Bus existe (`mcr.microsoft.com/azure-messaging/servicebus-emulator`), pero la decisión es no usarlo aquí. Tres razones:

1. **Exige un sidecar SQL** y una topología (colas/topics/suscripciones) declarada en un JSON. Montar eso para cada test ralentiza la suite y añade una superficie de bugs paralelos.
2. **El valor del submódulo está en la *decisión*** (qué filtro, qué ventana, qué servicio), que es lógica pura. El round-trip del SDK ya lo testea Microsoft.
3. **Lo único que ejercitaría el emulador es la SDK**, no los patrones que tú diseñas. Ese es trabajo de los tests de la propia librería de Microsoft.

Lo que sí podemos probar al 100% son los patrones de decisión. Y para validar el comportamiento real contra Service Bus se va con scripts `az` a un namespace de pruebas, una vez por configuración, no en cada commit.

---

## 9. La conversación con el equipo: "¿Service Bus o Event Grid?"

Pregunta que aparece en cuanto un sistema crece. Tres preguntas para responderla:

- **¿El consumer tira por pull o por push?** Service Bus es pull (el consumer pregunta "¿hay mensajes?"). Event Grid es push (el broker llama al consumer cuando hay un evento). Si tu consumer es un webhook HTTP o una Function con HttpTrigger, Event Grid encaja. Si tu consumer es una app que tiene su propio ciclo (worker, Function con ServiceBusTrigger), Service Bus encaja.
- **¿Necesitas garantías de entrega y DLQ por suscriptor?** Service Bus las tiene; Event Grid también tiene retry y DLQ pero con menos control.
- **¿El mensaje requiere orden FIFO?** Solo Service Bus con Sessions lo garantiza. Event Grid no garantiza orden.

La regla rápida que funciona en el 80% de los casos:

- **Eventos de negocio** que disparan trabajo (procesar pago, generar factura, mandar email) → Service Bus.
- **Notificaciones** que se quedan donde están (alertar, refrescar caché, actualizar dashboard) → Event Grid.

Service Bus es para "esto tiene que pasar"; Event Grid es para "esto ha pasado, avisad".

---

## 10. Los costes que conviene tener en la cabeza

Para que el árbol de decisión sea útil en proyectos reales, hay que conocer el coste aproximado de cada opción:

- **Storage Queue**: 0,36 € por millón de operaciones. Prácticamente gratis para volúmenes normales.
- **Service Bus Standard**: ~10 €/mes fijos solo por existir + 0,05 € por millón de operaciones. Es el coste de entrada de tener topics y filtros SQL.
- **Service Bus Premium**: 600 €/mes y subiendo (por messaging unit). Solo si necesitas VNet, mensajes > 256 KB, o aislamiento.
- **Event Grid**: 0,60 € por millón de eventos. Escalable y barato.
- **Event Hubs**: ~11 € por TU (Throughput Unit) al mes. Pago por capacidad, no por uso.

La diferencia entre Standard (~10 €/mes) y Premium (600 €/mes) es enorme y se confunde fácilmente. Antes de elegir Premium, verifica si **realmente** necesitas VNet integration, mensajes grandes o geo-DR. Si la respuesta es no, te ahorras 590 € al mes.

---

## 11. Glosario breve

- **Service Bus Queue**: cola punto a punto con garantías de entrega, DLQ, sessions, deduplicación.
- **Service Bus Topic**: variante pub/sub donde varias suscripciones reciben copia del mismo mensaje, cada una con su filtro y su DLQ.
- **Service Bus Standard vs Premium**: Standard cubre el 95% (~10 €/mes); Premium para VNet, > 256 KB, geo-DR.
- **Event Grid**: servicio de fan-out push a webhooks. Para eventos de notificación.
- **Event Hubs**: stream de eventos con retention y replay. Para telemetría e IoT.
- **Storage Queue**: cola simple barata sin pub/sub ni filtros. Para casos básicos.
- **ApplicationProperties**: diccionario de metadatos del mensaje (no el body), evaluable por filtros SQL.
- **MessageId**: id del mensaje. Si pones uno determinista (por operación), la deduplicación lo aprovecha.
- **Sessions**: mecanismo de Service Bus para garantizar FIFO dentro de un mismo SessionId.
- **Duplicate Detection**: capacidad del broker de descartar mensajes con MessageId visto en la ventana configurada (20 s a 7 días).
- **Dead-Letter Queue (DLQ)**: subcola hermana de cada cola/suscripción donde van los mensajes que fallaron o que se marcaron explícitamente como dead-letter.
- **Claim Check**: patrón para mensajes grandes — subes el blob, mandas en el mensaje un puntero (URI), el consumer descarga el blob al procesarlo.
- **SAS** (Shared Access Signature): autenticación legacy basada en token. Reemplazada por Managed Identity en sistemas modernos.

---

## 12. Cierre

S7.1 te da las tres herramientas mentales que diferencian un sistema de mensajería casual de uno enterprise: el árbol de decisión de qué servicio usar, los filtros SQL que el broker evalúa antes de entregar, y la deduplicación que convierte at-least-once en algo manejable. El código del ejemplo es las tres tablas materializadas como funciones puras que pruebas en milisegundos y aplicas a tu sistema con confianza.

Lo siguiente es [`S7.2 — Diseño event-driven`](../S7.2-diseno-event-driven/MANUAL.md), donde estos primitivos se combinan en patrones de arquitectura completos: pub/sub, Saga, Event Sourcing, CQRS y los anti-patterns más caros del estilo "usar eventos para todo".
