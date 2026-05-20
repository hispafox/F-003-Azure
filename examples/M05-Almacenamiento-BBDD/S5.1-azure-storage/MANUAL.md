# Manual del alumno — S5.1 · Azure Storage

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica del ejemplo: estructura, mapeo a slides, comandos, despliegue por Portal. Útil para quien ya quiere entrar al código. Este manual va antes: te cuenta para qué existe el ejemplo, qué decisión silenciosa quiere enseñarte y cómo leerlo. Cuando termines, abre el README y todo encajará más rápido.

Tiempo de lectura: ~30 min. Submódulo de teoría: [M05-S5.1](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.1-azure-storage-v3.md) (40 slides). Las primeras cuatro secciones son el marco mental; de la sección 5 a la sección 8 entras al detalle técnico; el resto es práctica, autoevaluación y un par de avisos antes de pasar a S5.2.

*Creado: 2026-05-19 23:50 +0200*

---

## 1. La idea en una frase

Llevas tres módulos usando Blob y Queue sin pararte a pensar cuál de ellos tocaba. Los triggers de Functions los enchufaban por ti, los marcabas en el `[BlobTrigger]` y a otra cosa. Aquí cambia. Dejas de usarlos por inercia y empiezas a elegirlos a propósito.

Y la diferencia entre elegir bien y elegir mal en un Storage Account no es de las que un compañero caza en code review. Es invisible al ojo y al test. La factura mensual es la que la enseña, varios meses después, cuando alguien pregunta por qué los gastos de Azure han subido un 30% sin que el tráfico haya cambiado.

Eso es S5.1: aprender a hacer esa elección antes de que la factura te la enseñe.

---

## 2. El problema real que hay detrás

Hace año y medio acabé en una reunión con la gente de cuentas de un cliente. Llevaban catorce meses pagando 1.200 € al mes en Cosmos DB. Cuatro GB de datos. La factura subía despacio, nadie se asustaba.

Lo abrimos: eran logs de auditoría. *Quién hizo qué y cuándo*. La query era siempre por día. El equipo los había metido en Cosmos porque "es la base de datos del proyecto" — la frase con la que se cae el 80% de las decisiones de almacenamiento en Azure. Lo mismo en Table Storage habría costado **siete céntimos al mes**. Sin exagerar. La diferencia entre una decisión silenciosa y otra, repetida durante un año, son catorce mil euros.

Esa anécdota es el submódulo entero.

Pensemos qué necesita la app de ventas que acompaña a todo M05 a lo largo de un día normal:

| Necesidad real | ¿Va en la base de datos? | Servicio correcto | Por qué |
| --- | --- | --- | --- |
| Guardar el PDF de cada factura emitida | No, es un archivo binario | **Blob** | Una BD no sirve archivos: los infla, los encarece, los devuelve mal |
| Anotar quién hizo qué (miles de líneas/día) | Sería caro y excesivo | **Table** | Volumen alto, consulta simple por fecha, sin relaciones |
| "Pedido nuevo, procésalo cuando puedas" | No, es un mensaje efímero | **Queue** | Desacopla quien avisa de quien trabaja |
| Que contabilidad abra una carpeta de red | No, es un disco compartido | **File** | Persona o sistema legado monta una unidad; no llama a una API |
| Imágenes del catálogo de producto | No, son archivos | **Blob** | Igual que las facturas; encima se sirven con CDN |
| Export CSV nocturno de ventas | No, archivo temporal | **Blob** (Cool) | Se genera, se descarga una vez, se archiva barato |

Cada fila es una decisión real. Ninguna debería acabar en tu base de datos relacional ni en Cosmos. Meterlas ahí es el error que el ejemplo te enseña a **no** cometer.

---

## 3. Por qué esto importa en tu stack

La teoría lo dice con la honestidad de quien lo ha visto muchas veces (Slide 2): *"El 80% de lo que necesitéis como desarrolladores pasa por aquí"*. Yo iría más lejos. Cuando empiezas un proyecto Azure, el Storage Account es lo primero que aparece y lo último que sale del *resource group*. Sobrevive a redeploys, a cambios de arquitectura, a migraciones de BD. Y todo el equipo lo toca sin pensarlo.

Cambio de stack respecto a M03/M04: ahora ya no es una Function con bindings haciendo magia. Es una **Minimal API** que envuelve los cuatro SDKs de Storage tras repos. ¿Por qué importa? Porque es como vas a acceder a Storage el 90% de las veces en tu trabajo — desde una app web normal, no desde un trigger con binding. Los tests vuelven al patrón de M02 (`WebApplicationFactory`), no al de Functions.

---

## 4. El modelo mental: el edificio con cuatro inquilinos

