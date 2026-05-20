# Manual del alumno — S3.P2 · Práctica: HTTP CRUD en memoria

Esto **no** es el [`README.md`](README.md). El README es el guion paso a paso: lista exacta de endpoints, comandos, despliegue por Portal. Este manual va antes: te cuenta por qué esta es la práctica más simple del módulo y cómo verla con respecto a S3.P (la integradora) y a S3.2 (el CRUD con middleware).

Tiempo de lectura: ~15 min. Práctica de referencia: [M03-S3.P2](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.P2-practica-http-crud-memoria-v1.md). Cinco endpoints HTTP CRUD sobre un repositorio singleton in-memory. **Cero dependencias externas** (sin Cosmos, sin Blob, sin Timer, sin emuladores). El ciclo de Functions reducido al hueso.

*Creado: 2026-05-20 12:02 +0200*

---

## 1. La idea en una frase

S3.P junta cuatro triggers en una app integradora. Esta práctica hace lo contrario: **reduce a uno**. Un único trigger HTTP con cinco endpoints CRUD (`GET /productos`, `GET /productos/{id}`, `POST /productos`, `PUT /productos/{id}`, `DELETE /productos/{id}`) sobre un repositorio singleton in-memory. Sin Cosmos, sin Blob, sin Timer. Sin Azurite, sin emulador de Cosmos. Arrancas con `func start` y `curl`, y en cinco minutos tienes una API funcionando.

¿Por qué existe esta práctica si S3.2 ya hace prácticamente lo mismo? Por dos razones. La primera, **simplicidad**: S3.2 trae middleware, Problem Details, validación con DataAnnotations, niveles de autorización mezclados. S3.P2 los deja fuera para que el alumno se centre en el **patrón mínimo viable**. La segunda, **alternativa pedagógica**: para quien se incorpora tarde al curso o prefiere empezar sin la complejidad de los emuladores, esta práctica es el punto de entrada más amable.

---

## 2. Cuándo elegir esta práctica vs S3.P

| Aspecto | S3.P2 (esta) | S3.P (integradora) |
| --- | --- | --- |
| Triggers | 1 (HTTP) | 4 (HTTP, Timer, Blob, Cosmos) |
| Dependencias externas | Ninguna | Storage + Cosmos |
| Emuladores requeridos en local | Ninguno | Azurite + Cosmos emulator |
| Tiempo estimado | ~30 minutos | ~60 minutos |
| Endpoints | 5 (CRUD completo) | ~7 (CRUD + estado) |
| Persistencia | In-memory (se pierde en reinicio) | In-memory (se pierde en reinicio) |
| Pedagógicamente | Punto de entrada amable | Cierre integrador |

**Cuándo elegir S3.P2**: te incorporas al curso tarde, prefieres empezar sin emuladores, quieres ver el patrón Functions más simple posible, o tienes 30 minutos en lugar de 60.

**Cuándo elegir S3.P**: ya dominas el patrón básico y quieres ver cómo múltiples triggers conviven en una sola app — la realidad de cualquier proyecto Functions serio.

**Si vas a hacer las dos**, empieza por S3.P2 (menos complejidad) y luego S3.P. El orden inverso también funciona pero te obliga a configurar emuladores la primera vez.

---

## 3. Lo que entrega

Tres piezas, igual que un controlador ASP.NET Core con el repositorio detrás:

| Pieza | Para qué | Dónde |
| --- | --- | --- |
| **`ProductosApi`** | Cinco funciones HTTP CRUD con atributos `[HttpTrigger]` | [`ProductosApi.cs`](src/AzureFunctions.Demo/Functions/ProductosApi.cs) |
| **`IProductoService` + `InMemoryProductoService`** | Repositorio singleton thread-safe con `ConcurrentDictionary` | servicio inyectado por DI |
| **Tres productos seed** | Datos iniciales en el constructor del servicio para que el GET no devuelva lista vacía nada más arrancar | en el constructor del `InMemoryProductoService` |

