# Manual del alumno — S3.5 · Trigger Cosmos DB: Change Feed

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: pasos por Portal (creación de Cosmos, containers, lease containers), comandos `az`, lista exacta de connection strings. Este manual va antes: te cuenta qué es el Change Feed conceptualmente, por qué es la base de cualquier arquitectura event-driven sobre Cosmos y cuál es el patrón que diferencia "un trigger" de "N consumidores independientes".

Tiempo de lectura: ~25 min. Submódulo de teoría: [M03-S3.5](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.5-trigger-cosmosdb-changefeed-v4.md). Reutiliza el skeleton de M03 y añade dos consumidores del Change Feed sobre `tienda/pedidos`: notificaciones por cambio de estado y materialización de resúmenes por cliente.

*Creado: 2026-05-20 12:02 +0200*

---

## 1. La idea en una frase

Cosmos DB mantiene **un log de cambios** de cada container — el Change Feed. Cada vez que un documento se crea o actualiza, Cosmos guarda esa versión en un log ordenado y append-only que tus consumidores pueden leer en su propio ritmo. Functions tiene un trigger (`[CosmosDBTrigger]`) que lee ese log y dispara una función con un batch de cambios. **Cada consumidor tiene su propio "marcador de lectura"** (el lease container), así que múltiples funciones pueden consumir el mismo Change Feed independientemente: si una se cae, las otras siguen funcionando.

Eso convierte a Cosmos en una plataforma **event-driven** sin Service Bus ni Event Grid. Inserta un pedido en `tienda/pedidos`, y a los pocos segundos se disparan automáticamente todas las funciones suscritas: una manda notificación, otra actualiza un dashboard, una tercera escribe en analytics. Sin pub/sub explícito, sin colas. El "evento" es el cambio en el documento; los suscriptores son funciones que leen el mismo log con leases distintos.

---

## 2. El problema real que hay detrás

Un equipo tenía un proceso de pedidos con varios efectos colaterales: cada vez que un pedido cambiaba de estado había que mandar un email al cliente, actualizar el dashboard de operaciones, y registrar el evento en analytics. La primera versión: un solo método grande que, después de guardar el pedido, llamaba a los tres servicios secuencialmente. Cuando uno fallaba (el de email tenía un blip en su API externa), el método entero fallaba, y el pedido a veces no quedaba consistente con sus efectos colaterales.

La refactorización con Change Feed: el método "crear pedido" se reduce a *escribir el pedido en Cosmos*. Tres funciones independientes consumen el Change Feed, cada una con su lease propio. Cuando la de email falla, las otras dos siguen funcionando. Y la de email tiene su propia política de retry sin afectar al resto. La consistencia se garantiza porque **el pedido sí queda escrito en Cosmos** — el log de cambios eventualmente entregará el cambio a cada consumidor cuando se recupere.

Lo que entrega:

| Pieza | Para qué | Dónde |
| --- | --- | --- |
| **`NotificacionesPedidoFunction`** | Consumidor 1: notificación por cambio de estado | [`NotificacionesPedidoFunction.cs`](src/AzureFunctions.Demo/Functions/NotificacionesPedidoFunction.cs) |
| **`MaterializarResumenClienteFunction`** | Consumidor 2: agrega cambios y escribe un resumen por cliente | [`MaterializarResumenClienteFunction.cs`](src/AzureFunctions.Demo/Functions/MaterializarResumenClienteFunction.cs) |
| **Lease containers distintos** | `leases-notificaciones` y `leases-resumenes` — cada consumidor independiente | atributos `LeaseContainerName = "..."` |
| **`[CosmosDBOutput]`** | Output binding que upserta el documento de salida en Cosmos | mismo decorator que escribe en `resumenes-clientes` |
| **Idempotencia por id estable** | `Id = $"resumen-{clienteId}"` → upsert determinista | construcción del documento del consumidor 2 |
| **At-least-once delivery** | El mismo cambio puede llegar dos veces; tu código asume idempotencia | `ConcurrentDictionary.GetOrAdd` por `(PedidoId, Estado)` |

