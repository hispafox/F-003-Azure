# Manual del alumno — S3.6 · Bindings de entrada y salida

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: comandos por Portal, scripts `az`, lista exacta de connection strings y endpoints. Este manual va antes: te cuenta por qué este es el submódulo de cierre conceptual del M03, qué nuevo aporta sobre los triggers ya vistos y cuál es el **patrón MultiResponse** que conviene tener en la cabeza para el resto del curso.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M03-S3.6](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.6-bindings-entrada-salida-v4.md). Cinco funciones pequeñas demostrando combinaciones de bindings (Cosmos input por id y por SqlQuery, multi-output HTTP+Cosmos+Queue, pipeline Cosmos→Blob, queue trigger con anti-pattern aware).

*Creado: 2026-05-20 12:02 +0200*

---

## 1. La idea en una frase

Los **triggers** que viste en S3.2-S3.5 son la mitad del modelo de Functions: qué dispara la función. La otra mitad son los **bindings**: cómo lee datos del exterior sin abrir clientes (`[CosmosDBInput]`, `[BlobInput]`) y cómo escribe a otros servicios sin instanciar SDKs (`[CosmosDBOutput]`, `[QueueOutput]`, `[BlobOutput]`). Una función completa puede declarar un trigger + N inputs + M outputs **solo con atributos**, y Functions se encarga del resto: leer el documento de Cosmos, ejecutar tu código, escribir el blob, enviar el mensaje a la queue. Cero código de I/O en tu lógica de negocio.

Y la pieza más elegante del modelo: **MultiResponse**. Una sola función puede producir tres efectos a la vez — devolver un HTTP 201, escribir un documento en Cosmos y encolar un mensaje en Queue Storage — declarando un POCO con propiedades anotadas. Si la validación falla, las propiedades de output se quedan a `null` y los bindings **no se materializan**. Es validación fail-safe a nivel de declaración, no de código.

---

## 2. El problema real que hay detrás

Un equipo tenía un endpoint "crear pedido" que hacía tres cosas: guardar el pedido en Cosmos, encolar un mensaje para procesado downstream y devolver el HTTP 201 al cliente. El código original era unas 60 líneas — abrir `CosmosClient`, escribir el documento, manejar excepción, abrir `QueueServiceClient`, encolar, manejar excepción, devolver respuesta. Tres operaciones de I/O encadenadas con manejo de errores entrelazado.

La versión con MultiResponse: una función de 25 líneas. Declaras un POCO `CrearPedidoResult` con tres propiedades (`HttpResponse`, `PedidoCosmos`, `MensajeCola`), las llenas o las dejas null según valide, devuelves el POCO. Functions hace los tres outputs por ti. Y la garantía importante: si la validación falla y dejas `PedidoCosmos = null`, **el documento no se escribe en Cosmos**. No es un check "por si las moscas en runtime", es declarativo en el modelo.

Esa diferencia se nota:

| Pieza | Código tradicional (SDK) | MultiResponse |
| --- | --- | --- |
| Líneas | ~60 | ~25 |
| Try/catch por operación | Sí, cada I/O | No, Functions maneja |
| Acoplamiento al SDK | `CosmosClient`, `QueueServiceClient` inyectados | Atributos declarativos |
| Tests | Mock de los dos clientes | Verificar el shape del POCO |
| Fail-safe en validación | Manual (early-return) | Declarativo (`null` = no output) |

Lo que entrega el ejemplo:

| Función | Demuestra | Slides |
| --- | --- | --- |
| `GET /api/pedidos/{clienteId}/{id}` | `[CosmosDBInput]` por id (route placeholders) | 4 |
| `GET /api/clientes/{clienteId}/pedidos` | `[CosmosDBInput]` con `SqlQuery` y parámetros dinámicos | 4, 10 |
| `POST /api/pedidos` | **MultiResponse**: HTTP + Cosmos + Queue en una función | 6, 24 |
| `GET /api/exportar/{clienteId}/{id}` | Pipeline `[CosmosDBInput]` → `[BlobOutput]` con `{DateTime:yyyy-MM-dd}` | 7, 10, 16 |
| `ProcesarPedidoCola` | `[QueueTrigger]` con anti-pattern aware (string raw + try/catch + log) | 19, 21 |