Cinco endpoints:

| Método | Ruta | Resultado |
| --- | --- | --- |
| `GET` | `/api/productos` | `200 OK` con la lista (3 seed iniciales) |
| `GET` | `/api/productos/{id}` | `200 OK` con producto, o `404 Not Found` |
| `POST` | `/api/productos` | `201 Created` con el producto creado |
| `PUT` | `/api/productos/{id}` | `200 OK` con el producto actualizado, o `404` |
| `DELETE` | `/api/productos/{id}` | `204 No Content`, o `404` |

Sin middleware, sin Problem Details, sin niveles de autorización mezclados. Cinco endpoints directos, una clase de funciones, un servicio.

---

## 4. Por qué empezar por aquí (cuando aplica)

Cuando alguien aprende Functions, el ciclo "edita código → arranca → prueba con curl" debería ser de minutos, no de cuarenta minutos configurando emuladores. Esta práctica está pensada para ese momento:

```
1. dotnet build
2. func start
3. curl http://localhost:7071/api/productos
4. listo
```

Cero `azurite --silent`, cero `docker run cosmos-emulator`, cero crear containers en Data Explorer. La función trigger HTTP solo necesita el runtime de Functions y un Storage Account (que en local es Azurite implícito o `AzureWebJobsStorage` apuntando a uno temporal). Si tu propósito es **entender Functions sin distracciones**, S3.P2 es el camino más corto.

Y la **limitación deliberada**: los datos viven en memoria. Reinicias la app, se pierde todo. Eso es el coste de simplicidad. Para persistencia real → Cosmos (M05) o Storage Tables. Aquí estás aprendiendo el ciclo de Functions, no construyendo un sistema de producción.

---

## 5. El modelo mental: la API más simple posible

Imagina que de las prácticas anteriores quitas todo lo que no es estrictamente "endpoint HTTP que lee/escribe un repositorio". Sin middleware. Sin DTOs separados. Sin validación con `[Required]`. Sin Problem Details. Sin function keys (todos `Anonymous` en local). Lo que queda son **cinco métodos en una clase** con atributos `[HttpTrigger]`, llamando a un servicio inyectado, devolviendo `OkObjectResult` o `NotFoundResult`.

```
   curl                  ProductosApi          IProductoService
    │                          │                     │
    │ POST /api/productos      │                     │
    │─────────────────────────▶│                     │
    │                          │ Crear(producto)     │
    │                          │────────────────────▶│
    │                          │                     │ _store.TryAdd
    │                          │ producto creado     │
    │                          │◀────────────────────│
    │ 201 Created              │                     │
    │◀─────────────────────────│                     │
```

Tres frases para fijar el modelo:

- **El ciclo "request → repo → response" es el mismo que en ASP.NET Core**. Lo que cambia es el bootstrap (`FunctionsApplication.CreateBuilder`) y la declaración del endpoint (atributo `[HttpTrigger]`). Si has hecho Web API, este código se lee literalmente igual.
- **El singleton in-memory es la versión mínima viable de persistencia**. Funciona perfecto durante una sesión, se pierde al reiniciar. Es deliberado: querer persistencia real significa salir de "Functions sola" y entrar en M05 (almacenamiento).
- **No hay middleware, pero las excepciones siguen siendo capturables**. Si tu código lanza, Functions devuelve un `500 Internal Server Error` automáticamente. No es bonito (no es Problem Details), pero funciona para una práctica de introducción. La versión profesional con middleware está en S3.2.

---