Piensa en un edificio de oficinas en el centro de Madrid. Una sola dirección, un portal, un vigilante en recepción. Dentro viven cuatro empresas distintas. Una asesoría fiscal en la planta primera. En la segunda, un estudio de arquitectura. En la tercera, una imprenta con mensajería propia. Y en la cuarta, un almacén compartido al que el resto sube y baja cosas a mano.

Comparten dirección, vigilante, seguro y compañía de mantenimiento. Pero cada una hace algo muy distinto. Y cada una tiene su propia entrada al piso.

Eso es un Storage Account. La cuenta es el edificio. **Blob**, **Table**, **Queue** y **File** son los cuatro inquilinos. Las decisiones del edificio — quién entra, en qué horario, qué tipo de cerradura — afectan a los cuatro: la redundancia, el cifrado mínimo, el firewall, cómo te autenticas. Pero cuando subes algo, vas al piso correcto. Y ahí es donde la gente se equivoca: meten facturas en Cosmos porque "es la base de datos" sin darse cuenta de que arriba, en el segundo, hay un estudio que las guarda por dos céntimos al gigabyte.

```
Storage Account  ── stventasprod ──  UNA cuenta · UNA factura
   │                                  Nombre único en TODO Azure (3-24
   │                                  chars, solo minúsculas y números)
   │
   ├── Blob      → archivos          https://stventasprod.blob.core.windows.net
   ├── Table     → NoSQL key-value   https://stventasprod.table.core.windows.net
   ├── Queue     → mensajes simples  https://stventasprod.queue.core.windows.net
   └── File      → disco compartido  https://stventasprod.file.core.windows.net
```

Tres decisiones de edificio se toman una vez y duran para siempre. La **redundancia** (cuántas copias y dónde — sección 7), la **seguridad** (quién puede entrar y cómo — sección 8) y el **tipo de cuenta**, que se llama Kind. Para esta última hay variantes; no te entretengas: para el 95% de los casos `StorageV2` es lo correcto y es lo único que soporta todo (Slide 3).

La pregunta que el ejemplo entrena, entonces, no es *"¿cómo subo un blob?"*. El SDK es trivial. Es *"¿a qué piso voy con esto?"*. Vuelve a esta imagen cada vez que dudes. El edificio te dice más que cualquier flowchart.

---

## 5. Los cuatro pisos, en detalle

### 5.1 Blob — el estudio de arquitectura, donde guardas archivos

Cualquier cosa que sería un fichero en disco vive en Blob: PDFs, imágenes, ZIPs, exports CSV, backups, logs. En el ejemplo subes una factura a `facturas/2026/05/f-1.csv`, la listas, la descargas y la borras. Lo típico en [`IBlobRepository.cs`](src/Storage.Demo.Api/Repositories/IBlobRepository.cs):

```csharp
var container = client.GetBlobContainerClient("facturas");
await container.CreateIfNotExistsAsync(PublicAccessType.None);
var blob = container.GetBlobClient("2026/05/f-1.csv");
await blob.UploadAsync(stream, new BlobUploadOptions {
    HttpHeaders = new BlobHttpHeaders { ContentType = "text/csv" },
    Metadata = new Dictionary<string,string> { ["clienteId"] = "cli-001" }
});
```

La jerarquía es siempre la misma: cuenta → *container* → blob. El container es la unidad de organización y de control de acceso; el blob es el archivo. Le puedes pegar metadata clave-valor para no tener que abrir el archivo cuando lo busques (clienteId, fechaEmisión). Y hay tres tipos de blob (Slide 6), aunque salvo que toques discos de VM, solo vas a usar **Block Blob** (archivos hasta 190 TB, subida paralela en bloques).

Y ahora la idea que el ejemplo machaca, la que te ahorra dos días de buscar bugs en producción:

> 🧠 **Las "carpetas" de Blob no existen.** `2026/05/f-1.csv` no es una carpeta con un archivo dentro. Es **un solo blob** cuyo nombre lleva barras. El portal te lo pinta como carpetas para tu comodidad, pero por dentro Storage no sabe lo que es una carpeta. Cuando "listas la carpeta del mes" en realidad estás **filtrando por prefijo** (`GetBlobsAsync(prefix: "2026/05/")`). El ejemplo lo expone como `GET /blob/facturas?prefijo=2026/05/`.

Por eso existe [`BlobPath.cs`](src/Storage.Demo.Api/Storage/BlobPath.cs): centraliza la convención de nombres por fecha en un sitio puro y testeable, en lugar de concatenar `/` a mano por todo el código. Pequeño detalle que parece de manual de estilo y es lo que evita el bug de "el listado del mes no me muestra los blobs porque alguien escribió la barra al revés".

