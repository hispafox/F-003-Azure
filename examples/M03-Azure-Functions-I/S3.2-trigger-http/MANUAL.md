# Manual del alumno — S3.2 · Trigger HTTP: endpoints, autenticación y enrutamiento

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: estructura, mapeo a slides, comandos, despliegue por Portal. Este manual va antes: te cuenta qué cambia respecto al `Hello` de S3.1, qué se mantiene de M02 y cuál es el patrón que vas a copiar en cada submódulo siguiente del M03.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M03-S3.2](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.2-trigger-http-v4.md). CRUD completo sobre `/api/productos` con validación, middleware y Problem Details — la base sobre la que se construyen los triggers no-HTTP de los siguientes submódulos.

*Creado: 2026-05-20 12:02 +0200*

---

## 1. La idea en una frase

S3.1 te enseñó el skeleton mínimo: un endpoint HTTP, un test, un deploy. S3.2 lo lleva a un CRUD real con paginación, filtros, validación y middleware. Y la sorpresa pedagógica más importante: **el código de las funciones HTTP es casi idéntico al de una Minimal API de M02**. Mismos `HttpRequest` e `IActionResult`, mismas `ReadFromJsonAsync` y `OkObjectResult`, misma DI por constructor, misma validación con DataAnnotations.

Lo que cambia respecto a M02 son **tres cosas operativas**: el bootstrap (`FunctionsApplication.CreateBuilder` en vez de `WebApplication.CreateBuilder`), el modelo de hosting (Consumption vs plan continuo) y la forma de declarar el endpoint (atributo `[HttpTrigger]` en lugar de `MapGet`/`MapPost`). El resto del 90% del código es transferible literal entre los dos mundos. Esto importa porque te permite decidir hosting por patrón de tráfico **sin reescribir la lógica de negocio**.

---

## 2. El problema real que hay detrás

Un equipo dudaba si su nueva API REST para gestionar productos debía vivir en App Service o en Functions. El argumento a favor de App Service: "es una API HTTP, las APIs van en App Service". El argumento a favor de Functions: "el tráfico es esporádico, en Consumption sería gratis". El debate duró días — hasta que alguien probó migrar un controlador de prueba. Resultado: **dos horas de trabajo** para llevar el CRUD entero al Worker SDK 2.x, exactamente lo mismo funcionando. La decisión bajó de "cuál tecnología elegimos" a "cuál hosting nos sale mejor en factura".

Esa anécdota es S3.2 entero. La pregunta importante no es "Functions o ASP.NET" sino "**Consumption o plan continuo**, para este patrón de tráfico". Y resolver esa pregunta ya no requiere reescribir nada gracias al modelo ASP.NET Core en Worker 2.x.

Lo que entrega la práctica:

| Pieza | Para qué | Dónde |
| --- | --- | --- |
| **CRUD completo** GET list / GET by-id / POST / PUT / DELETE | Patrón estándar de API REST adaptado a Functions | [`ProductosFunctions.cs`](src/AzureFunctions.Demo/Functions/ProductosFunctions.cs) |
| **Niveles de autorización mezclados** | `/ping` Anonymous, CRUD bajo `Function` (key) | atributos `[HttpTrigger(AuthorizationLevel...)]` |
| **Validación + Problem Details RFC 7807** | 400 para JSON malformado, 422 para DataAnnotations, 404 estructurado | `Validator.TryValidateObject` + helpers de Problem Details |
| **Middleware con `IFunctionsWorkerMiddleware`** | CorrelationId + ExceptionHandling antes del pipeline | [`Middleware/`](src/AzureFunctions.Demo/Middleware/) |
| **DI por constructor + Options** | `IProductoService`, `IOptions<ProductosOptions>`, `ILogger<...>` | `Program.cs` + `ProductosFunctions(...)` |
| **Tests por instanciación directa** | 22 tests sin runtime de Functions, con helper para fabricar requests | `tests/` + `HttpRequestFactory` |

Veintidós tests cubriendo cada verbo, cada código de error, cada combinación de filtros. La cobertura es alta porque, al ser un patrón estándar, los tests son mecánicos: cinco minutos cada uno.

---

## 3. Por qué esto importa en tu stack

El descubrimiento práctico que conviene fijar: **los Function HTTP triggers en Worker SDK 2.x son prácticamente Minimal API con otro `Program.cs`**. Si miras una función:

```csharp
[Function(nameof(Crear))]
public async Task<IActionResult> Crear(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "productos")] HttpRequest req)
{
    var dto = await req.ReadFromJsonAsync<CrearProductoDto>();
    // ... validación ...
    var producto = _service.Crear(dto);
    return new CreatedAtActionResult("Obtener", "Productos", new { id = producto.Id }, producto);
}
```

Y la equivalente en Minimal API de M02:

```csharp
app.MapPost("/api/productos", async ([FromBody] CrearProductoDto dto, IProductoService service) =>
{
    // ... validación ...
    var producto = service.Crear(dto);
    return Results.Created($"/api/productos/{producto.Id}", producto);
});
```

Son la misma idea con tres diferencias sintácticas: el atributo `[HttpTrigger]` en lugar de `MapPost`, recibir `HttpRequest` y leer manualmente el body (en lugar del binding automático), y devolver `IActionResult` en lugar de `IResult`. El 90% del código (validación, llamada al servicio, manejo de errores) es idéntico. Esa equivalencia es la que permite mover trozos entre los dos mundos según convenga.

Y la decisión que tomas en cuanto al hosting no es trivial pero sí localizada:

| Patrón de tráfico | Hosting recomendado |
| --- | --- |
| API pública con tráfico continuo, latencia crítica | App Service (M02) |
| API interna con tráfico esporádico | Functions Consumption (M03) |
| Microservicios pequeños donde quieres pagar por uso | Functions Flex Consumption |
| Mismo código en local y CI sin Azure | da igual, los dos van bien |

---

## 4. El modelo mental: la misma cocina, dos formas de pagar el alquiler

Imagina dos formas de tener una cocina profesional para tu negocio. La primera, **alquilas un local con cocina permanente**: pagas alquiler mensual, está siempre montada, llegas y cocinas. La segunda, **alquilas una cocina compartida por horas**: pagas solo cuando cocinas, te apuntan en el calendario, llegas y cocinas (con 2-3 minutos de preparación si no había nadie antes).

En las dos cocinas **tu receta es la misma**. Los ingredientes, los pasos, el horno a la misma temperatura, el plato igual de bueno. Lo que cambia es **cuándo pagas y cuánto pagas**. Si cocinas todos los días, el local propio sale mejor. Si cocinas tres veces por semana, la cocina compartida es más barata.

App Service vs Functions Consumption es exactamente esa decisión, aplicada a código HTTP. Tu API REST es la receta. App Service es el local propio (paga el plan, está siempre listo, sin cold start). Functions Consumption es la cocina compartida (paga por uso, 1-3 segundos de cold start ocasional).

```
                       Tu receta (la API REST de productos)
                       │
            ┌──────────┴──────────┐
            │                     │
        App Service           Functions Consumption
        (local propio)        (cocina por horas)
        ~10-70 €/mes          0 € hasta 1M ejecuciones/mes
        Siempre encendido     Scale-to-zero, cold start 1-3 s
```

Tres frases para fijar el modelo:

- **El código es 90% el mismo.** Mismos DTOs, misma validación, misma DI. Lo que cambia es el atributo del endpoint y el bootstrap.
- **La decisión es de hosting, no de tecnología.** Tu lógica de negocio no decide entre App Service y Functions; tu **patrón de tráfico** sí.
- **Los niveles de autorización son la primera diferencia visible.** En App Service usabas un middleware de auth; aquí cada función declara su nivel en el atributo (Anonymous, Function, Admin). Más simple, menos potente, suficiente para muchos casos.

---

## 5. Los tres niveles de autorización HTTP

Functions tiene un modelo de autorización propio que conviene entender de entrada. El atributo `[HttpTrigger(AuthorizationLevel.X, ...)]` declara quién puede llamar:

| Nivel | Quién puede llamar | Cómo |
| --- | --- | --- |
| **`Anonymous`** | Cualquiera, sin autenticación | `GET /api/ping` directo |
| **`Function`** | Quien tenga la **function key** | `GET /api/productos?code=<key>` o header `x-functions-key` |
| **`Admin`** | Quien tenga la **host key (master)** | igual con la master key |

