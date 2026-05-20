# Manual del alumno — S5.P2 · Práctica: Table Storage CRUD

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica del ejemplo: estructura, mapeo a slides, comandos de test, despliegue por Portal. Este manual va antes: te cuenta qué pone a prueba, qué tres trampas concretas tiene Table Storage y cómo demostrar el CRUD limpio.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M05-S5.P2](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.P2-practica-table-storage-crud-v1.md). Es el **cierre del módulo M05**: la práctica más corta y aterrizada, sobre el servicio más simple y barato de Storage.

*Creado: 2026-05-20 00:02 +0200*

---

## 1. La idea en una frase

S5.1 te enseñó cuándo Table es la respuesta correcta —datos clave-valor, volumen alto, consulta por partición, coste prioritario—. S5.P2 te pone a hacerlo: un CRUD completo de productos contra Table Storage, con las **tres trampas reales** que se llevan más por delante a la gente la primera vez. Caracteres prohibidos en las claves. Tipos que Table no soporta. Filtros OData a mano que se inyectan si no escapas. Esas tres se quedan grabadas si te tropiezas con ellas una vez. Aquí te las pongo delante para que te tropieces a propósito.

---

## 2. El entregable de la práctica

Una API REST sobre la tabla `productos` con seis endpoints, modelo `Producto` que implementa `ITableEntity`, validación de claves a la entrada y filtros OData construidos con escape correcto. Y cuatro decisiones explícitas que justifiquen el diseño:

| Decisión | Por qué | Dónde |
| --- | --- | --- |
| `PartitionKey = categoria` | Query frecuente es "productos por categoría" | [`Producto.cs`](src/Tables.Demo.Api/Domain/Producto.cs) |
| `Precio` en `double` (no `decimal`) | Table Storage **no soporta** `decimal` | misma |
| OData con escape de comillas | Sin escape → inyección trivial | [`ODataFilter.cs`](src/Tables.Demo.Api/Tables/ODataFilter.cs) |
| Validación de claves a la entrada | `/ \ # ?` y control chars rompen Table | [`TableKeys.cs`](src/Tables.Demo.Api/Tables/TableKeys.cs) |

Las cuatro están codificadas en clases puras —cada decisión, función pura testeable— y los tests cubren cada caso. El CRUD vive en [`ProductosService.cs`](src/Tables.Demo.Api/Domain/ProductosService.cs), expuesto desde [`ProductosEndpoints.cs`](src/Tables.Demo.Api/Endpoints/ProductosEndpoints.cs).

---

## 3. Las tres trampas en detalle

### 3.1 `decimal` no existe

```csharp
public sealed class Producto : ITableEntity
{
    public string PartitionKey { get; set; } = "";   // categoría
    public string RowKey { get; set; } = "";          // id
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Nombre { get; set; } = "";
    public double Precio { get; set; }                // ⚠ NO decimal
    public int Stock { get; set; }
}
```

Table Storage tiene una lista cerrada de tipos: `string`, `int32`, `int64`, `double`, `boolean`, `DateTime`, `Guid`, `Binary`. **`decimal` no está**. Si declaras `Precio` como `decimal`, el SDK no te avisa con un error de compilación bonito; te falla en tiempo de ejecución cuando intenta serializar.

¿Cómo se vive con esto? Para precios reales, el patrón profesional es **guardar el importe en céntimos como `long`** y formatear en la API. `1299.00 €` se almacena como `129900`. Te ahorras la imprecisión del coma flotante y te quitas de encima la incompatibilidad con Table. La práctica usa `double` por simplicidad didáctica; en producción seria, ve a `long` de céntimos.

### 3.2 `/ \ # ? ` y control chars rompen las claves

[`TableKeys.cs`](src/Tables.Demo.Api/Tables/TableKeys.cs) codifica esta regla:

```csharp
private static readonly char[] Prohibidos = ['/', '\\', '#', '?', '\t', '\n', '\r'];
// + control chars C0 (0x00-0x1F) y DEL + C1 (0x7F-0x9F)
```

Si tu `PartitionKey` o `RowKey` lleva uno de esos caracteres, la petición a Table falla con un error confuso (`InvalidInput`). Y como las claves vienen muchas veces de input de usuario o de IDs externos, este es el típico bug que aparece en producción cuando alguien sube un producto con `categoria = "papelería/oficina"`.

La práctica tiene dos funciones útiles:

- **`EsValida(key)`** — devuelve `true/false` rápido para validar antes de insertar. El endpoint `POST /productos` lo aplica y devuelve `400 Bad Request` si la clave no es válida. Mejor un 400 limpio que un 500 críptico.
- **`Sanitizar(raw)`** — sustituye los caracteres prohibidos por `-`. Útil cuando recibes IDs externos y prefieres normalizarlos en vez de rechazarlos.