Por completar: para archivos grandes el SDK paraleliza bloques solo (Slide 9), y si quieres que el navegador suba **directo** a Storage sin pasar por tu API — el patrón correcto para evitar que todos los megas pasen por tu servidor — generas un **SAS token** de escritura con caducidad y le das esa URL al frontend (Slide 10).

### 5.2 Table — la asesoría barata del primer piso

Datos sencillos clave-valor, sin relaciones, sin JOINs, en volumen alto: auditoría, logs estructurados, configuración, telemetría. En el ejemplo, cada acción del usuario es una entidad con `PartitionKey = fecha` y `RowKey = id`. Mira [`ITableRepository.cs`](src/Storage.Demo.Api/Repositories/ITableRepository.cs) y `AuditEntity` en [`Modelos.cs`](src/Storage.Demo.Api/Models/Modelos.cs).

El concepto que lo es todo en Table es la **clave compuesta**. La `PartitionKey` agrupa entidades que viven juntas físicamente. Consultar por PartitionKey es rápido y barato — los datos están co-localizados. En el ejemplo es la fecha (`2026-05-15`): todas las acciones de un día están juntas. La `RowKey` identifica la entidad dentro de la partición. Sumadas, son la clave única.

Consultar **sin** PartitionKey es un table scan, y los table scans con muchos datos son lentos y caros. Y aquí está la trampa que pillas tarde: Table no tiene índices secundarios, no tiene JOINs, no tiene esquema fijo. Si necesitas consultar por otro campo eficientemente, Table no es tu sitio. Pero si tu acceso es siempre por la misma clave de partición, no hay competencia.

Vuelvo a la historia del principio. Aquellos 1.200 € al mes en Cosmos DB eran para hacer exactamente lo que Table hace por monedas. ¿Por qué ocurre tanto? Por una combinación: Cosmos "suena moderno", Table "suena viejo", y nadie mira la factura hasta meses después. La regla es sencilla:

> 🧠 **Table mientras consultes por PartitionKey y no necesites queries complejas, índices secundarios, multi-región de escritura ni Change Feed.** Cosmos (S5.3) para cuando *sí* necesites eso. Table cuesta ~0,04 €/GB; Cosmos ronda los 25 €/GB más RU/s. Multiplica tu factura por el número de meses que llevas equivocado.

### 5.3 Queue — la mensajería del tercer piso

Desacoplar trabajo: "ha pasado X, que alguien lo procese luego". El que encola y el que procesa no se conocen. En el ejemplo encolas `"PED-1 listo para procesar"` y luego lo recibes — [`IQueueRepository.cs`](src/Storage.Demo.Api/Repositories/IQueueRepository.cs).

El mecanismo que tienes que entender es **peek-lock**, y no es obvio. Cuando recibes un mensaje, Storage **no lo borra**. Lo hace invisible durante un *visibility timeout* (30 s por defecto). Procesas, y si todo va bien le pides explícitamente que lo borre con su `PopReceipt`. Si tu proceso muere antes de borrarlo, el mensaje **reaparece** pasado el timeout y otro consumidor se lo lleva. Eso es lo que garantiza "al menos una vez" — y la razón por la que tu procesado tiene que ser **idempotente**. Si encolas un pedido y el procesado, al cabo de un fallo, descuenta stock dos veces, no tienes un bug de Queue: tienes un bug de diseño.

Otros detalles que se ven cuando tocas el SDK:

- `visibilityTimeout` al enviar = mensaje diferido. "Procésalo dentro de 5 minutos."
- `timeToLive` = el mensaje se autodescarta si nadie lo procesa a tiempo.
- `PeekMessagesAsync` = mira sin sacar.
- `ApproximateMessagesCount` = "aproximada" a propósito (no es un descuido del ejemplo). En sistemas distribuidos el conteo exacto en tiempo real es caro e inútil; te dan un aproximado y te ahorras problemas.

> 🧠 **Service Bus es el primo caro** (lo viste en M04, ~10 €/mes fijo). Queue Storage es casi gratis pero no garantiza FIFO, no tiene topics pub/sub, ni transacciones, ni dead-letter avanzado — solo una "poison queue" manual. Usa Queue para señales internas y trabajo encolado donde reprocesar un mensaje no es grave. Pasa a Service Bus el día que perder o desordenar un mensaje sea inaceptable.

### 5.4 File — el almacén del cuarto piso