---

## 3. Por qué esto importa en tu stack

Hasta aquí el patrón "Function = trigger + lógica" ha estado bien para enseñar el modelo. En proyectos reales, esa función pequeña que solo hacía una cosa **acaba teniendo que escribir en varios sitios**: cuando el cambio importa, suele importar en más de un sistema. Sin bindings, eso significa inyectar dos o tres SDKs, gestionar conexiones, escribir try/catch encadenados, lidiar con la ordenación de las operaciones (si Cosmos escribe pero Queue falla, ¿qué hacemos?).

Bindings cambian la conversación: cada output es declarativo y atómico desde tu punto de vista. Si los tres efectos son independientes (no necesitan ser transaccionales entre sí), MultiResponse hace exactamente lo que quieres. Si necesitan transaccionalidad (Cosmos y Queue tienen que ir juntos o ninguno), entonces es cuando sales del modelo declarativo y usas el SDK con un patrón de **outbox**: escribes en Cosmos en una transacción que incluye el evento, y otro consumidor lee el evento (Change Feed de S3.5).

Para 90% de los casos, MultiResponse + bindings son suficientes. Para el 10% restante (transaccionalidad estricta, paginación con `ContinuationToken`, batches transaccionales, lógica condicional sobre la escritura), inyectas el SDK y controlas manualmente. La regla mental: **empieza con bindings, migra a SDK cuando una función concreta crezca**.

---

## 4. El modelo mental: la mesa con tres salidas

Imagina una mesa de trabajo con tres salidas distintas: un buzón a la izquierda (Cosmos), una cinta transportadora a la derecha (Queue), una ranura encima (HTTP response). Cuando termines de procesar un pedido, **rellenas tres etiquetas** y las pones en las tres salidas. Si una etiqueta se queda en blanco, esa salida no entrega nada.

```
   Body JSON entrante
        │
        ▼
   ┌─────────────────────────────────┐
   │ CrearPedidoFunction             │
   │   Validar el DTO                │
   │   Si OK:                        │
   │     HttpResponse = 201 + pedido │
   │     PedidoCosmos  = Pedido(...)  │
   │     MensajeCola   = $"{id}"      │
   │   Si NO OK:                     │
   │     HttpResponse = 400 + error  │
   │     PedidoCosmos  = null         │
   │     MensajeCola   = null         │
   └────┬─────────────┬─────────────┬┘
        │             │             │
   ┌────▼───┐   ┌─────▼──────┐  ┌──▼─────────┐
   │  HTTP   │  │ [CosmosDB- │  │ [QueueOut- │
   │ Response│  │  Output]   │  │  put]      │
   │   201   │  │ pedidos/   │  │ pedidos-   │
   │   400   │  │            │  │ pendientes │
   └────────┘   └────────────┘  └────────────┘
```

Tres frases para fijar el modelo:

- **`null` en una propiedad de output significa "no escribas"**. Es la garantía fail-safe del modelo. Si tu validación falla y dejas `PedidoCosmos = null`, Cosmos no recibe nada. No hay efecto secundario accidental.
- **Las binding expressions usan placeholders**. `{id}` y `{clienteId}` vienen del route HTTP; `{DateTime:yyyy-MM-dd}` viene del binding expression del runtime; `{rand-guid}` genera un GUID. Te permiten construir paths dinámicos sin código de string interpolation en tu función.
- **Los bindings son declarativos pero limitados**. Para escenarios complejos (condicionales, paginación, batches), sales del modelo declarativo y usas el SDK. Los bindings no son una alternativa universal — son una capa elegante que cubre los casos comunes.

