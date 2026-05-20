# Manual del alumno — S4.5 · Testing y depuración

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica con el catálogo de patrones de testing consolidado de todo M03 y M04. Este manual va antes: te cuenta por qué la regla "la lógica vive en servicios, la función es pegamento" es la única que importa, qué cubre cada capa de la pirámide y por qué los tests bien hechos no garantizan que la app arranque.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M04-S4.5](../../../doc/M04-Azure-Functions-II/v4-actual/M04-S4.5-testing-depuracion-v4.md). Veinticuatro tests sobre dos funciones, distribuidos en las tres capas clásicas (unit / function / integration).

*Creado: 2026-05-20 15:10 +0200*

---

## 1. La idea en una frase

Una función bien diseñada no se testea con muchas herramientas: se testea con pocas, porque la mayor parte del código que merece la pena probar **no vive en la función**, vive en los servicios que la función orquesta. El descuento se calcula en un `DescuentoCalculator`; la función solo deserializa, llama al servicio y formatea la respuesta. Ese cambio de organización es lo que convierte un proyecto de Functions difícil de testear en uno trivial de testear.

Las tres capas de la pirámide caen en su sitio sin esfuerzo cuando la regla anterior se aplica: los servicios se prueban con unit-tests rápidos (la mayoría), las funciones se prueban con un test corto que verifica el wiring (pocos), y el roundtrip real contra Azure se prueba con un integration-test opcional (uno o dos, marcados como skippable).

---

## 2. El problema real que hay detrás

Un equipo arrancó un sistema de Functions sin disciplina de capas. Todo en la función: deserialización, validación, lógica de descuento, llamada a Service Bus, llamada a Cosmos, formato de la respuesta. Cuando llegó la hora de testear, montaron un proyecto de tests que ejecutaba el runtime de Functions con `WebApplicationFactory`, levantaba el host completo, y comprobaba el endpoint con `HttpClient`. Cada test tardaba dos o tres segundos en arrancar, y para ejecutar la suite había que tener Azurite corriendo, Cosmos emulator en marcha y un mock de Service Bus configurado.

El resultado: una suite de 40 tests que tardaba seis minutos en ejecutarse, fallaba en CI por timeouts intermitentes del emulador, y cubría poca lógica porque cambiar el cálculo del descuento exigía levantar el ecosistema entero solo para validar una multiplicación.

La reescritura no inventó nada nuevo. Movió el cálculo de descuento a `DescuentoCalculator`, el procesamiento de CSV a `CsvResumenService`, la limpieza programada a `LimpiezaService`. Las funciones quedaron como cuatro líneas cada una: deserializa, valida, llama al servicio, devuelve resultado. Los tests siguieron a esa reorganización:

- `DescuentoCalculator` se probó con un `[Theory]` de ocho `[InlineData]` cubriendo cada escalón del cálculo. Cada test tarda dos milisegundos.
- `PedidosApi` (la función) se probó con NSubstitute mockeando `IDescuentoCalculator`. Cuatro tests que verifican "deserializa OK", "body inválido → 400", "id vacío → 400", "el servicio se invoca con los argumentos correctos". Sin Azure, sin runtime de Functions.
- El roundtrip real con Azurite se mantuvo, pero como **un solo test marcado con `SkippableFact`** que se ejecuta si Docker está disponible y se salta limpiamente si no.

La suite pasó de seis minutos a cinco segundos. Y la cobertura de la lógica de negocio subió, porque añadir un escalón al descuento ya no requería levantar nada.

---

## 3. Por qué esto importa en tu stack

Si alguna vez has escrito tests que tardan tanto en arrancar como en correr, sabes el coste real: dejas de añadir tests. La barrera psicológica de "voy a tardar diez minutos en validar un cambio de dos líneas" es lo que mata a las suites lentas. Cuando los tests son rápidos, los escribes; cuando los escribes, descubres bugs antes de subirlos; cuando descubres bugs antes de subirlos, gastas menos noches debuggeando en producción.

La pirámide de tests no es una construcción académica: es la forma natural de organizar las pruebas cuando tu código está bien estructurado por capas. La función no merece muchos tests porque hace muy poco. El servicio merece muchos porque hace casi todo. El roundtrip merece uno o dos porque solo valida que el cableado funciona contra una infraestructura real.