Un recurso compartido SMB/NFS que se monta como unidad de red — `Z:\` en Windows, `mount -t cifs` en Linux. Existe el contrato [`IFileShareRepository.cs`](src/Storage.Demo.Api/Repositories/IFileShareRepository.cs) para que veas el SDK, pero **no tiene endpoint a propósito** (la explicación en sección 10).

¿Quién accede a File? Una persona que abre `\\servidor\carpeta` en su explorador, o un sistema legado que no sabe hablar HTTP. Casos típicos: un ERP antiguo que solo lee de unidades de red, un *lift & shift* desde un file server on-prem, una migración donde no se puede tocar el cliente. ¿Quién accede a Blob? Tu código, por API o SDK.

> 🧠 **Si lo que accede es tu código, casi siempre es Blob.** Blob es más barato, más escalable, tiene lifecycle, tiene versioning, tiene tiers. File está ahí cuando hay una persona o un sistema antiguo abriendo la unidad de red. En la práctica, en proyectos nuevos cloud-native, vas a usar File una vez de cada veinte. Si dudas, Blob.

---

## 6. Coste y ciclo de vida: el patrón frío/caliente

El almacenamiento en Azure tiene "temperaturas", y el dinero está en moverlas solas. Mira la tabla:

| Tier | Guardar (€/GB/mes) | Leer (€/10K ops) | Ideal para |
| --- | --- | --- | --- |
| **Hot** | ~0,018 € | ~0,004 € | datos activos, lectura frecuente |
| **Cool** | ~0,010 € | ~0,01 € | < 1 lectura/mes, 30+ días en frigorífico |
| **Cold** | ~0,005 € | ~0,01 € | raramente accedido, 90+ días |
| **Archive** | ~0,002 € | ~5 € + horas de espera | compliance, históricos, backup largo |

Léela despacio. El patrón es **invertido**: cuanto más frío, más barato guardar pero más caro (y más lento) operar. Archive cuesta nueve veces menos que Hot por GB, pero leer de Archive cuesta más o menos mil veces más que leer de Hot, y encima tarda horas porque hay que "rehidratar" el blob. Conclusión: el tier correcto depende de cuánto vas a tocar el dato, no de cuán importante te parece.

Lo bonito de esto es que **no lo cambias a mano** (Slide 5/28). En producción defines una *lifecycle policy* en JSON y Azure mueve los blobs solo:

```
factura recién creada          → Hot
a los 30 días sin tocarse      → Cool      (se accede poco ya)
a los 180 días                 → Archive   (casi nadie la pide)
al año                         → borrado   (retención cumplida)
```

> 🎓 **Por qué existe `AccessTierPolicy.cs` siendo lógica pura.** [Esa clase](src/Storage.Demo.Api/Storage/AccessTierPolicy.cs) modela la curva (`DiasACool=30`, `DiasAArchive=180`, `DiasABorrado=365`) como función pura. ¿Para qué? Para que entiendas y pruebes la decisión sin tener una cuenta real y sin esperar 30 días reales a que pase algo. El endpoint `GET /blob/tier-sugerido/{dias}` te deja mover la rueda y ver cómo cambia. En producción esto lo hace la policy de Azure; aquí está para enseñarte la idea.

---

## 7. Durabilidad y desastre: dos problemas distintos

Las decisiones que protegen a los cuatro pisos del edificio.

**Redundancia (SKU, Slide 4) — qué pasa si Azure tiene un mal día:**

| SKU | Copias | Dónde | Cuándo |
| --- | --- | --- | --- |
| **LRS** | 3 | mismo datacenter | desarrollo / prácticas |
| **ZRS** | 3 | 3 zonas, misma región | **suelo de producción** |
| **GRS / GZRS** | 6 | + región par (otro país) | crítico con DR |
| **RA-GRS / RA-GZRS** | 6 | GRS + lectura en la secundaria | DR con lectura inmediata |

El ejemplo usa **LRS** (es una práctica, 0 €). La lección es saber que en producción ZRS es el mínimo razonable, y que GRS te protege de perder una región entera a cambio de pagar el doble. Hasta aquí, lo que se ve en cualquier curso.

Lo que casi nunca se cuenta es esto: **la redundancia no te salva de ti mismo**. Tres copias en LRS son tres copias del mismo `DELETE`. Si un becario borra un container por error, GRS te lo replica seis veces hasta el otro continente. Para protegerte de error humano necesitas otras tres cosas:

- **Soft delete** (blobs y/o containers): lo borrado es recuperable N días (`az storage blob undelete`). Es la papelera de Storage. En producción se activa siempre, 30 días típicos.
- **Versioning**: cada modificación guarda la versión anterior. Te protege del "alguien sobreescribió mi blob bueno con uno malo", no solo del borrado.
- **Immutability (WORM)**: blobs que durante un periodo definido **no se pueden modificar ni borrar**, ni por un administrador. Compliance legal pura: facturación, registros sanitarios, *legal hold*. Una vez bloqueada la policy, nadie la salta. Ese es el punto.

> 🧠 La intuición clave: "tener seis copias" (GRS) y "poder deshacer un borrado" (soft delete) son problemas **distintos**. El primero te protege contra fallo de infraestructura; el segundo, contra error humano. Producción seria necesita los dos, y eso lo aprendes la primera vez que abres un ticket con Soporte para recuperar un container borrado.

---

## 8. Seguridad: cómo te conectas y el if de Program.cs

Tres niveles, de peor a mejor (Slide 17):

**1. Account Keys.** Dos claves con acceso total a toda la cuenta. Si una se filtra — en un repo público, en un log, en un appsettings subido por error — desastre. Las connection strings llevan la key dentro. Úsalas solo en desarrollo y contra Azurite. En producción ni las generes.

**2. SAS Tokens.** Acceso acotado: por tiempo, por permiso (solo lectura), por IP, por recurso (este blob, no toda la cuenta). Para dar acceso temporal a un tercero o al navegador. Mejor aún: **User Delegation SAS**, firmado con Entra ID en vez de con la account key — si la firma se filtra, no comprometes la clave maestra.

**3. Managed Identity + RBAC.** Sin claves, sin passwords, sin SAS. La identidad de la app la verifica Entra ID, y le asignas un rol mínimo (`Storage Blob Data Contributor`) sobre el recurso. Lo recomendado en producción.

Ahora mira [`Program.cs`](src/Storage.Demo.Api/Program.cs). Hay un `if` inocente sobre `StorageAccountUri` que, en cuatro líneas, es toda la Slide 17:

```csharp
if (!string.IsNullOrWhiteSpace(accountUri))
{
    var cred = new DefaultAzureCredential();
    builder.Services.AddSingleton(new BlobServiceClient(new Uri($"{accountUri}/"), cred));
    // ...
}
else
{
    var cs = conn ?? "UseDevelopmentStorage=true";
    builder.Services.AddSingleton(new BlobServiceClient(cs));
    // ...
}
```

Con URI configurada → `DefaultAzureCredential` (nivel 3, Managed Identity, producción). Sin URI → connection string (nivel 1, Azurite/desarrollo). Eso es. El "por qué" completo de `DefaultAzureCredential` y RBAC se profundiza en **S5.4**; aquí necesitabas saber que la decisión existe y cuál es la buena.

Y luego está la **red**. En producción se bloquea el acceso público (`default-action Deny`), se permite solo la IP de la oficina y los servicios Azure de confianza, y para acceso 100% privado un **Private Endpoint** (la cuenta solo es alcanzable desde tu VNet, jamás por internet). Eso es Slide 18; el ejemplo no lo configura porque corre en LRS de prácticas, pero en cuanto pongas tu primera cuenta en producción mira el checklist de sección 12.

---

## 9. Recorrido guiado: la app de ventas en un día

Lanza la API (ver sección 11) y abre [`api.http`](src/Storage.Demo.Api/api.http). No ejecutes por ejecutar: para cada paso, **antes** de mirar la respuesta, predice qué va a pasar. Luego pregúntate qué acabas de demostrar.

| # | Petición | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `POST /blob/facturas/2026/05/f-1.csv` (CSV en body) | `201 Created` | Subir = poner bytes en una clave. El `2026/05/` es parte del *nombre*, no una carpeta (sección 5.1). |
| 2 | `GET /blob/facturas?prefijo=2026/05/` | lista con `f-1.csv`, tamaño, `LastModified` | "Listar la carpeta" = filtrar por prefijo. |
| 3 | `GET /blob/facturas/2026/05/f-1.csv` | el CSV con el `Content-Type` que pusiste | Recuperas los bytes y los metadatos exactos. |
| 4 | `GET /blob/tier-sugerido/45` | `{ tier: "Cool", borrar: false }` | Lógica pura, sin Azure. Prueba con `15`, `200`, `400`. Es la curva de sección 6. |
| 5 | `POST /table` con la acción del usuario | `201 Created` con la entidad | Una línea de auditoría. Fíjate en `particion` (fecha) + `rowKey`: clave compuesta. |
| 6 | `GET /table/2026-05-15` | las entidades de ese día | Consultar por PartitionKey: la rápida y barata de Table (sección 5.2). |
| 7 | `POST /queue/pedidos` y luego `GET /queue/pedidos` | `202 Accepted`, luego el mensaje | Productor y consumidor no se conocen. Eso es desacoplar. |
| 8 | `GET /queue/pedidos/longitud` | `{ longitud: N }` | "Aproximada" a propósito (sección 5.3). |
| 9 | `DELETE /blob/facturas/2026/05/f-1.csv` y repite el 2 | `204` y luego lista vacía | Sin soft delete configurado: borrado = se fue. Volverás a este punto en sección 7. |

Un experimento que vale más que la teoría: **repite el paso 7 tres veces seguidas** y luego haz `GET` tres veces. Mira el orden en que salen los mensajes. Queue Storage no garantiza FIFO. Acabas de *ver* por qué un sistema de pedidos en orden no usa Queue Storage. El ejemplo no te lo cuenta; te deja descubrirlo. Si lo cazas tú, no se te olvida.

El paso 4 es el único que no llama a Azure. Es función pura ([`AccessTierPolicy.cs`](src/Storage.Demo.Api/Storage/AccessTierPolicy.cs)). Por eso tampoco necesita Azurite ni Docker — y los tests de esa decisión corren en milisegundos.

---

## 10. Por qué el código está organizado así

La estructura en tres capas no es una manía: es una lección en sí misma.

- **`Storage/` (lógica pura)** — `BlobPath`, `AccessTierPolicy`. Decisiones — cómo nombras un blob, a qué tier toca — sin Azure. Se prueban en milisegundos. La lógica de negocio no debe necesitar la nube para probarse, y este ejemplo te lo enseña con la decisión más pequeña que hay: una curva de días.
- **`Repositories/` (los SDKs envueltos)** — cada servicio detrás de una interfaz: `IBlobRepository`, `ITableRepository`, `IQueueRepository`, `IFileShareRepository`. El SDK de Azure no se esparce por toda la app; vive detrás de un contrato.
- **`Endpoints/`** — la Minimal API casi no tiene lógica: recibe, delega en el repo, responde. La capa web es pegamento, no inteligencia.

Y luego está la decisión que más gente pregunta:

> 🎓 **¿Por qué `IFileShareRepository` no tiene endpoint ni test de integración?** No es un olvido. Azurite, el emulador local, sabe imitar Blob, Table y Queue, pero **no Azure Files**. Cuando vi este ejemplo por primera vez la tentación era inventar un test que siempre se saltara, o un endpoint mock, para "tener cobertura". Eso habría sido peor que no tenerlo. El ejemplo deja el contrato y el SDK visibles para que los leas, y es honesto: File solo se valida contra un Storage real. Esa honestidad — no fingir cobertura que no existe — es parte de lo que el curso te enseña sobre testear integraciones con servicios cloud.

Sobre los tests del ejemplo, dos capas: 17 unit de `BlobPath` y `AccessTierPolicy` que corren siempre, y 1 de integración con `SkippableFact` que levanta **Azurite con Testcontainers**, ejercita Blob+Table+Queue contra la API real y se salta si no hay Docker. La suite siempre queda verde; con Docker corres también la integración. El conteo exacto está en el README; lo importante es entender por qué el `SkippableFact` no es trampa: es respeto al desarrollador que no tiene Docker en su máquina y aún así quiere ver verde el build.

---

## 11. Puesta en marcha, ejecución y pruebas

Sección operativa. De "repo clonado" a "ejemplo funcionando y verificado" sin adivinar nada. Datos verificados contra el repo.

### 11.1 Requisitos

| Requisito | Versión / cómo | Para qué | ¿Obligatorio? |
| --- | --- | --- | --- |
| .NET SDK | **10.x** — fijado en [`global.json`](global.json) (`10.0.300-preview…`, `rollForward: latestFeature`) | compilar y ejecutar | Sí |
| Azurite | `npm install -g azurite` **o** `docker run … azurite` | emular Blob/Table/Queue en local | Sí (para usar la API) |
| Docker | Docker Desktop | el test de integración (Testcontainers) | No — sin él se salta el test |
| Cliente REST | extensión *REST Client* de VS Code o `curl` | lanzar las peticiones de [`api.http`](src/Storage.Demo.Api/api.http) | Recomendado |

Comprueba el SDK: `dotnet --version` debe resolver a 10.x. Si tienes varias versiones, el `global.json` fuerza la correcta dentro de esta carpeta — no toques tu instalación global.

### 11.2 Compilar (verificación rápida sin nube)

```bash
cd examples/M05-Almacenamiento-BBDD/S5.1-azure-storage
dotnet build Storage.Demo.slnx
```

Debe terminar con **0 errores y 0 warnings**. El proyecto tiene `TreatWarningsAsErrors=true`, así que un warning *es* un fallo de build. Si compila, el grafo de tipos y el DI están bien aunque no hayas tocado Azure.

### 11.3 Arrancar Azurite (el emulador)

Elige una opción y déjala corriendo en su propia terminal:

```bash
# Opción A — Azurite por npm
azurite --silent --location ./.azurite