---

## 3. Por qué esto importa en tu stack

Cuando aparece la pregunta "¿cómo notifico a otros sistemas que pasó algo?", las opciones tradicionales son tres: **(a) llamar directamente** (acoplamiento fuerte, fallos en cascada), **(b) mensajería explícita con Service Bus** (mejor, pero hay que mantener la cola y publicar manualmente), **(c) eventos con Event Grid** (pub/sub completo, pero infraestructura adicional). Cosmos Change Feed añade una cuarta: **(d) cualquier escritura en Cosmos genera automáticamente un evento legible por N consumidores**.

La diferencia con Service Bus o Event Grid: **no escribes evento + estado, escribes solo estado**. El cambio en Cosmos **es** el evento. Eso simplifica el código (no hay que mantener consistencia entre "guardar pedido" y "publicar PedidoCreado") y elimina toda una clase de bugs.

| Patrón | Cuándo |
| --- | --- |
| **Change Feed** | Tu fuente de verdad ya es Cosmos. Quieres reaccionar a cambios sin código de publicación. |
| **Service Bus** | Necesitas garantías estrictas (FIFO, sessions, DLQ rica), o el productor no es Cosmos. |
| **Event Grid** | Múltiples productores heterogéneos, multitud de tipos de evento, gestión centralizada. |
| **Llamada directa** | Operación síncrona crítica donde el caller necesita confirmación inmediata. |

Cuando Cosmos ya está en tu arquitectura, Change Feed es **el camino más simple y más barato** para el patrón "reaccionar a cambios". No hay coste adicional — pagas las RUs que ya pagabas, el Change Feed viene incluido.

---

## 4. El modelo mental: el periódico con varios lectores

Imagina un periódico que se imprime cada mañana. Cada edición es **append-only**: una vez impresa, no se modifica. Distintas personas leen el periódico cada una a su ritmo: una lo lee en el desayuno, otra en el tren, una tercera lo deja para la noche. Cada uno guarda **un marcador** en la página por donde va. Si una persona se enferma y no lee durante una semana, el periódico no espera — su marcador se queda atrasado, y cuando vuelva leerá los siete números acumulados. Mientras tanto, los otros lectores siguen en su ritmo.

```
Cosmos container "pedidos"
    │
    │  cada escritura → entrada nueva en el Change Feed
    ▼
┌──────────────────────────────────────────────────────────────┐
│ Change Feed (log append-only, ordenado por partición)        │
│ [ped-001 v1] [ped-001 v2] [ped-002 v1] [ped-003 v1] [...]   │
└────────┬──────────────────────┬──────────────────────────────┘
         │                      │
         │ lease A              │ lease B
         │ (marcador A)         │ (marcador B)
         ▼                      ▼
┌────────────────────┐   ┌─────────────────────────────────┐
│ NotificacionesPedido│  │ MaterializarResumenCliente      │
│ función             │  │ función                         │
│                     │  │ + [CosmosDBOutput]              │
│ leases-notificacio… │  │ leases-resumenes                │
│ (container propio)  │  │ (container propio)              │
└────────────────────┘   └─────────────────────────────────┘
```

Tres frases para fijar el modelo:

- **El Change Feed es append-only y persistente**. Una vez registrado un cambio, está ahí indefinidamente. Si un consumidor se queda atrás, cuando vuelva sigue leyendo desde donde se quedó. No hay "ventana de tiempo" más allá de la retención de Cosmos.
- **Cada consumidor tiene su propio lease container**. El lease es un container pequeño de Cosmos donde Functions guarda los marcadores. Dos consumidores con leases distintos son independientes: cada uno avanza a su ritmo. Si uno se cae, los otros no se enteran.
- **At-least-once delivery, no exactly-once**. El mismo cambio puede llegar dos veces a tu consumidor — por retry, por reasignación de partición, por reinicio. Tu código tiene que ser **idempotente**. La lección de S3.3 vuelve aquí con más peso.

---

## 5. La regla del id estable (idempotencia en `[CosmosDBOutput]`)

