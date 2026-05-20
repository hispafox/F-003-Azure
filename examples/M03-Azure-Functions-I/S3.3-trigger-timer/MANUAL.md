# Manual del alumno — S3.3 · Trigger Timer: tareas programadas e idempotencia

Esto **no** es el [`README.md`](README.md) (que actualmente comparte contenido con S3.2 — léelo como referencia técnica complementaria del CRUD heredado, no como guion específico de Timer). Este manual cubre lo nuevo del submódulo: los dos `[TimerTrigger]` que se añaden sobre el skeleton, la sintaxis NCRONTAB, las trampas del retry/past-due y, sobre todo, **el patrón de idempotencia con `ConcurrentDictionary.TryAdd`** que conviene tener en la cabeza para cualquier proceso que se ejecute más de una vez.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M03-S3.3](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.3-trigger-timer-v4.md). Reutiliza el CRUD de productos de S3.2 y le añade dos timers (`CleanupCadaMinuto` y `InformeDiario`) más dos endpoints HTTP de soporte para ver los informes generados.

*Creado: 2026-05-20 12:02 +0200*

---

## 1. La idea en una frase

S3.2 demostró que en Functions HTTP el código es casi idéntico a Minimal API. S3.3 demuestra el otro lado del trato serverless: **lo que de verdad cambia con Functions es lo que App Service no hace bien — tareas programadas, eventos, mensajes**. Un `[TimerTrigger("0 0 6 * * *")]` reemplaza una VM dedicada con cron, un Hangfire complicado o un Quartz.NET con su propio scheduler. Sale cero euros al mes (treinta ejecuciones caben en la cuota gratis), no requiere mantenimiento, y el código son seis líneas.

La trampa que el ejemplo entrena: **una tarea programada se puede ejecutar dos veces**. Por un retry, por un scale-out, por un `IsPastDue` mal manejado. Si tu código no es idempotente, generas duplicados — facturas duplicadas, emails duplicados, informes duplicados. La forma correcta no es "rezar para que solo se ejecute una vez", es **escribir el código de forma que ejecutarlo dos veces no haga daño**.

---

## 2. El problema real que hay detrás

Un equipo tenía un proceso nocturno que generaba el informe del día anterior y lo mandaba por email a los stakeholders. Cron + script Python en una VM. Durante un año funcionó perfecto — un email por día. Hasta el día que la VM se cayó a las 02:00, el equipo de infra la reinició a las 03:30, y a las 04:00 el cron disparó otra vez "porque le tocaba la siguiente ejecución del día". Resultado: dos emails iguales a la misma hora del día siguiente, gente confundida, sospechas de seguridad ("¿alguien está cambiando los datos?"), un par de horas perdidas reconstruyendo qué pasó.

La causa raíz no era la VM ni el cron — eran herramientas de propósito general. La causa era que **el código generaba el informe sin verificar si ya existía uno para esa fecha**. Cualquier doble ejecución producía duplicado. Lo que esta práctica entrena es exactamente eso: **el patrón de idempotencia** con `TryAdd` sobre un `ConcurrentDictionary`, atómico, sin race conditions, sin invocar el factory dos veces.

Lo que entrega:

| Pieza | Para qué | Dónde |
| --- | --- | --- |
| **`CleanupCadaMinuto`** | Tarea recurrente alta frecuencia (cada minuto) con CRON en App Setting | [`TimerFunctions.cs`](src/AzureFunctions.Demo/Functions/TimerFunctions.cs) |
| **`InformeDiario`** | Tarea programada baja frecuencia (06:00 UTC diaria) | misma |
| **`IsPastDue` handling** | Detectar cuándo el runtime perdió ejecuciones | `if (timer.IsPastDue) { ... }` |
| **`GenerarSiNoExiste` con `TryAdd`** | Idempotencia atómica thread-safe | [`InMemoryInformeService.cs`](src/AzureFunctions.Demo/Services/InMemoryInformeService.cs) |
| **`/api/informes` + `/api/informes/{fecha}`** | Endpoints HTTP para verificar el resultado sin pinchar logs | [`InformesHttpFunctions.cs`](src/AzureFunctions.Demo/Functions/InformesHttpFunctions.cs) |
| **Servicios como Singleton** | El estado persiste entre ejecuciones del timer mientras la instancia esté caliente | `Program.cs` → `AddSingleton<IInformeService, ...>` |

