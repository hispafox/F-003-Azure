# Manual del alumno — S3.P · Práctica: Funciones con los 4 tipos de triggers

Esto **no** es el [`README.md`](README.md). El README es el guion paso a paso: lista de App Settings, comandos `az`, despliegue por Portal. Este manual va antes: te cuenta por qué esta práctica integradora cierra el módulo M03 y qué demuestra la convivencia de cuatro triggers en una sola Function App.

Tiempo de lectura: ~20 min. Práctica de referencia: [M03-S3.P](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.P-practica-4-triggers-v4.md). Junta lo aprendido en S3.2 (HTTP), S3.3 (Timer), S3.4 (Blob) y S3.5 (Cosmos Change Feed) en **una sola app** con tres singletons compartidos por DI y un endpoint `/api/estado` para inspeccionar el efecto agregado.

*Creado: 2026-05-20 12:02 +0200*

---

## 1. La idea en una frase

Hasta ahora cada submódulo del M03 enseñaba **un trigger en aislamiento**: una Function App con HTTP, otra con Timer, otra con Blob, otra con Cosmos. Esta práctica integradora demuestra el patrón real de Functions en proyectos serios: **una sola Function App con N triggers de naturaleza distinta conviviendo en el mismo proceso**, compartiendo servicios vía DI. Una HTTP para la API, un Timer para limpiezas periódicas, un Blob para imports, un Cosmos Change Feed para reaccionar a cambios — todo en un único Consumption plan, todo facturado por ejecución.

Esa convivencia es lo que hace a Functions económico. En App Service necesitarías cuatro apps separadas (una por trigger) o una sola app con código complicado para combinar todos los modelos. En Functions una sola app con cuatro atributos hace el trabajo. Coste base: cero euros mientras no se use; cuando hay tráfico, pagas solo las ejecuciones de cada trigger.

---

## 2. El problema real que hay detrás

Un equipo tenía un proyecto pequeño con varias necesidades dispares: una API REST para gestionar productos, un proceso nocturno de limpieza, un import periódico desde CSVs subidos por un proveedor, y reacción automática a nuevos pedidos. La arquitectura inicial: cuatro proyectos, cuatro despliegues, cuatro pipelines, cuatro App Service Plans, cuatro entradas en el portal. Coste base ~250 €/mes, código duplicado en cuanto algo se compartía entre los proyectos, monitoring fragmentado.

La refactorización con Functions: **una sola Function App** con cuatro funciones, cada una con su trigger. La API CRUD, el Timer de limpieza, el Blob trigger del import y el Change Feed de pedidos comparten el mismo `Program.cs`, los mismos singletons en DI, el mismo Application Insights. Despliegue único, pipeline única, métricas agregadas. Coste base: cero euros (Consumption); coste real: unos céntimos al mes por las ejecuciones.

Lo que entrega:

| Trigger | Endpoint / fuente | Función |
| --- | --- | --- |
| **HTTP** | `GET/POST /api/productos`, `GET /api/estado` | API REST + endpoint de inspección |
| **Timer** | NCRONTAB `0 */1 * * * *` (cada minuto) | Limpieza periódica + estadísticas |
| **Blob** | `uploads/{nombre}.csv` | Import de CSVs |
| **Cosmos Change Feed** | `tienda/pedidos` | Reacción automática a pedidos nuevos |

Y tres singletons compartidos vía DI: `IProductoService`, `ILimpiezaTracker`, `INotificacionLog`. La pieza didáctica es el endpoint `/api/estado` — devuelve el estado de los tres singletons en una sola respuesta, permitiéndote ver desde el navegador qué hizo el Timer en la última hora, cuántos CSVs procesó el Blob trigger, cuántos pedidos pasaron por el Change Feed.

---

## 3. Por qué esto importa en tu stack

Las tres ideas que conviene tener fijas al terminar esta práctica:

**Una Function App vale por muchas funciones.** No tienes que crear una Function App nueva por cada trigger. Mientras los triggers compartan stack (todas .NET 10 isolated) y tier (Consumption), conviven en el mismo proceso. Diferentes consumption plans solo aparecen si quieres aislamiento de fallos (un trigger pesado no debería afectar a los otros) o monitoring separado.