`MaterializarResumenClienteFunction` agrupa los pedidos del batch por `clienteId` y escribe un documento de resumen en `resumenes-clientes` por cada cliente afectado. El detalle importante está en cómo construye el `Id` del documento de salida:

```csharp
var resumen = new ResumenCliente
{
    Id = $"resumen-{clienteId}",     // ← id estable, no Guid.NewGuid()
    ClienteId = clienteId,
    TotalPedidos = grupos[clienteId].Count,
    ImporteAcumulado = grupos[clienteId].Sum(p => p.Total),
    UltimaActualizacion = DateTimeOffset.UtcNow
};
return resumen;   // [CosmosDBOutput] hace upsert
```

El `Id` es determinista — siempre `resumen-{clienteId}` para el mismo cliente. Como `[CosmosDBOutput]` hace **upsert** (insert si no existe, replace si existe), si la función se ejecuta dos veces con el mismo batch (at-least-once), la segunda ejecución **sobreescribe el documento** con el mismo contenido. Resultado: un solo documento por cliente, sin duplicados.

Si en lugar de un id estable usaras `Id = Guid.NewGuid()`, cada ejecución crearía un documento nuevo. Dos ejecuciones del mismo batch generarían dos documentos `resumen` para el mismo cliente — duplicación. Es el mismo patrón que aprendiste en S3.3 con `TryAdd`, ahora aplicado al output binding de Cosmos.

> 🧠 **`[CosmosDBOutput]` es upsert por id, no append**. Si el documento con ese id existe, lo reemplaza entero; si no, lo crea. Esa garantía sobre `Id` estable es lo que hace el patrón idempotente. La trampa: si tu lógica de cálculo es "sumar al valor actual", **no funciona con upsert** — necesitas leer primero, sumar, escribir. Para esos casos hay patrones más complejos (transaccional con stored procedures, change feed reading current state). El caso del ejemplo es directo porque "resumen actual" se calcula entero desde los pedidos del batch.

---

## 6. La regla del lease container distinto por consumidor

Cada `[CosmosDBTrigger]` tiene su propio atributo `LeaseContainerName`:

```csharp
// NotificacionesPedidoFunction
[CosmosDBTrigger(
    databaseName: "tienda",
    containerName: "pedidos",
    Connection = "CosmosDbConnection",
    LeaseContainerName = "leases-notificaciones",    // ← lease propio
    CreateLeaseContainerIfNotExists = true)]

// MaterializarResumenClienteFunction
[CosmosDBTrigger(
    databaseName: "tienda",
    containerName: "pedidos",
    Connection = "CosmosDbConnection",
    LeaseContainerName = "leases-resumenes",         // ← lease distinto
    CreateLeaseContainerIfNotExists = true)]
```

Los dos triggers leen del mismo container fuente (`pedidos`) pero con **leases independientes**. Eso significa:

- **Pueden avanzar a ritmos distintos**. Si `Notificaciones` procesa rápido y `MaterializarResumen` es lento, cada uno tiene su propio marcador. Cosmos no espera al lento.
- **Pueden fallar independientemente**. Si `MaterializarResumen` lanza excepción y queda en retry, `Notificaciones` sigue funcionando sin enterarse.
- **Pueden añadirse o quitarse sin afectar a los demás**. Crear una tercera función con un tercer lease `leases-analytics` empieza a procesar desde el momento en que se despliega, sin tocar las otras dos.

El error típico cuando empiezas con Change Feed: **dos triggers compartiendo el mismo lease**. Lo que pasa entonces es que **los dos consumidores se reparten los cambios** (no cada uno los ve todos). Si tu intención era "dos consumidores independientes", obtienes "un consumidor con dos instancias balanceando carga". Lo opuesto a lo que querías.

> 🧠 **La regla del lease**: **un lease container distinto por cada consumidor lógico**. Si quieres "notificaciones + analytics" como dos sistemas independientes, dos leases. Si quieres "notificaciones repartidas en paralelo entre N instancias para más throughput", un solo lease. Lo segundo es escalado dentro de un consumidor; lo primero es N consumidores. La diferencia es semántica y se materializa en el atributo.