---

## 3. Por qué esto importa en tu stack

Cualquier proyecto de cierto tamaño tiene varias "cosas que pasan por sí solas": informes nocturnos, limpieza de datos expirados, sincronizaciones programadas, recordatorios. Tradicionalmente vivían en VMs con cron, en Hangfire dentro de App Service (que requiere Always On y librerías propias), o en SQL Server Agent. Functions Timer es la opción más simple, más barata y más operativa:

- **Definición declarativa**: un atributo con CRON. Cambias el CRON en App Settings y se aplica sin redeploy.
- **Sin mantenimiento**: el runtime gestiona el storage (lock distribuido), los retries, el escalado.
- **Coste cero** para frecuencias razonables. Treinta ejecuciones al mes (informe nocturno) son gratis siempre. Mil ejecuciones al mes (cada minuto) son gratis siempre. Los millones de ejecuciones (cada segundo) ya empezarían a verse en la factura, pero ahí estás fuera del caso de uso normal.

La trampa que aparece cuando llevas un tiempo con Functions Timer es **asumir que cada ejecución es única**. No lo es. Lecciones que conviene aprender antes de necesitarlas:

| Situación | Qué pasa | Cómo lo manejas |
| --- | --- | --- |
| Tu Function App tiene varias instancias (Premium) | El timer dispara en una sola — el runtime hace lock distribuido en Storage | Nada que hacer, ya está cubierto |
| Una ejecución falla con excepción | El runtime puede reintentar según la política | El código debe ser idempotente |
| La Function App estaba parada cuando tocaba el CRON | Cuando arranca, `IsPastDue = true` y se dispara | Manejar `IsPastDue` o asumir que la siguiente ejecución cubre el hueco |
| Cambias el código y redeployas justo en el momento del trigger | El nuevo trigger puede ejecutarse dos veces (vieja + nueva instancia) | Idempotencia |

La regla mental: **no asumas exactly-once en Timer**. Asume **at-least-once**. Si tu lógica no tolera dos ejecuciones, hazla idempotente con un check + dictionary atómico (sección 5).

---

## 4. El modelo mental: el reloj con alarmas tardías

Imagina un despertador antiguo de cuerda. Lo programas a las 6:00 y suena cada día a esa hora. Pero el despertador tiene un detalle: si te lo olvidaste cargar la noche anterior y se quedó parado de las 02:00 a las 08:00, cuando lo vuelves a poner en marcha **suena inmediatamente** porque "tocaba a las 6:00 y ahora son las 8:00, tienes que hacer el desayuno ya". Eso es **past-due**: el despertador no descarta la alarma perdida, la dispara cuando puede, con un flag que dice "esto sonó tarde".

Functions Timer es ese despertador. La diferencia con un cron de Unix tradicional: cuando el host estaba parado y se reinicia, las ejecuciones perdidas **pueden dispararse con `IsPastDue = true`**. Tu código tiene que decidir qué hacer: ¿procesar igualmente (el informe del día anterior sigue siendo útil)?, ¿ignorar y esperar a la siguiente ejecución (la limpieza puede esperar)?, ¿alertar a alguien?

```
06:00 UTC del 2026-05-20 — host parado por mantenimiento
                          ↓ (la alarma se pierde)
08:00 UTC — host arranca otra vez
            ↓ (timer dispara con IsPastDue = true)
            ↓ ScheduleStatus.Last = ayer 06:00
            ↓ ScheduleStatus.Next = mañana 06:00
            ↓
            Tu código: ¿lo procesas o lo ignoras?
```