**Los singletons en Functions persisten entre invocaciones de cualquier trigger.** Si tu HTTP API actualiza un producto y el Timer lo lee dos minutos después, **lo ve actualizado** — los dos comparten la misma instancia de `IProductoService` en memoria. Esto te da una arquitectura ligera donde el "estado de la app" es un singleton compartido y los triggers son entradas distintas a la misma lógica. Y la limitación: el singleton se pierde cuando la Function App se reinicia (cold start, deploy, scale-out). Para datos persistentes, sigue siendo Cosmos / Storage / SQL.

**El endpoint de inspección es un patrón operativo subestimado.** En aplicaciones tradicionales el "estado interno" es opaco — para saber qué está haciendo tu app a las 3 AM hace falta loggear todo o usar profilers. En Functions con `/api/estado` haciendo un dump del estado de los singletons, una sola llamada te dice "el Timer corrió hace 35 segundos, encontró 12 productos sin stock, eliminó 3; el Blob trigger procesó dos CSVs en la última hora; el Change Feed lleva 47 notificaciones acumuladas". Es observabilidad gratuita.

---

## 4. El modelo mental: la oficina con cuatro puertas

Imagina una oficina con cuatro puertas distintas. Por la primera entran clientes (peticiones HTTP). Por la segunda, un reloj eléctrico que toca cada minuto (timer). Por la tercera, un camión de mercancías que descarga periódicamente (blobs). Por la cuarta, una línea telefónica directa con el almacén (Cosmos Change Feed). Cuatro entradas, cuatro tipos de evento, pero **la misma plantilla** en la sala central trabajando: si un cliente pide un producto, el archivo se actualiza; si el reloj suena, la plantilla mira el archivo y limpia; si el camión deja una caja, la plantilla la abre y actualiza el archivo; si llega una llamada del almacén, la plantilla anota en su libro.

```
Function App (la oficina)
  │
  ├── ProductosApi          ← Puerta 1: clientes con peticiones HTTP
  ├── LimpiezaProgramada    ← Puerta 2: reloj que suena cada minuto
  ├── ProcesarCsv           ← Puerta 3: camión con CSVs
  └── ReaccionarPedidos     ← Puerta 4: línea con Cosmos
       │
       ▼ (los cuatro comparten DI)
  ┌─────────────────────────────────────────┐
  │ IProductoService                        │
  │ ILimpiezaTracker                        │
  │ INotificacionLog                        │
  │ (tres singletons in-memory)             │
  └─────────────────────────────────────────┘
       │
       ▼
  GET /api/estado → dump consolidado de los tres
```

Tres frases para fijar el modelo:

- **El "trabajador" (tu lógica de negocio) es el mismo**. Lo que cambia es por dónde entra el evento. Esa unificación es lo que permite que cuatro triggers diferentes compartan la misma lógica sin duplicación.
- **Los singletons son la "memoria de la oficina"**. Mientras la oficina esté abierta (la Function App esté caliente), los archivos se mantienen. Cuando se cierra (cold start, redeploy), la memoria se borra y el camión que tenía la lista del día anterior tiene que volver a llamar.
- **`/api/estado` es la ventanilla pública**. Cualquiera puede asomarse y ver qué está pasando dentro. No es un endpoint de negocio — es de observabilidad. En producción real lo limitarías a `AuthorizationLevel.Function` o lo expondrías solo a sistemas de monitoring.

---

## 5. La técnica: tres singletons compartidos por DI

Mira `Program.cs`. Tres `AddSingleton` que registran los servicios compartidos:

```csharp
builder.Services.AddSingleton<IProductoService, InMemoryProductoService>();
builder.Services.AddSingleton<ILimpiezaTracker, InMemoryLimpiezaTracker>();
builder.Services.AddSingleton<INotificacionLog, InMemoryNotificacionLog>();
```

Y los cuatro triggers los reciben por constructor. `ProductosApi(IProductoService)`, `LimpiezaProgramada(IProductoService, ILimpiezaTracker)`, `ProcesarCsv(IProductoService)`, `ReaccionarPedidos(INotificacionLog)`. Cuando `ProductosApi` añade un producto vía POST, el Timer que se ejecute treinta segundos después lo verá — porque la instancia de `IProductoService` es **la misma**.