El ejemplo mezcla niveles deliberadamente: `/api/ping` es `Anonymous` (un health check público), todo el CRUD bajo `/api/productos` es `Function` (requiere key). Esa mezcla es el patrón típico de producción: endpoints públicos básicos + endpoints de datos protegidos con key.

> 🧠 **Function key vs autenticación seria.** El sistema de function keys está bien para APIs internas o protección básica, pero **no es autenticación seria**. La key es un string que va en la URL o en una cabecera; cualquiera con esa key tiene acceso completo al endpoint. Para producción con usuarios identificados, autorización fina o auditoría, hace falta **Entra ID / Easy Auth** (lo verás en M06). Aquí aprendes el patrón básico que cubre el 80% de los casos internos.

Y un detalle operativo: la function key la genera Azure automáticamente al crear la Function App. En el portal, *tu Function App → tu función → Get Function URL* te da la URL completa con la key. En CLI: `az functionapp keys list`. La key se rota con `az functionapp keys set`; cuando rotas, los clientes tienen que actualizarse.

---

## 6. Validación y Problem Details: dos códigos para dos problemas

El patrón de validación de `Crear` y `Actualizar` separa dos casos que mucha gente confunde:

```csharp
1. ReadFromJsonAsync<T>             → si falla por JSON malformado: 400 Bad Request
2. TryValidateObject(dto)            → si falla DataAnnotations:    422 Unprocessable Entity
3. service.Crear(dto)                → 201 Created con Location
```

**400 Bad Request** dice: "no entiendo lo que me has mandado". El JSON está roto, falta una llave, hay comas mal puestas. El cliente tiene un bug serializando.

**422 Unprocessable Entity** dice: "entiendo lo que me has mandado pero la semántica está mal". El JSON está bien, los campos son los esperados, pero `precio = -5` viola la regla `[Range(0, 999999)]`. El cliente sabe formar JSON, no conoce las reglas de negocio.