Tres frases para fijar el modelo:

- **Los timers se basan en CRON de seis campos** (NCRONTAB), no cinco como el cron de Unix. La diferencia: el primer campo son segundos. `0 0 6 * * *` = a los 0 segundos, del minuto 0, de las 6 — diariamente a las 06:00.
- **El husode horarios por defecto es UTC**. En la mayoría de planes Consumption no se puede cambiar. Para horas locales fijas, programa en UTC y haz la conversión mental cuando escribas la expresión.
- **El estado del scheduler vive en el Storage Account asociado**. Si lo borras o lo cambias, los timers pierden su histórico (`Last`, `Next`) y pueden disparar en momentos inesperados al volver a arrancar.

---

## 5. El patrón de idempotencia: `TryAdd`, no `GetOrAdd`

Aquí está la pieza didácticamente más valiosa del submódulo. Mira [`InMemoryInformeService.cs`](src/AzureFunctions.Demo/Services/InMemoryInformeService.cs):

```csharp
public (bool yaExistia, Informe informe) GenerarSiNoExiste(DateOnly fecha)
{
    var id = $"informe-{fecha:yyyy-MM-dd}";

    // Fast path: ya existe
    if (_store.TryGetValue(id, out var existente))
        return (yaExistia: true, informe: existente);

    // Construir el informe nuevo
    var stats = productos.GetStats();
    var nuevo = new Informe(id, fecha, stats.Total, stats.SinStock,
                            stats.ValorTotalStock, DateTimeOffset.UtcNow);

    // TryAdd: atómico. Solo un caller "gana" si hay contención.
    if (_store.TryAdd(id, nuevo))
        return (yaExistia: false, informe: nuevo);

    // Otro hilo nos ganó la carrera — devolvemos el existente.
    return (yaExistia: true, informe: _store[id]);
}
```

La pregunta importante: **¿por qué `TryAdd` y no `GetOrAdd`?**.

`GetOrAdd(key, factory)` parece más elegante: "dame el valor, si no existe llámame al factory para crearlo". Pero `ConcurrentDictionary.GetOrAdd` con factory tiene una garantía sutil: el factory **puede llamarse varias veces** bajo contención. Solo uno de los valores devueltos "gana" y se guarda, pero los otros factories también se ejecutan. Si tu factory tiene efectos secundarios (escribir en BD, llamar a una API), esos efectos se producen varias veces aunque solo uno gane el slot.

`TryAdd(key, value)` evita ese problema: construyes el valor primero (una vez), después intentas insertarlo. Si otro hilo lo metió antes, `TryAdd` devuelve `false` y el valor construido se descarta — pero **el factory no se ejecutó dos veces**. La diferencia con `GetOrAdd` es sutil pero importante cuando la construcción tiene efectos.

> 🧠 **La lección rara del HANDOFF.** Esta práctica está en el `HANDOFF.md` del repo como "Lección 4" — una de las trampas críticas del proyecto. La razón es que el patrón `GetOrAdd` aparece en muchos tutoriales como "lo idiomático", y solo cuando se mira fino se ve que rompe la idempotencia. En tus proyectos, si necesitas idempotencia exacta sobre un dictionary concurrente, **`TryAdd` es la operación correcta**. Resérvate `GetOrAdd` para casos donde el factory es puro (sin efectos secundarios).

Y el segundo detalle del ejemplo: el servicio se registra como **Singleton**, no Scoped ni Transient. Razón: el estado en memoria (`_store`) debe persistir entre ejecuciones del timer mientras la instancia de Functions esté caliente. Con Transient, cada ejecución crearía un servicio nuevo con un store vacío — la idempotencia se perdería.

---

## 6. NCRONTAB en seis campos

CRON de Unix tiene cinco campos (`minute hour day-of-month month day-of-week`). NCRONTAB de Functions tiene **seis** (`second minute hour day-of-month month day-of-week`). El primer campo extra son segundos. Es una sorpresa habitual que conviene memorizar:

| Expresión | Significado |
| --- | --- |
| `0 */5 * * * *` | Cada 5 minutos (segundo 0, minuto múltiplo de 5) |
| `0 0 * * * *` | Al inicio de cada hora |
| `0 0 6 * * *` | Diariamente a las 06:00 UTC |
| `0 0 9 * * MON-FRI` | Lunes a viernes a las 09:00 UTC |
| `0 30 9 1 * *` | El día 1 de cada mes a las 09:30 UTC |
| `*/30 * * * * *` | Cada 30 segundos (segundo múltiplo de 30) |

Y un detalle del ejemplo: `CleanupCadaMinuto` usa `[TimerTrigger("%CleanupCron%")]` — el `%...%` es la sintaxis de Functions para leer **el CRON desde una App Setting**. Si tienes la setting `CleanupCron = 0 */5 * * * *`, el timer se dispara cada 5 minutos. Cambias la setting a `0 */15 * * * *` y se dispara cada 15. **Sin redeploy**. Es el equivalente al patrón "App Settings sin redesplegar" que aprendiste en M02, aplicado a la frecuencia del scheduler.

> 🧠 **Probar expresiones CRON.** Antes de poner una expresión en producción, pruébala en <https://crontab.cronhub.io/> o equivalente. Es fácil escribir `0 0 0 * * *` pensando "diaria a las 00:00" y descubrir que ejecuta cada segundo durante el minuto 0 (de hecho `0 0 0 * * *` sí es diaria a las 00:00 porque hay un único segundo 0 dentro del minuto 0 de la hora 0; pero el día que querías "cada minuto" y escribes `* 0 * * * *`, ejecutas cada segundo del minuto 0 — bug clásico). Probar evita esa categoría de errores.

---

## 7. `IsPastDue` y cuándo importa

`TimerInfo.IsPastDue` se pone a `true` cuando el runtime detecta que se perdieron ejecuciones — porque la Function App estaba parada, porque el deploy tardó más de lo previsto, porque hubo un problema en el host. La pregunta de diseño: ¿qué hace tu código cuando se entera de esto?

Tres opciones:

- **Ignorarlo** y procesar normalmente. Apropiado si tu lógica es idempotente y "perder" información no es un problema (un cleanup que ya hizo otro ciclo no pasa nada por repetir).
- **Loggear y procesar**. Apropiado para informes y procesos donde la documentación del retraso es útil. El `CleanupCadaMinuto` del ejemplo hace esto: `logger.LogWarning("CleanupCadaMinuto past-due...")` y sigue.
- **Saltar la ejecución actual**. Apropiado cuando "demasiado tarde es como nunca" — por ejemplo, alertas que ya no tienen sentido enviar tres horas después. En ese caso: `if (timer.IsPastDue) return;`.

La lógica que importa entender: `IsPastDue = true` **no impide** que se dispare el timer. La ejecución se dispara igual; el flag es informativo. Tú decides si lo aprovechas para alertar, para variar el comportamiento o para ignorar.

---

## 8. Recorrido guiado

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | Local: `func start` con `CleanupCron = 0 */1 * * * *` | logs cada minuto de "CleanupCadaMinuto stats Total=3 ..." | Timer rápido funcionando — útil para observar en clase. |
| 2 | Espera al minuto 6 de la hora siguiente | log "InformeDiario {id} generado: ..." | El informe del día anterior se generó (CRON `0 0 6 * * *`). |
| 3 | `GET /api/informes` (con function key) | JSON con el informe del paso 2 | El endpoint HTTP para verificar el resultado del timer sin pinchar logs. |
| 4 | Para `func start` y vuelve a arrancarlo | el primer trigger de timer reporta `IsPastDue: true` en el log | Pasado el tiempo: el flag aparece (sección 7). |
| 5 | Cambia `CleanupCron` a `0 */5 * * * *` en `local.settings.json`, reinicia | nuevo log cada 5 minutos | CRON desde App Setting — el patrón "frecuencia configurable" (sección 6). |
| 6 | Lanza el test `InformeDiario_Is_Idempotent_Across_Multiple_Runs` | el test ejecuta el timer 5 veces seguidas, después verifica `Listar()` tiene **1 solo informe** | La idempotencia con `TryAdd` en acción (sección 5). |