> 🧠 **El patrón del timestamp invertido (slide 14).** La función `RowKeyTimestampInvertido` codifica un truco clásico: Table Storage **ordena los resultados por RowKey ascendente** y no se puede cambiar. Si quieres "los más recientes primero" sin recorrer toda la partición, en lugar de guardar el timestamp en RowKey tal cual, guardas `(MaxValue.Ticks - timestamp.Ticks)` formateado a 19 dígitos. Los más nuevos tienen RowKey "más pequeño" y aparecen primero. Es uno de esos detalles que la documentación menciona de pasada y que cambian totalmente la latencia de un listado.

### 3.3 Los filtros OData se construyen a mano

Table Storage no tiene un query builder tipado decente. Los filtros se pasan como **string en OData**:

```
PartitionKey eq 'electronica'
precio ge 50 and precio le 500
```

Si construyes ese string concatenando strings de usuario sin escapar, tienes una **inyección OData** equivalente a SQL injection. Una `categoria` que sea `' or PartitionKey ne '` te devuelve toda la tabla, ignorando el filtro.

[`ODataFilter.cs`](src/Tables.Demo.Api/Tables/ODataFilter.cs) lo resuelve con dos reglas:

```csharp
// En OData, una comilla simple dentro de un string se duplica
public static string Escapar(string valor) => valor.Replace("'", "''");

public static string PorParticion(string categoria)
    => $"PartitionKey eq '{Escapar(categoria)}'";

// Números: cultura invariante, sin comillas
public static string RangoPrecio(double min, double max)
{
    var lo = min.ToString(CultureInfo.InvariantCulture);
    var hi = max.ToString(CultureInfo.InvariantCulture);
    return $"precio ge {lo} and precio le {hi}";
}
```

Dos detalles que pasan por alto y duelen: `Escapar` duplica las comillas internas (regla OData), y la conversión de números pasa por `CultureInfo.InvariantCulture` para evitar el típico bug de "en mi máquina funciona, pero en el servidor con locale `es-ES` el filtro lleva una coma decimal y Table devuelve error de sintaxis".

> 🧠 **Por qué importa la cultura invariante.** En España, `(50.5).ToString()` te devuelve `"50,5"`. Table interpreta la coma como separador y peta. Esta clase pura te abstrae de la cultura del proceso. Es uno de los `ToString` que vale la pena auditar en cualquier código que serialice números a un protocolo de red.

---

## 4. Por qué este ejemplo es la práctica final del módulo

S5.P2 cierra M05 con el servicio más simple y barato de los seis vistos. Si has llegado hasta aquí entendiendo S5.1 a S5.5, la práctica se hace en una sentada y la usas como referencia para tus proyectos futuros donde aparezca Table.

Y por contraste con S5.P (Cosmos con Managed Identity), S5.P2 **sí usa connection string con AccountKey**. Es deliberado: Table Storage es muchas veces la primera elección para "una BD pequeña y barata", y en proyectos pequeños la connection string con key es lo normal. **En producción**, si pasas a Managed Identity (lo viste en S5.4 con Storage), también funcionaría — pero la práctica usa key para que veas las dos caras: cuándo MI compensa (S5.P) y cuándo connection string es suficiente (S5.P2, proyectos pequeños y entornos cerrados). Para Storage real en producción, recomendado MI; para una BD interna o una práctica, connection string es razonable.

---

## 5. Recorrido guiado

Lanza la API (sección 7) y abre [`api.http`](src/Tables.Demo.Api/api.http). Necesitas Azurite corriendo y la tabla `productos` creada (el script `01-provision.sh` lo hace, o créala desde Storage Explorer).

| # | Petición | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `POST /productos` con `{ partitionKey: "electronica", rowKey: "laptop001", precio: 1299.00, ... }` | `201 Created` con el producto | CRUD básico contra Table. |
| 2 | `POST /productos` con `{ partitionKey: "papelería/oficina", ... }` | `400 Bad Request` con mensaje claro | Validación de claves (sección 3.2). El `/` está prohibido. |
| 3 | `GET /tools/clave?valor=cat/egoria%23mala` | `{ valida: false, sanitizada: "cat-egoria-mala" }` | Las funciones puras de validación expuestas como herramienta. |
| 4 | `GET /productos/categoria/electronica` | lista filtrada por PartitionKey | Single-partition: rápido y barato. |
| 5 | `GET /productos` (sin filtro) | lista completa (scan) | Sin filtro = table scan; barato si la tabla es pequeña, caro si crece. |
| 6 | `GET /productos/electronica/laptop001` | el producto exacto | Read por PK + RK: la operación más rápida. |
| 7 | `PUT /productos/electronica/laptop001` con cambios | producto actualizado | Update con `ETag.All` (último que escribe gana). |
| 8 | `DELETE /productos/electronica/laptop001` | `204 No Content` | Borrar por PK + RK. |
| 9 | `GET /productos/precio?min=50&max=500` | `{ filtro: "precio ge 50 and precio le 500" }` | Filtro OData generado con cultura invariante (sección 3.3). |