# Opción B — Azurite por Docker (Blob 10000 · Queue 10001 · Table 10002)
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite
```

[`appsettings.Development.json`](src/Storage.Demo.Api/appsettings.Development.json) ya trae `StorageConnection = "UseDevelopmentStorage=true"`, el alias estándar que apunta a esos puertos. No hay que configurar nada más.

Aviso: Azurite no emula Azure Files. El `IFileShareRepository` solo funciona contra un Storage real (lo viste en sección 5.4 y sección 10).

### 11.4 Lanzar la API

```bash
dotnet run --project src/Storage.Demo.Api
```

- Escucha en **`http://localhost:5080`** (perfil `http` de [`launchSettings.json`](src/Storage.Demo.Api/Properties/launchSettings.json), entorno `Development`).
- Prueba de vida: `GET http://localhost:5080/health` → `{ "status": "ok" }`.

El curso nunca lanza la app por ti. Este `dotnet run` lo ejecutas tú; la verificación automatizada se queda en *build + test*.

Si arrancas la API **sin** Azurite, `/health` responde igual (no toca Storage), pero cualquier llamada a `/blob`, `/table` o `/queue` falla con error de conexión. Eso también es una lección: la app depende de que el almacenamiento exista y esté alcanzable.

### 11.5 Ejercitar el ejemplo