Un experimento didáctico: añade `Console.WriteLine` dentro del bloque que construye el `Informe` (entre el `TryGetValue` y el `TryAdd`). Lanza el test del paso 6. Verás que el factory se ejecuta **una sola vez** aunque el timer se dispare cinco veces. Compara con la versión `GetOrAdd` (cambia temporalmente el método): el factory se ejecuta varias veces en los ciclos paralelos del test. Ese contraste es la lección del `TryAdd` vs `GetOrAdd`.

---

## 9. Tests del proyecto

Los tests interesantes están en `TimerFunctionsTests.cs`:

- **`CleanupCadaMinuto_Runs_With_Default_TimerInfo`** — el happy path. Confirma que el timer no lanza excepción con un `TimerInfo()` por defecto.
- **`CleanupCadaMinuto_Handles_PastDue_TimerInfo`** — fabrica un `TimerInfo` con `IsPastDue = true` y `ScheduleStatus` simulado, comprueba que el código lo maneja sin caerse.
- **`InformeDiario_Generates_Informe_For_Yesterday`** — verifica que la ejecución genera un informe con la fecha del día anterior.
- **`InformeDiario_Is_Idempotent_Across_Multiple_Runs`** — ejecuta el timer **5 veces seguidas** y confirma que `informes.Listar()` devuelve un solo informe. Es el test de la sección 5 hecho explícito.

Tests por instanciación directa, sin `WebApplicationFactory`, sin runtime de Functions. Pasan `new TimerInfo()` o `new TimerInfo { IsPastDue = true, ... }` directamente. Es el mismo patrón que ya vimos en S3.1/S3.2, ahora con `TimerInfo` en lugar de `HttpRequest`.

---

## 10. Puesta en marcha, ejecución y pruebas

### 10.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y testear | Sí |
| Azure Functions Core Tools (`func`) | `func start` local | Recomendado |
| Azurite | emular Storage local (necesario para el lock del Timer) | Sí |
| Probador CRON (web) | verificar expresiones complejas | Recomendado |

### 10.2 Compilar y arrancar en local

```bash
cd examples/M03-Azure-Functions-I/S3.3-trigger-timer
dotnet build AzureFunctions.Demo.slnx                  # 0 errores

cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json
# Edita CleanupCron a una frecuencia útil para demo: "0 */1 * * * *" (cada minuto)

azurite --silent           # en otra terminal
cd src/AzureFunctions.Demo
func start
```

Verás los logs del timer aparecer cada minuto. Para el `InformeDiario`, modifica temporalmente la expresión `0 0 6 * * *` a algo como `0 */2 * * * *` (cada 2 minutos) si quieres observarlo en clase sin esperar al día siguiente.

### 10.3 Pasar los tests

```bash
dotnet test
```

Incluye los heredados de S3.2 (CRUD productos) + los nuevos de `TimerFunctionsTests.cs`. El test de idempotencia (`InformeDiario_Is_Idempotent_Across_Multiple_Runs`) es el más valioso pedagógicamente.

### 10.4 Desplegar a Azure (resumen)

Mismo patrón que S3.1/S3.2: RG + Storage + Function App Consumption Linux .NET 10 isolated. Además:

- **App Setting** `CleanupCron = 0 */5 * * * *` (o la frecuencia que decidas).
- Tras el deploy, los timers empiezan a dispararse según su CRON sin más configuración.
- Para verificar: *Portal → tu Function App → Functions → CleanupCadaMinuto → Monitor* muestra cada ejecución con su `IsPastDue`, duración y resultado.

