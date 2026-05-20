# Manual del alumno — S3.4 · Trigger Blob Storage

Esto **no** es el [`README.md`](README.md) (que actualmente comparte contenido con S3.2 — léelo como referencia técnica complementaria, no como guion específico de Blob). Este manual cubre lo nuevo del submódulo: el `[BlobTrigger]`, el patrón "Blob de entrada → resumen como Blob de salida" y la trampa clásica de los loops infinitos.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M03-S3.4](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.4-trigger-blob-storage-v4.md). Reutiliza el skeleton de S3.2/S3.3 y añade una función que importa productos desde CSVs subidos al container `uploads/`, generando un JSON de resumen en `procesados/`.

*Creado: 2026-05-20 12:02 +0200*

---

## 1. La idea en una frase

Hasta ahora las funciones se disparaban por petición HTTP (S3.2) o por reloj (S3.3). En S3.4 se disparan **por aparición de un archivo**. Subes un CSV al container `uploads/`, Azure Functions detecta el nuevo blob en pocos segundos y lanza tu función con el contenido del archivo como parámetro. Procesas, escribes el resultado en otro container y la función termina. **Cero endpoints HTTP, cero código de upload, cero "polling cada minuto a ver si llegó algo"**.

Este patrón cambia la forma de integrar sistemas. En lugar de "hazme un POST al endpoint con tu archivo en multipart", ofreces "sube el archivo directamente al container `uploads/`". El cliente sube con un SAS token (el patrón que viste en M05-S5.1), Functions se entera automáticamente, procesa. El "endpoint" desaparece — su lugar lo ocupa una convención sobre containers de Storage.

---

## 2. El problema real que hay detrás

Un proveedor externo tenía que mandar a una empresa un CSV con el catálogo actualizado cada semana. La solución original: el proveedor mandaba el CSV por email, alguien lo guardaba en una carpeta de red, otra persona ejecutaba un script Python a mano que importaba a la BD. Tres pasos manuales, dependencia humana, errores frecuentes (CSV mal codificado, alguien olvida ejecutar el import).

La solución con Blob trigger: el proveedor sube el CSV directamente al container `uploads/` (con un SAS token de upload con caducidad). Azure Functions detecta el blob nuevo, llama a `CsvImportFunction`, importa los productos a la BD, escribe un JSON de resumen en `procesados/{archivo}-resumen.json`. El proveedor o el operador pueden consultar `/api/imports/{archivo}` para ver el resultado: cuántas líneas se importaron, cuántas dieron error, cuáles fueron los errores específicos.

Resultado: **cero pasos manuales en el flujo normal**, retro alimentación automática vía el JSON de resumen, trazabilidad por archivo. Y la función se ejecuta solo cuando hay un CSV — el resto del tiempo es coste cero.

Lo que entrega:

| Pieza | Para qué | Dónde |
| --- | --- | --- |
| **`[BlobTrigger("uploads/{name}.csv", ...)]`** | Dispara la función con cada blob nuevo en el container | [`CsvImportFunction.cs`](src/AzureFunctions.Demo/Functions/CsvImportFunction.cs) |
| **`[BlobOutput("procesados/{name}-resumen.json", ...)]`** | El `return` se escribe automáticamente como blob en otro container | misma función |
| **`{name}` como placeholder de ruta** | Extrae el nombre del archivo y lo pasa como parámetro `string name` | atributo + parámetro |
| **`CsvProductosImporter`** | Lógica de parsing CSV y validación, separada de la función | [`Services/CsvProductosImporter.cs`](src/AzureFunctions.Demo/Services/CsvProductosImporter.cs) |
| **`IImportSummaryService`** | Guarda los resúmenes para que se puedan consultar via HTTP | + `InMemoryImportSummaryService` |
| **`/api/imports` + `/api/imports/{archivo}`** | Endpoints HTTP para ver los resúmenes sin pinchar el storage | [`ImportsHttpFunctions.cs`](src/AzureFunctions.Demo/Functions/ImportsHttpFunctions.cs) |

---

## 3. Por qué esto importa en tu stack

El patrón "Blob trigger + output binding a otro container" es la forma estándar de hacer **integraciones por archivo** en Azure sin escribir un microservicio entero. Cualquier flujo donde antes había:

- Un cron que polea una carpeta
- Un endpoint HTTP que recibe `multipart/form-data`
- Un FTP server con un script post-upload
- Un Logic App con conector de SharePoint

