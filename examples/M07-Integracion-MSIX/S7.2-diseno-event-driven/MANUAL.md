# Manual del alumno — S7.2 · Diseño event-driven

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta qué cambia en tu cabeza cuando pasas de sistemas síncronos a event-driven, por qué Saga no es un patrón de diseño nuevo sino la manera honesta de hacer transacciones distribuidas, qué hace que un nombre de evento sea bueno y por qué cualquier sistema serio acaba versionando sus eventos.

Tiempo de lectura: ~30 min. Submódulo de teoría: [M07-S7.2](../../../doc/M07-Integracion-MSIX/v3-actual/M07-S7.2-diseno-event-driven-v3.md). Tres piezas de lógica pura (advisor de diseño, validador anti-patterns, event store en memoria con replay y snapshots) y la materialización de la pregunta "¿es buen caso para event-driven?".

*Creado: 2026-05-20 19:55 +0200*

---

## 1. La idea en una frase

Event-driven cambia el contrato: tu API ya no espera al servicio de pagos, no espera al de inventario, no espera al de email. Devuelve 202 al cliente al instante y emite un **evento** ("PedidoCreado") que otros servicios consumen cuando puedan. A cambio renuncias a la consistencia inmediata: hay una ventana en que el pedido existe pero la factura todavía no. Esa renuncia tiene un nombre técnico —**eventual consistency**— y un coste operativo concreto: necesitas Correlation ID, idempotencia, Outbox pattern, monitorización de DLQ y, sobre todo, la conversación con producto sobre cómo se enseña al usuario que su pedido se está procesando.

El ejemplo materializa tres preguntas: ¿qué patrón de evento usar?, ¿es mi caso realmente bueno para event-driven o estoy complicándome la vida?, ¿cómo evito los cuatro anti-patterns más caros (comando disfrazado, dato sensible, sin versión, cadena demasiado larga)? Más un Event Store en memoria que demuestra replay + snapshot.

---

## 2. El problema real que hay detrás

Tres situaciones que motivaron la versión "event-driven hecha bien":

**Caso 1 — el commit distribuido que no era una transacción.** Una API de pedidos hacía cinco cosas en su endpoint: guardar pedido en BD, cobrar tarjeta, descontar stock, mandar correo, registrar en analytics. Cuando una de las cinco fallaba a mitad, el equipo intentó hacer un `try/catch` con rollback manual. **No funcionó.** Si el cobro tenía éxito y el descuento de stock fallaba después, la lógica de "deshacer el cobro" no era trivial (devolución parcial, error con el banco, log inconsistente). Decidieron migrar a Saga: cada paso emite un evento, el siguiente se entera y actúa, si algo falla emite eventos de **compensación** en orden inverso. El código de cada paso quedó simple y aislado; la complejidad se trasladó al esquema de eventos, donde se entiende mejor.

**Caso 2 — el evento "ProcesarCobro".** Un equipo emitía un evento llamado `ProcesarCobro` que llevaba campos como "monto" y "tarjeta". Otro equipo lo consumía y cobraba. Al cabo de meses, descubrieron que estaba mal pensado: `ProcesarCobro` es un **comando**, no un evento. El emisor está diciendo "haz esto" en lugar de "esto ha pasado". Cuando llegó la nueva integración con fraud-detection, hubo que renombrar todo a `PedidoConfirmado` y dejar que el servicio de cobro reaccionara como un consumer más. **Los eventos describen pasado, no futuro.**

**Caso 3 — el "Replay" para auditoría.** Un cliente regulatorio pidió poder reconstruir el estado de un pedido en un momento histórico cualquiera. La BD relacional solo guardaba el estado actual; el histórico era log de aplicación, imposible de query. La solución fue migrar a **Event Sourcing**: no se guarda el estado, se guarda la lista de eventos que lo produjeron. El estado se reconstruye con `Aggregate(Inicial, eventos, Aplicar)`. La auditoría es trivial: filtras hasta tal timestamp y replay. El precio: la complejidad cognitiva del equipo (todos tienen que entender el modelo) y la necesidad de **snapshots** cada N eventos para que el replay no tarde minutos.

