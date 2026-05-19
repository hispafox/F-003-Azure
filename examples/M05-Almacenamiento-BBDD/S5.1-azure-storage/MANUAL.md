# Manual del alumno — S5.1 · Azure Storage

> **Qué es este documento.** No es el [`README.md`](README.md) (ese es la
> ficha técnica: estructura de carpetas, mapeo a slides, cómo se construyó,
> cómo se testea, cómo se despliega). **Este manual explica el _para qué_
> y el _porqué_**: qué problema real resuelve este ejemplo, qué decisiones
> quiere enseñarte a tomar, cómo se ve cada cosa de verdad en el SDK y qué
> debes mirar al ejecutarlo. El README te dice *qué hay*; este manual te
> dice *por qué está y qué tienes que entender*.
>
> **Tiempo de lectura:** ~30 min · **Submódulo de teoría:**
> [M05-S5.1](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.1-azure-storage-v3.md)
> (40 slides) · **Cómo leerlo:** las secciones §1–§5 son el marco mental;
> §6–§9 son el contenido técnico en profundidad; §10–§16 son práctica,
> autoevaluación y cierre.

---

## 1. La idea en una frase

> Casi todo lo que una aplicación necesita guardar que **no es una base de
> datos relacional** cabe en los cuatro servicios de un Storage Account.
> Este ejemplo te enseña a **reconocer cuál de los cuatro toca** en cada
> situación, a usarlo correctamente desde .NET, y a no pagar de más por
> elegir el servicio equivocado.

El submódulo no va de "aprender un SDK" — los SDKs de Storage son de las
APIs más simples de Azure, tres líneas por operación. Va de **decidir
bien**. Elegir mal aquí no rompe la app: funciona igual con Cosmos DB que
con Table Storage, igual con Service Bus que con Queue Storage. Lo que
cambia es **la factura a fin de mes, la escalabilidad y cuánto te cuesta
recuperarte de un error**. Esa es exactamente la clase de decisión
silenciosa que un desarrollador toma solo, sin que nadie la revise en un
pull request, y que el curso quiere que sepas razonar.

---

## 2. El problema real que hay detrás

Imagina la aplicación de ventas que acompaña a todo el módulo M05. A lo
largo de un día normal necesita guardar cosas muy distintas:

| Necesidad real | ¿Va en la base de datos? | Servicio correcto | Por qué |
| --- | --- | --- | --- |
| El **PDF de cada factura** emitida | No, es un archivo binario | **Blob** | Una BD no es para archivos: los infla, los encarece y los sirve mal |
| **Quién hizo qué y cuándo** (auditoría, miles de líneas/día) | Sería caro y excesivo | **Table** | Volumen alto, consulta simple por fecha, sin relaciones |
| "Hay un pedido nuevo, **procésalo cuando puedas**" | No, es un mensaje efímero | **Queue** | Desacopla quien avisa de quien trabaja |
| Que contabilidad **abra una carpeta de red** con los informes | No, es un disco compartido | **File** | Una persona/sistema legado monta una unidad, no llama a una API |
| Las **imágenes de producto** del catálogo | No, son archivos | **Blob** | Igual que las facturas; además se sirven con CDN/SAS |
| Un **export CSV nocturno** de ventas | No, archivo temporal | **Blob** (tier Cool) | Se genera, se descarga una vez, se archiva barato |

Cada fila es una decisión real, y **ninguna debería acabar en tu base de
datos relacional ni en Cosmos DB**. Meterlas ahí es el error que este
ejemplo te enseña a *no* cometer. Las cuatro primeras filas son,
literalmente, endpoints de la API del ejemplo: lo que vas a ejecutar es
ese día normal de la app de ventas, en miniatura y reproducible en tu
máquina.

---

## 3. Por qué esto importa en tu stack

El submódulo de teoría lo dice sin rodeos (Slide 2): *"El 80% de lo que
necesitéis como desarrolladores pasa por aquí"*. Ya has usado Blob y Queue
sin pensarlo en los módulos de Functions (M03/M04): un `[BlobTrigger]`,
una cola que dispara una Function. Aquí dejas de usarlos "por inercia
porque el binding lo hacía solo" y empiezas a **elegirlos a propósito**,
desde código de aplicación normal.

El cambio de stack respecto a M03/M04 es deliberado y didáctico: ya **no
es una Azure Function**, es una **Minimal API de ASP.NET** que envuelve
los cuatro SDKs. Quiere que veas el patrón de acceso a datos "desde una
app web normal", que es como lo harás el 90% de las veces — y que el
patrón de tests vuelva al de M02 (`WebApplicationFactory`), no al de
Functions.

---

## 4. El modelo mental que tienes que construir

