# Manual del alumno — S4.P · Práctica de flujo completo

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, despliegue por Portal, scripts. Este manual va antes: te cuenta qué hace especial a esta práctica frente a los ejemplos sueltos del módulo, cómo se compone la idempotencia en dos capas y por qué la pregunta más importante al diseñar un flujo event-driven es "¿qué pasa si el mismo evento llega dos veces?".

Tiempo de lectura: ~30 min. Submódulo de teoría: [M04-S4.P](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.P-practica-flujo-completo-v4.md). Tres funciones encadenadas (HTTP → Cosmos Change Feed → Queue) más un endpoint de inspección, veintiún tests, cero servicios premium.

*Creado: 2026-05-20 15:30 +0200*

---

## 1. La idea en una frase

Hasta ahora cada submódulo de M03 y M04 enseñó un trigger o un patrón en aislamiento. Esta práctica los junta en un flujo realista: **un pedido entra por HTTP, se guarda en Cosmos, el Change Feed dispara la facturación, la factura se escribe a Blob y a una Queue, y un consumidor de la Queue notifica al final**. Tres saltos asíncronos, multi-output bindings en el paso central, idempotencia en dos capas, y un endpoint `GET /api/estado` que te deja inspeccionar el efecto agregado de todo el flujo desde una sola llamada.

Si el módulo hubiera sido un libro, esta práctica sería el capítulo donde por fin se ven los personajes juntos en una misma escena. Lo que en S3.5 era "el trigger de Cosmos", en S3.6 "el binding de salida múltiple" y en S4.1 "la cola con QueueTrigger" aquí son tres piezas del mismo organismo.

---

## 2. El problema real que hay detrás

Un equipo de e-commerce diseñó su flujo de pedidos así: el frontend hace POST a un endpoint, esa función llama a Cosmos para guardar el pedido, llama al servicio de facturación, llama al de envío de email, devuelve OK al cliente. Cuatro pasos en una sola función, todos síncronos.

Funcionó el primer trimestre. Cuando el tráfico subió, descubrieron tres problemas:

1. **La latencia del POST se acopló a la del servicio externo más lento.** Si el servicio de email tardaba 8 segundos esa tarde, el cliente esperaba 8 segundos por su 201.
2. **Un fallo del email rompía el pedido completo.** Si el SMTP rebotaba, la función lanzaba, el catch hacía `throw`, y aunque el pedido ya estaba en Cosmos, el cliente recibía 500 y volvía a clickar — pedido duplicado en Cosmos, dos facturas, dos emails.
3. **Reintentar era un drama.** Si la función fallaba en la línea del email, Service Bus no estaba en la ecuación; la lógica de reintento era manual (un Timer Job que cada noche miraba Cosmos buscando pedidos "sin notificar"). Lento, difícil de mantener, propenso a errores.

La reescritura adoptó el patrón que enseña esta práctica:

- **El POST solo persiste el pedido en Cosmos y devuelve 201 al instante.** Latencia constante, independiente de los servicios downstream.
- **El Change Feed de Cosmos dispara la facturación** en background. Si falla, el feed mantiene el cursor; el reintento es automático.
- **La factura se escribe a Blob y la notificación va a Queue** en la misma invocación (multi-output binding). Si el blob falla, el mensaje no se encola; si todo va bien, los dos efectos suceden atómicamente desde la perspectiva del trigger.
- **El email lo procesa un Queue trigger separado.** Si el SMTP rebota, la queue reintentará el mensaje con su política propia.

El cliente vio: latencia del POST baja de segundos a milisegundos, cero pedidos duplicados, cero reintentos manuales. Y un detalle no obvio: la operación se volvió mucho más fácil de razonar. Cada pieza tiene una responsabilidad clara, los puntos de fallo están aislados, y el endpoint `/api/estado` te dice de un vistazo qué está pasando.

---

## 3. Por qué esto importa en tu stack

Si tu sistema tiene una API que persiste algo y luego dispara N efectos derivados —notificar a otros servicios, generar archivos, agregar métricas—, el patrón de esta práctica es probablemente el que necesitas. Tres reglas que se quedan:

- **El paso de entrada solo hace lo mínimo persistente.** Guarda el pedido y devuelve 201. Cualquier efecto derivado va por background a través de un trigger.
- **El acoplamiento entre pasos pasa por almacén (Cosmos, Blob, Queue) en vez de por llamada directa.** Cada paso es independiente, cada uno se reintenta solo, y un fallo aguas abajo no rompe los pasos previos.
- **La idempotencia es obligatoria en cualquier consumer de Change Feed o de cola.** Both son at-least-once. Si tu lógica no es idempotente, vas a tener efectos duplicados el día en que el cursor se rebobine o un mensaje se reentregue.