Y luego está la otra cara: los tests bien hechos te dan una sensación falsa de seguridad si la app no arranca. Toda esa pirámide se viene abajo si `Program.cs` tiene un servicio inyectado pero no registrado. Esa es la lección dura del catálogo del README, y conviene tenerla muy presente desde el día uno.

---

## 4. La analogía vertebradora: el chef y el ayudante de cocina

Imagina una cocina profesional. El chef ejecutivo —el que diseña el plato, decide la cantidad de sal, ajusta el escalón de cocción— es el **servicio** (`DescuentoCalculator`). El que recibe la comanda del camarero, lee el papel, llama al chef gritando "¡un cordero al horno!" y luego pone el plato en la bandeja para que el camarero lo lleve es el **ayudante** (la función `PedidosApi`).

Cuando inspeccionas si la cocina trabaja bien, no le haces preguntas al ayudante. Le haces preguntas al chef:

- ¿Qué pasa si el pedido es de 99 €? El chef responde "0% de descuento". (Unit test).
- ¿Y si es de 500 €? "10%". (Unit test).
- ¿Y si me da un total negativo? "Te tiro un `ArgumentOutOfRangeException`". (Unit test).

Solo cuando quieres verificar que el ayudante hace su trabajo —que sabe leer la comanda, gritar al chef, formatear el plato— le haces un test al ayudante. Y en ese test, **el chef es un actor**: NSubstitute le pone un `IDescuentoCalculator` falso que devuelve siempre lo mismo. Lo que verificas es el cableado, no la cocina.

Y para que quede la duda del último tramo —"¿realmente se conectan el ayudante y el chef cuando todo está en su sitio?"—, hay **un test que abre la cocina entera**: arranca Azurite con Docker, sube un blob, valida que la función lo procesa. Solo uno. Y si Docker no está, se salta sin fallar.

Esa es la pirámide: pocas pruebas al ayudante, muchas al chef, una a la cocina entera. Y siempre la regla detrás de todo: si te encuentras escribiendo dos tests parecidos contra el ayudante, probablemente la lógica que estás validando debería vivir en el chef.

---

## 5. Recorrido por el código

### El servicio (`DescuentoCalculator`)

Es la pieza con más densidad de información del ejemplo. Veinte líneas que materializan la regla de descuento escalonada:

```csharp
var pct = total switch
{
    < 100m => 0m,
    < 500m => 0.05m,
    < 1000m => 0.10m,
    _ => 0.15m,
};
return Math.Round(total * pct, 2);
```

Sin dependencias externas, sin estado, sin async. Una función pura que recibe un `decimal` y devuelve un `decimal`. Eso es lo que la hace **trivial de testear**: nueve `[InlineData]` cubren cada escalón y los bordes (99.99 está abajo del primer escalón, 100 está en el segundo).

La regla operativa que conviene interiorizar: si tu servicio tiene `try/catch`, side effects o estado mutable, considéralo una señal de que está haciendo demasiado. Refactorízalo a un núcleo puro y un envoltorio fino que se ocupe de los efectos. La parte pura es la que vas a probar a fondo; la fina puede quedarse con dos tests integradores.

### La función como pegamento (`PedidosApi`)

Cuatro líneas que importan:

```csharp
pedido = await JsonSerializer.DeserializeAsync<Pedido>(req.Body, ...);
if (pedido is null || string.IsNullOrWhiteSpace(pedido.Id))
    return new BadRequestObjectResult(...);
if (pedido.Total < 0)
    return new BadRequestObjectResult(...);
return new OkObjectResult(_calculator.Aplicar(pedido));
```

Ningún cálculo. Ningún acceso a Azure. Ningún logging "decisivo". Solo la validación mínima del input y la delegación al servicio. Y por eso los tests de la función son **cuatro**, no veinte:

- POST con un body válido → el servicio se invoca con el `Pedido` deserializado y la respuesta lleva su resultado.
- POST con body JSON corrupto → 400 con `error: "Body JSON inválido"`.
- POST con id vacío → 400.
- POST con total negativo → 400.

Eso es todo el cableado que puede romperse. Y el test inyecta un `IDescuentoCalculator` mockeado con NSubstitute, no el de verdad — la lógica del descuento ya se probó en su capa.

### El blob y el timer como triggers finos (`TareasFunctions`)