```
Storage Account  ── stventasprod ──  UNA cuenta · UNA factura · nombre
   │                                  único en TODO Azure (3-24 chars,
   │                                  solo minúsculas y números)
   │
   ├── Blob      → archivos          https://stventasprod.blob.core.windows.net
   ├── Table     → NoSQL key-value   https://stventasprod.table.core.windows.net
   ├── Queue     → mensajes simples  https://stventasprod.queue.core.windows.net
   └── File      → disco compartido  https://stventasprod.file.core.windows.net
```

Quédate con esta imagen: **un Storage Account es un edificio con cuatro
inquilinos** que comparten dirección (la cuenta, las claves, el firewall,
la redundancia) pero hacen cosas distintas y tienen su propio
subdominio/endpoint. Decisiones que se toman *una vez por edificio* y
afectan a los cuatro:

- **Kind:** siempre `StorageV2` (Slide 3) — el único que soporta todo
  (los cuatro servicios, access tiers, lifecycle). Hay variantes premium
  y ADLS Gen2, pero V2 es el 95% de los casos.
- **Redundancia (SKU):** cuántas copias y dónde → §8.
- **Seguridad:** acceso público, TLS mínimo, firewall, cómo se
  autentica → §9.

La pregunta que este ejemplo entrena **no es** "¿cómo subo un blob?" (el
SDK es trivial). Es **"¿a qué inquilino llamo, y qué decisiones de
edificio me afectan?"**.

---

## 5. Los cuatro servicios, en profundidad

Esta es la parte central. Para cada servicio: **para qué sirve**, **cómo
se ve de verdad en el SDK** (con el código real del ejemplo), **cuándo
NO** usarlo, y **cuál es su "primo caro"**. El arte está en saber cuándo
el barato basta.

### 5.1 Blob — archivos

Cualquier cosa que sería un fichero en disco: PDFs, imágenes, ZIPs,
exports CSV, backups, logs. En el ejemplo subes una factura a
`facturas/2026/05/f-1.csv`, la listas, la descargas y la borras —el ciclo
CRUD completo— en
[`IBlobRepository.cs`](src/Storage.Demo.Api/Repositories/IBlobRepository.cs).

**Cómo se ve de verdad** (lo esencial del SDK, Slides 7-8):

```csharp
var container = client.GetBlobContainerClient("facturas");
await container.CreateIfNotExistsAsync(PublicAccessType.None); // sin acceso anónimo
var blob = container.GetBlobClient("2026/05/f-1.csv");
await blob.UploadAsync(stream, new BlobUploadOptions {
    HttpHeaders = new BlobHttpHeaders { ContentType = "text/csv" },
    Metadata = new Dictionary<string,string> { ["clienteId"] = "cli-001" }
});
```

Tres conceptos que tienes que entender, no solo copiar:

- **Jerarquía `Account → Container → Blob`.** El *container* es la unidad
  de organización y de control de acceso. El *blob* es el archivo.
- **Tipos de blob** (Slide 6): **Block Blob** (99 % de los casos —
  archivos hasta 190 TB, subida en bloques paralelos), **Append Blob**
  (solo añade al final: ideal para logs), **Page Blob** (random R/W,
  discos de VM; casi nunca lo tocarás).
- **Metadata.** Cada blob lleva pares clave-valor (`clienteId`,
  `fechaEmision`). Útil para no tener que abrir el archivo para saber qué
  contiene.

> 🧠 **La idea que el ejemplo machaca: las "carpetas" NO existen.**
> `2026/05/f-1.csv` **no** es una carpeta con un archivo dentro: es _un
> único blob_ cuyo nombre contiene barras. El portal y el Storage Explorer
> te lo pintan como carpetas para tu comodidad, pero por dentro es un
> nombre plano. Consecuencias prácticas:
> - "Listar la carpeta del mes" = **filtrar por prefijo**
>   (`GetBlobsAsync(prefix: "2026/05/")`). El ejemplo lo expone como
>   `GET /blob/facturas?prefijo=2026/05/`.
> - El SDK *también* puede simular navegación jerárquica con
>   `GetBlobsByHierarchyAsync(delimiter: "/")` —devuelve "prefijos" como
>   si fueran carpetas— pero sigue siendo una ilusión sobre nombres
>   planos.
> - Por eso existe [`BlobPath.cs`](src/Storage.Demo.Api/Storage/BlobPath.cs):
>   centraliza la convención de nombres por fecha en un sitio **puro y
>   testeable**, en vez de concatenar `/` a mano por todo el código.
> Interioriza esto: es el malentendido nº 1 con Blob Storage.