Abre [`api.http`](src/Storage.Demo.Api/api.http) con *REST Client* y lanza las peticiones en el orden de sección 9. Por línea de comandos:

```bash
# Subir una factura (el body crudo es el contenido del blob)
curl -X POST http://localhost:5080/blob/facturas/2026/05/f-1.csv \
  -H "Content-Type: text/csv" --data-binary $'factura,total\nF-1,1299.99'

# Listar "la carpeta del mes" = filtrar por prefijo
curl "http://localhost:5080/blob/facturas?prefijo=2026/05/"

# Sugerencia de tier (lógica pura, sin Azurite)
curl http://localhost:5080/blob/tier-sugerido/45
```

sección 9 tiene el guion completo; esto es el "cómo invocarlo".

### 11.6 Pasar los tests

```bash
dotnet test Storage.Demo.slnx
```

| Sin Docker | Con Docker corriendo |
| --- | --- |
| **18 pass · 1 skip · 0 fail** | **19 pass · 0 skip · 0 fail** |

Los 18 unit (`BlobPath`, `AccessTierPolicy`) corren siempre. El de integración es un `SkippableFact`: levanta Azurite con Testcontainers y ejercita Blob+Table+Queue por la API real (`WebApplicationFactory`). Sin Docker se salta y la suite sigue verde; con Docker pasa también a verde.