## 6. Recorrido guiado

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `func start` | logs "Job host started" con 5 funciones registradas | El skeleton mínimo arrancando. |
| 2 | `curl http://localhost:7071/api/productos` | JSON con los 3 productos seed | El GET lista funcionando. |
| 3 | `curl http://localhost:7071/api/productos/p-001` | `200 OK` con el producto | GET por id. |
| 4 | `curl http://localhost:7071/api/productos/no-existe` | `404 Not Found` | El servicio devuelve null → handler responde 404. |
| 5 | `curl -X POST .../api/productos -d '{...}'` | `201 Created` con el nuevo producto (id asignado) | POST + asignación de id por el servicio. |
| 6 | Repite el GET de la lista | ahora son 4 productos | El singleton mantiene el estado. |
| 7 | `curl -X PUT .../api/productos/{id} -d '{...}'` | `200 OK` con el producto modificado | PUT del happy path. |
| 8 | `curl -X DELETE .../api/productos/{id}` | `204 No Content` | DELETE. |
| 9 | Repite GET por ese id | `404 Not Found` | Confirmación del DELETE. |
| 10 | **Para `func start` y vuelve a arrancarlo**. Repite el paso 2 | otra vez los 3 productos seed iniciales | **El estado in-memory se pierde en reinicio**. Limitación deliberada. |

El paso 10 es el más didáctico de toda la práctica. La primera vez que ves que los datos desaparecen tras un reinicio, se cementa la idea de que **in-memory no es persistencia real**. Para datos que sobrevivan reinicios, módulo siguiente.

---

## 7. La lección DI mínima

Como en todas las prácticas de M03, **la regla del HANDOFF aplica**: los tests instancian `ProductosApi` con `new` directamente, y si te olvidas de registrar `IProductoService` en `Program.cs`, los tests pasan pero la Function App revienta en runtime.

Aquí solo hay un servicio para registrar:

```csharp
builder.Services.AddSingleton<IProductoService, InMemoryProductoService>();
```

Una línea. Difícil olvidar. Pero el patrón es el mismo que verás en S3.6 con seis servicios y en proyectos reales con quince: **cruza a mano los constructores de cada función contra el `Program.cs`**.

---

## 8. Tests del proyecto

Tests por instanciación directa con el helper `HttpRequestFactory` (heredado de S3.2). Cubren los cinco endpoints en sus happy paths y casos de error:

- `GET /productos` devuelve la lista (3 productos seed por defecto).
- `GET /productos/{id}` devuelve 200 si existe, 404 si no.
- `POST /productos` devuelve 201 + asigna id.
- `PUT /productos/{id}` devuelve 200 si existe, 404 si no.
- `DELETE /productos/{id}` devuelve 204 si existe, 404 si no.

Y tests de `InMemoryProductoService` directos (sin pasar por la función). Más rápidos, más enfocados a la lógica del servicio.

Sin `WebApplicationFactory`, sin emuladores, sin Azure. Es la suite más rápida del módulo M03.

---

## 9. Puesta en marcha

### 9.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar | Sí |
| Azure Functions Core Tools | `func start` | Recomendado |
| `curl` o REST Client (VS Code) | probar los endpoints | Sí |

**No necesitas Azurite ni emulador de Cosmos.** Esa es la gracia de esta práctica.

### 9.2 Compilar y arrancar

```bash
cd examples/M03-Azure-Functions-I/S3.P2-practica-http-crud-memoria
dotnet build AzureFunctions.Demo.slnx    # 0 errores

cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json

cd src/AzureFunctions.Demo
func start
# → http://localhost:7071/api/productos
```

Sigue el recorrido de la sección 6 con `curl` o con el `api.http` del proyecto.

### 9.3 Pasar los tests

```bash
dotnet test
```

Todos verdes, segundos. Sin Azure ni emuladores.

### 9.4 Desplegar a Azure (resumen)

El patrón es el mismo que S3.1-S3.6: RG + Storage Account + Function App Consumption Linux .NET 10 isolated. **Importante**: aunque la app no use Storage para nada (los productos viven en memoria), Functions necesita un Storage Account asociado para el runtime. No te lo puedes saltar.