---

## 7. At-least-once: el mismo cambio puede llegar dos veces

Three escenarios donde el Change Feed entrega un cambio más de una vez:

- **Reasignación de partición**. Functions reparte particiones entre instancias. Si una instancia muere, su partición pasa a otra que **vuelve a procesar desde el lease guardado**. Si el lease no se había avanzado tras la última invocación, los cambios procesados antes se reprocesan.
- **Excepción no controlada**. Si tu handler lanza, el runtime considera la invocación fallida y reintenta. Si la primera vez procesaste 5 documentos del batch y el sexto lanzó, en el retry los 5 anteriores llegan otra vez.
- **Reinicio o redeploy**. Cuando reinicias la Function App, el último batch que estaba procesando puede no haber actualizado el lease. Al volver, se procesa desde el lease anterior.

La consecuencia: **tu código debe ser idempotente**. Los dos patrones del ejemplo:

| Función | Patrón de idempotencia |
| --- | --- |
| `NotificacionesPedidoFunction` | `ConcurrentDictionary.GetOrAdd` con clave `(PedidoId, Estado)` — si ya hay notificación para ese pedido + estado, no se duplica |
| `MaterializarResumenClienteFunction` | `Id = $"resumen-{clienteId}"` + upsert via `[CosmosDBOutput]` — sobreescribe consistentemente |

Si tu lógica tiene **efectos externos no idempotentes** (mandar email, llamar a API de cobro), tienes que protegerlos tú: almacenar "ya he mandado email para esta transición" antes de mandar, comprobarlo antes de cada envío. La regla mental: **trata cada handler de Change Feed como un método que puede ejecutarse 2-3 veces para el mismo input**. Si ese supuesto rompe tu código, el código está mal.

---

## 8. Recorrido guiado

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | Arranca el emulador local (`./scripts/99-emulator.sh up`) y la Function App (`func start`) | logs "Job host started" sin errores | El skeleton con dos `[CosmosDBTrigger]` registrados. |
| 2 | Inserta un pedido en el container `pedidos` con `clienteId: "cli-A"` y `estado: "confirmado"` | en 5-10 s: log "Notificación enviada: ped-001 confirmado" + "Resumen actualizado para cli-A" | Los dos triggers procesan el mismo cambio independientemente. |
| 3 | `curl /api/notificaciones` | array con la notificación recién creada | Endpoint HTTP de inspección. |
| 4 | `curl /api/resumenes/cli-A` | JSON con `totalPedidos: 1, importeAcumulado: 150` | El upsert escribió en `resumenes-clientes` (verifícalo también desde el Data Explorer). |
| 5 | Inserta dos pedidos más del mismo cliente | el resumen pasa a `totalPedidos: 3, importeAcumulado: 450` | El mismo documento `resumen-cli-A` se actualiza (upsert por id estable). |
| 6 | Actualiza un pedido cambiando `estado` a `"cancelado"` | nueva notificación para el mismo `pedidoId` pero distinto `estado` | La clave de idempotencia es `(PedidoId, Estado)`, no solo `PedidoId`. |
| 7 | Para la Function App y reinicia | al arrancar, los cambios pendientes se procesan; los ya procesados no se duplican | Resilencia + idempotencia en acción. |

Un experimento útil: en el paso 6, inserta **varios pedidos con el mismo estado del mismo cliente** muy rápido. Verás que la función agrupa en el batch (un solo `Procesar` con N pedidos) y actualiza el resumen con la suma agregada. Eficiente: una llamada a Cosmos para escribir el resumen, no N.

Y otro experimento didáctico: borra el container `leases-notificaciones` desde el Data Explorer mientras la app está parada. Cuando reinicies, **el trigger empezará desde el cambio actual** (no desde el principio del feed), porque sin lease no sabe dónde estaba. Si quieres "reprocesar todo", añade `StartFromBeginning = true` en el atributo y borra el lease antes de arrancar.