Un experimento didáctico: prueba el paso 5 con la tabla casi vacía (rápido) y con muchos productos (lento; cara la operación). Es la diferencia entre "Table es perfecto" y "Table no se usa así". El listado completo de una tabla grande es un anti-patrón: filtra siempre que puedas.

---

## 6. Tests y por qué hay tres capas

**19 pass · 1 skip · 0 fail** sin Docker, **20 pass · 0 skip** con Docker:

- **CAPA 1 · Unit** — `TableKeys` (validación, sanitización, timestamp invertido) y `ODataFilter` (escape, rango con cultura invariante). Pura.
- **CAPA 0 · DI** — resuelve `IProductosService` del `WebApplicationFactory` real. El `TableClient` se construye lazy (no abre conexión en el ctor), así que la capa corre sin Docker. Es la lección DI sin necesidad de Azurite.
- **CAPA 2 · Integration** — `SkippableFact` contra **Azurite** levantado con Testcontainers. Mismo patrón de S5.1 (Azurite sí emula Table Storage, a diferencia de Files). Si no hay Docker, se salta; con Docker, ejercita el CRUD entero por la API real.

> 🎓 **Por qué la app no crea la tabla al arrancar.** Mismo criterio que S5.2/S5.3: el bootstrap de schema no vive en `Program.cs`. La tabla la crea el script `01-provision.sh` o el test de integración explícitamente. El ctor del servicio solo construye el `TableClient` (lazy) → el test de DI funciona sin Azure ni Docker, y producción sigue el principio de "el esquema se aplica con aprobación, no por arranque".

---

## 7. Puesta en marcha

### 7.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y ejecutar | Sí |
| Azurite | emular Table Storage local | Sí (o un Storage real) |
| Docker | levantar Azurite o el contenedor de Testcontainers | Recomendado |

### 7.2 Compilar y arrancar

```bash
cd examples/M05-Almacenamiento-BBDD/S5.P2-practica-table-storage-crud
dotnet build Tables.Demo.slnx                          # 0 errores, 0 warnings

# Azurite en otra terminal
azurite --silent --location ./.azurite

# Crear la tabla 'productos' una vez (o usa Storage Explorer)
az storage table create --name productos --connection-string "UseDevelopmentStorage=true"

# Lanzar la API
dotnet run --project src/Tables.Demo.Api
# → http://localhost:5087
```

Prueba de vida: `GET http://localhost:5087/health` → `{ "status": "healthy" }`. La tabla la crea `scripts/01-provision.sh` automáticamente con productos de seed; usar el script directo es más cómodo si pruebas contra Azure real.

### 7.3 Pasar los tests

```bash
dotnet test Tables.Demo.slnx
# Sin Docker:        19 pass · 1 skip · 0 fail
# Con Docker (Azurite arrancable): 20 pass · 0 skip · 0 fail
```

### 7.4 Despliegue a Azure (entregable)

Los pasos están en el [`README.md`](README.md) y automatizados en `scripts/01-provision.sh`. Resumido:

1. **Storage Account** Standard LRS (lo más barato).
2. **Tabla `productos`** creada desde Storage Browser o con `az storage table create`.
3. **Connection string** en App Settings como `Storage:ConnectionString`. Lleva la AccountKey — para una práctica vale; para producción seria, migra a Managed Identity (patrón S5.4 / S5.P).
4. Despliegue y verificación con `02-smoke-test.sh`.

### 7.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `Connection refused` | Azurite no está corriendo | arranca Azurite (sección 7.2) |
| `TableNotFound` al insertar | falta crear la tabla `productos` | crea la tabla; `01-provision.sh` lo hace por ti |
| `InvalidInput` al crear | PartitionKey o RowKey con caracteres prohibidos | la API ya devuelve `400` antes; revisa el input |
| Decimales formateados como `1,299.00` raros | cultura del proceso ≠ invariante | el `ODataFilter` ya usa `InvariantCulture`; si concatenas a mano, hazlo tú también |
| CAPA 2 sale como *skip* | sin Docker | esperado |

---

## 8. Comprueba que lo has entendido