Tras el deploy, los endpoints están en `https://<func>.azurewebsites.net/api/productos`. Si dejaste el atributo en `AuthorizationLevel.Function`, necesitas la function key (`?code=...`).

### 9.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `func start` falla con "no functions found" | falta el `local.settings.json` con `AzureWebJobsStorage` | copia desde `local.settings.json.example` |
| El POST devuelve 500 sin más info | excepción no controlada (mala deserialización de JSON?) | en local mira la consola; en Azure añade Application Insights |
| Tras reiniciar pierdo todos los productos | comportamiento esperado | sección 6, paso 10 — para persistencia real, M05 |
| `Unable to resolve service` | falta `AddSingleton<IProductoService>` | regla del HANDOFF |

### 9.6 Limpieza

`Portal → Resource groups → rg-curso-m03-sp2 → Delete`.

---

## 10. Ideas para llevarte

Lo más útil de esta práctica es **el reflejo del ciclo mínimo**. Cuando empiezas un proyecto Functions nuevo y quieres validar "el setup funciona", **este es el patrón a copiar**: una función HTTP, un servicio in-memory, `func start`, `curl`. Si eso responde, el resto del proyecto puede crecer encima con confianza. Si no responde, hay un problema de setup que conviene resolver antes de meter más complejidad.

Sobre los **singletons in-memory**: están bien para esta práctica y para datos verdaderamente efímeros (caches operativas, métricas agregadas). Para datos persistentes en cualquier proyecto real, **persistencia externa siempre**. El día que tu Function App se reinicia (cold start, deploy, scale-out) y los datos del cliente desaparecen, descubres por qué.

Sobre la **simplicidad pedagógica**: si vas a explicar Functions a alguien que nunca lo ha tocado, este es **el ejemplo por donde empezar**. S3.1 enseña el bootstrap, pero S3.P2 enseña el ciclo completo end-to-end en mínima expresión. De aquí se puede saltar a S3.2 (middleware), S3.3 (timer), etc.

Y un consejo pragmático: cuando hagas tu primera Function App propia, no añadas Application Insights, ni middleware, ni Key Vault references desde el principio. **Haz el equivalente de S3.P2 primero**, verifica que funciona, y luego empieza a añadir capas. La complejidad se acumula rápido; la deuda de "no entendí qué era esa capa que añadí copiando-pegando" se paga durante meses.

---

## 11. Comprueba que lo has entendido

1. ¿Por qué Functions necesita un Storage Account asociado aunque esta práctica no use storage para nada? *(sección 9.4 + S3.1)*
2. Tras un POST que crea un producto, la app se reinicia y haces GET. ¿Qué ves y por qué? *(sección 6 paso 10)*
3. ¿Qué diferencias hay entre esta práctica y S3.2 si las dos hacen "HTTP CRUD"? *(sección 2 + S3.2)*
4. Añades una función nueva `ListarPorCategoria(IProductoService srv, ICategoriaService cat)` y el constructor falla en runtime. ¿Por qué los tests no lo cazaron? *(sección 7)*
5. ¿Cuándo recomendarías a un compañero empezar por S3.P2 vs S3.P? *(sección 2)*
6. Si el patrón es tan simple, ¿por qué existe S3.2 con middleware y Problem Details? *(secciones 1, 5)*

<details>
<summary>Respuestas</summary>