...puede reescribirse con Blob trigger en unas decenas de líneas de C# y un container de Storage. El coste operativo cae prácticamente a cero — no hay servidor que mantener, no hay endpoint que monitorizar, el escalado es automático.

Y hay una decisión sutil: **el cliente no necesita una API tuya para subir el archivo**. Le das un SAS token con permiso de write sobre el container `uploads/`, válido por unos minutos. El cliente hace `PUT` directo al endpoint del Storage Account (no a tu app), Azure se encarga de la autenticación y los megas/gigas. Tu Function App no ve los bytes del upload — solo se dispara cuando el blob está completo. Es un patrón muy escalable: los uploads no pasan por tu compute.

| Antes (endpoint multipart) | Con Blob trigger |
| --- | --- |
| Cliente hace `POST /api/upload` a tu API | Cliente hace `PUT` directo al Storage con SAS |
| Tu API recibe los bytes, los procesa, escribe | Storage recibe los bytes; tu API se entera por evento |
| El tamaño del upload pasa por tu compute | El compute solo se ejecuta al procesar |
| Si la API se cae, los uploads fallan | Storage acepta el upload aunque la API esté caída — los blobs se procesarán cuando vuelva |

---

## 4. El modelo mental: el buzón con timbre

Imagina un edificio con un buzón en la entrada y un timbre interior conectado. Cada vez que alguien deja una carta en el buzón, suena el timbre dentro. Sale alguien, recoge la carta, la procesa (la abre, la lee, la archiva), y deja una nota en otra bandeja diciendo "carta recibida y procesada". El timbre vuelve a su silencio hasta la próxima carta.

El **buzón** es el container `uploads/`. El **timbre** es el Blob trigger. Quien sale a recogerla es **tu función**. La **bandeja** donde deja la nota es el container `procesados/` (el output binding). La regla importante: la persona que sale a procesar **no echa cartas de vuelta al buzón**. Si lo hiciera, sonaría el timbre otra vez y entraría en bucle.

```
Cliente               Container uploads/        Function App           Container procesados/
  │                          │                       │                          │
  │ PUT con SAS              │                       │                          │
  │────────────────────────▶│                       │                          │
  │                          │  blob "ventas.csv"    │                          │
  │                          │ creado                │                          │
  │                          │──── (evento) ───────▶│                          │
  │                          │                       │ procesa CSV               │
  │                          │                       │ + escribe resumen         │
  │                          │                       │──────────────────────────▶│
  │                          │                       │                          │ blob
  │                          │                       │                          │ ventas.csv-resumen.json
```

Tres frases para fijar el modelo:

- **El blob de entrada y el de salida deben estar en containers distintos.** Si la función escribiera en `uploads/`, generaría un blob nuevo allí mismo y se dispararía a sí misma — loop infinito que cobra ejecuciones y produce blobs en cascada hasta que alguien lo para. Por eso `[BlobTrigger("uploads/...")]` y `[BlobOutput("procesados/...")]` apuntan a containers diferentes.
- **El placeholder `{name}` extrae el nombre del archivo sin extensión.** En `uploads/{name}.csv`, si subes `ventas-2026-05-20.csv`, `{name}` será `ventas-2026-05-20`. Puedes usarlo en el output binding (`procesados/{name}-resumen.json` → `procesados/ventas-2026-05-20-resumen.json`) y como parámetro `string name` en el método.
- **La latencia entre subir el blob y disparar la función es de segundos, no inmediata.** En Consumption con el método de detección por polling (el que Azure usa cuando no configuras EventGrid), suele tardar 5-30 segundos. Para latencia menor, hay que configurar Event Grid → Blob events, pero para la mayoría de los casos el polling basta.

---

## 5. La regla del container distinto (y por qué el ejemplo la subraya)

Mira el atributo combinado de [`CsvImportFunction.cs`](src/AzureFunctions.Demo/Functions/CsvImportFunction.cs):

```csharp
[Function(nameof(ImportarCsv))]
[BlobOutput("procesados/{name}-resumen.json", Connection = "AzureWebJobsStorage")]
public string ImportarCsv(
    [BlobTrigger("uploads/{name}.csv", Connection = "AzureWebJobsStorage")] string contenido,
    string name)
```

El input es `uploads/{name}.csv` y el output `procesados/{name}-resumen.json`. **Dos containers distintos**. Si los dos fueran `uploads/`:

```
1. Alguien sube ventas.csv a uploads/
2. La función se dispara, procesa, devuelve un JSON
3. El JSON se escribe como blob en uploads/ — porque BlobOutput apunta ahí
4. El runtime detecta el blob nuevo en uploads/
5. Vuelve a 1 (con el JSON como input, que probablemente no parsea como CSV)
6. ... loop hasta que alguien para la Function App
```

Es uno de los errores más caros que se ven cuando alguien empieza con Blob triggers. La función entra en bucle, cada iteración cuesta ejecuciones (Consumption) y produce blobs nuevos en el container, y el blob original que querías procesar puede quedar sepultado entre miles de resultados intermedios. La factura del primer mes incluye un par de millones de ejecuciones inesperadas.

> 🧠 **La regla mental: input y output binding del mismo trigger deben apuntar a containers distintos.** Si la función necesita escribir en el mismo container por algún motivo extraño, usa un prefijo o un sufijo en el nombre del blob que el `BlobTrigger` no recoja — pero la opción correcta y simple es separar containers desde el día uno. El ejemplo lo hace explícito con `uploads/` (entrada) y `procesados/` (salida).

Y hay una segunda razón menos obvia para separar containers: **permisos**. Al proveedor que sube los CSVs le das un SAS token con write sobre `uploads/`. No tiene visibilidad ni acceso a `procesados/`. La separación física simplifica el modelo de seguridad.

---

## 6. El output binding: `return` que se escribe como blob

`[BlobOutput("procesados/{name}-resumen.json", ...)]` aplicado al método hace algo elegante: **el valor de retorno se escribe automáticamente como blob**. No tienes que crear un `BlobServiceClient`, abrir un stream, escribir, cerrar. Devuelves el string JSON con el resumen y Functions se encarga.

```csharp
public string ImportarCsv(...)
{
    var resultado = importer.Import($"{name}.csv", contenido);
    summary.Registrar(resultado);
    return JsonSerializer.Serialize(resultado, JsonOptions);  // ← se escribe como blob
}
```

¿Qué pasa si quieres escribir más de un blob? Hay varias opciones:

- **Devolver `IEnumerable<string>` o tipos coleccionables** con bindings que lo soporten.
- **Usar `IAsyncCollector<T>`** inyectado por parámetro para hacer `await collector.AddAsync(blob)` varias veces.
- **Inyectar `BlobServiceClient` por DI** y escribir manualmente con el SDK.

El ejemplo usa la forma más simple (return como single output) porque el caso es 1-a-1: un CSV de entrada genera un resumen de salida. Para casos más complejos, los otros patrones están documentados en `Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs`.

> 🧠 **Output binding vs SDK explícito.** El output binding es elegante para casos sencillos: el código se ve declarativo, no hay manejo de connection strings ni de excepciones de I/O en tu función. Pero pierde control: si la escritura falla, la función falla entera, no puedes hacer retry específico de la escritura. Para escenarios donde la lógica de escritura es compleja (varios blobs, lógica condicional, retry por blob), inyectar `BlobServiceClient` por DI es más explícito y testeable. El ejemplo está bien con output binding porque es 1-a-1.

---

## 7. La lección de DI que el HANDOFF advierte

Mira [`Program.cs`](src/AzureFunctions.Demo/Program.cs) — un comentario explícito:

```csharp
// TODOS los servicios que inyectan las funciones por constructor deben
// registrarse aquí o el host no puede instanciarlas:
//   CsvImportFunction     → ICsvProductosImporter, IImportSummaryService
//   ImportsHttpFunctions  → IImportSummaryService
//   InformesHttpFunctions → IInformeService
//   TimerFunctions        → IProductoService, IInformeService
//   ProductosFunctions    → IProductoService
builder.Services.AddSingleton<IProductoService, InMemoryProductoService>();
builder.Services.AddSingleton<IInformeService, InMemoryInformeService>();
builder.Services.AddSingleton<IImportSummaryService, InMemoryImportSummaryService>();
builder.Services.AddSingleton<ICsvProductosImporter, CsvProductosImporter>();
```

Esta lista es la del HANDOFF del proyecto — Lección 1, **"Bug de DI latente"**: los tests de Functions instancian con `new` y **no ejercitan el contenedor DI**. Si te olvidas de registrar `ICsvProductosImporter` aquí, los tests pasan (le pasas un mock al `new CsvImportFunction(mock, ...)`) pero la Function App real falla en runtime: *"Unable to resolve service for type ICsvProductosImporter"*.

Por eso el comentario está ahí — para que cada vez que añadas una función nueva, cruces a mano sus parámetros de constructor con los `AddSingleton` del `Program.cs`. Es el bug que más frustración causa la primera vez y el que el comentario evita.