---

## 5. Input bindings: leer sin abrir cliente

Mira `GetPedidoByIdFunction.cs`:

```csharp
[Function(nameof(GetPedidoById))]
public IActionResult GetPedidoById(
    [HttpTrigger(AuthorizationLevel.Function, "get",
        Route = "pedidos/{clienteId}/{id}")] HttpRequest req,
    [CosmosDBInput(
        databaseName: "tienda",
        containerName: "pedidos",
        Connection = "CosmosDbConnection",
        Id = "{id}",
        PartitionKey = "{clienteId}")] Pedido? pedido)
{
    return pedido is null
        ? new NotFoundResult()
        : new OkObjectResult(pedido);
}
```

El atributo `[CosmosDBInput]` declara: "antes de llamar a mi función, lee el documento con ese `Id` y esa `PartitionKey` de Cosmos y pásamelo como parámetro". Si el documento existe, llega como `Pedido pedido`. Si no existe, llega como `null`. **Cero `CosmosClient`, cero `ReadItemAsync`, cero try/catch de excepciones 404**.

Los `{id}` y `{clienteId}` son binding expressions: vienen del route del HTTP trigger. Functions hace match entre `Route = "pedidos/{clienteId}/{id}"` y la URL real, captura los segmentos, y los inyecta en los placeholders del input. Es exactamente lo mismo que harías a mano con string interpolation, pero declarativo.

Y la otra variante en `GetPedidosPorClienteFunction.cs`:

```csharp
[CosmosDBInput(
    SqlQuery = "SELECT * FROM c WHERE c.clienteId = {clienteId} ORDER BY c.fechaCreacion DESC",
    ...)] IEnumerable<Pedido> pedidos
```

`SqlQuery` te permite consultas más expresivas. Los `{...}` siguen sustituyéndose por valores del route. Útil para listados filtrados, ordenaciones, agregaciones simples (las complejas mejor con SDK).

> 🧠 **El input binding ejecuta la query antes de tu función**. Eso significa que si tu lógica decide no usar el resultado, **igualmente pagaste las RUs**. Para casos donde la lectura depende de validar el input primero, conviene inyectar `CosmosClient` por DI y leer condicionalmente. La regla: bindings son ergonómicos cuando siempre vas a usar el dato; SDK cuando hay condicionalidad.

---

## 6. MultiResponse: la pieza más elegante del modelo

Mira `CrearPedidoFunction.cs` y su tipo de retorno `CrearPedidoResult`:

```csharp
public sealed class CrearPedidoResult
{
    [HttpResult]
    public IActionResult HttpResponse { get; init; } = null!;

    [CosmosDBOutput(
        databaseName: "tienda",
        containerName: "pedidos",
        Connection = "CosmosDbConnection")]
    public Pedido? PedidoCosmos { get; init; }

    [QueueOutput("pedidos-pendientes")]
    public string? MensajeCola { get; init; }
}
```

Tres propiedades, tres atributos, tres efectos distintos en una sola función. El handler simplemente construye el POCO:

```csharp
public CrearPedidoResult Crear(...)
{
    var dto = await req.ReadFromJsonAsync<CrearPedidoDto>();
    var (valido, errores, pedido) = handler.ValidarYConstruir(dto);

    if (!valido)
        return new CrearPedidoResult { HttpResponse = new BadRequestObjectResult(errores) };
        // PedidoCosmos y MensajeCola se quedan en null → no se materializan

    return new CrearPedidoResult
    {
        HttpResponse = new CreatedAtActionResult(...),
        PedidoCosmos = pedido,
        MensajeCola = pedido.Id
    };
}
```

La sintaxis es C# normal — devuelves un objeto. Functions inspecciona las propiedades, encuentra los atributos `[CosmosDBOutput]` y `[QueueOutput]`, y ejecuta los outputs **después** de tu función. Si las propiedades son `null`, los outputs **se saltan**. No hay try/catch alrededor de "y si Cosmos falla pero Queue ya escribió" — Functions garantiza que los outputs son consistentes con el POCO final.