1. ¿Por qué `Precio` está como `double` y no como `decimal`? ¿Qué patrón usarías en producción para dinero real? *(sección 3.1)*
2. Un usuario sube una `categoria = "papelería/oficina"`. ¿Qué responde la API y por qué? ¿Cómo lo manejarías si quisieras aceptarla? *(sección 3.2)*
3. ¿Por qué `ODataFilter.RangoPrecio` usa `CultureInfo.InvariantCulture` para formatear los números? *(sección 3.3)*
4. ¿Cuál es la diferencia operativa entre `GET /productos` (sin filtro) y `GET /productos/categoria/electronica`? *(sección 5)*
5. ¿Por qué la app no crea la tabla `productos` en `Program.cs` y cómo se prepara la base? *(sección 6)*
6. S5.P usa Managed Identity y S5.P2 usa connection string con AccountKey. ¿En qué contexto cada una es la elección correcta? *(sección 4)*

<details>
<summary>Respuestas</summary>

1. Porque **Table Storage no soporta `decimal`**: serializaría mal. `double` cabe en los tipos permitidos. Para dinero real, el patrón profesional es guardar el importe **en céntimos como `long`** (1299.00 € → 129900) y formatear en la API. Te quitas la imprecisión del flotante y la incompatibilidad con Table.
2. La API devuelve **`400 Bad Request`** con mensaje claro: "PartitionKey/RowKey obligatorios y sin `/ \ # ?` ni control chars". El `/` es uno de los caracteres prohibidos de Table. Si quisieras aceptar la entrada del usuario, llamarías a `TableKeys.Sanitizar("papelería/oficina")` y guardarías `"papelería-oficina"`, devolviéndole al usuario la versión normalizada. La validación a la entrada es la opción más limpia; sanitizar antes de insertar es la pragmática.
3. Porque en España (o cualquier locale con coma decimal), `(50.5).ToString()` devuelve `"50,5"`, y Table rechaza el filtro con coma. Con `InvariantCulture` siempre obtienes `"50.5"`. El bug de "en mi máquina funciona, en el servidor falla por locale distinto" es uno de los clásicos al serializar números a protocolos de red. Esta clase te abstrae del problema.
4. `GET /productos` sin filtro es un **table scan**: Table recorre todas las particiones. Rápido con pocos productos, caro y lento cuando la tabla crece. `GET /productos/categoria/electronica` filtra por PartitionKey: Table sabe a qué partición ir y devuelve solo esos datos. Es la diferencia entre "todas las sucursales" y "una sucursal" (de la analogía de Cosmos en S5.3, que aplica igual aquí). Salvo en tablas muy pequeñas, **filtra siempre por PartitionKey** si tu acceso lo permite.
5. Porque el bootstrap de schema no debe vivir en el arranque de la app: race conditions con múltiples instancias, no es atómico con el deploy, no es revisable. Mismo criterio que las migraciones de S5.2 (anti-patrón 8) y los containers de S5.3. La tabla la crea el script `01-provision.sh` (en el deploy) o el test de integración (en su scope). El ctor del servicio solo construye el `TableClient`, que es lazy.
6. **Managed Identity (S5.P)** es la elección correcta para producción seria: cero secretos, rotación instantánea, auditoría completa. Te enseña el patrón que vas a aplicar en tu trabajo real, especialmente con Cosmos y SQL. **Connection string (S5.P2)** es razonable para proyectos pequeños, entornos cerrados, prácticas de curso y CI/CD donde la rotación de la AccountKey está en otro pipeline. Saber elegir es parte de la madurez: no toda app necesita el coste de configurar MI; no toda app puede permitirse vivir con una key.

</details>

---

## 9. Hasta aquí

Con S5.P2 cierras M05. Has pasado por las cuatro decisiones grandes del almacenamiento en Azure: qué guardar fuera de la base de datos (S5.1), cuándo SQL es la respuesta (S5.2), cuándo Cosmos lo justifica (S5.3), cómo conectarte sin secretos a los tres (S5.4) y cómo prepararte para el día en que algo se rompa (S5.5). Las dos prácticas (S5.P y S5.P2) son lo que cementa los conceptos: el CRUD keyless contra Cosmos y el CRUD simple contra Table.

Lo que viene a continuación es **M06: Seguridad, Autenticación e Identidad**. Si M05 te enseñó cómo guardar los datos y conectarte a ellos sin secretos, M06 te lleva a la capa de arriba: **cómo el usuario llega a tu app y demuestra quién es**. OAuth2, OpenID Connect, JWT, Entra ID end-to-end. Vas a ver muchos conceptos por primera vez, y un par de patrones (DefaultAzureCredential, Managed Identity) que ya te resultarán familiares.