El coste mental es mayor que el de la versión síncrona —tienes que diseñar para asincronía— pero el coste operativo es mucho menor. Y a partir de cierto volumen de tráfico, el coste operativo es lo único que importa.

---

## 4. La analogía vertebradora: la línea de montaje del coche

Imagina una fábrica de coches con tres estaciones de trabajo en línea:

- **Estación 1 — Pedido recibido**. Un operario recibe la hoja de pedido del cliente, comprueba que está bien rellenada, le pone un sello con número único y la deja en una bandeja de salida. Cuelga "OK, recibido" para el cliente. **Esto es la `CrearPedidoFunction`**: deserializa el JSON, valida los items, calcula el total, devuelve 201 con el id. El pedido queda persistido en Cosmos.
- **Estación 2 — Facturación**. Un operario de la siguiente estación va recogiendo las hojas de la bandeja de Estación 1 según llegan. Por cada hoja: comprueba el sello para evitar trabajar dos veces sobre la misma (idempotencia, "esta ya la hice"), calcula el IVA, imprime la factura en papel (la deja en una carpeta — el blob) y mete un papelito en un tubo neumático (la queue) avisando a la Estación 3. **Esto es la `ProcesarNuevosPedidosFunction`**, disparada por el Change Feed de Cosmos.
- **Estación 3 — Notificación**. Un operario recibe el papelito por el tubo neumático, lo lee, marca en el panel central "factura del pedido X entregada" y archiva. **Esto es la `NotificarFacturaFunction`**, disparada por el QueueTrigger.

Y por encima de todo, **un encargado** mira el panel central y al final del día puede decir "se recibieron 234 pedidos, se facturaron 234, se notificaron 234". Si los tres números no coinciden, sabe que hay algo atascado entre dos estaciones y puede investigarlo. **El encargado es `GET /api/estado`**, el endpoint de inspección agregada.

Hay dos detalles operativos importantes de la línea de montaje que merece la pena nombrar:

- Si la Estación 2 se atranca, las hojas se acumulan en la bandeja de salida de la Estación 1. **No se pierden.** El sistema no falla; solo se enlentece. Cuando la Estación 2 vuelve a la marcha, recupera la cola sola. Esto es lo bonito del Change Feed: el cursor es persistente.
- Si la Estación 3 está saturada porque el SMTP va lento, los papelitos se acumulan en el tubo neumático (la queue). **No se pierden.** El sistema no falla en la Estación 2; solo se acumula carga en el tubo. Cuando la Estación 3 vuelve a la marcha, drena la cola sola.

Mantén la imagen de la fábrica. Toda la práctica se entiende mejor desde ahí.

---

## 5. Recorrido por el código

### Paso 1 — `CrearPedidoFunction` (HTTP + `CosmosDBOutput`)

El patrón de **multi-output binding** apareció en S3.6: una función puede devolver un objeto cuyas propiedades llevan atributos de bindings distintos, y el runtime escribe cada salida en su destino.

```csharp
public sealed class CrearPedidoResult
{
    [HttpResult]
    public IActionResult Http { get; set; } = null!;

    [CosmosDBOutput(databaseName: "tienda", containerName: "pedidos", ...)]
    public Pedido? PedidoCosmos { get; set; }
}
```

La función deserializa, valida con `IPedidoFactory.Crear`, devuelve un `CrearPedidoResult` con dos propiedades:
- `Http` lleva el `201 Created` que ve el cliente.
- `PedidoCosmos` lleva el documento que se persiste en Cosmos.

La elegancia del binding: el cliente recibe la respuesta **antes de que el `[CosmosDBOutput]` haya terminado de escribir** (el runtime gestiona la escritura tras devolver el `IActionResult`). Pero el documento siempre se escribe — el binding solo se omite si la función lanza una excepción.

Y ya sobre el documento hay un detalle de modelado importante: el `Pedido` tiene un campo `Estado` con valor `"nuevo"`. Eso es la primera capa de la idempotencia de la siguiente función.

### Paso 2 — `ProcesarNuevosPedidosFunction` (Cosmos Change Feed + multi-output)

El trigger es el corazón del flujo:

```csharp
[CosmosDBTrigger(
    databaseName: "tienda",
    containerName: "pedidos",
    Connection = "CosmosDbConnection",
    LeaseContainerName = "leases-flujo",
    CreateLeaseContainerIfNotExists = true)]
IReadOnlyList<Pedido> cambios
```

El Change Feed te entrega batches de documentos que se han creado o modificado desde el último cursor. Por cada documento del batch hace dos comprobaciones:

```csharp
if (pedido.Estado != "nuevo") continue;                 // primera capa
if (!_tracker.TryMarcarFacturado(pedido.Id)) continue;  // segunda capa
```

**Por qué dos capas y no una sola:**

- La primera capa (`Estado != "nuevo"`) protege contra el caso "ya facturé esto y guardé el cambio". Si en la lógica de facturación cambiáramos `pedido.Estado = "facturado"` y lo escribiéramos de vuelta a Cosmos, el siguiente paso por el Change Feed lo vería con estado distinto y lo saltaría. Es una defensa basada en el estado del documento.
- La segunda capa (`TryMarcarFacturado`) protege contra el caso "el Change Feed me reentregó el mismo cambio antes de que yo haya podido escribir el cambio de estado". Es una defensa basada en un registro in-memory atomicista, exactamente como la idempotency store de S4.3.

Las dos capas son redundantes a propósito. Si solo tienes la primera, hay una ventana de carrera entre "recibo el documento" y "marco como facturado" que un Change Feed muy activo puede aprovechar. Si solo tienes la segunda, perdiste el documento si tu instancia se reinicia (el `ConcurrentDictionary` se vacía). Las dos juntas cubren los dos casos.

Y después del par de guardas, el cuerpo es directo: genera factura, escribe a blob y a queue, vuelve:

```csharp
return new ProcesarResult
{
    FacturaJson = _facturas.SerializarFactura(factura),
    MensajeCola = _facturas.SerializarMensaje(factura),
};
```

```csharp
public sealed class ProcesarResult
{
    [BlobOutput("facturas/{rand-guid}.json", Connection = "AzureWebJobsStorage")]
    public string? FacturaJson { get; set; }

    [QueueOutput("facturas-generadas", Connection = "AzureWebJobsStorage")]
    public string? MensajeCola { get; set; }
}
```

El binding `{rand-guid}` es una expresión que el runtime resuelve en runtime con un GUID nuevo cada invocación. No hay forma de poner el `pedido.Id` ahí porque el trigger del Change Feed no expone propiedades del documento como binding expressions (sí lo hacen los triggers de Blob o de Queue). Si necesitas que el blob lleve un nombre derivado del id del pedido, hay que escribirlo desde el código con un `BlobServiceClient` en lugar de con un output binding. Para esta práctica, `{rand-guid}` es suficiente; el `pedido.Id` viaja dentro del JSON.

### Paso 3 — `NotificarFacturaFunction` (QueueTrigger)

El paso 3 es simple. Lee el mensaje de la queue (un JSON con el id del pedido y los datos de la factura), lo deserializa, "notifica" (en este ejemplo, loguea — en producción enviaría un email), y registra en el tracker.

```csharp
[QueueTrigger("facturas-generadas", Connection = "AzureWebJobsStorage")]
string mensajeJson
```

La queue de Azure Storage trae su propio mecanismo de reintento: si la función lanza, el mensaje vuelve a estar visible tras el `visibilityTimeout` y se reintenta. Después de cinco fallos pasa a la **poison queue** (`facturas-generadas-poison`). En esta práctica no implementamos un consumer de la poison; se podría añadir trivialmente con otro `[QueueTrigger("facturas-generadas-poison")]`.

### El endpoint de inspección — `EstadoFunction`

Diez líneas que devuelven un snapshot agregado:

```csharp
return new OkObjectResult(new
{
    creados = _tracker.TotalCreados,
    facturados = _tracker.TotalFacturados,
    notificados = _tracker.TotalNotificados,
    pendientes = _tracker.TotalCreados - _tracker.TotalNotificados,
});
```

Cuando despliegues el flujo y mandes un pedido al endpoint POST, sabes que el flujo está sano si treinta segundos después `creados`, `facturados` y `notificados` son los tres iguales. Si no lo son, sabes en qué estación se atascó: si `creados=1, facturados=0`, problema entre Estación 1 y Estación 2 (probablemente el Change Feed no está disparando); si `facturados=1, notificados=0`, problema en Estación 3 (queue trigger no está consumiendo).