### 10.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| El timer no se dispara nunca | el host está parado, o el Storage perdió el lock | revisa que la Function App tiene `Always On` (Premium) o que el Storage está correctamente asociado |
| `IsPastDue` aparece todo el tiempo | la app está reinitándose con frecuencia | revisa Application Insights por excepciones en arranque |
| El informe aparece dos veces el mismo día | la idempotencia no se está aplicando o el dictionary es nuevo cada ejecución | confirma que el servicio está registrado como `AddSingleton`, no `AddScoped` |
| Cambias `CleanupCron` y el timer sigue con la frecuencia vieja | la setting no se aplicó al runtime — reinicia la Function App | `az functionapp restart` o desde portal |
| Logs muestran `Cron Parse Error` | NCRONTAB de 6 campos, no 5 | añade el campo de segundos al inicio |

### 10.6 Limpieza

`Portal → Resource groups → rg-curso-m03-s33 → Delete`.

---

## 11. Ideas para llevarte

Lo más importante de S3.3 es **el reflejo de la idempotencia**. Cualquier tarea programada que escribas en tu vida profesional debería poder ejecutarse dos veces sin daño. La forma estándar — chequear si la operación ya se hizo y, si no, hacerla atómicamente con un dictionary o una transacción BD — cabe en cinco líneas de código. La alternativa (rezar) cabe en menos líneas pero falla el primer día que el sistema se sale del happy path.

Sobre `TryAdd` vs `GetOrAdd`: el primero es el patrón correcto cuando la construcción del valor tiene efectos secundarios. El segundo está bien para factories puros sin side effects. Si dudas, **`TryAdd` es más seguro** — solo "fallas" en el sentido de descartar el valor construido, no en el de ejecutar efectos dos veces.

Sobre NCRONTAB: ponlo en App Settings cuando puedas (`[TimerTrigger("%MyCron%")]`). El día que tengas que cambiar la frecuencia "solo en producción para hacer una prueba", la diferencia entre cambiar una App Setting (30 segundos) y redeplegar la app (15 minutos + posible bug en otra cosa) es importante.

Sobre los **tests del Timer**: aunque son tests "raros" (un timer probado fuera del runtime no es 100% realista), tienen valor — verifican la lógica del handler, la idempotencia, el comportamiento ante `IsPastDue`. Lo que no verifican es que el CRON se interprete bien por el runtime (eso lo prueba el deploy a Azure).

---

## 12. Comprueba que lo has entendido

1. Una tarea nocturna genera un email a las 03:00. La VM se cae a las 02:30 y se reinicia a las 06:00. ¿Qué pasa con la ejecución de las 03:00? ¿Cómo lo manejaría Functions Timer? *(sección 4, 7)*
2. ¿Por qué `InMemoryInformeService` se registra como `Singleton` y no como `Scoped`? *(sección 5)*
3. Tu código usa `_store.GetOrAdd(id, k => GenerarInforme(k))` en un timer. El test de idempotencia dispara el timer cinco veces y verifica que `GenerarInforme` se llama una sola vez. ¿Pasa el test? ¿Por qué? *(sección 5)*
4. Quieres que un timer se ejecute cada 30 minutos. ¿Qué expresión NCRONTAB usas y por qué NO es `*/30 * * * *`? *(sección 6)*
5. Configuras `CleanupCron = 0 */1 * * * *` en App Settings, redeployas la app. ¿Tienes que reiniciar la Function App para que se aplique? *(sección 6 + sección 10.5)*
6. Un timer se dispara con `IsPastDue = true`. Tu informe es "el resumen de ventas del día anterior". ¿Lo procesas igualmente, lo ignoras o lo procesas y alertas? *(sección 7)*

<details>
<summary>Respuestas</summary>