---

## 9. Tests del proyecto

Veintiocho tests cubren los dos handlers más los servicios y endpoints de inspección. Los más valiosos pedagógicamente:

- **`InMemoryNotificacionServiceTests` (5)**, incluye un **test de concurrencia**: 100 hilos compitiendo por insertar la misma clave `(PedidoId, Estado)` → solo uno gana. Es la verificación explícita del `GetOrAdd` bajo paralelismo, equivalente al test de idempotencia que viste en S3.3.
- **`NotificacionesPedidoFunctionTests` (8)** — caja completa: notificación por estado, idempotencia sobre el mismo batch, varios estados del mismo pedido, batch vacío, error individual que no aborta el batch.
- **`MaterializarResumenClienteFunctionTests` (6)** — agrupación por cliente, totales correctos, idempotencia con id estable, upsert vs append, descarte de pedidos sin `clienteId`.

Tests por instanciación directa, **sin emulador real**. Arrancar el emulador en CI es frágil (~1-2 min de boot, problemas de cert, dependencia de Docker en runners). La validación end-to-end se hace con `03-smoke-test.sh` contra Cosmos real o emulador local cuando hace falta.

Y sigue valiendo la advertencia de los anteriores: estos tests **no ejercen el contenedor de DI**. La lista de servicios en `Program.cs` que comentar contra los constructores aplica igual aquí.

---

## 10. Puesta en marcha, ejecución y pruebas

### 10.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y testear | Sí |
| Azure Functions Core Tools (`func`) | `func start` local | Recomendado |
| Docker | arrancar el emulador local de Cosmos | Recomendado |
| Suscripción Azure con Cosmos DB serverless | desplegar | Solo si vas a desplegar |

### 10.2 Compilar y arrancar en local

```bash
cd examples/M03-Azure-Functions-I/S3.5-trigger-cosmosdb-changefeed
dotnet build AzureFunctions.Demo.slnx       # 0 errores

cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json
# La connection string del emulador ya está en el example

# Arrancar emulador:
./scripts/99-emulator.sh up

# Crear los containers desde el Data Explorer del emulador:
#   tienda > pedidos        (PK /clienteId)
#   tienda > resumenes-clientes  (PK /clienteId)
# Los lease containers los crea el runtime automáticamente

# Arrancar la Function App:
cd src/AzureFunctions.Demo
func start
```

Inserta un pedido manualmente en el Data Explorer y observa los logs.

### 10.3 Pasar los tests

```bash
dotnet test
```

Resultado: **28 pass · 0 fail**. Sin Azure, sin Docker, sin emulador.

### 10.4 Desplegar a Azure (resumen)

El detalle por Portal está en el [`README.md`](README.md). Pasos clave:

1. **RG + Cosmos DB serverless** (SQL API).
2. **Database `tienda`** con containers `pedidos` (PK `/clienteId`) y `resumenes-clientes` (PK `/clienteId`).
3. **Function App** Consumption Linux .NET 10 isolated (mismo patrón que S3.1-S3.4).
4. **App Setting** `CosmosDbConnection` con la connection string primaria de Cosmos.
5. **Deploy** desde VS Code.
6. **Verificar** insertando un pedido en `pedidos` desde el Data Explorer y observando el Monitor de las funciones.

Los lease containers (`leases-notificaciones`, `leases-resumenes`) los crea el runtime automáticamente la primera vez gracias a `CreateLeaseContainerIfNotExists = true`.

### 10.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| El trigger no se ejecuta tras desplegar | falta `CosmosDbConnection` o el nombre no coincide con `Connection = "..."` | revisa App Settings y atributo |
| Procesa documentos viejos al primer arranque | el lease nunca existió o se borró | comportamiento esperado si `StartFromBeginning = false`; pon `true` y borra el lease para reprocesar todo |
| El mismo pedido aparece dos veces | at-least-once delivery, normal | implementa idempotencia con clave estable (`(PedidoId, Estado)` o `Id = "resumen-..."` para upsert) |
| Tarda mucho en detectar cambios | `feedPollDelay` por defecto (5s) | reduce en `host.json` si necesitas menor latencia |
| Eliminaciones no se notifican | por diseño del Change Feed | soft delete con campo `eliminado: true`, o `AllVersionsAndDeletes` mode (requiere continuous backup) |
| Los dos consumidores compiten por los mismos cambios | comparten el mismo lease container | dales leases distintos (sección 6) |