### 11.7 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `Connection refused` en `/blob`, `/table`, `/queue` | Azurite no está corriendo | arranca Azurite (sección 11.3) en otra terminal |
| El build falla por un warning | `TreatWarningsAsErrors=true` | corrige el warning; aquí no se silencian |
| `dotnet --version` no es 10.x | falta SDK 10 | instálalo; `global.json` ya lo fuerza en esta carpeta |
| El puerto 5080 está ocupado | otra app lo usa | ciérrala o cambia `applicationUrl` en `launchSettings.json` |
| El test de integración sale como *skip* | sin Docker | esperado; arranca Docker si quieres correrlo |
| Puertos 10000-10002 ocupados | otra instancia de Azurite/Storage | mata el proceso o usa la opción Docker con otro mapeo |

### 11.8 Contra un Storage real (opcional)

Para salir del emulador y usar Managed Identity (sin secretos): configura `StorageAccountUri=https://<cuenta>.blob.core.windows.net`, deja `StorageConnection` vacío y `Program.cs` usará `DefaultAzureCredential` (sección 8). El aprovisionamiento por **Portal** y los scripts `az` de complemento están en el [`README.md`](README.md).

---

## 12. Checklist de producción (y de qué te protege cada línea)

| Casilla | De qué te protege |
| --- | --- |
| Public access deshabilitado | Que cualquiera en internet lea tus blobs por URL |
| Firewall `Deny` + excepciones | Acceso desde redes no autorizadas |
| TLS 1.2 mínimo | Interceptación de datos en tránsito |
| Managed Identity (no account keys) | Una key filtrada dando acceso total (sección 8) |
| Soft delete + versioning | Un borrado o sobrescritura por error (sección 7) |
| Lifecycle management | Pagar tier Hot por datos que nadie mira (sección 6) |
| Redundancia ≥ ZRS | Perder datos si cae una zona o región (sección 7) |
| Diagnostic logs + alertas | No enterarte de un ataque o fuga de egress |
| Resource Lock | Que alguien borre la cuenta entera por error |

---

## 13. Ideas para llevarte

Lo que más útil te va a resultar a medio plazo es interiorizar la pregunta del edificio. Antes de guardar algo, te preguntas qué es: un archivo, una clave-valor, un mensaje, un disco compartido. El servicio correcto casi se elige solo. Esa pregunta es todo el ejemplo.