Los tres casos enseñan que event-driven no es un atajo. Es un cambio de modelo que da beneficios reales (desacoplamiento, escala independiente, resiliencia) a cambio de costes operativos concretos (eventual consistency, idempotencia, complejidad de testing). El submódulo te ayuda a distinguir cuándo vale la pena de cuándo no.

---

## 3. Por qué esto importa en tu stack

Tres preguntas que el equipo nuevo te va a hacer cuando arranque un sistema en Azure:

- **¿Lo hacemos síncrono o event-driven?** La respuesta correcta no es "event-driven mola". Es: pásale los criterios del slide 13 al problema y a ver qué dice. CRUD simple, consistencia fuerte, volumen bajo → monolito síncrono. Múltiples consumers, procesamiento pesado, escalado independiente → event-driven.
- **¿Qué patrón de evento?** Notification (solo el id), Carried-State (con los datos en el payload) o Sourcing (todo es evento). Tres opciones según trade-offs.
- **¿Cómo gestiono las transacciones distribuidas?** Saga: choreography para flujos cortos (≤4 pasos sin lógica condicional), orchestration con Durable Functions para los demás.

Si tienes las respuestas, el equipo arranca sin atascos. Sin ellas, se mete en event-driven "porque mola" y descubre los costes operativos seis meses tarde.

---

## 4. La analogía vertebradora: el coro y la sinfónica

Imagina dos formas de tocar música compleja:

**Sinfónica** (orchestration): hay un director con la partitura completa. Levanta la batuta para cada sección, marca entradas, controla tempos, decide cuándo cada instrumento toca. Si alguien se equivoca, el director lo nota inmediatamente y lo corrige. Es lo que ofrece **Durable Functions con un Orchestrator**: el flujo se describe linealmente, el coordinador conoce el estado, las decisiones están en un solo sitio. Es el patrón correcto cuando hay cinco o más pasos o cuando hay lógica condicional ("si el cliente es premium, salta los pasos 3 y 4").

**Coro a capella** (choreography): no hay director. Cada cantante sabe **escuchar a los demás** y reaccionar. Cuando la soprano sostiene una nota larga, los tenores entran en armonía. Cuando todos llegan a un acorde, hay una pausa natural. Funciona magníficamente para piezas cortas y bien ensayadas; pero si la pieza es larga o complicada, sin director acaban desincronizados. En event-driven, **choreography** es el flujo donde cada servicio reacciona a eventos sin un coordinador: el de pagos escucha `PedidoCreado`, cobra, emite `PagoConfirmado`; el de envíos escucha `PagoConfirmado`, manda el paquete. Funciona para flujos de 2-4 pasos sin lógica condicional.

Y luego están las **partituras**. Si un cantante se equivoca a mitad de pieza, hay dos formas de gestionarlo:

- **Sinfónica**: el director detiene la pieza, retrocede al compás equivocado, vuelve a empezar desde ahí (compensación gestionada por el orchestrator).
- **Coro**: el cantante que se equivoca da una nota cualquiera y los demás reaccionan corrigiendo (eventos de compensación que cada servicio escucha y procesa).

Los eventos del coro siguen una regla: **siempre describen lo que acaba de pasar**, nunca lo que tiene que pasar. La soprano no dice "tenores, entrad ya". Sostiene su nota, y los tenores deciden por su cuenta que esa nota es su entrada. Eso es lo que diferencia un **evento** (`PedidoCreado`) de un **comando** (`ProcesarCobro`).

Mantén la imagen mientras lees el código. La sinfónica es orchestration, el coro es choreography, y la regla de oro es: los eventos cuentan historia, los comandos dan órdenes.

---

## 5. Recorrido por el código

### `EventDesignAdvisor.RecomendarPatron` — tres patrones de evento

Slide 6 del submódulo, materializada como función pura:

```csharp
public static PatronEvento RecomendarPatron(
    bool consumidorAutonomo, bool eventosPequenos, bool auditTrailCompleto)
{
    if (auditTrailCompleto) return PatronEvento.EventSourcing;
    if (consumidorAutonomo && !eventosPequenos)
        return PatronEvento.EventCarriedStateTransfer;
    return PatronEvento.EventNotification;
}
```