### 10.6 Limpieza

`Portal → Resource groups → rg-curso-m03-s35 → Delete`. Borra Cosmos + Function App + Storage. **Importante**: Cosmos serverless no tiene coste fijo pero un account abandonado puede acumular RUs si queda con datos. Borrar el RG en cuanto acabes la demo es lo seguro.

---

## 11. Ideas para llevarte

Lo más útil de esta práctica es **interiorizar Change Feed como pieza de arquitectura**. Cuando Cosmos esté en tu sistema y necesites "que algo reaccione a cambios en X container", la primera opción correcta es Change Feed con un consumidor nuevo en una Function App. Es más simple, más barato y más resiliente que la alternativa (publicar un evento a Service Bus desde el código que escribe, mantener la suscripción).

Sobre **leases independientes**: un lease container por cada consumidor lógico. Si añades una nueva función que reacciona a cambios, dale un lease propio. Compartir leases para "balancear carga" es un caso distinto y poco común — la mayoría de las veces lo que quieres es N consumidores independientes.

Sobre **idempotencia**: aplica la regla del id estable siempre que el output sea un upsert. Para efectos externos no idempotentes (emails, llamadas a APIs de pago), guarda explícitamente "ya hice esto para esta clave" antes de hacerlo, y compruébalo. La regla del HANDOFF aplica: at-least-once es la garantía; exactly-once es responsabilidad tuya.

Y un consejo pragmático: **el Change Feed Estimator** en el portal de Cosmos te dice cuánto lag tiene cada consumidor. Si crece sostenidamente, tienes un problema (consumidor lento, errores que generan retries, escalado insuficiente). Es una métrica operativa muy útil; configúrala como alerta para los consumidores críticos.

---

## 12. Comprueba que lo has entendido

1. Tu app guarda pedidos en Cosmos y necesita: notificar al cliente, actualizar el dashboard y registrar en analytics cada cambio. ¿Tres llamadas síncronas, Service Bus, o Change Feed con tres consumidores? ¿Por qué? *(secciones 2, 3)*
2. ¿Qué pasa si dos `[CosmosDBTrigger]` tienen el mismo `LeaseContainerName`? *(sección 6)*
3. Una función con `[CosmosDBOutput]` devuelve un documento con `Id = Guid.NewGuid()`. La función se ejecuta dos veces con el mismo batch (at-least-once). ¿Cuántos documentos se crean en el container de salida? *(sección 5)*
4. ¿Cuándo conviene `StartFromBeginning = true` y cuándo dejarlo en `false`? *(sección 8 experimento)*
5. El Change Feed Estimator del portal muestra lag creciente en `leases-notificaciones`. ¿Qué significa y qué pasos tomar? *(sección 11)*
6. Tu función handler de Change Feed manda un email externo y luego escribe en Cosmos. La función falla **después de mandar el email** pero antes de escribir. ¿Qué pasa en el retry? ¿Cómo lo proteges? *(sección 7)*

<details>
<summary>Respuestas</summary>