Cuando llegues a M04-S4.5 verás un patrón explícito de **test de contenedor DI** que cubre esta laguna (resolver el host real y comprobar que todas las funciones se instancian). De momento, la regla es manual: añade función → revisa Program.cs.

---

## 8. Recorrido guiado

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | Local: `func start` con Azurite levantado | logs "Job host started" sin errores | El skeleton arranca. |
| 2 | Sube un CSV a `uploads/` con `az storage blob upload --auth-mode login` (o con Storage Explorer) | en 5-30 segundos: log "CSV detectado en uploads: ventas.csv (X chars)" | El Blob trigger detecta el archivo. |
| 3 | Después del paso 2: log "CSV ventas.csv: 50/52 ok, 2 errores" | el `CsvProductosImporter` parseó y reportó | El return va al BlobOutput automáticamente. |
| 4 | Lista `procesados/` con Storage Explorer | aparece `ventas.csv-resumen.json` | Output binding en acción. |
| 5 | `curl http://localhost:7071/api/imports/ventas.csv?code=...` (en Azure; en local sin code) | JSON con el resumen completo (líneas ok/error, errores específicos) | Endpoint HTTP de soporte para no tener que ir al storage. |
| 6 | Sube **dos CSVs distintos** rápido seguidos | cada uno dispara su función — paralelismo automático según Consumption | Functions escala instancias para procesar uploads en paralelo. |
| 7 | Sube un CSV con extensión incorrecta (`.txt`) | la función **no se dispara** (el trigger es `*.csv`) | El patrón del trigger filtra qué blobs disparan. |

Un experimento muy útil para entender el flujo: sube el mismo `ventas.csv` dos veces (con `--overwrite`). La función se dispara dos veces porque la **escritura** del blob (no su existencia) es el evento. Cada upload con `overwrite` cuenta como un blob nuevo desde el punto de vista del trigger.

Y un experimento opcional para ver la lección de "container distinto": cambia temporalmente el BlobOutput a `uploads/{name}-resumen.json`, sube un CSV, observa cómo entra en bucle (cada resumen genera otro trigger porque también termina en `.csv` desde el punto de vista del patrón si la convención fuera distinta — en este caso `.json` no coincide con el filtro `*.csv`, así que el bucle no se da; pero si el output fuera `.csv`, sí). **No lo hagas en Azure real** — solo en local con Azurite, y para inmediatamente cuando entiendas el concepto.

---

## 9. Tests del proyecto

Los tests de S3.4 están en `tests/AzureFunctions.Demo.Tests/` heredando los de S3.2/S3.3 y añadiendo:

- **Tests de `CsvProductosImporter`** — parsing de CSV bien/mal formado, validación de cada fila, suma de líneas ok/error. Son tests de servicio puro, sin Functions.
- **Tests de `CsvImportFunction`** — instancian la función con mocks de `ICsvProductosImporter` e `IImportSummaryService`, llaman al método con un string CSV de prueba, verifican el JSON devuelto y que `summary.Registrar` se llamó con el resultado esperado.
- **Tests de `ImportsHttpFunctions`** — los endpoints HTTP de consulta, igual patrón que los CRUD de S3.2.

Como siempre con Functions: tests por instanciación directa, sin runtime. **Y como siempre con Functions, la laguna del DI no se cubre con estos tests** — la regla del HANDOFF aplica.

---

## 10. Puesta en marcha, ejecución y pruebas

### 10.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y testear | Sí |
| Azure Functions Core Tools (`func`) | `func start` local | Recomendado |
| Azurite | emular Storage local — el Blob trigger lo necesita | Sí |
| Storage Explorer o `az storage blob upload` | subir CSVs de prueba a Azurite | Recomendado |

### 10.2 Compilar y arrancar en local

```bash
cd examples/M03-Azure-Functions-I/S3.4-trigger-blob-storage
dotnet build AzureFunctions.Demo.slnx     # 0 errores

cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json

azurite --silent           # otra terminal

cd src/AzureFunctions.Demo
func start
# → http://localhost:7071/api/* (los endpoints HTTP)
# → BlobTrigger escuchando uploads/{name}.csv
```

Crea los dos containers en Azurite (con Storage Explorer o `az`):

```bash
az storage container create --name uploads      --connection-string "UseDevelopmentStorage=true"
az storage container create --name procesados   --connection-string "UseDevelopmentStorage=true"
```

Y sube un CSV de prueba:

```bash
az storage blob upload --container-name uploads --name ventas.csv \
  --file ./ventas.csv --connection-string "UseDevelopmentStorage=true"
```

A los pocos segundos verás los logs del trigger disparándose.

### 10.3 Pasar los tests

```bash
dotnet test
```

Tests heredados + nuevos del importer y la función. Sin Azure, sin Docker, sin Azurite (los tests instancian directamente).

### 10.4 Desplegar a Azure (resumen)

Mismo patrón que los anteriores: RG + Storage + Function App Consumption Linux .NET 10 isolated. **Importante**: crea los containers `uploads` y `procesados` en el Storage Account después de aprovisionar (`az storage container create`).

La connection string `AzureWebJobsStorage` ya apunta al storage por defecto cuando creas la Function App, así que los blob bindings usan el mismo storage que el runtime. Si quisieras un Storage Account separado para los datos, configura una App Setting nueva (`MyDataStorage`) y cambia los atributos a `Connection = "MyDataStorage"`.

### 10.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| El trigger no se dispara aunque el CSV está en `uploads/` | falta el container `uploads` (Azure no lo crea automáticamente) | créalo con `az storage container create` |
| Latencia de 30+ segundos antes del trigger | método de detección por polling (default en Consumption) | normal; configura Event Grid → Blob events para latencia menor |
| La función entra en bucle infinito | output y trigger apuntan al mismo container | separa: trigger en `uploads/`, output en `procesados/` (sección 5) |
| `Unable to resolve service` al desplegar | falta `AddSingleton<TuServicio>` en `Program.cs` | revisa la lista de servicios contra los constructores (sección 7) |
| Subes un CSV y se dispara dos veces | el cliente hizo `overwrite` o hubo retry | el código debe ser idempotente (lección S3.3) |

### 10.6 Limpieza

`Portal → Resource groups → rg-curso-m03-s34 → Delete`. Borra Storage (incluidos `uploads/` y `procesados/`) + Function App.

---

## 11. Ideas para llevarte

Lo más útil de esta práctica es **interiorizar el patrón "integración por archivo"**. Cuando veas un flujo donde un sistema externo necesita enviar datos por lotes y el otro sistema necesita procesarlos, la primera pregunta correcta es: ¿podemos usar Blob trigger? Casi siempre la respuesta es sí, y el código resultante es mucho más simple que la alternativa (endpoint multipart, FTP, conectores caros).

Sobre la **regla del container distinto**: tatúatela. Es el error más caro de los Blob triggers para principiantes, y el debugger no te lo señala con un error claro — lo descubres cuando la factura del primer mes incluye millones de ejecuciones inesperadas. Containers separados desde el día uno.

Sobre el **DI**: la lección del HANDOFF (cruzar a mano constructores contra `Program.cs`) sigue valiendo. En M04-S4.5 verás cómo automatizarlo con un test que resuelva el host real. Hasta entonces, el comentario en `Program.cs` listando qué servicio inyecta qué función es la mejor defensa.

Y sobre **output bindings**: úsalos cuando el patrón es 1-a-1 (un input, un output). Para casos más complejos, inyecta el SDK por DI y controla la escritura manualmente. El output binding es elegancia hasta que se queda pequeño; no fuerces.

---

## 12. Comprueba que lo has entendido

1. Tu cliente externo necesita mandarte un archivo CSV diario. ¿Por qué Blob trigger es mejor opción que un endpoint HTTP multipart? *(sección 3)*
2. Configuras `[BlobTrigger("logs/{name}.log")]` y `[BlobOutput("logs/{name}-procesado.json")]`. ¿Qué pasa al subir el primer log? *(sección 5)*
3. `{name}` en `uploads/{name}.csv`. Subes `ventas-2026-05-20.csv`. ¿Cuál es el valor de `name` que recibe tu función? ¿Y el blob que se crea en `procesados/{name}-resumen.json`? *(sección 4)*
4. Subes un CSV de 500 MB. ¿Cuánto compute consume tu Function App durante el upload? *(sección 3)*
5. Añades una función nueva con constructor `MiFuncion(IMiServicio servicio)`. Los tests pasan. Despliegas y la Function App falla con "Unable to resolve service for type IMiServicio". ¿Qué falta y por qué los tests no lo cazaron? *(sección 7)*
6. Tu función procesa un CSV y devuelve un string JSON. El `[BlobOutput]` escribe ese JSON como blob automáticamente. ¿En qué escenarios este patrón se queda corto y conviene inyectar `BlobServiceClient`? *(sección 6)*

