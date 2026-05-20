# Manual del alumno — S5.3 · Cosmos DB

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica del ejemplo: estructura, mapeo a slides, comandos de test, despliegue por Portal. Útil cuando vas a tocar código. Este manual va antes: te cuenta para qué existe el ejemplo, qué decisiones quiere enseñarte y cómo leerlo. Cuando termines, abre el README y todo encajará más rápido.

Tiempo de lectura: ~30 min. Submódulo de teoría: [M05-S5.3](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.3-cosmosdb-v3.md) (~33 slides). Las primeras cuatro secciones son el marco mental; de la sección 5 a la sección 8 entras al detalle técnico; el resto es práctica, autoevaluación y un par de avisos antes de pasar a S5.4.

*Creado: 2026-05-20 00:02 +0200*

---

## 1. La idea en una frase

S5.2 te enseñó cuándo SQL es la respuesta correcta: relaciones, JOINs, transacciones ACID. S5.3 entrena el otro lado de la decisión, dentro de las bases de datos: cuándo Cosmos DB **sí** se justifica, y cómo diseñarlo para que la factura mensual no sea una lotería. Spoiler para que no te pille: lo que de verdad cuesta en Cosmos no es el precio por gigabyte. Son las queries mal diseñadas.

En Cosmos hay **tres decisiones de diseño** que se toman al principio y duran para siempre: la **partition key**, el **modelo de datos** y el **nivel de consistencia**. Las tres se eligen antes de escribir una sola línea de SDK. Si las eliges bien, Cosmos te da escala global, latencia de un dígito y un coste predecible. Si las eliges mal, descubres en producción que cambiar la partition key implica recrear el container desde cero, migrando datos a mano. Una decisión literalmente irreversible. Y ahí es donde empieza el dolor.

Aquí aprendes a tomar las tres antes de teclear.

---

## 2. El problema real que hay detrás

Un equipo me consultó hace tiempo por un caso clásico: su API contra Cosmos DB funcionaba perfecta los primeros meses. Latencia bajísima, factura razonable. Y un lunes por la mañana, sin haber tocado el código, empezó a devolver errores `HTTP 429 — Request rate is large` a la cara de los usuarios. Throttling.

La query culpable era esta:

```sql
SELECT * FROM c WHERE c.total > 100
```

Sin partition key en el filtro. Cross-partition. Cuando la base tenía 500 documentos costaba 5 RU y nadie se daba cuenta. Con 50.000 documentos costaba 1.500 RU por ejecución, y un pico de tráfico bastaba para saturar el throughput aprovisionado. **Las RU son la moneda de Cosmos**: cada operación consume su parte, hay un presupuesto por segundo, y si te pasas, Cosmos te corta — no se cae, te corta. La diferencia entre una query bien diseñada (1 RU) y una mal diseñada (1.500 RU) son tres órdenes de magnitud y, en producción, son la diferencia entre un servicio que funciona y uno que tira 429s.

Esto es el equivalente Cosmos de la historia que abría S5.1: un cliente pagando 1.200 € al mes en Cosmos por logs que en Table costaban siete céntimos. La factura silenciosa que se cobra cuando alguien diseña sin entender la herramienta.

Ahora piensa en lo que necesita una API de pedidos contra Cosmos DB:

| Necesidad real | ¿Y por qué Cosmos? | Dónde lo verás |
| --- | --- | --- |
| "Dame todos los pedidos de un cliente" rápido | Partition por `/clienteId` → single-partition, ~3 RU | [`IPedidoRepository.cs`](src/Cosmos.Demo.Api/Repositories/IPedidoRepository.cs), `PorClienteAsync` |
| "Dame el pedido X del cliente Y" rapidísimo | Read-by-id dentro de su partición = **1 RU** | `PedidoRepository.GetAsync` |
| Pedido + cliente + items en una sola lectura | Modelo desnormalizado (embed) → 1 documento, 1 RU | [`Modelos.cs`](src/Cosmos.Demo.Api/Domain/Modelos.cs) |
| "Borrar" sin perder el rastro para Change Feed | Soft delete: `Eliminado = true`, no DELETE real | `PedidoRepository.SoftDeleteAsync` |
| Pedido + movimiento de saldo atómicamente | TransactionalBatch en la misma partición | `CrearPedidoConMovimientoAsync` |

Cada fila demuestra una de las tres decisiones de diseño. Y la mayoría sale natural cuando la partition key está bien elegida; sale forzada cuando no.

---

## 3. Por qué esto importa en tu stack

Ya usas Cosmos en algún sitio. Lo conectaste con Functions vía Change Feed en M03/M04 sin pararte a mirar mucho: marcaste el binding, te traía documentos cuando cambiaban. Funcionaba. Y precisamente porque funciona, la mayor parte del equipo nunca llega a aprender lo que pasa por debajo. Hasta que un día llega el 429.

Lo que cambia respecto a S5.2 es la mentalidad. En SQL piensas en **filas y joins**. En Cosmos piensas en **documentos JSON y particiones**. El motor es completamente distinto: el modelo de coste no es CPU, es **Request Units por segundo**; las queries baratas las tienes que diseñar tú, no las descubre el optimizador; y `JOIN` entre containers, simplemente, no existe.