1. Porque **el runtime de Functions usa Storage para metadatos internos**: locks distribuidos (Timer triggers en multi-instance), leases (Cosmos Change Feed, Blob trigger), function key, registro de instancias. La asociación se hace al crear la Function App vía la App Setting `AzureWebJobsStorage`. **Sin Storage, la Function App no arranca**. Para esta práctica concreta donde tus datos viven en memoria, el Storage Account está prácticamente vacío — pero el runtime lo necesita igual. Es uno de los requisitos no negociables del modelo.
2. **Ves los 3 productos seed iniciales**, no el que creaste. El singleton `InMemoryProductoService` se pierde en cualquier reinicio: cold start tras 20 min sin tráfico (en Consumption), nuevo deploy, scale-out, parada explícita. Cuando la app vuelve a arrancar, el constructor se ejecuta de cero y vuelve a sembrar los 3 productos. La práctica es deliberadamente in-memory para que esa limitación se vea **en directo**. Para persistencia que sobreviva reinicios, Cosmos (M05-S5.3) o SQL (M05-S5.2).
3. **S3.2 trae cosas que S3.P2 quita a propósito**: middleware (`CorrelationIdMiddleware`, `ExceptionHandlingMiddleware`), Problem Details RFC 7807 (400/404/422 estructurados), validación con DataAnnotations + `TryValidateObject`, mezcla de niveles de autorización (`Anonymous` + `Function`). S3.P2 quita todo eso. La diferencia es que **S3.2 es la versión profesional** del CRUD HTTP en Functions; **S3.P2 es la versión mínima viable**. Para enseñar el patrón sin distracciones, S3.P2. Para enseñar el patrón completo de producción, S3.2.
4. Te olvidaste de `builder.Services.AddSingleton<ICategoriaService, ...>()` en `Program.cs`. Los tests no lo cazaron porque **instancian la función directamente con `new ListarPorCategoria(mockProductos, mockCategorias)`** — pasándole los mocks a mano, sin usar el contenedor de DI. El host real sí usa el contenedor, y al no encontrar `ICategoriaService` registrado, falla al instanciar la función con "Unable to resolve service". Es el bug latente del HANDOFF, Lección 1. La defensa manual: cruzar a mano los constructores con `Program.cs` cada vez que añades una función nueva.
5. **S3.P2 cuando** el compañero nunca ha tocado Functions y quieres el "Hello World end-to-end" en quince minutos sin tocar emuladores, o cuando se incorpora al curso tarde y necesita el camino más rápido al ciclo de Functions. **S3.P cuando** ya hizo S3.2-S3.5 y quiere ver cómo conviven los cuatro triggers en una sola app (la realidad de cualquier proyecto serio). El orden ideal: S3.P2 (~30 min) → resto de submódulos individuales → S3.P (~60 min) como cierre integrador. Pero si solo tiene tiempo para una, S3.P por completitud, S3.P2 por simplicidad de entrada.
6. Porque **simplicidad pedagógica ≠ patrón profesional**. S3.P2 quita el middleware para que el alumno se enfoque en el patrón básico. Pero **en producción real ese middleware sí hace falta**: capturar excepciones para devolver Problem Details estructurados, generar Correlation IDs para trazabilidad, controlar autorización por endpoint. S3.2 enseña eso porque cualquier API HTTP que se ponga en producción seria los necesita. Las dos prácticas son complementarias, no redundantes: S3.P2 te enseña el ciclo, S3.2 te enseña qué le añades cuando va a producción.

</details>

---

## 12. Hasta aquí

Vuelve a la idea del **ciclo mínimo viable** de la sección 4. Cuando empieces tu próximo proyecto Functions, hazlo en este orden: `dotnet new`, una función HTTP que devuelva "ok", `func start`, `curl`. Si responde, sabes que el setup funciona y puedes crecer. La complejidad se añade encima.

Con esta práctica cierras el **módulo M03 entero**: principios serverless (S3.1), cuatro triggers (S3.2 HTTP, S3.3 Timer, S3.4 Blob, S3.5 Cosmos Change Feed), bindings de entrada/salida (S3.6) y las dos prácticas integradoras. Lo siguiente del curso es **M04 — Azure Functions II**, donde aparecen **Durable Functions** (workflows con estado), **retry policies dedicadas por trigger**, **observabilidad avanzada** y patrones de **testing** que cubren la laguna del DI que aparece en este módulo. Functions deja de ser "funciones aisladas" y empieza a ser una plataforma para orquestaciones serias.