<details>
<summary>Respuestas</summary>

1. Cuatro razones principales: **(a) los bytes no pasan por tu compute** — el cliente sube directo a Storage con SAS, tu Function App se entera por evento sin procesar megas/gigas; **(b) escalabilidad gratis** — Storage acepta uploads concurrentes sin que tú configures nada; **(c) resiliencia** — si tu API está caída, Storage acepta el upload igual y la función lo procesará cuando vuelva; **(d) simplicidad** — cero código de upload, multipart, validación de chunks. El endpoint multipart es la opción correcta si necesitas validar/transformar el archivo en tiempo real (rechazar uploads inválidos antes de aceptarlos) o si los clientes no pueden hablar Storage directamente.
2. **Loop infinito**. El primer log dispara la función, que escribe `log1-procesado.json` en el mismo container `logs/`. El runtime detecta el blob nuevo. Aunque el filtro original es `*.log` y este nuevo es `.json`, **si el patrón fuera `*` o si el output fuera `*.log`, entraría en bucle**. Con `.log` vs `.json` el bucle se evita por casualidad. La regla correcta: **containers separados**. Aquí el error sería poner los dos en `logs/` aunque con extensiones distintas — funciona hoy pero el día que alguien cambia `procesado.json` a `procesado.log`, bug.
3. **`name = "ventas-2026-05-20"`** (sin extensión, el placeholder captura todo hasta el último punto del filtro). El blob de output será **`procesados/ventas-2026-05-20-resumen.json`**. El patrón `{name}` toma todo lo que va donde está el placeholder y lo expone como parámetro `string name`. Útil para mantener trazabilidad: el archivo original y su resumen comparten la parte significativa del nombre.
4. **Cero compute durante el upload**. El cliente está subiendo directo al Storage Account (no a tu Function App). Tu compute solo se activa cuando el blob está **completo** y el runtime detecta el evento. Si el cliente tarda 5 minutos en subir los 500 MB, tu Function App está inactiva durante esos 5 minutos. Cuando termina, el trigger se dispara y procesa. Es una de las grandes ventajas del patrón Blob: la separación entre "absorber el upload" (Storage, escalable y barato) y "procesar el contenido" (Functions, pagas solo por ejecución).
5. **Falta `builder.Services.AddSingleton<IMiServicio, MiServicioImpl>()` en `Program.cs`**. Los tests no lo cazan porque **instancian `MiFuncion` directamente** con `new MiFuncion(new MiServicioMock())` — saltándose el contenedor de DI. El host real de Functions sí usa el contenedor y, al no encontrar el registro, falla en runtime. Es el bug latente del HANDOFF, Lección 1. La defensa manual: cruzar a mano constructores contra registros de `Program.cs` después de añadir cualquier función. La defensa automática (más adelante): un test que resuelva el host real y compruebe que todas las funciones se instancian (lo verás en M04-S4.5).
6. Tres escenarios donde el output binding se queda corto: **(a) varios blobs de salida** — el binding del return es 1-a-1; para N blobs por ejecución hace falta `IAsyncCollector<T>` o inyectar `BlobServiceClient`; **(b) lógica condicional** — si solo escribes el blob bajo ciertas condiciones, el output binding obliga a devolver `null` (lo cual a veces sí escribe un blob vacío, según versión del binding) y conviene controlar explícitamente; **(c) retry específico de la escritura** — si la escritura falla y quieres reintentar solo esa parte (no el procesamiento completo), inyectar el client te da el control. Para el caso simple del ejemplo (1 CSV → 1 resumen), el output binding es ideal.

</details>

---

## 13. Hasta aquí

Vuelve a la imagen del buzón con timbre de la sección 4. Cuando veas un flujo de integración entre dos sistemas donde uno necesita mandar datos por archivos, **piensa primero en Blob trigger**. Es la opción más simple, más barata y más operativa para el 80% de los casos.

Lo siguiente es [`S3.5 — Trigger Cosmos DB Change Feed`](../S3.5-trigger-cosmosdb-changefeed/MANUAL.md), donde el atributo cambia a `[CosmosDBTrigger]` y la función pasa de "ejecutar cuando llega un archivo" a "ejecutar cuando cambia un documento en Cosmos". El patrón sigue siendo el mismo (skeleton, DI, idempotencia), pero introduce el **Change Feed**: el log de cambios que Cosmos mantiene y que Functions consume con leases distribuidos. Es la base de cualquier arquitectura event-driven sobre Cosmos.