El cambio de stack respecto a S5.2: ya no hay EF Core ni migraciones. Es el SDK `Microsoft.Azure.Cosmos` directo, sin ORM. Sin proveedor in-memory tipo SQLite (en S5.3 no hay capa Component como en S5.2: el SDK de Cosmos no tiene equivalente). El round-trip se prueba contra el **emulador de Cosmos** en Docker, que arranca lento y a veces no arranca — por eso el `SkippableFact` no es trampa, es la única forma sensata de mantener verde la suite sin un emulador siempre disponible.

---

## 4. El modelo mental: la biblioteca con sucursales

Piensa en una biblioteca municipal con sucursales repartidas por barrios. Una biblioteca central marca las reglas — qué se cataloga, qué reglas de préstamo, qué horarios — pero los libros viven repartidos por las sucursales según un criterio que se decidió al inaugurar: por inicial del apellido del autor, por ejemplo. Sucursal A para autores A-D, sucursal B para E-J, y así.

Si entras pidiendo "todos los libros de García", la bibliotecaria sabe exactamente a qué sucursal mandarte: a la sucursal G, donde están todos juntos. Vas, los miras, te llevas el que quieras. Rápido. Barato. Una llamada de teléfono.

Si entras pidiendo "todos los libros publicados después de 2020 con más de 300 páginas", la cosa cambia. La bibliotecaria tiene que llamar a **todas las sucursales**, esperar a que cada una revise sus fichas, recopilar lo que digan y devolverte la lista. Lento. Caro. Múltiples llamadas. Y si hay treinta sucursales, la consulta se multiplica por treinta.

Eso es Cosmos DB. La **cuenta** es la biblioteca municipal. La **base de datos** es una colección de catálogos. El **container** es un catálogo concreto — *pedidos*, *productos* — con sus reglas y su criterio de reparto. La **partition key** es ese criterio: cómo se reparten los documentos por sucursales. Y las **Request Units** son el coste de cada operación: barato cuando preguntas por la sucursal correcta, caro cuando obligas a llamar a todas.

```
Cosmos DB Account (cosmos-ventas-prod)
├── Database: "tienda"
│   ├── Container: "pedidos"   (partition key: /clienteId)
│   │   ├── { id: "ped-1", clienteId: "cli-001", items: [...], total: 1059.97 }
│   │   ├── { id: "ped-2", clienteId: "cli-001", items: [...], total:   29.99 }
│   │   └── { id: "ped-3", clienteId: "cli-002", items: [...], total:   59.90 }
│   │
│   └── Container: "productos" (partition key: /categoria)
│       ├── { id: "p-1", categoria: "electrónica", nombre: "Laptop"     }
│       └── { id: "p-2", categoria: "accesorios",  nombre: "Mouse"      }
```

Tres frases para fijar la imagen:

- **El container es como una tabla, pero sin schema fijo.** Cada documento puede tener campos diferentes. JSON puro.
- **La partition key se elige al crear el container y NO se puede cambiar.** Si te equivocas, recreas y migras a mano. Es la decisión más importante de Cosmos y la que más gente da por hecha.
- **No hay JOINs entre containers.** Si quieres "pedido + cliente" en una lectura, el cliente vive **dentro** del pedido (desnormalizado). Punto.

Vuelve a la biblioteca cuando dudes. Cada vez que oigas "single-partition" o "cross-partition", piensa "llamada a una sucursal" o "llamada a todas". Cada vez que oigas "RU", piensa "lo que cuesta esa llamada". El resto del submódulo se entiende mejor con esa imagen detrás.

---

## 5. Las tres decisiones que define el diseño de Cosmos

### 5.1 Partition key: la decisión que no puedes deshacer

[`PartitionKeyAdvisor.cs`](src/Cosmos.Demo.Api/Cosmos/PartitionKeyAdvisor.cs) codifica las tres reglas de la slide 5 como tabla de decisión pura. Una partition key es **buena** cuando cumple las tres:

```csharp
if (cardinalidad < UmbralBajaCardinalidad) return Mala;  // pocas particiones
if (!distribucionUniforme)                  return Mala;  // hot partition
if (!alineadaConQueryFrecuente)             return Mala;  // siempre cross-partition
return Buena;
```

- **Alta cardinalidad** — muchos valores distintos. `clienteId` (miles) sí, `pais` (en torno a 200) no, `estado` (4 valores) jamás.
- **Distribución uniforme** — el volumen por partición se parece. Si el 80% de tus clientes están en España y partes por `pais`, la partición *ES* se sobrecarga y las otras se aburren. Hot partition.
- **Alineada con la query frecuente** — si filtras por `clienteId` el 80% de las veces, ese es tu candidato. Si filtras por `categoría` y particionas por `clienteId`, todas tus queries son cross-partition desde el día uno.

En el ejemplo, el container `pedidos` parte por `/clienteId` ([`CosmosDefaults.cs`](src/Cosmos.Demo.Api/Cosmos/CosmosDefaults.cs), `PartitionKeyPath`) y la query principal es *"dame los pedidos del cliente X"*. Las tres reglas alineadas. La diferencia entre `~3 RU` por query y `30+ RU` por query es exactamente esto.