Los tres patrones:

- **Event Notification**: el evento lleva solo el id. `PedidoCreado(pedidoId: "abc")`. El consumer que lo necesite va a la API a buscar los detalles. **Pros**: payload mínimo, sin duplicación. **Contras**: cada consumer hace su llamada (N+1), acoplamiento al servicio fuente.
- **Event-Carried State Transfer**: el evento lleva los datos relevantes. `PedidoCreado(pedidoId: "abc", clienteId: "123", total: 250, items: [...])`. El consumer trabaja autónomamente sin volver a llamar al servicio fuente. **Pros**: consumers autónomos, sin acoplamiento. **Contras**: payload mayor, posible duplicación de estado.
- **Event Sourcing**: el estado **es** la lista de eventos. No hay tabla `Pedido` con una fila por pedido; hay una tabla `Eventos` con todas las cosas que pasaron al pedido. El estado actual se reconstruye con `Aggregate(eventos, Aplicar)`. **Pros**: auditoría perfecta, replay, time-travel. **Contras**: complejidad cognitiva, requiere snapshots, query del estado actual no es trivial.

La regla práctica: **empieza con Event-Carried State Transfer**. Es el que mejor balance da entre desacoplamiento y simplicidad. Event Notification cuando los eventos son grandes y el consumer probablemente no los necesita todos. Event Sourcing solo cuando la auditoría/replay es un requisito explícito.

### `EventDesignAdvisor.EsBuenCaso` — ¿realmente es buen caso?

Quizá la función más educativa del ejemplo. Recibe ocho banderas (señales a favor o en contra) y dice "sí, ve con event-driven" o "no, complícate menos":

```csharp
var aFavor = new List<string>();
if (multiplesConsumidores) aFavor.Add("Varios servicios reaccionan al mismo evento...");
if (procesamientoPesado) aFavor.Add("Procesamiento pesado que no debe bloquear al usuario...");
if (escaladoIndependiente) aFavor.Add("Necesita escalar servicios de forma independiente...");
// ... más a favor

var enContra = new List<string>();
if (crudSimple) enContra.Add("Es un CRUD simple de un solo servicio (slide 13 — NO).");
if (consistenciaFuerteInmediata) enContra.Add("Exige consistencia fuerte inmediata...");
if (volumenBajo) enContra.Add("Volumen bajo: un monolito basta...");
// ... más en contra

bool recomendado = aFavor.Count > enContra.Count;
```

Suma señales y compara. Es honesta sobre la idea: **event-driven NO es siempre mejor**. Hay tres casos donde un monolito gana claramente:

1. **CRUD simple** de un solo servicio: si una API solo tiene una BD y unos endpoints CRUD, meter mensajería es complicarse sin razón.
2. **Consistencia fuerte inmediata**: si tu negocio requiere ver el resultado de una operación al instante de hacerla, no hay event-driven que lo dé sin parches. Una transferencia bancaria necesita "el dinero salió de A y entró en B atómicamente" — eso es transacción ACID, no Saga eventual.
3. **Volumen bajo**: si manejas 10 requests al día, un monolito sirve. El coste operativo de event-driven —Service Bus, monitorización de DLQ, idempotencia, Correlation ID, eventual consistency en la UX— solo se amortiza con volumen.

Cuando el equipo nuevo dice "queremos hacer event-driven porque es lo moderno", pasarle esta función con sus señales reales suele aclarar la decisión.

### `EventDesignAdvisor.RecomendarSaga` y `SecuenciaCompensacion`

Para gestionar transacciones distribuidas, dos preguntas:

```csharp
public static EstiloSaga RecomendarSaga(int pasos, bool logicaCondicional) =>
    pasos >= 5 || logicaCondicional
        ? EstiloSaga.Orchestration
        : EstiloSaga.Choreography;
```