Y la pieza que junta todo, `EstadoFunction`:

```csharp
public IActionResult GetEstado(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "estado")] HttpRequest req,
    IProductoService productos,
    ILimpiezaTracker limpieza,
    INotificacionLog notificaciones)
{
    return new OkObjectResult(new
    {
        productos = new { total = productos.Count, sinStock = productos.SinStock },
        ultimaLimpieza = limpieza.UltimaEjecucion,
        notificaciones = notificaciones.UltimasN(10)
    });
}
```

Recibe los tres singletons, los lee, devuelve un JSON con el estado consolidado. Como los servicios son los mismos que usan los otros triggers, el endpoint ve **el estado real en este momento**. No hay "fetch desde otra base de datos" ni "consulta a Service Bus para ver mensajes". Es una lectura directa de memoria.

> 🧠 **El patrón "singleton compartido + endpoint de inspección" es transferible**. En tus proyectos, si tu Function App tiene varios triggers operando sobre el mismo dominio (productos, pedidos, sesiones), regístralos como singletons y añade un `/api/estado` que los exponga. Es debugging operativo gratis: cinco minutos de escritura para una visibilidad enorme cuando algo va mal en producción a las 3 AM.

---

## 6. La regla del DI cruzado

La lección recurrente del HANDOFF aplica aquí en su forma más explícita: **con cuatro triggers, tienes que cruzar cuatro constructores contra el `Program.cs`**. Si te olvidas de registrar uno de los singletons, **tres triggers funcionan bien y el cuarto revienta en runtime**. Y el orden importa: si `ReaccionarPedidos` depende de `INotificacionLog` pero te olvidas del `AddSingleton`, el problema solo aparece cuando llegue un cambio al Change Feed — puede ser horas o días después del deploy.

Por eso el `Program.cs` del ejemplo lleva un comentario explícito listando qué función inyecta qué:

```csharp
// TODOS los servicios que inyectan las funciones por constructor:
//   ProductosApi          → IProductoService
//   EstadoFunction        → IProductoService, ILimpiezaTracker, INotificacionLog
//   LimpiezaProgramada    → IProductoService, ILimpiezaTracker
//   ProcesarCsv           → IProductoService
//   ReaccionarPedidos     → INotificacionLog
builder.Services.AddSingleton<IProductoService, InMemoryProductoService>();
builder.Services.AddSingleton<ILimpiezaTracker, InMemoryLimpiezaTracker>();
builder.Services.AddSingleton<INotificacionLog, InMemoryNotificacionLog>();
```

Cuando añadas un trigger nuevo en tu proyecto, añade su línea al comentario antes de escribir el código. Es la disciplina mínima que evita el bug latente.

---

## 7. Recorrido guiado

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `func start` con Azurite + emulador de Cosmos | logs "Job host started" con los 7 triggers registrados (4 + Hello + Ping + Estado) | La convivencia de los 4 triggers en una sola app. |
| 2 | `POST /api/productos` con `{nombre, precio, stock}` | `201 Created` | HTTP trigger funcionando — el patrón de S3.2. |
| 3 | `GET /api/estado` | JSON con `productos.total = 4` (los 3 seed + el nuevo) | El singleton refleja el estado actual. |
| 4 | Espera al siguiente minuto (Timer cada 1 min) | log "LimpiezaProgramada: total=4 sinStock=0 acciones=0" | Timer disparándose y leyendo el mismo singleton. |
| 5 | `GET /api/estado` | el campo `ultimaLimpieza` ahora tiene timestamp reciente | El Timer escribió en `ILimpiezaTracker`, que el endpoint lee. |
| 6 | Sube `ventas.csv` al container `uploads/` | log "ProcesarCsv: ventas.csv importado, 50 productos" | Blob trigger procesando archivo (S3.4). |
| 7 | `GET /api/estado` | `productos.total = 54` | El Blob trigger añadió al **mismo** singleton que la HTTP API y el Timer leen. |
| 8 | Inserta un pedido en `tienda/pedidos` | log "ReaccionarPedidos: pedido ped-001 confirmado" | Change Feed disparándose (S3.5). |
| 9 | `GET /api/estado` | `notificaciones` array con la notificación reciente | El Change Feed escribió en `INotificacionLog`, el endpoint lo lee. |