> 🧠 **La decisión que no se puede deshacer.** Cambiar la partition key de un container existente requiere **crear un container nuevo y copiar los datos**. No hay "alter table". Por eso es tan importante que lo pienses despacio antes de crear el container — más despacio que cuando diseñas una tabla SQL, donde un `ALTER` siempre te saca del paso.

### 5.2 Request Units: la moneda invisible

Cada operación consume un número de RU. Es la unidad universal de coste de Cosmos — y, en serverless, lo que pagas. [`RuEstimator.cs`](src/Cosmos.Demo.Api/Cosmos/RuEstimator.cs) modela los órdenes de magnitud que tienes que tener metidos en la cabeza (slide 7):

```csharp
public const double LeerPorId           = 1;    // 1 RU
public const double QuerySingle         = 3;    // 2-3 RU
public const double EscrituraPorDoc     = 5;    // 5-6 RU
public const int    FactorCrossPartition = 10;  // x10 a x100
```

Léelo como una escala logarítmica: leer por id es la operación más barata que existe (1 RU), una query bien diseñada dentro de una partición ronda los 3, escribir cuesta 5 por documento, y una query cross-partition multiplica por entre 10 y 100 lo que ya costaba la single-partition. Eso quiere decir que la misma query mal escrita puede pasar de costar 3 RU a costar 300 RU sin que cambie ni una letra en tu código de aplicación — solo cambiando si filtras o no por la partition key.

> 🧠 **La regla práctica que más dinero ahorra.** Siempre que puedas, **lee por id** (`ReadItemAsync`). Cuando no puedas, **incluye la partition key en `QueryRequestOptions`** para que la query sea single-partition. Cross-partition solo cuando realmente necesitas escanear todo (informes nocturnos, agregados globales), y entonces piensa si esa query debería vivir en Cosmos o en otro sitio (Synapse Link, materialized view, una tabla de agregados).

El endpoint `GET /cosmos/ru-estimado?op=…&docs=…` te deja jugar con la calculadora sin tocar Azure. Pruébala — predice el coste antes de mirar la respuesta.

### 5.3 Consistencia: cinco niveles y un default que casi siempre acierta

[`ConsistencyAdvisor.cs`](src/Cosmos.Demo.Api/Cosmos/ConsistencyAdvisor.cs) codifica los cinco niveles de la slide 11 como tabla de decisión:

| Nivel | Garantía | RU |
| --- | --- | --- |
| **Strong** | La lectura siempre devuelve la última escritura. Todos ven lo mismo. | 2x |
| **Bounded Staleness** | Lecturas con desfase máximo N versiones o T tiempo. | 2x |
| **Session** (default) | Consistente dentro de la sesión del cliente que escribió. | 1x |
| **Consistent Prefix** | Nunca ves escrituras fuera de orden. | 1x |
| **Eventual** | Sin garantías. La más rápida y la más barata. | 1x |

> 🧠 **Session es lo correcto para el 90% de los casos.** Si tú acabas de crear un pedido, tu siguiente `GET` ese pedido lo ve — eso te lo garantiza Session. Otro usuario en otra sesión puede ver una versión ligerísimamente antigua, pero hablamos de milisegundos. Strong (todos ven lo mismo siempre) dobla las RU y añade latencia: úsalo solo si el negocio lo exige — saldos financieros, inventario crítico. Eventual (la más rápida) para telemetría y feeds donde nadie va a notar un desfase.

La consistencia se puede **debilitar** desde el cliente (Session → Eventual para un endpoint concreto) pero **no reforzar** por encima de la configurada en la cuenta. [`Program.cs`](src/Cosmos.Demo.Api/Program.cs) expone esa opción vía configuración (`CosmosConsistency`).

---

## 6. El modelo de datos: desnormaliza o muere

Aquí está el otro cambio mental fuerte respecto a SQL. En relacional **normalizas** para evitar duplicación: cliente en `Clientes`, items en `Items`, pedido en `Pedidos`, y resuelves "pedido completo" con tres JOINs. En Cosmos **desnormalizas** para optimizar lecturas: el pedido lleva el cliente y los items **dentro**, en un solo documento JSON. Resuelves "pedido completo" con una lectura.

Mira [`Modelos.cs`](src/Cosmos.Demo.Api/Domain/Modelos.cs):

```csharp
public sealed class Pedido
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ClienteId { get; set; } = "";        // partition key
    public string ClienteNombre { get; set; } = "";    // desnormalizado
    public List<PedidoItem> Items { get; set; } = [];
    public decimal Total { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public bool Eliminado { get; set; }                // soft delete
    public DateTimeOffset? EliminadoEn { get; set; }
}
```

`ClienteNombre` está duplicado dentro del pedido. Sí, ya sé lo que estás pensando — *"¿y si el cliente cambia de nombre?"*. Tienes razón en preguntarlo. La respuesta honesta: tendrías que actualizarlo en todos sus pedidos, normalmente con una función que reaccione al Change Feed. Pero piensa la frecuencia: la query "obtener pedido completo" se ejecuta diez mil veces al día. La operación "el cliente cambió de nombre" se ejecuta una vez al año, si se ejecuta. En relacional pagas un JOIN cada lectura para evitar un cambio raro. En Cosmos eliges al revés. La desnormalización gana por goleada cuando los ratios son los de este caso.