Es la pieza más útil del ejemplo para depurar en producción. Cuesta diez líneas y vale oro la primera vez que algo no funciona en Azure y necesitas saber qué.

---

## 6. La idempotencia, mirada despacio

El comentario "idempotencia en dos capas" del README es la frase clave de esta práctica. Vale la pena verla a ras de suelo:

**Primera capa — el `estado` del documento.** Es semántica del dominio. Un pedido "nuevo" se factura; un pedido "facturado" no se vuelve a facturar. Si añadiéramos una activity "actualizar estado tras facturar" que escribiera `pedido.Estado = "facturado"` de vuelta en Cosmos, el siguiente disparo del Change Feed sobre ese documento lo descartaría con la primera guarda. Esta práctica no implementa el cambio de estado de vuelta para mantener el ejemplo simple, pero conceptualmente está ahí.

**Segunda capa — el tracker in-memory con `TryAdd`.** Es la red de seguridad inmediata. Aunque el Change Feed te reentregue el mismo documento exactamente igual (mismo `Estado = "nuevo"`, mismo todo) en un margen de segundos, el `TryMarcarFacturado` devuelve `false` la segunda vez y la función salta. Es la lección de S3.5 (idempotency store con `TryAdd` atómico) trasladada a esta práctica.

¿Por qué no basta con una de las dos?

- Si solo tuvieras el `estado`: el cambio de estado de "nuevo" a "facturado" implica una escritura a Cosmos, que tarda decenas de milisegundos. En ese intervalo, el Change Feed puede reentregar el mismo documento si su cursor se rebobina (cosa que pasa al reiniciar la function host, por ejemplo). Tendrías dos facturas para el mismo pedido.
- Si solo tuvieras el tracker: cuando tu function host reinicia, el `ConcurrentDictionary` se queda vacío. El Change Feed reanuda desde el cursor anterior y te reenvía todos los documentos cuyo `_ts` es posterior al último checkpoint. Sin la guarda del estado, los facturas todos otra vez.

Las dos juntas cubren las dos ventanas. Es uno de los patrones más útiles que aprenderás en el módulo.

---

## 7. Cómo probarlo en local

Necesitas tres cosas corriendo en background:

- **Azurite** para Blob y Queue: `azurite --silent --location ./azurite-data`.
- **Cosmos emulator** en `https://localhost:8081` con la database `tienda` y el container `pedidos` (partition key `/clienteId`). El leases container `leases-flujo` lo crea el trigger solo al arrancar.
- **Storage Explorer** para mirar el container `facturas` y la queue `facturas-generadas` cuando empiecen a moverse cosas.

Una vez en marcha:

```bash
cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json
func start --csharp
```

Mandar un pedido:

```bash
curl -X POST http://localhost:7071/api/pedidos \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId":"c1",
    "clienteNombre":"Pedro",
    "items":[{"productoId":"p1","nombre":"Mouse","cantidad":2,"precioUnitario":15}]
  }'
```

Respuesta inmediata: `201 Created` con el id del pedido. Espera 10-20 segundos (el Change Feed no es instantáneo en el emulador):

```bash
curl http://localhost:7071/api/estado
# { "creados": 1, "facturados": 1, "notificados": 1, "pendientes": 0 }
```

Mira la queue `facturas-generadas` en Storage Explorer: estará vacía (el QueueTrigger ya consumió el mensaje). Mira el container `facturas`: hay un blob `{guid}.json` con la factura serializada.

**Para validar la idempotencia**: manda el mismo pedido con el mismo id otra vez. El estado debería seguir igual (sigue siendo `creados=2` porque cada POST cuenta como creación, pero `facturados=1` y `notificados=1` — el segundo nunca llegó a facturar). En producción, el cliente debería usar un idempotency-key en el header en vez de mandar el mismo body, pero la lógica del tracker es la que protege contra el reenvío del Change Feed.

> Yo no lanzo apps. Tú haces `func start --csharp` y `dotnet test`.

---

## 8. Los tests son la documentación viva

Los 21 tests cubren el flujo sin Cosmos ni Azure Storage reales, instanciando las funciones a mano e inyectando los servicios:

- **`PedidoFactoryTests`** (6) — validación del payload del POST: items vacíos, cantidad cero, precio negativo, cálculo del total con múltiples items.
- **`FacturaGeneratorTests`** (5) — IVA del 21% aplicado y redondeado correctamente, número de factura único, formato del JSON con camelCase, mensaje de cola con los campos esperados.
- **`FlujoFunctionsTests`** (10) — la pieza más densa. Cubre las cuatro funciones individualmente y luego el flujo completo:
  - `CrearPedido` con body válido devuelve 201 + el pedido en Cosmos.
  - `CrearPedido` con body inválido devuelve 400 y NO escribe en Cosmos.
  - `ProcesarNuevosPedidos` con un pedido en estado "nuevo" genera factura y mensaje.
  - `ProcesarNuevosPedidos` con el mismo pedido dos veces genera **una sola** factura (la prueba de idempotencia).
  - `ProcesarNuevosPedidos` con un pedido en estado distinto de "nuevo" lo salta.
  - `NotificarFactura` con un mensaje válido marca como notificado.
  - `NotificarFactura` con un mensaje malformado loguea error y no rompe.
  - `Estado` devuelve los contadores agregados correctamente.
  - Snapshot del flujo completo: tres invocaciones encadenadas reproducen el `creados=1, facturados=1, notificados=1` end-to-end.

El test de idempotencia es probablemente el más importante. Si alguien refactoriza el tracker o la función central y rompe la atomicidad del `TryMarcarFacturado`, este test salta — y te ahorra el incidente de "cobramos dos veces al cliente" en producción.

---

## 9. Lo que el ejemplo NO incluye y por qué

Cuatro cosas que un sistema real añadiría y que esta práctica deja fuera por motivos pedagógicos:

- **Poison queue handling**. El mensaje que falla cinco veces va a `facturas-generadas-poison`. En producción tendrías otra función consumiendo esa poison para alertar/cuarentenar. Es trivial añadirlo con otro `[QueueTrigger("facturas-generadas-poison")]` siguiendo el patrón de `ProcesarDeadLetterFunction` de S4.3.
- **Persistir el tracker en Cosmos/Table**. El `ConcurrentDictionary` se pierde al reiniciar. Para producción, el tracker sería un container de Cosmos con TTL (24h) o una tabla de Storage. La interfaz `IFlujoTracker` está pensada para ese cambio: implementación nueva, sin tocar las funciones.
- **Idempotency key en el POST**. El cliente podría mandar un header `Idempotency-Key: ...` para evitar duplicados en la entrada. Es la pieza simétrica del tracker, pero del lado del cliente. No es trivial añadirlo y suele aparecer en la siguiente iteración del sistema.
- **Application Insights con dashboard**. El `/api/estado` da una vista in-process. En producción quieres una métrica `customMetrics/pedidosFacturados` con un panel que muestre el ratio facturados/creados a lo largo del tiempo. Se cubre en M08.

Si el ejemplo intentara cubrir todo eso, la lección central —el patrón de tres pasos con idempotencia en dos capas— quedaría enterrada bajo capas operativas. Mejor dejarlo limpio y explicar qué falta.

---

## 10. Glosario breve

- **Change Feed**: log persistente de cambios en Cosmos DB. Cada actualización o inserción genera una entrada; el trigger consume el feed manteniendo un cursor. At-least-once por diseño.
- **Multi-output binding**: una función que devuelve un objeto con propiedades anotadas con distintos `[Output]` attributes. El runtime escribe cada propiedad en su destino correspondiente.
- **`{rand-guid}`**: binding expression que el runtime resuelve con un GUID nuevo en cada invocación. Útil cuando el nombre del blob/file/recurso no depende de datos del trigger.
- **Poison queue**: queue paralela donde Azure Storage Queue deposita mensajes que fallaron cinco veces. Equivalente a la dead-letter queue de Service Bus, pero más simple.
- **Lease container**: container de Cosmos que el trigger del Change Feed usa para mantener su cursor (el "qué documento procesé el último"). Lo crea solo si pones `CreateLeaseContainerIfNotExists = true`.
- **At-least-once**: garantía de "al menos una vez" en la entrega. Significa que un mismo evento **puede** entregarse más veces. La idempotencia no es opcional.

---

## 11. Cierre

Esta práctica es el "junté las piezas" del módulo. Tres triggers distintos colaborando, multi-output binding en el medio, idempotencia en dos capas, endpoint de inspección. Si tienes claro este patrón, puedes diseñar la mayoría de flujos event-driven de Azure Functions con confianza.

Lo siguiente es [`S4.P2 — Práctica Durable Hello World`](../S4.P2-practica-durable-hello-world/MANUAL.md), una práctica más corta y autocontenida para fijar el patrón de orchestrator + activities de S4.2 con un ejemplo mínimo y reproducible.