Un experimento útil: ejecuta el flujo completo (pasos 2-9) y al final llama a `/api/estado` una vez más. Ves los tres efectos de los tres triggers distintos consolidados en una sola respuesta JSON. **Esa visibilidad es lo que diferencia una Function App "que funciona" de una "que se puede operar"**: con `/api/estado` un operador a las 3 AM puede saber en cinco segundos qué está haciendo la app sin abrir Application Insights ni leer logs.

Y un experimento más conceptual: para la Function App. Espera dos minutos. Vuelve a arrancarla. Llama a `/api/estado` justo después: **los singletons están vacíos**. Los productos se borraron, las notificaciones se perdieron. Reinicio = pérdida de memoria. Para persistencia real, los singletons in-memory son insuficientes — necesitas Cosmos / SQL / Tables. Esta práctica usa singletons in-memory **a propósito**, para que el patrón se vea limpio sin el ruido de la persistencia.

---

## 8. Tests y por qué importa probar la composición

La práctica trae tests para cada función individualmente (heredando los patrones de S3.2-S3.5) y un test específico de **composición**: `EstadoFunctionTests` verifica que cuando registras los tres singletons en DI y los inyectas en `EstadoFunction`, el endpoint devuelve el shape esperado del JSON.

La razón: en aplicaciones con varios triggers, el bug más común no es "una función concreta tiene un bug", es "la composición de servicios no funciona". El test del estado verifica exactamente eso: con los tres servicios resueltos del contenedor real (no mocks), el endpoint produce la respuesta esperada. Si alguien refactoriza un servicio y rompe la interfaz, el test salta.

Sigue valiendo la advertencia: estos tests **no ejercen el grafo completo de DI con las cuatro funciones**. Para eso hay un patrón en M04-S4.5 (resolver el host real y comprobar que las funciones instancian). Hasta entonces, **comentario en `Program.cs` listando cada función con sus dependencias** es la mejor defensa.

---

## 9. Puesta en marcha, ejecución y pruebas

### 9.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar | Sí |
| Azure Functions Core Tools | `func start` | Recomendado |
| Azurite | emular Storage (queue + blob para el Blob trigger) | Sí |
| Emulador de Cosmos (Docker) | Change Feed local | Recomendado (sin él, el Cosmos trigger no dispara en local) |

### 9.2 Compilar y arrancar en local

```bash
cd examples/M03-Azure-Functions-I/S3.P-practica-4-triggers
dotnet build AzureFunctions.Demo.slnx        # 0 errores

cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json

# En terminales separadas:
azurite --silent
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest

# Crea en el emulador: database tienda, container pedidos (PK /clienteId)
# Crea en Azurite: container uploads (con az storage container create)

cd src/AzureFunctions.Demo
func start
```

Sigue el recorrido de la sección 7 para probar los cuatro triggers.

### 9.3 Pasar los tests

```bash
dotnet test
```

Tests heredados de S3.2-S3.5 + tests específicos de la composición. Sin Azure ni emuladores.

### 9.4 Desplegar a Azure (resumen)

Mismo patrón que los anteriores: RG + Storage Account + Cosmos serverless + Function App Consumption Linux .NET 10 isolated. Crear los recursos relacionados:

- Container `uploads` en el Storage Account.
- Database `tienda` y container `pedidos` (PK `/clienteId`) en Cosmos.
- App Setting `CosmosDbConnection` con la connection string de Cosmos.
- Lease containers (`leases-pedidos`) los crea el runtime automáticamente.