> 🧠 **La fail-safe del null**. Es la propiedad que más cambia el código. En un modelo tradicional, "no escribir en Cosmos si la validación falla" requiere acordarse de no llamar a `CosmosClient` — un olvido es un bug. En MultiResponse, el modelo declarativo lo garantiza: validas, decides qué outputs llenar, y los que dejas null no se ejecutan. Es **imposible olvidarse**: literalmente no escribes el código de "no escribir". Esa garantía estructural es lo que más valor tiene del patrón.

Y un detalle importante: **los outputs son independientes entre sí, no transaccionales**. Si Cosmos acepta el write pero Queue falla, el documento queda en Cosmos sin mensaje en cola. Functions no hace rollback. Si necesitas transaccionalidad estricta, el patrón correcto es: escribir solo en Cosmos en una transacción que incluya el evento como documento aparte, y procesar ese evento via Change Feed (S3.5). MultiResponse vale cuando los efectos son **independientes y la inconsistencia ocasional es tolerable**.

---

## 7. Binding expressions: paths dinámicos sin código

`ExportarPedidoFunction.cs` combina un input de Cosmos con un output a Blob, donde el path del blob se construye dinámicamente:

```csharp
[BlobOutput(
    "exports/{DateTime:yyyy-MM-dd}/pedido-{clienteId}-{id}.json",
    Connection = "AzureWebJobsStorage")]
```

Tres tokens:

- **`{DateTime:yyyy-MM-dd}`** — el runtime lo sustituye por la fecha actual con ese formato. `2026-05-20` por ejemplo. Útil para organizar exports por día.
- **`{clienteId}` y `{id}`** — vienen del route HTTP del trigger. Permite que cada export tenga un nombre único basado en el pedido.

El blob final acaba siendo algo como `exports/2026-05-20/pedido-cli-A-ped-001.json`. **Cero código de construcción del path** — todo viene de la expresión.

Otros tokens útiles disponibles en binding expressions:

| Token | Resultado |
| --- | --- |
| `{DateTime}` | Timestamp actual ISO 8601 |
| `{DateTime:yyyy-MM-dd}` | Solo la fecha con formato |
| `{rand-guid}` | GUID aleatorio |
| `{queueTrigger}` | El mensaje del queue trigger (cuando aplica) |
| `{ClientId}` (custom) | Propiedad de tu DTO si está bindeado |

Lo importante operativo: si tu container `exports/` empieza a llenarse, **ordenado por día está bien**. Si tu container `imports/` tuviera todos los archivos sueltos, navegarlo en un año sería insufrible. Las expresiones de fecha son una de esas decisiones pequeñas que escalan bien.

---

## 8. Queue trigger con anti-pattern aware

`ProcesarPedidoColaFunction.cs` consume mensajes de la cola — el otro lado de la moneda del `[QueueOutput]` que vimos en el MultiResponse:

```csharp
[Function(nameof(ProcesarPedidoCola))]
public void Procesar(
    [QueueTrigger("pedidos-pendientes", Connection = "AzureWebJobsStorage")]
    string mensajeRaw,
    ILogger<ProcesarPedidoColaFunction> logger)
{
    if (string.IsNullOrWhiteSpace(mensajeRaw))
    {
        logger.LogWarning("Mensaje vacío descartado");
        return;
    }

    try
    {
        var pedidoId = mensajeRaw.Trim();
        logger.LogInformation("Procesando pedido {PedidoId}", pedidoId);
        // ... lógica de procesado ...
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error procesando mensaje: {MensajeRaw}", mensajeRaw);
        throw; // ← propagar para que Functions aplique poison queue
    }
}
```

Tres decisiones aquí merecen comentario porque van contra el "tutorial naive":