> 🧠 **El POCO va sin atributos de serializador.** El cliente Cosmos se configura con `CosmosPropertyNamingPolicy.CamelCase` ([`Program.cs`](src/Cosmos.Demo.Api/Program.cs)) y `Id` se convierte en `"id"` (lo exige Cosmos), `ClienteId` en `"clienteId"`. Sin `[JsonProperty]` por todas partes. Eso sí: **el SDK 3.x usa Newtonsoft.Json por defecto** y necesita que lo referencies explícitamente — si no, el build falla. El csproj del ejemplo lo trae.

### 6.1 Soft delete: por qué nunca un DELETE real

El Change Feed de Cosmos —ese log de cambios que ya conectaste con Functions— **no registra los DELETE**. Si una función downstream necesita reaccionar cuando "un pedido se cancela", un `DeleteItemAsync` simplemente desaparece. La función nunca se entera.

Por eso el ejemplo no borra de verdad: marca un campo `Eliminado = true` con un `UpsertItemAsync` ([`PedidoRepository.SoftDeleteAsync`](src/Cosmos.Demo.Api/Repositories/IPedidoRepository.cs)). El Change Feed lo ve como un **UPDATE** —un cambio en el documento— y las funciones que escuchan pueden reaccionar leyendo el campo `eliminado`. Los lectores normales filtran por `eliminado = false` y para ellos el pedido "no existe" (`GetAsync` devuelve `null`, devolviendo `404`). Patrón estándar Cosmos, slide 12.

### 6.2 TransactionalBatch: ACID dentro de una partición

Cosmos **no tiene transacciones globales** como SQL, pero sí ofrece transacciones **dentro de una misma partición lógica**. `TransactionalBatch` te permite agrupar varias operaciones contra la misma partition key y ejecutarlas atómicamente — o pasan todas, o no pasa ninguna. El método `CrearPedidoConMovimientoAsync` es el ejemplo:

```csharp
var batch = container.CreateTransactionalBatch(new PartitionKey(dto.ClienteId))
    .CreateItem(pedido)
    .CreateItem(mov);

using var resp = await batch.ExecuteAsync();
return (resp.IsSuccessStatusCode, resp.RequestCharge);
```

Pedido y movimiento contable, ambos partition key `/clienteId`, atómicos. Si algo falla, ninguno queda escrito. Esto es lo más parecido a ACID que vas a encontrar en Cosmos. Si necesitas transacciones que **crucen** particiones, Cosmos no es tu sitio: vuelve a S5.2.

---

## 7. Acceso al SDK: por dentro de los repos

[`IPedidoRepository.cs`](src/Cosmos.Demo.Api/Repositories/IPedidoRepository.cs) tiene una particularidad que vale la pena señalar: **cada método devuelve las RU consumidas**.

```csharp
Task<(Pedido? pedido, double ru)> GetAsync(string clienteId, string id);
Task<(IReadOnlyList<Pedido> pedidos, double ru)> PorClienteAsync(string clienteId);
```

Ese `double ru` no es decoración. La propiedad `RequestCharge` de cada respuesta de Cosmos te dice exactamente cuántas RU costó esa operación. **Devuélvelas siempre** en tu API durante el desarrollo: en cuanto la tengas conectada al portal y mires una traza, verás cuáles son tus queries caras y cuáles las baratas. La diferencia entre 1 RU y 300 RU es la diferencia entre un servicio que escala y uno que no, y suele estar a una `PartitionKey` de distancia.

Lee `GetAsync`:

```csharp
var resp = await container.ReadItemAsync<Pedido>(id, new PartitionKey(clienteId));
return resp.Resource.Eliminado
    ? (null, resp.RequestCharge)
    : (resp.Resource, resp.RequestCharge);
```

Read-by-id con la partition key explícita. **1 RU**. Lo más barato que Cosmos te ofrece. El filtro por `Eliminado` es el soft delete — para los lectores, el documento "no existe".

Y `PorClienteAsync`:

```csharp
var opciones = new QueryRequestOptions { PartitionKey = new PartitionKey(clienteId) };
using var it = container.GetItemQueryIterator<Pedido>(query, requestOptions: opciones);
```

La `PartitionKey` en `QueryRequestOptions` es lo que convierte una query potencialmente cross-partition en single-partition. **~3 RU**. Sin ese options, Cosmos tendría que recorrer todas las particiones — la "llamada a todas las sucursales" del modelo mental. La línea es minúscula. La diferencia, dos órdenes de magnitud.

---

## 8. CosmosClient en DI y el if que no existe

Mira [`Program.cs`](src/Cosmos.Demo.Api/Program.cs). Aquí no hay un `if` como en S5.1 o S5.2, porque la decisión de "Managed Identity o connection string" en Cosmos es más uniforme: se hace toda en la cadena de conexión, no en el código. Lo que sí tienes es una decisión silenciosa y crítica:

```csharp
builder.Services.AddSingleton(_ =>
{
    var cs = cfg["CosmosDbConnection"];
    if (string.IsNullOrWhiteSpace(cs))
        cs = CosmosDefaults.EmuladorConnectionString;  // pública, no es secreto

    var options = new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
            { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase },
        ConnectionMode = ConnectionMode.Direct,
        MaxRetryAttemptsOnRateLimitedRequests = 9,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
    };
    return new CosmosClient(cs, options);
});
```

**Singleton siempre.** Esta es la regla más importante del SDK de Cosmos. `CosmosClient` gestiona su propio pool de conexiones, mantiene metadatos cacheados de las particiones y aprende rutas óptimas con el tiempo. Crear uno por petición es un error de rendimiento grave: pagas el handshake completo cada vez, te quedas sin caché y vas más lento. **Uno por aplicación, registrado como singleton en DI.**

Y luego el retry de 429 — los nueve reintentos con hasta 30 segundos de espera total. Eso es lo que convierte un pico de tráfico en una latencia un poco mayor en vez de en una cascada de errores al usuario. Sin esto, en el momento en que tocas el throughput aprovisionado, los 429 empiezan a llegar a tu API. Con esto, el SDK reintenta con backoff. La historia del lunes por la mañana con la que abre este manual, si esa app hubiera tenido el retry, habría sido un blip de latencia en vez de un incendio.

> 🎓 **La clave pública del emulador en `CosmosDefaults.EmuladorConnectionString` no es un secreto.** Es la clave fija documentada por Microsoft del emulador local. Solo funciona contra `https://localhost:8081/`; contra una cuenta real no hace nada. Está ahí para que el ejemplo arranque sin configuración y para que la **CAPA 0 de DI** pueda resolver el contenedor sin variables de entorno.

---

## 9. Recorrido guiado: una API de pedidos en Cosmos

Lanza el emulador y la API (ver sección 11) y abre [`api.http`](src/Cosmos.Demo.Api/api.http). Para cada paso, predice **qué va a pasar y cuántas RU costará** antes de mirar la respuesta.

| # | Petición | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `POST /pedidos` con cliente `cli-001`, dos items | `201 Created` con `ruConsumidas` ~5-7 RU | Crear documento + embed de items y nombre cliente (sección 6). |
| 2 | `GET /pedidos/cli-001` | lista de pedidos del cliente, `ru` ~3 | Single-partition: la PK va en `QueryRequestOptions` (sección 7). |
| 3 | `GET /pedidos/cli-001/{id}` (id del paso 1) | el pedido entero, `ru = 1` | Read-by-id dentro de su partición — la operación más barata. |
| 4 | `DELETE /pedidos/cli-001/{id}` | `204 No Content` | Soft delete: upsert con `Eliminado=true`, NO un DELETE real (sección 6.1). |
| 5 | Repite el paso 3 con ese id | `404 Not Found` | Para los lectores, el pedido "no existe". Para el Change Feed, hubo un UPDATE. |
| 6 | `POST /pedidos/con-movimiento` | `200` con `ru` y `ok: true` | TransactionalBatch: pedido + movimiento atómicos en la misma partición (sección 6.2). |
| 7 | `GET /cosmos/partition-key?cardinalidad=5&uniforme=true&alineada=true` | `{ veredicto: "Mala" }` | Lógica pura: baja cardinalidad → mala (sección 5.1). |
| 8 | `GET /cosmos/partition-key?cardinalidad=5000&uniforme=true&alineada=true` | `{ veredicto: "Buena" }` | Las tres reglas cumplidas. |
| 9 | `GET /cosmos/consistencia?financiero=false&ultimaEscritura=false&latenciaMinima=false` | `{ nivel: "Session", multiplicadorRu: 1 }` | El default que vale para el 90% (sección 5.3). |
| 10 | `GET /cosmos/ru-estimado?op=QueryCrossPartition&docs=10` | `~75 RU` | El coste estimado de una query cross-partition (sección 5.2). Compara con `op=LeerPorId` → 1. |

Un experimento que vale más que la teoría: con la API corriendo y datos creados, abre el portal de Cosmos (o el emulador) y mira la métrica de RU por operación. Las del paso 3 estarán pegadas al suelo. Las del paso 2, un poco más arriba. Si modificaras la query para quitar la `PartitionKey` de las opciones, verías el salto. **El SDK te chiva el coste en tiempo real; aprovéchalo durante el desarrollo, no esperes a producción.**

Los pasos 7, 8, 9 y 10 son los únicos que no llaman a Cosmos: lógica pura. Por eso los unit tests de esas clases corren sin Docker en milisegundos (sección 10).

---

## 10. Por qué el código y los tests están así

La estructura sigue el patrón de S5.1 y S5.2:

- **`Cosmos/` — lógica pura.** `PartitionKeyAdvisor`, `RuEstimator`, `ConsistencyAdvisor`, `CosmosDefaults`. Decisiones modeladas como funciones puras — testeables en milisegundos sin emulador, sin Docker, sin Azure.
- **`Domain/Modelos.cs`** — el POCO desnormalizado, sin atributos de serializador.
- **`Repositories/`** — el repo que envuelve el `Container` y devuelve RU.
- **`Endpoints/`** — Minimal API fina.

Y los tests, en **tres capas** (Cosmos no tiene equivalente in-memory de SQLite, así que no hay CAPA Component):