### 9.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| Algunos triggers se registran pero el endpoint `/api/estado` falla con "Unable to resolve service" | falta `AddSingleton<X>` en `Program.cs` | regla del HANDOFF (sección 6) |
| El Timer se ejecuta pero `productos.total` siempre es 3 (los seed) | no se está creando productos nuevos por HTTP, o los singletons no son singletons | verifica `AddSingleton`, no `AddScoped` ni `AddTransient` |
| Tras reiniciar, los datos se pierden | comportamiento esperado | sección 7, último experimento — los singletons in-memory pierden estado en reinicio |
| El Blob trigger no se dispara aunque sube el CSV | falta el container `uploads` o connection string incorrecto | crea el container, verifica `AzureWebJobsStorage` |
| Change Feed no procesa pedidos nuevos | falta el container `pedidos` o el `CosmosDbConnection` | crear container, configurar setting |

### 9.6 Limpieza

`Portal → Resource groups → rg-curso-m03-sp → Delete`.

---

## 10. Ideas para llevarte

Lo más útil de esta práctica es **adoptar la mentalidad "una Function App, N triggers"**. Cuando arranques un proyecto serverless nuevo, no crees una Function App por trigger — empieza con una sola y añade triggers según necesites. Solo divídela en varias si: (a) los triggers tienen vidas operativas muy distintas y quieres monitoring separado, (b) un trigger pesado afecta al rendimiento de los otros y quieres aislamiento, (c) los equipos responsables son distintos.

Sobre el **patrón "singletons compartidos + `/api/estado`"**: aplícalo desde el primer proyecto. Es debugging gratis. Un endpoint que devuelve "el estado interno actual de mi Function App" es invaluable cuando algo va mal en producción y los logs no son suficientes. Cinco minutos de escritura, años de utilidad.

Sobre **el bug latente del DI**: con cuatro triggers, el riesgo es cuatro veces mayor. La regla práctica: **el comentario en `Program.cs` listando función → servicios** antes de escribir el `AddSingleton`. Si te olvidas de añadir el comentario, te olvidarás del registro. Si el comentario está bien, el registro casi nunca falla.

Y sobre los **singletons in-memory**: están bien para esta práctica pedagógica y para datos verdaderamente efímeros (caches operativas, métricas agregadas). Para datos persistentes, **siempre persistencia externa**. Los singletons se pierden en cualquier reinicio, y los reinicios son frecuentes en Consumption (cold start tras 20 min, deploys, scale-out).

---

## 11. Comprueba que lo has entendido

1. Tu HTTP API añade un producto vía POST. Treinta segundos después, el Timer se ejecuta y procesa la lista de productos. ¿Ve el producto nuevo? ¿Por qué? *(sección 5)*
2. Reinicias la Function App tras el flujo del paso 1. Llamas a `/api/estado` inmediatamente. ¿Qué ves y por qué? *(sección 7, último experimento)*
3. Añades un trigger nuevo `ReaccionarMensajeServiceBus` que necesita `IMensajeProcesador`. Los tests pasan. Despliegas y el trigger falla. ¿Qué te olvidaste y dónde está la pista? *(sección 6)*
4. Tu equipo planea separar los cuatro triggers en cuatro Function Apps. ¿Cuál es el coste base de cada opción y qué razones legítimas hay para separar? *(sección 3)*
5. `/api/estado` es `Anonymous`. ¿Es correcto en producción? ¿Qué cambiarías? *(sección 4)*
6. El Change Feed encuentra 100 pedidos sin procesar en su primera ejecución tras un deploy. `INotificacionLog` queda con 100 notificaciones. Luego la app se reinicia. ¿Las 100 notificaciones se reprocesan o se pierden? *(sección 7, secciones 4 y 7 de S3.5)*

<details>
<summary>Respuestas</summary>