> 💡 **Archivos grandes y upload directo (Slides 9-10).** Para ficheros >
> 256 MB el SDK paraleliza bloques solo. Y para que el navegador suba
> directo a Storage **sin pasar por tu API** (ahorra ancho de banda y
> CPU), generas un **SAS token** de escritura con caducidad y le das esa
> URL al frontend. El ejemplo no implementa subida directa, pero entender
> que existe te evita el antipatrón de "todos los MB pasan por mi API".

### 5.2 Table — NoSQL barato

Datos sencillos clave-valor, sin relaciones, sin JOINs, en volumen alto:
auditoría, logs estructurados, configuración, telemetría. En el ejemplo,
cada acción del usuario es una entidad con `PartitionKey = fecha` y
`RowKey = id` —ver
[`ITableRepository.cs`](src/Storage.Demo.Api/Repositories/ITableRepository.cs)
y `AuditEntity` en [`Modelos.cs`](src/Storage.Demo.Api/Models/Modelos.cs).

**El concepto que lo es todo en Table: la clave compuesta.**

- **`PartitionKey`**: agrupa entidades que se guardan *juntas*
  físicamente. Consultar **por PartitionKey es rápido y barato** (datos
  co-localizados). En el ejemplo es la fecha (`2026-05-15`): todas las
  acciones de un día viven juntas.
- **`RowKey`**: identifica la entidad dentro de la partición.
  `PartitionKey + RowKey` = clave primaria única.
- **Consultar sin PartitionKey = table scan**: lento y caro con muchos
  datos. **No hay índices secundarios, no hay JOINs, no hay esquema
  fijo.** Si necesitas consultar por otro campo eficientemente, Table ya
  no es tu sitio.
- **Batch de hasta 100 operaciones** en la *misma* partición, de forma
  transaccional (`SubmitTransactionAsync`).

> 🧠 **La decisión que vale dinero:** Table cuesta **~0,04 €/GB**. Cosmos
> DB hace lo mismo y mucho más, pero ronda **~25 €/GB** y además cobra por
> RU/s aprovisionadas o consumidas. Para un log de auditoría que escribes
> mucho y consultas por día, elegir Cosmos "porque es más moderno"
> multiplica la factura por cientos sin aportar nada. **La regla:**
> Table mientras consultes por PartitionKey y no necesites queries
> complejas, índices secundarios, multi-región de escritura ni Change
> Feed; Cosmos (S5.3) cuando *sí* necesites eso.

### 5.3 Queue — mensajería simple

Desacoplar trabajo: "ha pasado X, que alguien lo procese luego". El que
encola y el que procesa **no se conocen**. En el ejemplo encolas
`"PED-1 listo para procesar"` y luego lo recibes —
[`IQueueRepository.cs`](src/Storage.Demo.Api/Repositories/IQueueRepository.cs).

**El mecanismo que tienes que entender (Slide 13): peek-lock.** Recibir un
mensaje *no lo borra*. Lo hace **invisible** durante un *visibility
timeout* (p. ej. 30 s). Procesas, y si todo va bien **lo borras tú**
explícitamente con su `PopReceipt`. Si tu proceso muere antes de borrarlo,
el mensaje **reaparece** pasado el timeout y otro lo procesa. Esto es lo
que garantiza "al menos una vez" — y por qué tu procesado debe ser
**idempotente**.

Otros detalles reales que el SDK expone y conviene conocer:

- **`visibilityTimeout` al enviar** = mensaje diferido ("procésalo dentro
  de 5 min").
- **`timeToLive`** = el mensaje se autodescarta si nadie lo procesa a
  tiempo.
- **`PeekMessagesAsync`** = mirar sin sacar de la cola.
- **`ApproximateMessagesCount`** = longitud **aproximada**. No es un
  descuido del ejemplo (`GET /queue/{cola}/longitud`): en un sistema
  distribuido un conteo exacto en tiempo real es caro e inútil; te dan un
  aproximado a propósito.

> 🧠 **El primo caro es Service Bus** (lo viste en M04, ~10 €/mes fijo).
> Queue Storage es casi gratis pero **no** garantiza orden (FIFO), **no**
> tiene topics pub/sub, **no** tiene transacciones ni dead-letter
> avanzado (solo "poison queue" manual). **La regla:** Queue para señales
> internas y trabajo encolado donde *reprocesar* un mensaje no es grave;
> Service Bus cuando perder o desordenar un mensaje es inaceptable
> (facturación, pagos, pedidos con garantía de orden).

### 5.4 File — disco compartido

Un recurso compartido SMB/NFS que se monta como unidad de red (`Z:\` en
Windows, `mount -t cifs` en Linux). Existe el contrato
[`IFileShareRepository.cs`](src/Storage.Demo.Api/Repositories/IFileShareRepository.cs)
para que veas el SDK, pero **no tiene endpoint a propósito** (lo explica
§11).

> 🧠 **La regla de decisión:** ¿accede una *persona* o un sistema *legado*
> montando una unidad de red? → **File**. ¿accede tu *código* por
> API/SDK? → **Blob** (más barato, más escalable, más features:
> lifecycle, versioning, tiers). Casos típicos de File: un ERP antiguo
> que solo sabe leer de `\\servidor\carpeta`, "lift & shift" de un file
> server on-premises, o aplicaciones que comparten un directorio. Si
> dudas y el que accede es tu propio código, **casi siempre es Blob**.

---

## 6. Coste y ciclo de vida: tiers + lifecycle

Aquí está la segunda gran lección del submódulo: **el almacenamiento
tiene "temperaturas" y el dinero está en moverlas solas.**

| Tier | Guardar (€/GB/mes) | Leer (€/10K ops) | Ideal para |
| --- | --- | --- | --- |
| **Hot** | ~0,018 € | ~0,004 € | datos activos, lectura frecuente |
| **Cool** | ~0,010 € | ~0,01 € | < 1 lectura/mes, retención 30+ días |
| **Cold** | ~0,005 € | ~0,01 € | raramente accedido, 90+ días |
| **Archive** | ~0,002 € | ~5 € + horas de espera | compliance, históricos, backup largo |

Lee la tabla dos veces. El patrón es **invertido**: cuanto más frío,
**más barato guardar pero más caro (y lento) operar**. Archive cuesta 9×
menos que Hot por GB, pero leer de Archive cuesta ~1000× más y tarda
horas (rehidratación). Conclusión: el tier correcto depende de **cuánto
vas a tocar el dato**, no de cuán "importante" es.

**Lifecycle Management (Slide 5/28).** En producción no cambias tiers a
mano: defines una *policy* JSON y Azure mueve los blobs solo:

```
factura recién creada          → Hot
a los 30 días sin tocarse      → Cool      (se accede poco ya)
a los 180 días                 → Archive   (casi nunca se mira)
al año                         → borrado   (retención cumplida)
```

> 🎓 **Por qué [`AccessTierPolicy.cs`](src/Storage.Demo.Api/Storage/AccessTierPolicy.cs)
> existe.** Esa curva (`DiasACool=30`, `DiasAArchive=180`,
> `DiasABorrado=365`) es exactamente lo que la lifecycle policy hace en
> Azure. El ejemplo la modela como **función pura** para que entiendas y
> *pruebes* la decisión sin una cuenta real y sin esperar 30 días: el
> endpoint `GET /blob/tier-sugerido/{dias}` te deja jugar con la curva en
> tiempo real. Es didáctica pura — en producción esto lo hace la policy,
> no tu código.

---

## 7. Durabilidad y desastre: qué te salva de un borrado

Decisiones de "edificio" que protegen *los cuatro* servicios.

**Redundancia (SKU, Slide 4) — qué pasa si se cae un datacenter:**

| SKU | Copias | Dónde | Cuándo |
| --- | --- | --- | --- |
| **LRS** | 3 | mismo datacenter | desarrollo / prácticas |
| **ZRS** | 3 | 3 zonas, misma región | **mínimo para producción** |
| **GRS / GZRS** | 6 | + región par (otro país) | producción crítica con DR |
| **RA-GRS / RA-GZRS** | 6 | GRS + **lectura** en la secundaria | DR con lectura inmediata / repartir lecturas |

El ejemplo y sus scripts usan **LRS** (es una práctica, ~0 €). La lección
es saber que en producción **ZRS es el suelo** y que GRS te protege de
perder una región entera, a cambio de ~2× el coste.

**Soft delete / versioning / immutability (Slides 19, 29-30) — qué te
salva de _ti mismo_:** la redundancia te protege de que Azure falle;
**no** de que alguien ejecute un `DELETE` por error. Para eso:

- **Soft delete** (blobs y/o containers): lo borrado es **recuperable**
  N días (`az storage blob undelete`). Es el "papelera de reciclaje" de
  Storage. En producción: actívalo siempre (30 días típico).
- **Versioning**: cada modificación guarda una versión anterior →
  recuperas el contenido previo, no solo "que existía".
- **Immutability (WORM)**: blobs que **no se pueden modificar ni borrar**
  durante un periodo (compliance legal, "legal hold"). Una vez bloqueada
  la policy, *nadie* —ni un admin— la salta. Eso es el punto.

> 🧠 **Idea para llevarte:** "tener 6 copias" (GRS) y "poder deshacer un
> borrado" (soft delete) son problemas **distintos**. El primero es
> durabilidad ante fallo de infraestructura; el segundo, protección ante
> error humano. Producción seria necesita **los dos**.

---

## 8. Seguridad: cómo te conectas (y el if de Program.cs)

Tres niveles, de peor a mejor (Slide 17):

1. **Account Keys** — dos claves con **acceso total** a toda la cuenta.
   Si una se filtra (en un repo, un log, un appsettings subido a git),
   *desastre*. Las connection strings llevan la key dentro. Úsalas solo
   en **desarrollo / Azurite**.
2. **SAS Tokens** — acceso **acotado**: por tiempo, por permiso
   (solo lectura), por IP, por recurso (este blob, no toda la cuenta).
   Para dar acceso temporal a un tercero o al navegador. Aún mejor:
   **User Delegation SAS**, firmado con Entra ID en vez de con la account
   key.
3. **Managed Identity + RBAC** — **sin claves, sin passwords, sin SAS**.
   La identidad de la app la verifica Entra ID; le asignas un rol mínimo
   (`Storage Blob Data Contributor`) sobre el recurso. Es lo recomendado
   en producción.

> 🎓 **Por qué `Program.cs` tiene ese `if`.** Mira
> [`Program.cs`](src/Storage.Demo.Api/Program.cs): si hay
> `StorageAccountUri` configurada → registra los clientes con
> `DefaultAzureCredential` (**nivel 3**, Managed Identity, producción).
> Si no → connection string (**nivel 1**, Azurite/desarrollo). Esa
> bifurcación es, en cuatro líneas, toda la Slide 17. El "por qué"
> completo de `DefaultAzureCredential` y RBAC es **S5.4**: aquí solo
> necesitas ver *que la decisión existe y cuál es la buena*.

**Networking (Slide 18):** complementario — en producción se bloquea el
acceso público (`default-action Deny`), se permite solo la IP de la
oficina + servicios Azure de confianza, y para acceso 100 % privado un
**Private Endpoint** (la cuenta solo es alcanzable desde tu red virtual,
nunca por internet).

---

## 9. Recorrido guiado: la app de ventas en un día

Lanza la API (ver §11) y abre
[`api.http`](src/Storage.Demo.Api/api.http). No ejecutes por ejecutar:
para cada paso, antes de mirar la respuesta, **predice qué va a pasar** y
luego pregúntate *"¿qué acabo de demostrar?"*.

| # | Petición | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `POST /blob/facturas/2026/05/f-1.csv` (CSV en el body) | `201 Created` | Subir = poner bytes en una clave. El `2026/05/` es parte del *nombre*, no una carpeta. |
| 2 | `GET /blob/facturas?prefijo=2026/05/` | lista con `f-1.csv`, su tamaño y `LastModified` | "Listar la carpeta" = **filtrar por prefijo**. Confirma §5.1. |
| 3 | `GET /blob/facturas/2026/05/f-1.csv` | el CSV, con el `Content-Type` que pusiste | Recuperas exactamente los bytes y metadatos que subiste. |
| 4 | `GET /blob/tier-sugerido/45` | `{ tier: "Cool", borrar: false }` | **Lógica pura, sin tocar Azure.** Prueba `15`→Hot, `200`→Archive, `400`→`borrar:true`. Es la curva de lifecycle (§6). |
| 5 | `POST /table` (acción del usuario) | `201 Created` con la entidad | Una línea de auditoría. Fíjate en `particion` (fecha) + `rowKey`: la clave compuesta. |
| 6 | `GET /table/2026-05-15` | las entidades de ese día | Consultar **por PartitionKey** = la consulta rápida y barata de Table (§5.2). |
| 7 | `POST /queue/pedidos` y luego `GET /queue/pedidos` | `202 Accepted`, luego el mensaje | Productor y consumidor **no se conocen**: eso es desacoplar. |
| 8 | `GET /queue/pedidos/longitud` | `{ longitud: N }` | "Aproximada" a propósito (§5.3), no un bug. |
| 9 | `DELETE /blob/facturas/2026/05/f-1.csv`, repite el paso 2 | `204`, luego lista vacía | El ciclo CRUD se cierra; sin soft delete configurado, borrado = se fue. |

> 💡 **Experimento recomendado:** repite el paso 7 tres veces seguidas y
> luego haz `GET` tres veces. Observa el orden en que salen los mensajes:
> Queue Storage **no garantiza FIFO**. Acabas de *ver* por qué para
> pedidos en orden necesitarías Service Bus (§5.3). El ejemplo no te lo
> cuenta: te deja descubrirlo.

> 💡 El paso 4 es el único que **no llama a Azure** — es una función pura.
> Está ahí para que entiendas la *decisión* de lifecycle sin necesitar
> nube ni esperar 30 días.

---

## 10. Por qué el código está organizado así

La estructura en tres capas es una lección en sí misma:

- **`Storage/` (lógica pura)** — `BlobPath`, `AccessTierPolicy`.
  Decisiones (cómo se nombra un blob, a qué tier toca) **sin Azure**. Se
  testean en milisegundos, sin Docker, sin nube. *Mensaje: la lógica de
  negocio no debe necesitar la nube para probarse.*
- **`Repositories/` (los SDKs envueltos)** — cada servicio tras una
  interfaz (`IBlobRepository`...). *Mensaje: el SDK de Azure no se
  esparce por toda la app; vive detrás de un contrato que se puede
  sustituir o testear.*
- **`Endpoints/` (Minimal API fina)** — reciben, delegan en el repo,
  responden. Casi sin lógica. *Mensaje: la capa web es "pegamento", no
  donde vive la inteligencia.*

**Y por qué los tests están en dos capas** (mira el README para el
detalle de comandos; aquí el *porqué*):

- **Unit (17 tests)** — `BlobPath` y `AccessTierPolicy`. Rápidos, sin
  Azure. Prueban *decisiones*.
- **Integration (1 test, `SkippableFact`)** — round-trip **real** de
  Blob+Table+Queue contra **Azurite** levantado con Testcontainers, a
  través de la API completa. Si no hay Docker, **se salta** y la suite
  sigue verde. *Mensaje: la integración con la nube se prueba de verdad,
  pero no debe bloquear a quien no tiene Docker.*

> 🎓 **Por qué `IFileShareRepository` no tiene endpoint ni test de
> integración.** No es un olvido. Azurite emula Blob, Table y Queue, pero
> **no Azure Files**. En vez de fingir un test que siempre pasa o siempre
> se salta, el ejemplo deja el contrato y el código SDK visibles para que
> los *leas*, y es honesto: File solo se valida contra un Storage real.
> Esa honestidad —no inventar cobertura falsa— es parte de lo que el
> curso te enseña sobre testear integraciones con servicios cloud.

---

## 11. Puesta en marcha, ejecución y pruebas

Esta sección es **operativa y técnica**: lo que tecleas, en qué orden y
qué deberías ver. Objetivo: pasar de "repo clonado" a "ejemplo
funcionando y verificado" sin adivinar nada.

### 11.1 Requisitos

| Requisito | Versión / cómo | Para qué | ¿Obligatorio? |
| --- | --- | --- | --- |
| .NET SDK | **10.x** — fijado en [`global.json`](global.json) (`10.0.300-preview…`, `rollForward: latestFeature`) | compilar y ejecutar | Sí |
| Azurite | `npm install -g azurite` **o** `docker run … azurite` | emular Blob/Table/Queue en local | Sí (para usar la API) |
| Docker | Docker Desktop | el test de **integración** (Testcontainers) | No — sin él, ese test se *salta* |
| Cliente REST | extensión *REST Client* de VS Code, o `curl` | lanzar las peticiones de [`api.http`](src/Storage.Demo.Api/api.http) | Recomendado |

> Comprueba el SDK: `dotnet --version` debe resolver a 10.x. Si tienes
> varias versiones, `global.json` fuerza la correcta dentro de esta
> carpeta — no toques tu instalación global.

### 11.2 Compilar (verificación rápida sin nube)

```bash
cd examples/M05-Almacenamiento-BBDD/S5.1-azure-storage
dotnet build Storage.Demo.slnx
```

Debe terminar con **0 errores y 0 warnings**. No es casualidad: el
proyecto tiene `TreatWarningsAsErrors=true`, así que un warning *es* un
fallo de build. Si compila, el grafo de tipos y el DI están bien formados
aunque todavía no haya tocado Azure.

### 11.3 Arrancar Azurite (el emulador)

Elige **una** opción y déjala corriendo en su propia terminal:

```bash
# Opción A — Azurite por npm
azurite --silent --location ./.azurite

# Opción B — Azurite por Docker (Blob 10000 · Queue 10001 · Table 10002)
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite
```

[`appsettings.Development.json`](src/Storage.Demo.Api/appsettings.Development.json)
ya trae `StorageConnection = "UseDevelopmentStorage=true"`, que es el
alias estándar que apunta a esos puertos de Azurite. **No hay que
configurar nada más** para el modo local.

> Azurite **no emula Azure Files**: el `IFileShareRepository` solo
> funciona contra un Storage real (§5.4, §10). Es esperado, no un fallo
> de tu entorno.

### 11.4 Lanzar la API

```bash
dotnet run --project src/Storage.Demo.Api
```

- Escucha en **`http://localhost:5080`** (HTTP plano, perfil `http` de
  [`launchSettings.json`](src/Storage.Demo.Api/Properties/launchSettings.json),
  entorno `Development`).
- Prueba de vida: `GET http://localhost:5080/health` → `{ "status": "ok" }`.

> El curso **nunca** lanza la app por ti: este `dotnet run` lo ejecutas
> tú. La verificación automatizada se queda en *build + test* (§11.6).
> Si arrancas la API **sin Azurite**, `/health` responde igual (no toca
> Storage) pero cualquier `/blob`, `/table` o `/queue` fallará con un
> error de conexión: eso confirma que la app depende de que el
> almacenamiento exista y esté alcanzable.

### 11.5 Ejercitar el ejemplo

Abre [`api.http`](src/Storage.Demo.Api/api.http) con la extensión *REST
Client* y lanza las peticiones en el orden de §9, o por línea de comandos:

```bash
# Subir una factura (el body crudo es el contenido del blob)
curl -X POST http://localhost:5080/blob/facturas/2026/05/f-1.csv \
  -H "Content-Type: text/csv" --data-binary $'factura,total\nF-1,1299.99'

# Listar "la carpeta del mes" = filtrar por prefijo
curl "http://localhost:5080/blob/facturas?prefijo=2026/05/"

# Sugerencia de tier (lógica pura, no necesita Azurite)
curl http://localhost:5080/blob/tier-sugerido/45
```

Sigue el recorrido guiado de **§9** para saber *qué mirar* en cada
respuesta — esa tabla es el guion; esto es solo el cómo invocarlo.

### 11.6 Pasar los tests

```bash
dotnet test Storage.Demo.slnx
```

Resultado esperado y cómo interpretarlo:

| Sin Docker | Con Docker corriendo |
| --- | --- |
| **18 pass · 1 skip · 0 fail** | **19 pass · 0 skip · 0 fail** |

- Los **18 unit** (`BlobPath`, `AccessTierPolicy`) corren siempre: son
  lógica pura, sin Azure, en milisegundos.
- El **1 de integración** es un `SkippableFact`: levanta **Azurite con
  Testcontainers**, ejercita Blob+Table+Queue por la API real
  (`WebApplicationFactory`) y se **salta** si no encuentra Docker. Que
  aparezca como *skip* **no es un fallo** — es el diseño (la suite sigue
  verde sin Docker). Con Docker, ese test pasa a verde.

### 11.7 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `Connection refused` en `/blob`,`/table`,`/queue` | Azurite no está corriendo | arranca Azurite (§11.3) en otra terminal |
| El build falla por un *warning* | `TreatWarningsAsErrors=true` | corrige el warning; aquí no se silencian |
| `dotnet --version` no es 10.x | SDK 10 no instalado | instala .NET SDK 10; `global.json` ya fuerza la versión en esta carpeta |
| El puerto 5080 está ocupado | otra app lo usa | cierra esa app o cambia `applicationUrl` en `launchSettings.json` |
| El test de integración sale como *skip* | no hay Docker | **es lo esperado**; arranca Docker si quieres ejecutarlo |
| Puertos 10000-10002 ocupados | otra instancia de Azurite/Storage | mata el proceso previo o usa la opción Docker con otro mapeo |

### 11.8 Contra un Storage real (opcional)

Para salir del emulador y usar **Managed Identity** (sin secretos):
configura `StorageAccountUri=https://<cuenta>.blob.core.windows.net`,
deja `StorageConnection` vacío y `Program.cs` usará
`DefaultAzureCredential` (§8). El aprovisionamiento por **Portal** y los
scripts `az` de complemento están en el [`README.md`](README.md) —este
manual no los repite a propósito.

---

## 12. Checklist de producción (y por qué cada casilla)

El submódulo cierra con un checklist (Slide 25). No lo memorices: entiende
*de qué te protege cada línea*.

| Casilla | De qué te protege |
| --- | --- |
| Public access deshabilitado | que cualquiera en internet lea tus blobs por URL |
| Firewall `Deny` + excepciones | acceso desde redes no autorizadas |
| TLS 1.2 mínimo | interceptación de datos en tránsito |
| Managed Identity (no account keys) | que una key filtrada dé acceso total (§8) |
| Soft delete + versioning | un borrado/sobrescritura por error (§7) |
| Lifecycle management | pagar tier Hot por datos que nadie mira (§6) |
| Redundancia ≥ ZRS | perder datos si cae una zona/región (§7) |
| Diagnostic logs + alertas | no enterarte de un ataque o fuga de egress |
| Resource Lock | que alguien borre la *cuenta entera* por error |

---

## 13. Las cinco ideas para llevarte (si olvidas todo lo demás)

1. **Antes de guardar algo, pregúntate qué es** (archivo / clave-valor /
   mensaje / disco compartido) y el servicio correcto casi se elige solo.
2. **Elegir el servicio es una decisión de coste y escala**, no de gusto.
   Table vs Cosmos, Queue vs Service Bus: el barato suele bastar; usa el
   caro solo cuando necesites lo que el barato no da.
3. **Las "carpetas" de Blob no existen.** Nombre plano con barras; listar
   = filtrar por prefijo.
4. **El frío es barato de guardar y caro de operar.** Tiers + lifecycle =
   ahorro automático, cero código en producción.
5. **Hay tres formas de autenticarse y solo una es buena en prod.**
   Account key (dev) → SAS (acceso acotado) → **Managed Identity**
   (producción, sin secretos). El `if` de `Program.cs` es esa decisión.

---

## 14. Comprueba que lo has entendido

Responde sin mirar atrás. Si dudas, vuelve a la sección indicada.

1. Tu app genera 2 M de líneas de auditoría/mes y solo las consultas por
   fecha. ¿Table o Cosmos? ¿Por qué? *(§5.2)*
2. Necesitas que los pedidos se procesen **en orden** y que ninguno se
   pierda. ¿Queue Storage o Service Bus? *(§5.3)*
3. Subes `informes/2025/q1.pdf`. ¿Cuántas carpetas se crean en Azure?
   *(§5.1)*
4. Un blob lleva 200 días sin tocarse. Según `AccessTierPolicy`, ¿en qué
   tier debería estar? ¿Por qué eso ahorra dinero pese a que leerlo sea
   más caro? *(§6)*
5. ¿Por qué `Program.cs` ramifica según `StorageAccountUri`? ¿Qué camino
   usarías en producción y qué nivel de seguridad es? *(§8)*
6. Un becario ejecuta `DELETE` sobre el container `facturas` en
   producción. Tienes GRS activado. ¿Recuperas los datos? ¿Qué te habría
   salvado? *(§7)*
7. ¿Por qué `tier-sugerido` es el único endpoint que no necesita Azurite?
   *(§6, §10)*

<details>
<summary>Respuestas</summary>

1. **Table.** Consultas solo por PartitionKey, sin queries complejas;
   Cosmos costaría ~500× más sin aportar nada aquí.
2. **Service Bus.** Queue Storage no garantiza FIFO ni tiene
   dead-letter/transacciones; perder o desordenar un pedido es
   inaceptable.
3. **Cero.** Se crea **un blob** llamado `informes/2025/q1.pdf`. Las
   carpetas son una ilusión del visor sobre un nombre plano.
4. **Archive** (≥180 días). Guardar en Archive cuesta ~9× menos que en
   Hot; un dato de 200 días casi no se lee, así que el coste alto de leerlo
   (raro) no compensa el ahorro de almacenarlo (constante).
5. Hay dos modos: con URI → **Managed Identity** (nivel 3, sin secretos,
   producción); sin URI → connection string (nivel 1, Azurite/dev). En
   producción, siempre el camino de la URI.
6. **No con GRS.** GRS replica el `DELETE` a las 6 copias —protege de
   fallo de infraestructura, no de error humano. Lo que te salva es
   **soft delete** (`undelete` dentro de la ventana de retención) y/o
   versioning. Son problemas distintos (§7).
7. Porque es **lógica pura** (`AccessTierPolicy.Sugerir`): no llama a
   ningún SDK de Storage, solo evalúa la curva de días. Por eso también
   se testea sin Docker.

</details>

---

## 15. Dónde encaja esto en el módulo

Este es el primer ejemplo de M05 y monta el escenario (la app de ventas)
que el resto reutiliza. El arco del módulo es: *¿es base de datos o no?*
→ si **no**, lo viste aquí; si **sí**, ramifica en relacional vs NoSQL;
y todo se conecta **sin secretos**.

- **S5.2 — Azure SQL** · cuando los datos **sí** son relacionales (FK,
  JOINs, transacciones ACID): el otro lado de la decisión de §2.
- **S5.3 — Cosmos DB** · el "primo caro" de Table, en detalle: cuándo el
  coste extra **sí** se justifica.
- **S5.4 — Managed Identity** · el "por qué" completo del `if` de
  `Program.cs` (§8): conectarse a Storage **sin un solo secreto**.

> Si solo te llevas una frase de S5.1: **antes de guardar algo,
> pregúntate qué es**, y el servicio correcto —y su coste— casi se elige
> solo. Esa pregunta es todo el ejemplo.