- **CAPA 1 · Unit** — `Unit_PartitionKeyAdvisorTests`, `Unit_ConsistencyAdvisorTests`, `Unit_RuEstimatorTests`. La lógica pura. Rápida, sin Cosmos.
- **CAPA 0 · DI** — `DiContainer_Tests`. Resuelve `CosmosClient`, `Container` y el repo del `WebApplicationFactory` real en un scope. **No toca Cosmos.** Esto funciona porque el SDK de Cosmos es **lazy**: construir `CosmosClient` + `GetContainer` **no abre conexión**. La conexión se abre con la primera operación real. Por eso esta capa corre **siempre, sin Docker**.
- **CAPA 2 · Integration** — `Integration_CosmosEmuladorTests`. Round-trip real contra el **emulador de Cosmos** en Docker (Testcontainers.CosmosDb) vía la API completa: crear pedido (embed), read-by-id, query single-partition, soft delete (que después devuelve 404), TransactionalBatch. `SkippableFact` con un detalle importante: **captura cualquier excepción de arranque del emulador y se salta**. El emulador de Cosmos es pesado y, a veces, simplemente no arranca a tiempo. Mejor un skip honesto que un build rojo intermitente.

> 🎓 **Por qué no hay CAPA "component" tipo S5.2.** En S5.2, SQLite in-memory hacía de SQL Server light: el mismo modelo EF Core se ejercitaba sin Docker. Cosmos **no tiene proveedor in-memory** equivalente. Lo testeable sin infra vive en CAPA 1 (lógica pura); lo demás necesita el emulador o queda fuera. Mejor reconocerlo y diseñar los tests en consecuencia que inventar un mock falso que pase los tests y rompa en producción.

---

## 11. Puesta en marcha, ejecución y pruebas

Sección operativa. Datos verificados contra el repo.

### 11.1 Requisitos

| Requisito | Versión / cómo | Para qué | ¿Obligatorio? |
| --- | --- | --- | --- |
| .NET SDK | **10.x** — fijado en [`global.json`](global.json) | compilar y ejecutar | Sí |
| Docker | Docker Desktop | levantar el emulador de Cosmos (la API y CAPA 2) | Sí (para usar la API o correr CAPA 2) |
| Cliente REST | extensión *REST Client* de VS Code o `curl` | lanzar [`api.http`](src/Cosmos.Demo.Api/api.http) | Recomendado |

Aviso: el emulador de Cosmos pesa y tarda en estar *ready* la primera vez (1-3 minutos). Tenlo en cuenta antes de probar.

### 11.2 Compilar (verificación rápida sin Cosmos)

```bash
cd examples/M05-Almacenamiento-BBDD/S5.3-cosmosdb
dotnet build Cosmos.Demo.slnx
```

Debe terminar con **0 errores y 0 warnings** (`TreatWarningsAsErrors=true`).

### 11.3 Arrancar el emulador de Cosmos

```bash
docker run -d -p 8081:8081 -p 10250-10255:10250-10255 \
  mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

Espera a que esté *ready* (`curl -k https://localhost:8081/_explorer/index.html` debe responder). [`appsettings.Development.json`](src/Cosmos.Demo.Api/appsettings.Development.json) trae `CosmosDbConnection` vacío, que en `Program.cs` se traduce a la clave pública del emulador (`CosmosDefaults.EmuladorConnectionString`). No hay que configurar nada más.

Antes de probar la API, crea la base de datos `tienda` y el container `pedidos` con partition key `/clienteId`. Lo puedes hacer desde el Data Explorer del emulador (`https://localhost:8081/_explorer/index.html`) o dejando que el test de integración lo cree por ti la primera vez.

### 11.4 Lanzar la API

```bash
dotnet run --project src/Cosmos.Demo.Api
```

- Escucha en **`http://localhost:5083`** ([`launchSettings.json`](src/Cosmos.Demo.Api/Properties/launchSettings.json), perfil `http`).
- Prueba de vida: `GET http://localhost:5083/health` → `{ "status": "ok" }`.

El curso nunca lanza la app por ti. Este `dotnet run` lo ejecutas tú; la verificación automatizada se queda en *build + test*.

### 11.5 Ejercitar el ejemplo

```bash
# Crear un pedido (devuelve id y ru consumidas)
curl -X POST http://localhost:5083/pedidos -H "Content-Type: application/json" \
  -d '{ "clienteId":"cli-001","clienteNombre":"Pedro García",
        "items":[{"productoId":"p-1","nombre":"Laptop","cantidad":1,"precio":999.99}] }'

# Leer por id (sustituye {id} por el devuelto arriba) — 1 RU
curl http://localhost:5083/pedidos/cli-001/{id}

# Listar pedidos del cliente — single-partition, ~3 RU
curl http://localhost:5083/pedidos/cli-001

# Lógica pura: ¿es buena esta partition key candidata?
curl "http://localhost:5083/cosmos/partition-key?cardinalidad=5000&uniforme=true&alineada=true"
```

La sección 9 tiene el guion completo con qué demuestra cada paso.

### 11.6 Pasar los tests

```bash
dotnet test Cosmos.Demo.slnx
```