- **Choreography**: si tienes ≤4 pasos sin lógica condicional, cada servicio reacciona al evento anterior y emite el siguiente. Sin coordinador. Simple, descentralizado.
- **Orchestration**: si tienes 5 o más pasos, o lógica condicional, necesitas un coordinador. Durable Functions (S4.2) es la herramienta. El flujo se describe linealmente en un orquestador.

Y la compensación:

```csharp
public static IReadOnlyList<string> SecuenciaCompensacion(
    IReadOnlyList<string> pasosCompletados, int falloEnPaso)
{
    int completados = Math.Min(falloEnPaso - 1, pasosCompletados.Count);
    return [.. pasosCompletados.Take(completados)
        .Reverse()
        .Select(p => $"Compensar: {p}")];
}
```

Si fallas en el paso 4, compensas los pasos 1, 2 y 3 **en orden inverso** (3, 2, 1). Si el paso 3 era "descontar stock", "compensar: descontar stock" significa "devolver al stock". Si el paso 2 era "cobrar", "compensar: cobrar" significa "reembolsar". El orden inverso es importante: deshacer en el orden contrario al que se hizo.

### `EventValidator` — los cuatro anti-patterns que te matarán

Slide 20, materializada como reglas:

**Anti-pattern 1 — Comando disfrazado.** Si el nombre del evento empieza por un verbo imperativo (`Enviar`, `Crear`, `Procesar`, `Cobrar`...), no es un evento, es un comando. La validación detecta esto:

```csharp
private static readonly string[] VerbosComando =
[
    "enviar", "crear", "procesar", "cobrar", "reservar", "borrar",
    "eliminar", "actualizar", "cancelar", "validar", "generar", ...
];

if (VerbosComando.Any(v => tipo.StartsWith(v, StringComparison.OrdinalIgnoreCase)))
    problemas.Add($"'{tipo}' parece un COMANDO, no un evento...");
```

Naming correcto: `PedidoCreado`, `PagoConfirmado`, `StockReservado`, `EmailEnviado`. Todos en pasado. El cambio de naming es trivial pero cambia el modelo mental: el emisor cuenta lo que pasó, el consumer decide qué hacer.

**Anti-pattern 2 — Dato sensible en el evento.** Si un campo se llama `password`, `secret`, `token`, `apiKey`, `cvv`, `tarjeta`, `iban`... estás filtrando datos sensibles. Los eventos pasan por colas, se persisten, se inspeccionan en herramientas de auditoría. **Cualquier persona con acceso a la cola lo lee.** La forma correcta: pasar una **referencia** (id, hash, URI) y dejar que el consumer autorizado lo recupere de un servicio seguro (Key Vault si es un secret, BD si es un dato).

**Anti-pattern 3 — Sin versión.** Cuando publicas un evento, alguien lo va a consumir. Cuando cambies el evento (añadir campo, renombrar), los consumers antiguos rompen. La solución: incluir desde el día uno un campo `version` o `schemaVersion`. Los consumers pueden inspeccionarlo y adaptar. Sin él, cada cambio es un riesgo en producción.

**Anti-pattern 4 — Cadenas demasiado largas.** Si tu flujo es `A → B → C → D → E → F` con seis eventos encadenados, has perdido la trazabilidad. Cuando algo falla, "¿en qué paso?" requiere correlacionar logs de seis servicios distintos. La regla: **máximo 3-4 saltos** en choreography; si necesitas más, usa orchestration con un orquestador que tenga visibilidad completa.

### `EventStore` — replay + snapshot en memoria

La pieza más densa de Event Sourcing. Un append-only store con snapshots cada N eventos:

```csharp
public long Append(string streamId, EventoPedido evento)
{
    var stream = _streams.TryGetValue(streamId, out var s) ? s : _streams[streamId] = [];
    stream.Add(evento);
    long version = stream.Count;

    if (version % _snapshotCada == 0)
    {
        _snapshots[streamId] = PedidoProjection.Reconstruir(EstadoPedido.Inicial, stream);
        SnapshotsTomados++;
    }
    return version;
}

public EstadoPedido Cargar(string streamId)
{
    // ... usa el último snapshot + replay solo de lo posterior
}
```

Tres ideas que conviene interiorizar:

1. **El estado se reconstruye con `Aggregate(eventos, Aplicar)`**, donde `Aplicar` es una función pura `(estado, evento) → nuevo_estado`. Esa función es la **proyección**. Es código tuyo, no de Cosmos ni de ningún servicio: tú decides cómo cada evento modifica el estado.
2. **Los snapshots son la optimización para no replay millones de eventos**. Cada N eventos (típicamente 50-100), guardas el estado actual. Para cargar el estado actual, partes del último snapshot y reproduces solo los eventos posteriores. Sin snapshots, un agregado con 10.000 eventos tarda decenas de milisegundos cada vez que se carga.
3. **El estado actual es derivado**, no fuente. La fuente de verdad es la lista de eventos. Si encuentras un bug en la lógica de proyección, lo corriges y vuelves a reproducir desde cero — el estado nuevo es coherente con la nueva proyección sobre los mismos eventos. **Esto es time-travel**, y es la magia de Event Sourcing.

En producción el `EventStore` no está en memoria, está en Cosmos DB o en una BD diseñada (Marten sobre PostgreSQL, EventStoreDB). Pero el modelo es el mismo: append-only, replay, snapshots periódicos.

---

## 6. La conversación con Producto: eventual consistency

Una parte clave del submódulo que se cuela en cualquier proyecto event-driven real: la **conversación con producto** sobre qué hacer cuando un pedido existe pero la factura todavía no (porque está en cola). Tres opciones de UX:

**Opción A — Esconder el estado intermedio.** El frontend muestra "Procesando…" con un spinner hasta que la factura está. Es la peor opción operativamente: pierdes el beneficio de la latencia baja que te dio event-driven.

**Opción B — Mostrar estado parcial honestamente.** "Tu pedido está confirmado. La factura se generará en breve y la recibirás por email." Honesto, fácil de aceptar para el usuario, y aprovecha la velocidad de la respuesta inicial.

**Opción C — Mostrar el resultado optimista.** El frontend muestra "Factura: pendiente" pero permite continuar. Si la factura tarda más de lo esperado, polleo o se notifica. Más complejo pero da mejor UX cuando funciona.

La conversación con Producto **tiene que pasar** antes de implementar event-driven. Si no, el equipo de frontend te va a poner spinners y vais a haber sacrificado consistencia por nada. La opción B es lo que mejor funciona en el 80% de casos.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/EventDriven.Demo.Api
# http://localhost:5097
```

Endpoints:

```http
### ¿Qué patrón para un consumer autónomo con eventos no pequeños?
POST http://localhost:5097/eventdriven/patron
Content-Type: application/json

{ "consumidorAutonomo": true, "eventosPequenos": false, "auditTrailCompleto": false }
# → EventCarriedStateTransfer

### ¿Es buen caso event-driven?
POST http://localhost:5097/eventdriven/caso
Content-Type: application/json

{
  "multiplesConsumidores": true,
  "procesamientoPesado": true,
  "escaladoIndependiente": true,
  "disponibilidadSobreConsistencia": true,
  "equipoPuedeComplejidad": true,
  "crudSimple": false,
  "consistenciaFuerteInmediata": false,
  "volumenBajo": false
}
# → { recomendado: true, razones: [...] }

### Validar nombre y campos del evento
POST http://localhost:5097/eventdriven/validar
Content-Type: application/json

{
  "tipo": "ProcesarCobro",
  "campos": ["monto", "tarjeta", "cvv"]
}
# → { valido: false, problemas:
#     ["'ProcesarCobro' parece un COMANDO...",
#      "Campo 'tarjeta' expone datos sensibles...",
#      "Campo 'cvv' expone datos sensibles...",
#      "El evento no está versionado..."] }