- **Recibir `string` raw, no tipos fuertes**. La tentación es declarar `[QueueTrigger(...)] CrearPedidoDto dto` para que Functions deserialize automáticamente. **Si el mensaje no parsea**, Functions lanza una excepción **antes de llegar a tu código** — y el log dice algo poco útil. Recibiendo `string`, deserializas tú con try/catch y **puedes loggear el payload exacto** para diagnosticar.
- **Try/catch explícito**. Cualquier excepción no controlada hace que Functions reintente automáticamente (por defecto 5 veces, configurable en `host.json`). Después de los retries, el mensaje va a **poison queue** (`pedidos-pendientes-poison`). El try/catch te permite loggear contexto (correlationId, payload) **antes** del `throw` que dispara el retry. Sin él, tienes el stack trace pero no sabes qué payload causó el problema.
- **`throw` al final del catch**. Después de loggear, **propagas la excepción**. Functions lo necesita para marcar la invocación como fallida y aplicar la política de retry/poison queue. Si "tragas" la excepción (`catch { return; }`), el mensaje se considera procesado OK y desaparece — incluso si tu lógica no terminó. Es un anti-pattern silencioso que pierde datos.

> 🧠 **Poison queue: el último recurso**. Tras N retries fallidos, Functions mueve el mensaje a `<queue>-poison`. **Tú tienes que monitorizarla** — Azure no te avisa automáticamente. La regla operativa: configura una alerta de "longitud > 0" en la poison queue. Si llega un mensaje ahí, algo pasó que merece atención humana. Sin esa alerta, los mensajes poison se acumulan en silencio.

---

## 9. Recorrido guiado

Lanza la app local (sección 11) y abre [`api.http`](src/AzureFunctions.Demo/api.http) con la extensión REST Client.

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `POST /api/pedidos` con body válido (`clienteId`, `total > 0`, `notas` < 200 chars) | `201 Created` + Cosmos tiene el documento + Queue tiene un mensaje | MultiResponse al completo (sección 6). |
| 2 | `POST /api/pedidos` con `total: -5` | `400 Bad Request` con detalle de validación; Cosmos sin documento; Queue sin mensaje | Fail-safe del `null`: las validaciones fallidas no escriben outputs. |
| 3 | `GET /api/pedidos/{clienteId}/{id}` con el id del paso 1 | `200 OK` con el pedido | Input binding por id + partitionKey (sección 5). |
| 4 | `GET /api/pedidos/{clienteId}/no-existe` | `404 Not Found` | El input binding entrega `null` cuando el documento no existe — el handler responde 404 sin try/catch. |
| 5 | `GET /api/clientes/{clienteId}/pedidos` | `200 OK` con la lista de pedidos del cliente | Input binding por SqlQuery con placeholder dinámico. |
| 6 | `GET /api/exportar/{clienteId}/{id}` | `200 OK` con el path del blob; en Storage Explorer aparece `exports/2026-05-20/pedido-...-....json` | Pipeline Cosmos→Blob con binding expression de fecha (sección 7). |
| 7 | Espera unos segundos tras el paso 1 | log "Procesando pedido {id}" en la consola | `ProcesarPedidoCola` consume el mensaje encolado en el paso 1. |
| 8 | Inserta un mensaje **JSON malformado** directamente en la cola | log "Error procesando mensaje: {payload}" + retry hasta poison queue | Anti-pattern aware en acción (sección 8). |

Un experimento útil: ejecuta el paso 1 con `total: 0`. El validador rechaza, ves un `400`. Comprueba Cosmos y Queue: **vacíos**. Esa garantía estructural (nada se escribió porque las propiedades quedaron null) es la diferencia entre MultiResponse y un código tradicional con `if/early-return` que es fácil de olvidar.

Y un experimento más operacional: en el paso 8, **vigila también la queue principal** (`pedidos-pendientes`) en Storage Explorer. Verás el mensaje malformado aparecer, "desaparecer" cuando Functions lo coge, "volver" tras el fallo, repetirse 5 veces. Después del quinto retry, aparece en `pedidos-pendientes-poison`. Esa transición se ve en directo y enseña el ciclo de retries de Queue Storage.