| Sin Docker | Con Docker y el emulador arrancable |
| --- | --- |
| **25 pass · 1 skip · 0 fail** | **26 pass · 0 skip · 0 fail** |

- **CAPA 1 (unit)** corre siempre — lógica pura, sin Cosmos.
- **CAPA 0 (DI container)** también corre siempre. Esto es **la diferencia con S5.2**: el SDK de Cosmos es lazy, construir `CosmosClient` y `GetContainer` no abre conexión, así que el grafo se puede resolver sin Docker. Es la mejor cobertura "DI sin Docker" que tienes.
- **CAPA 2 (`Integration_CosmosEmuladorTests`)** es `SkippableFact`: intenta levantar el emulador; si no arranca por cualquier motivo, se salta. La suite siempre verde sin Docker; con Docker y el emulador funcionando, pasa también a verde.

### 11.7 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `Connection refused` en `localhost:8081` | Emulador no está corriendo | arranca el contenedor (sección 11.3) |
| El emulador tarda 1-3 min en estar listo | Es lo normal en la primera vez | espera; verifica con `curl -k https://localhost:8081/_explorer/index.html` |
| `SslPolicyErrors: RemoteCertificateChainErrors` | Certificado autofirmado del emulador | importa el certificado del emulador a tu trust store o usa `ConnectionMode.Gateway` con `HttpClientFactory` que ignore validación (solo dev) |
| `NotFound` al primer POST | Falta crear `tienda`/`pedidos` con PK `/clienteId` | créalo desde el Data Explorer o lanza primero el test de integración |
| CAPA 2 sale como *skip* | Docker no está, o el emulador no arrancó | esperado; no es fallo |
| El test devuelve 429 en CAPA 2 | El emulador tiene throughput muy bajo | normal en el emulador; en Azure real con serverless no pasa |

### 11.8 Contra una cuenta de Cosmos real (opcional)

Configura `CosmosDbConnection` con la cadena de Azure. Recomendado sin key (Managed Identity), profundizado en S5.4:

```
AccountEndpoint=https://<cuenta>.documents.azure.com:443/
```

Y el rol *Cosmos DB Built-in Data Contributor* asignado a la identidad de la app. El detalle del aprovisionamiento por **Portal** (cuenta serverless, base, container con PK, consistencia) está en el [`README.md`](README.md).

---

## 12. Checklist de producción (y de qué te protege cada línea)

| Casilla | De qué te protege |
| --- | --- |
| Partition key elegida con las tres reglas (cardinalidad, distribución, alineación) | Hot partitions, cross-partition queries forzadas, throughput desperdiciado |
| Modelo desnormalizado donde toca | Múltiples lecturas por operación → coste RU multiplicado |
| Read-by-id siempre que sea posible | Pagar 100x más por un acceso que podría costar 1 RU |
| `PartitionKey` en `QueryRequestOptions` siempre que se filtre por ella | Que una query inocente se convierta en cross-partition |
| Soft delete en vez de DELETE | Que el Change Feed pierda eventos críticos |
| TransactionalBatch para operaciones que deben ser atómicas en una partición | Estado inconsistente entre documentos relacionados |
| Consistencia: Session por defecto, Strong solo donde el negocio lo exige | RU duplicadas y latencia añadida sin motivo |
| CosmosClient como singleton, **siempre** | Pool de conexiones recreado por petición → caída de rendimiento |
| Retry de 429 configurado (`MaxRetryAttemptsOnRateLimitedRequests`) | Cascadas de errores al primer pico de tráfico |
| Newtonsoft.Json referenciado explícitamente | Build roto al actualizar el SDK |
| Indexing policy revisada (excluir campos grandes que no consultas) | RU consumidas en escrituras por indexar campos inútiles |
| Monitorización de RU por query con `RequestCharge` | Detectar la query cara antes de que la pague el cliente |

---

## 13. Ideas para llevarte

La lección eje es esta: en Cosmos, el coste se decide al **diseñar**, no al codificar. La partition key, el modelo de datos y el nivel de consistencia se eligen una vez al principio, y de esas tres elecciones depende si tu factura ronda los céntimos al día o se dispara. El SDK no te va a salvar de una mala elección; lo único que hace el SDK es ejecutar lo que le pides, y si lo que le pides es caro, te lo cobra.

Sobre la **partition key**, mi recomendación honesta: dedica más tiempo a elegirla del que dedicarías a diseñar una tabla en SQL. En SQL, un `ALTER TABLE` te saca de un mal día; en Cosmos, cambiar la partition key implica un proyecto de migración. Pregúntate con sinceridad: ¿cuál es la query más frecuente? Si el 80% del tiempo vas a filtrar por `clienteId`, la partition key es `clienteId`. Si dudas entre dos candidatas, mira tu Application Insights o tus logs: la que aparece más, gana.

Sobre las **RU**, una regla práctica que ahorra más dolores que ninguna otra: durante el desarrollo, devuelve siempre las RU consumidas en la respuesta de la API. Es feo en producción —tendrás que quitarlas o esconderlas tras un flag de dev—, pero durante el desarrollo es lo que te avisa el día que escribes una query mal y empieza a costar 50 veces más. La señal está ahí, solo tienes que mirarla.