El patrón se repite. La función del timer recibe el `TimerInfo`, llama a `_limpieza.Limpiar(...)` y loguea. La función del blob recibe el contenido como string, llama a `_csv.Procesar(...)` y loguea. Ninguna de las dos contiene lógica.

¿Cómo se testea la limpieza? Se testea el `LimpiezaService` directamente, sin necesidad de levantar el host de Functions ni esperar al CRON. Una unit test que pasa el corte temporal y verifica que devuelve el número correcto de elementos eliminados.

¿Cómo se testea el parseo del CSV? Se testea el `CsvResumenService.Procesar(string contenido, string nombre)` con un string en memoria. El blob trigger es solo el cable que conecta `BlobTrigger` con el servicio.

### El test de integración con Azurite (`Integration_AzuriteBlobTests`)

Aquí está el patrón más útil del archivo:

```csharp
[SkippableFact]
public async Task SubirBlob_LoDescargaConElMismoContenido()
{
    Skip.IfNot(DockerEstaDisponible(), "Docker no disponible — test saltado");

    await using var azurite = new AzuriteBuilder().WithImage("...").Build();
    await azurite.StartAsync();
    // ... usa BlobServiceClient contra azurite.ConnectionString
}
```

`SkippableFact` es la pieza clave. En una máquina con Docker, el test arranca un contenedor Azurite (cuesta unos pocos segundos), sube un blob, lo descarga, asserta. En una máquina sin Docker —o en un agente CI mínimo— el test se salta limpiamente con un mensaje informativo, **sin marcarse como fallo**. Es la forma de tener integration tests "opcionales" sin que la suite verde dependa de tener emuladores instalados.

La regla práctica: un integration test que falla cuando no debería —porque el emulador no está disponible— acaba siendo ignorado por todo el equipo. Mejor que se salte explícitamente. Cuando lo necesitas (antes de un release importante, en el agente CI con Docker preinstalado), lo ejecutas y comprueba el cableado de verdad.

---

## 6. La lección dura: los tests no ejercitan el contenedor de DI

Hay un punto del catálogo de patrones que merece su propia sección, porque es el origen de bugs que **no detecta ningún test** del estilo del ejemplo:

Los `[Function]` se testean instanciándolos a mano con `new PedidosApi(calculator)`. Eso es correcto y eficiente. Pero significa que **el contenedor de DI nunca se ejercita en los tests**: si `Program.cs` registra `IDescuentoCalculator` pero te olvidas de registrar `ILogger<PedidosApi>`, los tests siguen verdes (porque tú estás pasando el logger a mano), y la app **falla en runtime** con `Unable to resolve service for type 'ILogger<...>'`.

Pasó de verdad en S3.4 del curso, con `IInformeService`, `IImportSummaryService` e `ICsvProductosImporter`. Los tres fueron inyectados en una función y no registrados en `Program.cs`. La suite de 48 tests pasó al 100%. La app rota arrancaba bien al principio (los servicios se resuelven perezosamente), pero a la primera invocación del endpoint reventaba con un 500 y el mensaje de "Unable to resolve service" en los logs.

La regla operativa que se quedó tras esa lección:

> Tras escribir las funciones, **cruza a mano cada parámetro del constructor de cada `[Function]`** —y de los servicios que esos parámetros traen consigo— con los `AddSingleton/AddScoped/AddTransient` de `Program.cs`. Es una revisión visual de tres minutos. Te ahorra horas de debugging en runtime.

Algunos equipos automatizan esto con un test extra que carga el host real de Functions y comprueba que todos los servicios se resuelven (`host.Services.GetRequiredService<PedidosApi>()`). Es válido y vale la pena, pero requiere levantar el host, así que tarda lo suyo. La revisión manual es suficiente para proyectos pequeños y medianos.

---

## 7. El catálogo del README, contextualizado

El README de este submódulo es un catálogo de los descubrimientos de testing acumulados a lo largo de M03 y M04. Aquí vale la pena explicar el "por qué" de los que más fácilmente confunden:

**`FakeServiceBusMessageActions`** (visto en S4.1 y S4.3). La clase abstracta `ServiceBusMessageActions` no se puede mockear directamente con NSubstitute (los métodos no son todos virtuales). La salida es derivar una clase manual que registre las invocaciones en propiedades booleanas (`CompleteLlamada`, `DeadLetterLlamada`, etc.) y los argumentos. Es feo, pero es la única forma de testear el `switch` del clasificador de errores sin Service Bus real.