Los dos casos llegan con un cuerpo en formato **Problem Details RFC 7807** — el estándar HTTP para representar errores de forma estructurada. Algo así:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "El JSON está malformado",
  "traceId": "abc-123-def"
}
```

El `traceId` viene del middleware de correlación (sección 7) y vincula esa respuesta de error con los logs del servidor — útil cuando un usuario reporta un fallo y necesitas reconstruir qué pasó.

> 🧠 **400 vs 422 no es decoración.** Las APIs serias usan los dos códigos correctamente porque permiten a los clientes reaccionar de forma distinta. Un 400 normalmente es un bug del cliente que el desarrollador tiene que arreglar; un 422 es un error de input del usuario que la UI debe mostrar como mensaje validador ("el precio no puede ser negativo"). Si devuelves 400 para todo, los clientes tienen que parsear el `detail` con texto libre. Si distingues, hay reglas claras.

---

## 7. El middleware: `CorrelationId` antes que `ExceptionHandling`

El Worker SDK 2.x soporta middleware al estilo ASP.NET Core con la interfaz `IFunctionsWorkerMiddleware`. El ejemplo registra dos en orden estricto:

```csharp
builder.UseMiddleware<CorrelationIdMiddleware>();        // primero: genera el ID
builder.UseMiddleware<ExceptionHandlingMiddleware>();    // segundo: lo envuelve
```

**`CorrelationIdMiddleware`** se ejecuta primero. Lee el header `X-Correlation-Id` del request (si viene del cliente) o genera uno nuevo. Lo guarda en `context.Items` y lo añade como header de respuesta. Así cualquier excepción que ocurra después tiene un ID al que asociarse, y el cliente recibe ese mismo ID en la respuesta para incluirlo en su informe de bug.

**`ExceptionHandlingMiddleware`** se ejecuta después. Hace `try`/`catch` alrededor del `await next(context)`. Si captura una excepción, devuelve un Problem Details con `traceId = context.InvocationId`. **Importante**: solo lo hace si la función es HTTP (`req.HttpContext` existe). Para triggers no-HTTP (Timer, Blob), deja que la excepción suba — porque el runtime de Functions necesita verla para marcar la ejecución como fallida y aplicar el retry policy.

> 🎓 **El orden importa.** Si `ExceptionHandling` se registrara antes que `CorrelationId`, una excepción se capturaría antes de que existiera el ID, y la respuesta no podría incluirlo. Cuando registres tu propio middleware en otros proyectos, mantén la regla: **primero los que enriquecen contexto, después los que reaccionan a errores**.

Y un detalle del modelo: el middleware de Functions ve **`FunctionContext`** (no `HttpContext` directamente). Tienes que `await context.GetHttpRequestDataAsync()` o equivalente para acceder al request HTTP — sí está ahí, pero pasa por un nivel de indirección extra. Es una diferencia con ASP.NET Core que no es grave pero conviene tener presente.

---

## 8. Recorrido guiado

Lanza la Function App en local (sección 10) y abre [`api.http`](src/AzureFunctions.Demo/api.http) — trae diez peticiones listas que cubren todos los caminos.

| # | Petición | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `GET /api/ping` | `200 OK` con `pong` | El endpoint `Anonymous` — público, sin key. |
| 2 | `GET /api/productos` (sin key) | `401 Unauthorized` | El nivel `Function` exige key. |
| 3 | `GET /api/productos?code=<key>` | `200` lista vacía con header `X-Total-Count: 0` | El header de paginación estándar. |
| 4 | `POST /api/productos?code=<key>` con JSON válido | `201 Created`, `Location: /api/productos/{id}` | El happy path del create. |
| 5 | `GET /api/productos/{id}?code=<key>` | `200` con el producto creado | Read por ID. |
| 6 | `POST /api/productos?code=<key>` con JSON roto | `400 Bad Request` con Problem Details | JSON malformado (sección 6). |
| 7 | `POST /api/productos?code=<key>` con `precio: -5` | `422 Unprocessable Entity` con detalle de validación | DataAnnotations violada (sección 6). |
| 8 | `PUT /api/productos/9999?code=<key>` | `404 Not Found` con Problem Details | El path-id no existe. |
| 9 | `DELETE /api/productos/{id}?code=<key>` | `204 No Content` | Delete del happy path. |
| 10 | `GET /api/productos/{id}?code=<key>` (ya borrado) | `404 Not Found` | Confirma que el delete tuvo efecto. |

Un experimento que aporta: en el paso 3, añade `?porPagina=500` (un valor por encima del máximo configurado en `ProductosOptions:MaxPorPagina = 100`). La API responde con la lista pero el `X-Total-Count` y los items se calculan con `porPagina = 100` — el código hace clamp al máximo de los Options. Es el patrón estándar de "no fallar al cliente que pide algo razonable pero abusivo, simplemente acotarlo".

---

## 9. Tests y por qué hay un helper `HttpRequestFactory`

Veintidós tests, todos instanciando las funciones directamente (sin `WebApplicationFactory`). Para que eso sea ergonómico, hay un helper `HttpRequestFactory` que fabrica `HttpRequest` con body JSON, query strings o cuerpos malformados:

```csharp
var req = HttpRequestFactory.WithJsonBody(new CrearProductoDto("Lápiz", "papelería", 0.50m));
var result = await function.Crear(req);
Assert.IsType<CreatedAtActionResultBase>(result);
```

Sin el helper, fabricar un `HttpRequest` con `DefaultHttpContext` requiere unas líneas largas de boilerplate (configurar `Request.Body` con `MemoryStream`, setear `ContentType`, etc.). Con el helper, una línea por test.

**`ProductosListarTests`** (5) cubre los filtros y el header `X-Total-Count`. **`ProductosCrudTests`** (9) cubre cada verbo con su happy path y sus 404/422/400. **`InMemoryProductoServiceTests`** (4) son unit tests puros del servicio, sin tocar funciones — son los más rápidos. Y `HelloFunctionTests` (3) + `PingFunctionTests` (1) son heredados.

Sigue valiendo la advertencia de S3.1: **estos tests no ejercen el contenedor de DI**. Si cambias el constructor de `ProductosFunctions` para añadir un servicio nuevo, los tests tienen que pasártelo en el `new ProductosFunctions(...)`. Cruzar a mano el constructor contra `Program.cs` sigue siendo obligatorio.

---

## 10. Puesta en marcha, ejecución y pruebas

### 10.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y testear | Sí |
| Azure Functions Core Tools (`func`) | `func start` local | Recomendado |
| Azurite | emular Storage local | Sí |
| Extensión REST Client (VS Code) | abrir `api.http` | Recomendado |

### 10.2 Compilar y arrancar en local

```bash
cd examples/M03-Azure-Functions-I/S3.2-trigger-http
dotnet build AzureFunctions.Demo.slnx       # 0 errores

cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json

# Azurite en otra terminal:
azurite --silent

# Arrancar:
cd src/AzureFunctions.Demo
func start
# → http://localhost:7071/api/ping y /api/productos
```

En local **las function keys no se aplican** — todos los endpoints responden sin `?code=`. Las keys solo se exigen en Azure real.

### 10.3 Pasar los tests

```bash
dotnet test
```

Resultado: **22 pass · 0 fail**. Sin Azure, sin Docker, sin emulador.

### 10.4 Desplegar a Azure (resumen)

El detalle por Portal está en el [`README.md`](README.md). Pasos clave:

1. **RG + Storage + Function App** (igual que S3.1).
2. **App Settings**: `Productos__MaxPorPagina = 100`, `Productos__PorPaginaPorDefecto = 20`.
3. **Deploy** desde VS Code o `scripts/02-deploy.sh`.
4. **Obtener function key**: *Portal → tu Function App → Functions → tu función → Get Function URL*, o `az functionapp keys list`.
5. **Verificar** con `curl 'https://<func>.azurewebsites.net/api/productos?code=<key>'`.

### 10.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `401 Unauthorized` en endpoints CRUD | falta `?code=<key>` o header `x-functions-key` | obtén la key con `az functionapp keys list` y añádela |
| Local responde sin key, Azure exige key | `AuthorizationLevel.Function` solo se aplica en Azure | comportamiento esperado |
| El `X-Total-Count` no aparece en la respuesta | el cliente filtra ese header (CORS) | añade el header a `Access-Control-Expose-Headers` en CORS |
| 400 con Problem Details vacío | el JSON estaba muy malformado y `ReadFromJsonAsync` lanzó | el middleware sí captura, revisa el `traceId` en los logs |
| 422 sin detalle de campo concreto | DataAnnotations no genera mensaje por defecto en cada `[Required]` | añade `ErrorMessage = "..."` en los atributos |

### 10.6 Limpieza

`Portal → Resource groups → rg-curso-m03-s32 → Delete`. Borra Storage + Function App + plan Consumption.

---

## 11. Ideas para llevarte

Lo más útil de esta práctica es **fijar la equivalencia ASP.NET Core / Functions HTTP**. Cuando dudes "esto en App Service o en Functions", la decisión es de hosting (patrón de tráfico, presupuesto), no de tecnología. Tu código sigue siendo prácticamente el mismo entre los dos mundos.

Sobre los **niveles de autorización**: `Function` es suficiente para APIs internas o protecciones básicas. Para producción con usuarios reales, lo que vale es **Entra ID + Easy Auth** (M06). El sistema de function keys es práctico y rápido pero no es autenticación seria.

Sobre la **separación 400 vs 422**: úsala desde el primer endpoint que escribas. El cliente que recibe tus respuestas necesita poder distinguir "JSON roto" de "valor inválido"; si devuelves 400 para todo, los clientes parsean el `detail` con texto libre y los frontends muestran mensajes genéricos. Distinguir cuesta cinco minutos y ahorra fricción durante años.

Y sobre el **middleware**: la regla del orden — primero los que enriquecen contexto, después los que reaccionan a errores — es general en ASP.NET Core, no específica de Functions. Si tienes que añadir un middleware nuevo (auth custom, logging enriquecido, rate limiting básico), aplica esa regla y revisa la posición de los existentes.

---

## 12. Comprueba que lo has entendido

1. ¿Qué porcentaje del código de un Function HTTP trigger es transferible literal a una Minimal API de M02? ¿Cuáles son las tres diferencias concretas? *(sección 3)*
2. Tu cliente manda `{ "precio": "esto-no-es-un-número" }`. ¿La API responde 400 o 422? ¿Y si manda `{ "precio": -5 }`? *(sección 6)*
3. Configuras `Productos__MaxPorPagina=100`. El cliente pide `?porPagina=500`. ¿Qué pasa y por qué se hizo así? *(sección 8 experimento)*
4. En el middleware, ¿por qué `CorrelationIdMiddleware` se registra antes que `ExceptionHandlingMiddleware`? *(sección 7)*
5. Un endpoint Anonymous y otro Function en la misma Function App: ¿cómo se diferencia operativamente quién puede llamar a cada uno? *(sección 5)*
6. ¿Por qué los tests pasan sin function key aunque el atributo declare `AuthorizationLevel.Function`? *(sección 10.5)*

<details>
<summary>Respuestas</summary>

1. **Aproximadamente el 90%** del código es transferible literal. Las tres diferencias: **(a) bootstrap** — `FunctionsApplication.CreateBuilder` en lugar de `WebApplication.CreateBuilder`; **(b) declaración del endpoint** — atributo `[HttpTrigger]` en una clase con método, en lugar de `MapGet/MapPost` sobre `app`; **(c) tipo de respuesta y lectura del body** — `IActionResult` con `ReadFromJsonAsync` manual sobre `HttpRequest`, en lugar de binding automático con `IResult`. Todo lo demás (validación, DI, Options, lógica de negocio) es idéntico.
2. El primer caso (`"precio": "esto-no-es-un-número"`) es **400 Bad Request**: el JSON no se puede deserializar al tipo `decimal` esperado — fallo de sintaxis del cliente. El segundo (`"precio": -5`) es **422 Unprocessable Entity**: el JSON sí se deserializa correctamente, pero el valor viola `[Range(0, 999999)]` — error semántico que el cliente debe mostrar como validación de campo. Los dos códigos permiten a los frontends reaccionar de forma diferenciada (uno es bug del cliente, otro es mensaje al usuario).
3. La API **acepta la petición y hace clamp** a 100. Lo que ves en la respuesta son 100 items y `X-Total-Count` calculado consistente con `porPagina = 100`. Se hizo así porque es más amable con clientes que piden algo abusivo por error o desconocimiento — fallar con 400 sería pedante. La regla práctica: si el cliente pide un valor inválido pero razonable, acota; si pide algo claramente erróneo (texto donde va número), devuelve 400.
4. Porque `CorrelationId` **genera el ID** y lo guarda en `context.Items` / header de respuesta. Si una excepción ocurre después (en el handler del endpoint o más abajo), `ExceptionHandling` la captura y devuelve un Problem Details con `traceId = ese ID`. Si el orden fuera al revés, la excepción se capturaría antes de que existiera el ID, y la respuesta no tendría correlación. La regla general: **primero los middlewares que enriquecen contexto, después los que reaccionan a errores**.
5. Lo único que cambia es **si la URL requiere `?code=<function-key>`**. El endpoint `Anonymous` se llama directamente (`GET /api/ping`); el endpoint `Function` se llama con la key (`GET /api/productos?code=abc123...`). Sin la key correcta, el `Function` responde `401 Unauthorized` antes de llegar a tu código. La function key la genera Azure automáticamente al crear la Function App y se gestiona desde *Portal → tu Function → Function Keys* o con `az functionapp keys list/set`.
6. Porque **`AuthorizationLevel.Function` solo se aplica cuando la Function App corre en Azure**, no en local. Los tests instancian la función directamente con `new ProductosFunctions(...)` — el atributo `[HttpTrigger]` no se evalúa, solo se usa para registro en el runtime real. En local con `func start` lo mismo: las function keys están deshabilitadas en `local.settings.json` por defecto (`"AuthLevel": "Anonymous"` implícito). Cuando despliegas a Azure, el runtime evalúa el atributo y rechaza peticiones sin key. Es comportamiento esperado pero confunde la primera vez.

</details>

---

## 13. Hasta aquí

Vuelve a la imagen de la cocina compartida vs el local propio de la sección 4. Tu API REST es la misma receta; la decisión es de **dónde la sirves**, no de cómo la cocinas. Y entre las dos opciones de hosting puedes moverte sin reescribir prácticamente nada porque el modelo ASP.NET Core en Worker SDK 2.x es casi idéntico al de Minimal API.

Lo siguiente es [`S3.3 — Trigger Timer`](../S3.3-trigger-timer/MANUAL.md), donde el atributo `[HttpTrigger]` se sustituye por `[TimerTrigger("0 0 * * * *")]` y la función pasa de "endpoint HTTP" a "tarea programada cron". El resto del skeleton (Program.cs, middleware, DI, tests) sigue siendo el mismo. Verás que la lección de S3.1 ("cambia el trigger, no el patrón") se cumple en serio.