Y una última recomendación, menos comentada: **el modelo desnormalizado no es opcional en Cosmos**. La tentación cuando vienes de SQL es modelar Cosmos como si fueran tablas, pensando que normalizar es "mejor diseño". No lo es en Cosmos. Es exactamente el camino directo a las queries cross-partition de la historia con la que abre este manual. Si tu instinto cuando ves un documento de Cosmos es decir "esto debería estar en otra colección", para. Probablemente es la respuesta equivocada para este motor.

---

## 14. Comprueba que lo has entendido

Sin mirar atrás. Si dudas, vuelve a la sección.

1. Tu API contra Cosmos empieza a devolver 429 cuando tu base supera los 50.000 documentos. La query culpable es `SELECT * FROM c WHERE c.estado = "pendiente"`. ¿Qué falla y qué arreglas? *(sección 5.2, sección 7)*
2. ¿Por qué `CosmosClient` se registra como singleton siempre? ¿Qué pasa si lo creas por petición? *(sección 8)*
3. Tu cliente cambia de nombre. Tienes 500 pedidos suyos con el nombre embebido. ¿Es un problema? ¿Por qué seguimos desnormalizando? *(sección 6)*
4. Eliges como partition key `/fecha` (un campo `yyyy-MM-dd`). Te pasan los datos en producción y empieza a ir lento. ¿Qué regla rompiste? *(sección 5.1)*
5. ¿Por qué el repositorio del ejemplo no llama a `DeleteItemAsync`? ¿Qué hace en su lugar y por qué? *(sección 6.1)*
6. Tienes que registrar pedido + movimiento contable atómicamente. ¿Qué herramienta de Cosmos usas y qué limitación tiene? *(sección 6.2)*
7. ¿Por qué la CAPA 0 de DI corre sin Docker, si S5.3 no tiene equivalente in-memory de SQLite? *(sección 10)*

<details>
<summary>Respuestas</summary>

1. La query es **cross-partition**: no filtra por la partition key (`/clienteId`), así que Cosmos tiene que recorrer todas las particiones. Con pocos documentos no se notaba; con muchos sí. Lo arreglas filtrando también por `clienteId` y pasando la `PartitionKey` en `QueryRequestOptions` para que sea single-partition (factor 10x a 100x menos RU). Si no puedes filtrar por cliente, esta query no debería vivir en Cosmos: piénsala como agregado nocturno con Synapse Link o materialized view.
2. Porque `CosmosClient` gestiona su propio pool de conexiones y cachea metadatos de partición. Crear uno por petición paga el handshake completo cada vez, pierde la caché y va más lento. Es uno de los anti-patrones más comunes y caros.
3. Hay que actualizar el nombre en los 500 pedidos (normalmente con una función que reaccione al Change Feed). Sí, da trabajo. Pero la query "obtener pedido completo" se ejecuta 10.000 veces al día y "cambiar nombre" se ejecuta una vez al año, si se ejecuta. La desnormalización gana por goleada en cuanto los ratios son desbalanceados, que es lo habitual.
4. **Hot partition**. La fecha de hoy se convierte en una partición que recibe el 100% de las escrituras del día; las demás están casi vacías. Rompes la regla de "distribución uniforme". Y como bonus, queries por cliente serían cross-partition. Ambas reglas rotas a la vez.
5. Porque el Change Feed **no registra los DELETE** (slide 12). Si una función downstream tiene que reaccionar a "se canceló un pedido", un DELETE real no le llega — se desentera. El repo hace un upsert con `Eliminado=true`, que el Change Feed ve como UPDATE. Para los lectores normales el pedido "no existe" (la lectura filtra por `Eliminado=false`).
6. `TransactionalBatch`. La limitación importante: **solo funciona dentro de una misma partición lógica**. Las operaciones del batch tienen que compartir partition key. Si necesitas atomicidad entre particiones, Cosmos no es tu sitio — vuelve a SQL.
7. Porque el SDK de Cosmos es **lazy**: construir `CosmosClient` + `GetContainer` no abre conexión. La conexión se abre con la primera operación real (un `ReadItemAsync`, un `CreateItemAsync`). El grafo de DI se puede resolver completo —comprobando que todos los servicios están registrados— sin tocar el emulador. Eso te da la lección DI gratis, sin Docker.

</details>

---

## 15. Hasta aquí

Vuelve a la biblioteca con sucursales de la sección 4. Cada cliente tiene su sucursal — su partición — y mientras preguntes por una sucursal concreta, todo va rápido y barato. En cuanto preguntas por algo que cruza sucursales, el coste se multiplica. Esa imagen, sumada al `RequestCharge` que el SDK te devuelve en cada respuesta, es básicamente todo lo que necesitas para diseñar bien con Cosmos.

S5.4 cierra el círculo de M05 con una pregunta que ha aparecido un par de veces en S5.1, S5.2 y aquí: **conectarse sin secretos**. Ni connection strings con keys, ni cadenas con passwords. Solo Managed Identity y RBAC, que verifica Azure por ti contra Entra ID. Lo vas a usar contra Storage, contra SQL y contra Cosmos a la vez —sí, contra los tres, con la misma identidad— y vas a entender por qué esa decisión es la que separa un proyecto de juguete de uno que se puede dejar en producción.