**`ServiceBusModelFactory`** (S4.3). Cuando quieres construir un `ServiceBusReceivedMessage` con `DeadLetterReason` y `DeadLetterErrorDescription` ya pobladas (para simular un mensaje que llega a la DLQ), descubres que el factory **no tiene esos parámetros**. La razón es que esas propiedades se leen de `ApplicationProperties` con claves bien conocidas. Así que se pasan por el diccionario `properties:` con `"DeadLetterReason"` y `"DeadLetterErrorDescription"` como claves. No es intuitivo y no está bien documentado por Microsoft, pero el patrón funciona.

**`TaskOrchestrationContext` con NSubstitute** (S4.2). Es el caso más espinoso porque la superficie del contexto de un orquestador Durable son unos 20 métodos virtuales — demasiado para hacer un fake a mano. Con `Substitute.For<TaskOrchestrationContext>()` resuelves el grueso, pero quedan tres sutilezas que conviene tener cazadas:

1. `CreateReplaySafeLogger<T>()` devuelve `null` por defecto en el mock. Si el orquestador llama a `_context.CreateReplaySafeLogger<Mi>()` y obtiene null, peta a la siguiente línea con `NullReferenceException`. Hay que configurarlo explícitamente: `ctx.CreateReplaySafeLogger<Mi>().Returns(NullLogger<Mi>.Instance)`.
2. `TaskFailedException` tiene un constructor público `(taskName, taskId, inner)`. Sirve para simular el fallo de una activity tras agotar reintentos en un test de saga. No es obvio que sea público; en muchos ejemplos online lo construyen por reflection.
3. `GetInput<T>()` también devuelve el default(T) si no lo configuras. Para un orquestador que arranca con un input no trivial (`Pedido`, por ejemplo), el test tiene que configurar `ctx.GetInput<Pedido>().Returns(unPedido)` antes de invocar la lógica del orquestador.

**`Activator.CreateInstance` ignora parámetros opcionales** (S4.3). Si tienes una excepción custom con firma `(string mensaje, Exception? inner = null)` y la quieres construir desde reflection con `Activator.CreateInstance(tipo, "msg")`, **falla** con `MissingMethodException`. No hay una sobrecarga real de un solo argumento — el segundo parámetro tiene default, pero el ABI sigue siendo `(string, Exception?)`. La salida: en los `[Theory]` usa `TheoryData<Exception>` con instancias construidas explícitamente (`new ErrorTransitorioException("x")`), nunca reflection.

**`[ExponentialBackoffRetry]` no compila en `ServiceBusTrigger`**. El analyzer `AZFW0012` lo rechaza con un mensaje claro. Es la fricción inherente de mezclar dos sistemas de retry: el del runtime de Functions y el de Service Bus. La regla es: en triggers que tienen retry propio (Service Bus, Event Grid), confía en el suyo. El atributo solo aplica a triggers sin retry propio (Timer, Event Hub).

Todos estos puntos están en el README como catálogo para no perderlos entre submódulos. Aquí los traigo con la motivación para que entiendas por qué cada uno está en la lista.

---

## 8. Cómo probarlo en local

`dotnet test` es la ceremonia diaria:

```bash
dotnet test
# 23 passed, 1 skipped (integration, sin Docker), 0 failed
```

Lo importante de esa salida es el `skipped`. Si Docker está corriendo en tu máquina, el test de integración se ejecuta y entra en `passed`. Si no, se salta sin fallar. Esa es la promesa de `SkippableFact`: la suite siempre queda verde, ejecutes donde la ejecutes.

Para depurar en VS Code, el `.vscode/launch.json` del ejemplo trae una configuración `Attach to .NET Functions` que se conecta al proceso del `func host`. El flujo es:

1. En una terminal: `func start --csharp`.
2. En VS Code: Run → "Attach to .NET Functions" → seleccionar el proceso.
3. Poner breakpoints donde quieras.
4. Lanzar una petición con `api.http` o con curl.

El proceso se para en el breakpoint igual que cualquier ASP.NET Core. Es el tipo de cosa que te ahorra muchas horas cuando estás peleando con un bug que solo se reproduce con un payload concreto.

> Yo no lanzo apps. Tú haces `func start --csharp` y `dotnet test`.

---