---

## 10. Cuándo NO usar bindings

Los bindings son brillantes para casos comunes pero **no son universales**. Cuatro escenarios donde inyectar el SDK por DI es preferible:

- **Operaciones condicionales complejas**: "upsert si la versión es mayor, ignora si es menor, escribe en otra collection si X". Bindings asumen "lo que devuelves se escribe"; lógica condicional sobre la escritura no encaja.
- **Paginación con `ContinuationToken`**: cuando una query devuelve más de lo que cabe en una llamada y necesitas iterar páginas. Input bindings cargan todo o nada.
- **Batches transaccionales**: `Container.CreateTransactionalBatch()` para escribir varios documentos atómicamente. El binding equivalente no existe.
- **Tests unitarios con mocks del cliente**: si quieres testear la función mockeando `CosmosClient` para simular fallos específicos (timeout, throttling, conflicto de versión), tener el cliente inyectado es más fácil que simular el binding.

La regla práctica: **empieza con bindings, migra a SDK cuando una función concreta crezca**. Si el 90% de tus funciones siguen siendo declarativas y el 10% complejas tienen SDK inyectado, estás en el balance correcto. Si todas tus funciones inyectan SDKs, probablemente estás perdiendo la ergonomía del modelo de Functions.

---

## 11. Puesta en marcha, ejecución y pruebas

### 11.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y testear | Sí |
| Azure Functions Core Tools (`func`) | `func start` local | Recomendado |
| Azurite | emular Storage local (queue + blob) | Sí |
| Emulador de Cosmos (Docker) | input/output bindings de Cosmos en local | Recomendado |

### 11.2 Compilar y arrancar en local

```bash
cd examples/M03-Azure-Functions-I/S3.6-bindings-entrada-salida
dotnet build AzureFunctions.Demo.slnx       # 0 errores

cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json

# Azurite + emulador de Cosmos en terminales separadas
azurite --silent
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest

# Crear database y container en el emulador (Data Explorer del emulador):
#   tienda > pedidos (PK /clienteId)
# La queue 'pedidos-pendientes' y el container 'exports' los crea Azurite al primer uso

cd src/AzureFunctions.Demo
func start
```

### 11.3 Pasar los tests

```bash
dotnet test
```

Resultado: **27 pass · 0 fail**. Sin Azure, sin emulador (los tests instancian las funciones y verifican el shape del `CrearPedidoResult` y otros POCOs).

### 11.4 Desplegar a Azure (resumen)

El detalle por Portal está en el [`README.md`](README.md). Pasos clave:

1. **RG + Storage Account** (crear las queues `pedidos-pendientes` y el container `exports` manualmente desde *Queues* y *Containers* del storage).
2. **Cosmos DB serverless** con `tienda/pedidos` (PK `/clienteId`).
3. **Function App** Consumption Linux .NET 10 isolated. **Importante**: configurar como `AzureWebJobsStorage` el mismo Storage Account del paso 1, así los outputs de Queue y Blob apuntan al sitio correcto.
4. **App Setting** `CosmosDbConnection` con la connection string de Cosmos.
5. **Deploy** desde VS Code.
6. **Verificar** con el flujo del `api.http`.

### 11.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `Unable to resolve service for type IPedidosHandler` | falta `AddSingleton<IPedidosHandler, PedidosHandler>()` en Program.cs | regla del HANDOFF: cruzar constructores con registros |
| El POST funciona pero no aparece nada en la queue | confusión entre el storage de runtime y el de outputs | revisa que `AzureWebJobsStorage` y el `Connection` del `[QueueOutput]` apuntan al mismo storage |
| El blob se escribe pero el path tiene literal `{DateTime:yyyy-MM-dd}` | el binding expression no se interpretó (versión vieja del runtime?) | verifica que estás en Worker 2.x; las expressions funcionan en 1.x con sintaxis ligeramente distinta |
| La validación falla pero el documento se escribe igual | olvidaste poner `PedidoCosmos = null` en el caso de error | mira el código del handler — la fail-safe es solo si dejas null explícitamente |
| Poison queue acumula mensajes | falta alerta o monitorización | añade alerta en *Azure Monitor* sobre `length > 0` de la poison queue |