1. **La ejecución de las 03:00 se pierde** mientras la VM está caída. Cuando arranca a las 06:00, el cron de Unix tradicional **no la recupera** — espera la siguiente ejecución según el calendario. **Functions Timer es distinto**: cuando el host arranca tras una parada, el runtime detecta que se perdieron ejecuciones (basándose en `ScheduleStatus.Last` guardado en Storage) y dispara la función con `IsPastDue = true`. Tu código decide qué hacer (procesar el informe atrasado, ignorarlo, alertar). Es comportamiento configurable; el runtime te da la información, tú decides la política.
2. Porque el estado en memoria (`_store`, el `ConcurrentDictionary`) debe **persistir entre ejecuciones del timer mientras la instancia esté caliente**. Con `AddScoped` (vida de request), cada ejecución crearía un servicio nuevo con un dictionary vacío — la idempotencia se perdería porque no recordaría que ya generó el informe en la ejecución anterior. Con `AddSingleton`, el servicio se construye una vez al arrancar la Function App y vive hasta que la instancia se libera (cold start). Es exactamente la garantía que necesitas para que el dictionary recuerde lo procesado.
3. **El test puede fallar**. `GetOrAdd(key, factory)` con factory tiene una garantía sutil: el factory **puede llamarse varias veces bajo contención**. Solo uno de los valores devueltos "gana" y se guarda en el diccionario, pero los otros factories también se ejecutan. Si `GenerarInforme` se cuenta para el assert (con un contador o spy), verá que se llamó dos o tres veces, no una. Por eso el ejemplo usa `TryAdd`: el valor se construye primero (una vez), y si otro hilo nos ganó la carrera el valor construido se descarta — pero el factory no se ejecutó dos veces. La regla mental: si el factory tiene efectos secundarios (DB, API, file), **`TryAdd` es el patrón correcto**.
4. Usas `0 */30 * * * *`. **NO** es `*/30 * * * *` porque NCRONTAB tiene **seis campos** (segundo + minuto + hora + día-mes + mes + día-semana), no cinco. La expresión `*/30 * * * *` con cinco campos en NCRONTAB se interpreta mal y suele dar un error de parsing. La regla: añade el campo de segundos al inicio cuando vengas de cron de Unix. Pruébalo en un validador NCRONTAB antes de desplegar.
5. **No necesitas reiniciar manualmente**, pero **hay un detalle**: cambiar una App Setting **reinicia la Function App automáticamente** (~30 segundos). En cuanto el reinicio termine, el nuevo CRON se aplica. Si quieres acelerar: en el portal puedes hacer Restart manual, o `az functionapp restart`. La diferencia con el patrón de App Settings de M02 es que aquí el cambio sí dispara reinicio (porque afecta a la inicialización de los triggers); en M02 dependía del setting concreto.
6. **Probablemente lo procesas igualmente y loggeas** que estaba past-due. Un resumen del día anterior **sigue siendo útil** aunque llegue dos horas tarde — los stakeholders prefieren recibirlo a las 09:00 con un asterisco "informe retrasado" que no recibirlo. Si fuera una alerta de "el sistema se ha caído hace 5 minutos" sí ignorarías el past-due de tres horas (ya no es relevante). La decisión depende del contexto del proceso: informes y limpiezas suelen procesar siempre; alertas y notificaciones efímeras suelen ignorar past-due.

</details>

---

## 13. Hasta aquí

Vuelve a la imagen del despertador con alarmas tardías de la sección 4. Lo importante no es la frecuencia del CRON; es que tu código **sepa qué hacer cuando le toque ejecutarse dos veces**. La idempotencia con `TryAdd` cabe en cinco líneas y te ahorra el día que un retry, un past-due o un scale-out disparan tu tarea por partida doble.

Lo siguiente es [`S3.4 — Trigger Blob Storage`](../S3.4-trigger-blob-storage/MANUAL.md), donde el atributo `[TimerTrigger]` se sustituye por `[BlobTrigger]` y la función pasa de "ejecutar cada X" a "ejecutar cuando aparezca un archivo en este container". El patrón sigue siendo el mismo (mismo Program.cs, mismos tests, misma DI). Lo nuevo será el poison queue, los reintentos automáticos y el patrón de procesado idempotente aplicado a archivos.