1. **Sí, lo ve**. Los tres servicios están registrados como **`AddSingleton`** en `Program.cs`. Eso significa que la **misma instancia** de `IProductoService` la inyectan los cuatro triggers. Cuando la HTTP API hace `productos.Crear(...)`, está modificando el estado del singleton compartido. Treinta segundos después, cuando el Timer accede a `productos.GetAll()`, lee la **misma instancia** — ve el producto nuevo. La unificación del estado entre triggers es el corazón del patrón. Si los servicios fueran `Scoped` o `Transient`, cada invocación tendría su propio servicio y se perdería esa visibilidad cruzada.
2. **Ves los 3 productos seed iniciales, no el que añadiste**. Los singletons in-memory se pierden en cualquier reinicio (cold start, deploy, scale-out, parada explícita). Cuando la app vuelve a arrancar, `InMemoryProductoService` se construye de cero y vuelve a sembrar sus 3 productos por defecto. Es la limitación deliberada del patrón pedagógico: queremos mostrar la convivencia de triggers sin el ruido de la persistencia real. Para datos que sobrevivan reinicios, hace falta storage externo (Cosmos, SQL, Tables).
3. Te olvidaste de añadir `builder.Services.AddSingleton<IMensajeProcesador, ImplProcesador>()` en `Program.cs`. **La pista está en el comentario** — si actualizaste el comentario antes de escribir la función (la disciplina del HANDOFF), te das cuenta inmediatamente de que falta el registro al revisar el bloque. Si no actualizaste el comentario, descubres el bug al desplegar — los tests pasan porque instancian la función directamente con `new ReaccionarMensajeServiceBus(mockedProcesador)`. Es el bug latente clásico de Functions DI.
4. **Una Function App: 0 € base** (Consumption mientras no hay tráfico) + ejecuciones reales. **Cuatro Function Apps**: cada una con su `AzureWebJobsStorage` (es un Storage Account distinto si no compartes; ~0,02 €/mes cada uno) + ejecuciones. La diferencia de coste base es de céntimos, no significativa. **Razones legítimas para separar**: **(a)** los triggers tienen patrones de tráfico muy distintos y quieres autoscaling independiente, **(b)** un trigger pesado bloquea el host y afecta a los otros (por ejemplo, un Blob trigger procesando archivos de gigabytes), **(c)** los equipos responsables son distintos y prefieren despliegues separados, **(d)** quieres aislar el blast radius de fallos (un bug en uno no afecta a los otros). Para proyectos pequeños y medianos, **una sola Function App suele ser correcto**.
5. **No es correcto en producción**. `/api/estado` expone el estado interno de tu app — productos en memoria, timestamps de timers, notificaciones recientes. Eso puede revelar volúmenes de negocio, patrones de uso, contenido sensible. **En producción, cambia a `AuthorizationLevel.Function`** (función key) o, mejor, protege detrás de Easy Auth con Entra ID restringido a tu sistema de monitoring. La práctica usa `Anonymous` por simplicidad pedagógica — para que puedas probarlo con `curl` sin gestionar keys.
6. **Se reprocesan**. El Change Feed mantiene los cambios en Cosmos (no en `INotificacionLog`). Cuando la app reinicia, `INotificacionLog` se construye vacío otra vez. **El lease container guarda el punto donde quedó el Change Feed**, pero el contenido del log de notificaciones in-memory se pierde. La próxima vez que arranque, el Change Feed mira el lease, ve que ya procesó hasta el pedido N, y solo procesa cambios desde ahí — no reprocesa los 100. **Pero `INotificacionLog` queda vacío** porque es in-memory. Si necesitas que las notificaciones sobrevivan reinicios, persístelas en Cosmos / Storage / SQL — no en memoria. La regla: in-memory para "estado operativo efímero", persistencia externa para "datos de negocio".

</details>

---

## 12. Hasta aquí

Vuelve a la imagen de la oficina con cuatro puertas de la sección 4. Misma plantilla trabajando, cuatro tipos de evento entrando. Esa unificación es lo que hace de Functions una plataforma elegante para proyectos pequeños y medianos: arquitectura ligera, despliegue único, coste cero hasta que se usa.

Lo siguiente es [`S3.P2 — HTTP CRUD en memoria`](../S3.P2-practica-http-crud-memoria/MANUAL.md), la otra práctica del módulo. Es la versión **más simple** posible — un solo trigger HTTP, repositorio singleton in-memory, sin dependencias externas. Pensada para alumnos que se incorporan tarde o que prefieren empezar sin emuladores. Cuando termines las dos prácticas, has visto Functions desde el "Hello World" más simple hasta la app integradora con cuatro triggers conviviendo. Lo que viene a continuación, en M04, son **Durable Functions, retry policies dedicadas y observabilidad serverless avanzada** — Functions aplicado a workflows con estado.