### Event Sourcing — añadir eventos y cargar estado
POST http://localhost:5097/eventdriven/sourcing
# → demuestra append → cargar reproducido vs cargar con snapshot
```

Los 36 tests cubren todas las ramas del advisor, los cuatro anti-patterns del validator, el replay del event store con y sin snapshot, la compensación inversa con varios casos límite. La parte más educativa de los tests es la que demuestra que cargar tras un snapshot reproduce solo los eventos posteriores (`UltimoReplayCount` mide exactamente cuántos).

Para validar contra una arquitectura real:

- Cosmos DB (serverless) con un container de eventos y otro de read model.
- Service Bus Topic con suscripciones por cada consumer (cobros, inventario, emails).
- Functions consumiendo cada suscripción.
- Change Feed de Cosmos como **Outbox** (el patrón del slide 11): garantiza que cada cambio en el write model genera un evento publicado, sin double-write.

El script `01-verify-eventdriven.sh` inventaría: topic + suscripciones + DLQ + container Cosmos. Solo lectura.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. El Outbox pattern (slide 11)

Mencionado de pasada en el README pero merece atención: el **Outbox pattern** es la solución al problema de "guardar en BD y publicar en Service Bus atómicamente". Sin Outbox, tienes dos opciones malas:

- **Publicar primero, luego guardar**: si BD falla después del publish, hay un evento que dice "PedidoCreado" pero no hay pedido en BD. Bug fantasma.
- **Guardar primero, luego publicar**: si publish falla después del guardar, hay un pedido en BD pero ningún consumer se enteró. Bug invisible.

La solución correcta: guardar el pedido **y el evento** en la misma transacción ACID en BD. Luego un proceso aparte (poller o Change Feed) lee la tabla de eventos y los publica. Si publish falla, se reintenta; si guardar falla, no hay nada. **Atomicidad garantizada**.

En Azure, el atajo más elegante: **Change Feed de Cosmos DB**. Cosmos garantiza que cada cambio en un container genera una entrada en su feed. Tu app guarda el pedido en Cosmos (transacción única); una Function con CosmosDBTrigger lee el feed y publica el evento a Service Bus. **Sin Outbox manual, sin doble write, sin trabajo extra**.

Lo viste implementado en S4.P (M04). Aquí cierra el círculo conceptualmente.

---

## 9. Glosario breve

- **Event-driven architecture (EDA)**: arquitectura donde los servicios se comunican emitiendo y consumiendo eventos asíncronos en lugar de llamarse directamente.
- **Event Notification**: evento que solo lleva un id; el consumer va a la fuente a buscar los detalles.
- **Event-Carried State Transfer**: evento que lleva los datos relevantes en el payload; el consumer es autónomo.
- **Event Sourcing**: el estado es derivado de la lista de eventos; no se guarda el estado, se guardan los eventos.
- **Saga**: patrón para transacciones distribuidas como secuencia de pasos, cada uno con compensación inversa por si algo falla.
- **Choreography**: cada servicio escucha eventos y emite los suyos, sin coordinador central.
- **Orchestration**: un orquestador centralizado conoce el flujo completo y coordina los pasos.
- **Outbox pattern**: técnica para garantizar atomicidad entre guardar en BD y publicar a Service Bus.
- **Correlation ID**: id único de una operación de negocio que se propaga por todos los eventos relacionados. Permite tracear el flujo en logs y observability.
- **Eventual consistency**: garantía de que el sistema acaba siendo consistente, pero no inmediatamente.
- **Replay**: reconstrucción del estado reproduciendo los eventos desde el principio (o desde un snapshot).
- **Snapshot**: foto del estado en un momento dado, guardada para evitar replay completo en cada carga.
- **Proyección**: función pura `(estado, evento) → nuevo_estado` que define cómo cada evento modifica el estado.

---

## 10. Cierre

S7.2 te da tres tablas mentales que cambian la conversación de "vamos a hacer eventos porque sí" a "vamos a hacer eventos por estas razones concretas, conscientes de estos costes". El árbol de decisión (¿es buen caso?), el catálogo de patrones (Notification/Carried-State/Sourcing) y los anti-patterns (no comandos disfrazados, no datos sensibles, sí versionado, no cadenas largas) son lo que separa un sistema event-driven robusto de un experimento que se cae a los seis meses.

Lo siguiente es [`S7.3 — API Management`](../S7.3-api-management/MANUAL.md), donde la conversación se mueve del backend asíncrono al gateway que pone delante de tus APIs: políticas, versionado, rate limiting y los tiers.