### 11.6 Limpieza

`Portal → Resource groups → rg-curso-m03-s36 → Delete`.

---

## 12. Ideas para llevarte

Lo más útil de esta práctica es **adoptar el patrón MultiResponse como tu modelo por defecto** cuando una función tiene más de un efecto. El código es más corto, más declarativo y la fail-safe del `null` elimina una clase de bugs por olvido. Cuando llegues a un proyecto real con Functions, este patrón es el que te diferencia de quien escribe funciones de 100 líneas con tres clientes inyectados.

Sobre **binding expressions con fecha**: úsalas para organizar outputs en containers. `exports/{DateTime:yyyy-MM-dd}/...` o `logs/{DateTime:yyyy}/{DateTime:MM}/...` son inversiones de cinco segundos que escalan bien. Sin ellas, en un año tu container es un caos.

Sobre **queue triggers anti-pattern aware**: la combinación "string raw + try/catch + log del payload + throw" es la receta correcta. La tentación de declarar el parámetro como tipo fuerte para "que Functions deserialice" se paga la primera vez que un mensaje malformado pasa por ahí y no tienes ni idea de qué payload era. Mejor un poco más de código y mucha mejor observabilidad.

Y sobre **cuándo no usar bindings**: empieza siempre con bindings. La migración a SDK es trivial cuando una función concreta crece (10-20 líneas extras); empezar todo con SDK te hace perder la ergonomía del modelo de Functions. Si tu equipo cae en "inyectamos todo, los bindings son magia que no controlamos", revisa.

---

## 13. Comprueba que lo has entendido

1. ¿Por qué `[CosmosDBInput]` con `null` en el parámetro NO causa NullReferenceException pero ahorra el `try/catch` de "documento no existe"? *(sección 5)*
2. En `CrearPedidoResult`, dejas `PedidoCosmos = null` por una validación fallida pero **olvidas** dejar `MensajeCola = null`. ¿Qué pasa? *(sección 6)*
3. `BlobOutput("exports/{DateTime:yyyy-MM-dd}/{rand-guid}.json")`. Si la función se ejecuta dos veces para el mismo input, ¿el blob es el mismo? *(sección 7)*
4. Un mensaje JSON malformado llega a `pedidos-pendientes`. Tu `[QueueTrigger]` recibe `CrearPedidoDto dto` directamente. ¿Qué ves en los logs y por qué `string mensajeRaw` es mejor? *(sección 8)*
5. Tu función necesita escribir en Cosmos **atómicamente con** un evento en otra collection (transaccional). ¿MultiResponse o SDK? ¿Por qué? *(secciones 6, 10)*
6. La poison queue `pedidos-pendientes-poison` lleva una semana acumulando mensajes. ¿Por qué Azure no te avisó y cuál es la regla operativa? *(sección 8)*

<details>
<summary>Respuestas</summary>