Sobre la **decisión barato vs caro**, la regla práctica que yo defiendo: el barato suele bastar. Cosmos y Service Bus son potentes, pero solo merecen su factura cuando necesitas lo que el barato no da — y eso es menos a menudo de lo que la gente cree. Cuando dudes, empieza por Table o Queue y cambia el día que la limitación duela. Es más fácil migrar a Cosmos cuando ya entiendes tus accesos que migrar desde Cosmos a Table dos años después con un Excel de costes.

Y un par de detalles que te ahorran días buscando bugs: las carpetas de Blob no existen, listar es filtrar por prefijo; el almacenamiento frío es barato de guardar y caro de operar, así que tiers + lifecycle te ahorran dinero solo; y hay tres formas de autenticarte pero solo una es buena en producción — Managed Identity. El `if` de `Program.cs` es esa decisión.

Si tuviera que destacar uno por encima del resto, sería el último. Los demás son tácticos. Pasar de claves a Managed Identity es lo que diferencia un proyecto de juguete de uno que se puede defender en una auditoría.

---

## 14. Comprueba que lo has entendido

Sin mirar atrás. Si dudas, vuelve a la sección.

1. Tu app genera 2 millones de líneas de auditoría al mes y solo las consultas por fecha. ¿Table o Cosmos? ¿Por qué? *(sección 5.2)*
2. Necesitas que los pedidos se procesen **en orden** y que ninguno se pierda. ¿Queue Storage o Service Bus? *(sección 5.3)*
3. Subes `informes/2025/q1.pdf`. ¿Cuántas carpetas se crean en Azure? *(sección 5.1)*
4. Un blob lleva 200 días sin tocarse. Según `AccessTierPolicy`, ¿en qué tier debería estar? ¿Por qué eso ahorra dinero si leerlo es más caro? *(sección 6)*
5. ¿Por qué `Program.cs` ramifica según `StorageAccountUri`? ¿Qué camino usarías en producción y qué nivel de seguridad es? *(sección 8)*
6. Un becario ejecuta `DELETE` sobre el container `facturas` en producción. Tienes GRS activado. ¿Recuperas los datos? ¿Qué te habría salvado? *(sección 7)*
7. ¿Por qué `tier-sugerido` es el único endpoint que no necesita Azurite? *(sección 6, sección 10)*

<details>
<summary>Respuestas</summary>

1. **Table.** Solo consultas por PartitionKey, sin queries complejas. Cosmos costaría unas 500 veces más sin aportar nada para este acceso. Recuerda la historia del principio: catorce meses, 1.200 €/mes, en un caso que en Table habría costado céntimos.
2. **Service Bus.** Queue Storage no garantiza FIFO ni tiene dead-letter, transacciones ni topics. Perder o desordenar un pedido es inaceptable en facturación o pagos.
3. **Cero.** Se crea un solo blob llamado `informes/2025/q1.pdf`. Las carpetas son una ilusión del visor sobre un nombre plano. Si listas con `prefix: "informes/2025/"` lo encuentras; si listas sin prefijo te aparece tal cual.
4. **Archive** (≥180 días). Guardar en Archive cuesta unas nueve veces menos que en Hot. Un dato de 200 días casi no se lee, así que el coste alto de cada lectura (rara) no compensa el ahorro de almacenarlo (constante, todos los meses).
5. Con URI → Managed Identity (nivel 3, sin secretos, producción). Sin URI → connection string (nivel 1, Azurite/dev). En producción, siempre el camino de la URI. El nivel 3 es lo que te recomienda el checklist y lo que te ahorra el "alguien filtró la key en un repo".
6. No con GRS. GRS replica el `DELETE` a las seis copias, también al otro continente. Te protege de fallo de infraestructura, no de error humano. Lo que te salva es soft delete (`undelete` dentro de la ventana de retención) o versioning. Y por eso producción seria activa los dos.
7. Porque `AccessTierPolicy.Sugerir` es lógica pura: evalúa la curva de días, no llama a ningún SDK. Por eso también se testea sin Docker. Es el `endpoint` que demuestra que la decisión vive antes del SDK.

</details>

---

## 15. Hasta aquí

Vuelve un momento a la imagen del edificio de sección 4. Cuatro inquilinos distintos viviendo con la misma dirección, las mismas llaves de edificio y, ahora ya lo sabes, una factura completamente distinta cada uno. Cuando alguien te diga "lo meto en Storage", la pregunta correcta es siempre la misma: ¿en qué piso?

S5.2 te lleva al otro lado de la decisión. Cuando los datos sí piden relaciones, transacciones y un esquema fijo — el catálogo de productos y los pedidos descontando stock atómicamente — Storage ya no sirve. Azure SQL entra en escena con su motor relacional, EF Core encima, y una capa de problemas nueva: connection pooling, retry de errores transitorios, migraciones que nunca, nunca aplicas al arrancar la app. Lo aviso porque ese último te lo van a pedir el primer día que pongas algo en producción. Aprende ahí por qué no.