1. **Change Feed con tres consumidores independientes**. Razones: **(a) desacoplamiento** — el código que guarda el pedido no sabe nada de notificaciones, dashboards o analytics, solo escribe a Cosmos; **(b) resilencia** — si analytics tiene un problema con la API externa, notificaciones y dashboard siguen funcionando, cada uno con su propio lease; **(c) coste** — no añades infraestructura nueva (Service Bus o Event Grid), pagas las RUs que ya pagabas, el Change Feed es gratis; **(d) operacional** — añadir un cuarto consumidor mañana es desplegar una función nueva con un lease propio, sin tocar las otras tres. Llamadas síncronas serían acoplamiento fuerte (un fallo cae todo). Service Bus sería válido pero hay que publicar manualmente desde el código que guarda — más superficie de bug.
2. **Los dos triggers se reparten los cambios entre ellos** en lugar de ver cada uno todos los cambios. Functions usa el lease para coordinar qué instancia procesa qué partición; si los dos triggers comparten lease, Functions los trata como **dos instancias del mismo consumidor lógico**, balanceando carga. Si tu intención era "dos consumidores independientes", lo que obtienes es "un consumidor con dos instancias paralelas". Para independencia: lease container distinto por consumidor.
3. **Dos documentos**. Cada ejecución crea un `Guid` nuevo, así que el upsert se aplica a un id distinto cada vez — termina creando uno nuevo. Resultado: duplicación. El bug clásico de at-least-once + id aleatorio. La solución es **id estable basado en la clave del agregado** (`Id = $"resumen-{clienteId}"`, `Id = $"notif-{pedidoId}-{estado}"`), de forma que dos ejecuciones del mismo input apunten al mismo documento y el upsert lo reemplace consistentemente.
4. **`StartFromBeginning = true`** cuando despliegas un consumidor nuevo y quieres que procese **todo el histórico** del Change Feed (útil para inicializar una vista materializada con datos pasados). **`StartFromBeginning = false`** (default) cuando quieres procesar solo cambios futuros — el comportamiento estándar de "suscribirse a partir de ahora". En producción, casi siempre `false`; pon `true` solo en migraciones o inicializaciones controladas y vuelve a `false` después.
5. **Lag creciente significa que el consumidor está procesando más lento que la entrada de cambios**. Tres causas comunes: **(a) el handler tarda demasiado** — código pesado, llamadas externas lentas, retry storms; **(b) errores frecuentes** — cada excepción reprocesa el batch, duplicando trabajo; **(c) escalado insuficiente** — una instancia procesando lo que necesita más. Pasos: revisar los logs del consumidor para errores, mirar el tiempo medio por invocación, considerar Premium plan con más instancias, ajustar `MaxItemsPerInvocation` para batches más pequeños y más frecuentes, o (si el consumidor tiene I/O pesado) hacer el trabajo asíncrono y devolver rápido del handler.
6. **En el retry, el email se manda otra vez**. El handler se reprocesa entero porque la primera invocación falló — `Cosmos` no sabe que ya mandaste el email, solo sabe que tu función falló. Resultado: dos emails al cliente. Para protegerlo: **almacena el "ya hice esto"** antes del efecto externo. Por ejemplo, escribe en Cosmos un documento `notificacion-{pedidoId}-{estado}` con upsert atómico **antes** de mandar el email. Si el handler se ejecuta dos veces, el segundo intento ve que ya existe el documento y no manda el email. El patrón se llama "transactional outbox" o "idempotency key" y es el equivalente externo de lo que `ConcurrentDictionary.GetOrAdd` hace en memoria. En sistemas reales con efectos no reversibles (pagos), esta protección no es opcional.

</details>

---

## 13. Hasta aquí

Vuelve a la imagen del periódico con varios lectores de la sección 4. Cada lector con su marcador, leyendo a su ritmo, indiferentes los unos a los otros. Si añades un cuarto lector mañana, los tres anteriores no se enteran. Esa imagen captura toda la arquitectura event-driven que Cosmos te ofrece sin coste adicional: el log existe, lo lees con tu propio lease, y tu consumidor es independiente del resto.

Lo siguiente es [`S3.6 — Bindings de entrada y salida`](../S3.6-bindings-entrada-salida/MANUAL.md), que cierra la parte conceptual del M03. Hasta aquí hemos visto **triggers** (HTTP, Timer, Blob, Cosmos). En S3.6 entran los **input bindings** (leer un blob de entrada por parámetro sin abrir cliente) y **output bindings hacia más servicios** (Service Bus, Event Grid, Tables). Es el conocimiento que cierra la caja de herramientas de Functions antes de las prácticas.