1. Porque **declaras el parámetro como `Pedido?` (nullable)**. Cuando el documento existe, Functions lo deserializa y te lo pasa. Cuando no existe, te pasa `null`. Tu código simplemente revisa con `pedido is null ? 404 : 200`. No hay try/catch porque Functions captura el "no existe" como `null` en lugar de propagar una `CosmosException` 404. Es ergonomía pura: el código es declarativo, no hay manejo de excepciones para casos esperados.
2. **El mensaje se encola igual**, aunque la validación haya fallado y el HTTP haya respondido 400. La fail-safe del `null` solo aplica si la propiedad **es explícitamente null**. Si la dejaste con un valor (porque te olvidaste de ponerla a null en el path de error), Functions ve un valor y ejecuta el output. Resultado: cliente recibe 400 pensando que nada pasó, mientras que el mensaje se procesa downstream con un ID inválido. Es un bug por olvido sutil. La defensa: tener un único path de retorno con todo a null por defecto y rellenar solo los que aplican (estilo `early return` con el POCO ya inicializado).
3. **No, son blobs distintos** porque `{rand-guid}` genera un GUID **nuevo en cada ejecución**. Cada llamada produce un blob diferente. Si quisieras idempotencia (mismo input = mismo blob), el binding expression tendría que usar **valores del input estables** (por ejemplo `{id}` del route), no `{rand-guid}`. Esto es importante en contextos at-least-once: si la función se ejecuta dos veces para el mismo pedido, con `{rand-guid}` tienes dos blobs; con `{id}` tienes el mismo blob (upsert idempotente).
4. **Functions lanza una excepción de deserialización antes de llegar a tu código**. El log dice algo como "Failed to deserialize message to type CrearPedidoDto" con stack trace, **pero no logueaba el payload** — el mensaje malformado se va a poison queue después de 5 retries fallidos sin que tengas idea de qué texto era. Con `string mensajeRaw`, tú deserializas con `try`, y en el `catch` logueas el `mensajeRaw` exacto **antes** del `throw`. Diferencia operativa: con string raw tienes "el payload exacto que causó el bug"; con tipo fuerte tienes "algo pasó al deserializar". El primero es accionable, el segundo no.
5. **SDK con patrón outbox**, no MultiResponse. MultiResponse **no es transaccional**: si Cosmos acepta el write pero la cola falla, te quedas inconsistente. Para transaccionalidad estricta entre dos efectos, el patrón correcto es escribir **ambos** en Cosmos en una transacción (el pedido + un documento "evento") usando `TransactionalBatch`. Después, otro consumidor lee el evento via Change Feed (S3.5) y produce el efecto secundario. Ese patrón se llama outbox y garantiza consistencia. MultiResponse sirve cuando los efectos son **independientes y la inconsistencia ocasional es tolerable** (típico: notificación que se puede perder ocasionalmente).
6. Porque **Azure no monitoriza tus poison queues por ti**. Es responsabilidad tuya configurar la alerta. La regla operativa: para cada queue de tu sistema, **configurar una alerta en Azure Monitor sobre la métrica `QueueMessageCount` o `MessageCount` de la poison queue asociada**: si la longitud > 0 durante más de X minutos, alerta a tu Action Group. Sin esa alerta, los mensajes poison se acumulan en silencio y descubres el problema cuando el cliente reporta que "lleva una semana sin recibir su confirmación". El umbral de alerta razonable: 1 mensaje. Cualquier mensaje en poison queue **merece atención humana**.

</details>

---

## 14. Hasta aquí

Vuelve a la imagen de la mesa con tres salidas de la sección 4. Cuando termines de procesar un input, **rellenas etiquetas** y las pones donde corresponda. Si una etiqueta se queda en blanco, esa salida no entrega nada. Esa mecánica simple cubre el 90% de los casos donde una función tiene varios efectos.

Con S3.6 cierras la parte conceptual del módulo M03. Has visto los cuatro triggers principales (HTTP, Timer, Blob, Cosmos), el catálogo de bindings (input y output) y el patrón MultiResponse. Lo siguiente son las **prácticas integradoras**: [`S3.P`](../S3.P-practica-4-triggers/MANUAL.md) consolida los 4 triggers en un único proyecto, y [`S3.P2`](../S3.P2-practica-http-crud-memoria/MANUAL.md) hace una práctica corta enfocada en HTTP CRUD desde cero. Lo siguiente del curso son **M04 Functions II** (Durable Functions, retry policies dedicadas, observabilidad serverless) y los temas posteriores donde verás Functions como pieza de arquitecturas más grandes.