## 9. Lo que el ejemplo deja deliberadamente fuera

Tres cosas que aparecen en el submódulo y no se materializan en código:

- **E2E real en staging**. Requeriría un Function App desplegado y datos de prueba en Azure. Es un test caro de ejecutar y normalmente se hace una vez por release, no por commit.
- **Bogus para datos falsos**. Es una librería excelente para generar nombres, direcciones, etcétera, en tests. Sobre un dominio trivial como `Pedido(Id, Total)` no aporta — los `[InlineData]` cubren los casos esenciales sin overhead.
- **Application Insights end-to-end**. Es la pieza de observabilidad que cierra el ciclo de "qué pasa en producción". Se cubre en M08, donde hay sitio para diseñar dashboards y queries.

---

## 10. La mentalidad que cierra M04

Has visto a lo largo del módulo cinco temas distintos: Event Grid y Service Bus (S4.1), Durable Functions (S4.2), errores y dead-letter (S4.3), despliegue y versionado (S4.4) y testing y depuración (S4.5). Si miras atrás, todo tiene un hilo común: **la arquitectura buena de Functions es la que separa la lógica de los efectos**, y los efectos los maneja el sistema (Service Bus, Cosmos, Durable, Polly, el orquestador), no tu código.

- Service Bus se ocupa de la entrega at-least-once; tu código se ocupa de la idempotencia.
- Durable se ocupa de la persistencia y el replay; tu código se ocupa de la coordinación lineal.
- Polly se ocupa del retry y el circuit breaker; tu código se ocupa de la clasificación de errores.
- Los slots y feature flags se ocupan del rollback; tu código se ocupa del versionado del contrato.

Cuando interiorizas esa separación, los tests se simplifican porque solo tienes que probar la parte de "tu código" — la parte de los efectos la prueba el integration test ocasional contra el emulador. Cuando no la interiorizas, todos los tests se vuelven integration tests gigantes, lentos y frágiles.

Eso es lo que cierra M04. Los dos siguientes son las prácticas que consolidan todo lo visto.

---

## 11. Glosario breve

- **Pirámide de tests**: muchos unit-tests (capa ancha y rápida), pocos function-tests (mediano), uno o dos integration-tests (capa fina y lenta). El equilibrio que más cobertura efectiva da por euro de CPU.
- **NSubstitute**: librería de mocking para .NET. Genera un fake automáticamente para cualquier interface o clase con miembros virtuales. Alternativa moderna a Moq.
- **`SkippableFact`** (xUnit): atributo del paquete `Xunit.SkippableFact` que permite saltar un test en runtime con `Skip.If(...)` o `Skip.IfNot(...)` sin marcarlo como fallo.
- **Testcontainers**: librería que arranca contenedores Docker desde un test para tener servicios reales en local (Azurite, Postgres, Redis...). Excelente para integration tests reproducibles.
- **`WebApplicationFactory`**: utilidad de ASP.NET Core para arrancar el host en memoria desde un test. **No aplica al worker aislado de Functions** — los `[Function]` se instancian directamente.
- **Fake vs mock**: a efectos prácticos suelen usarse como sinónimos. Convención útil: "fake" para una clase escrita a mano con comportamiento controlado (`FakeServiceBusMessageActions`); "mock" para una instancia generada por una librería (NSubstitute).

---

## 12. Cierre

La pirámide de tests no es magia. Es la consecuencia natural de poner la lógica en servicios y dejar la función como pegamento. Una vez que la estructura está, los tests caen solos en su sitio: muchos unit-tests sobre los servicios, pocos function-tests sobre el wiring, uno o dos integration-tests opcionales para el roundtrip real. Y por encima de todo eso, una regla operativa que ningún test te da: cruzar a mano que el contenedor de DI tiene registrado todo lo que las funciones inyectan.

Si lees el catálogo del README con calma, te quedan cinco o seis patrones reutilizables para cualquier proyecto de Functions que escribas a partir de ahora. Ese catálogo es probablemente la parte más útil del módulo para llevarte al día a día.

Lo siguiente son [`S4.P — Práctica de flujo completo`](../S4.P-practica-flujo-completo/MANUAL.md) y [`S4.P2 — Práctica Durable Hello World`](../S4.P2-practica-durable-hello-world/MANUAL.md), las dos prácticas integradoras que cierran el módulo.
