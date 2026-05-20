# Manual del alumno — S3.1 · Principios del cómputo sin servidor

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: estructura, mapeo a slides, comandos, despliegue por Portal. Este manual va antes: te cuenta por qué este es el primer ejemplo del módulo, qué cambio mental hay que hacer respecto a M02 y cómo leer el "skeleton canónico" que vas a reutilizar en S3.2–S3.6.

Tiempo de lectura: ~20 min. Submódulo de teoría: [M03-S3.1](../../../doc/M03-Azure-Functions-I/v4-actual/M03-S3.1-principios-computo-sin-servidor-v4.md). Primer ejemplo de Azure Functions — un `Hello` minimal que sirve para verificar que la Function App arranca y para fijar el patrón que se repite los demás submódulos.

*Creado: 2026-05-20 12:02 +0200*

---

## 1. La idea en una frase

En M02 desplegabas una app que **siempre estaba encendida**: el App Service Plan se factura por las horas que el plan existe, independientemente del tráfico. En M03 cambias a un modelo distinto: **pagas solo por las ejecuciones**. La Function App vive en plan Consumption, se "duerme" cuando no hay tráfico, y cuando llega un evento (una petición HTTP, un blob nuevo, un mensaje en cola) se despierta, ejecuta tu código, y se vuelve a apagar. El primer millón de ejecuciones al mes es gratis.

Para muchos workloads — APIs internas con tráfico esporádico, procesos batch, integraciones — la diferencia es entre pagar diez euros al mes y pagar cero. Y para tu código, el cambio es modesto: las funciones se escriben distinto al controlador de un ASP.NET (cada una declara su trigger), pero la DI, los `ILogger`, los Options con validación y los patrones de configuración que aprendiste en M02 siguen valiendo igual.

Esta práctica te enseña el **skeleton mínimo** sobre el que se construye todo M03. Un solo HTTP trigger, tres tests, despliegue por Portal en cinco minutos. Lo justo para fijar el patrón.

---

## 2. El problema real que hay detrás

Una pequeña tienda online tenía un proceso nocturno que mandaba un email a los clientes con su factura PDF del día. Vivía en una VM Linux dedicada con cron — la VM costaba 50 €/mes y se usaba durante los 90 segundos que tardaba el job. El equipo migró ese proceso a una Azure Function con Timer trigger. El cron se traduce literalmente al atributo del trigger; la VM se apaga. Resultado: el proceso sigue corriendo cada noche a las 3 AM, pero el coste es **inferior a un céntimo al mes** porque las 30 ejecuciones mensuales caben de sobra en la cuota gratuita del primer millón.

Esa historia es M03 en pequeño. La pregunta a hacerse en cualquier servicio Azure: **¿este código se ejecuta todo el rato o solo ante eventos esporádicos?**. Si es lo segundo, Consumption es muchas veces la respuesta correcta — más barato, más simple operativamente, sin VM que mantener.

Lo que entrega esta práctica:

| Pieza | Para qué | Dónde la verás |
| --- | --- | --- |
| **`FunctionsApplication.CreateBuilder`** | Bootstrap moderno del Worker SDK 2.x | [`Program.cs`](src/AzureFunctions.Demo/Program.cs) |
| **`[Function]` + `[HttpTrigger]`** | Anatomía de una función: nombre + trigger + binding | [`HelloFunction.cs`](src/AzureFunctions.Demo/Functions/HelloFunction.cs) |
| **`host.json`** | Configuración del host (timeout, routePrefix, etc.) | [`host.json`](src/AzureFunctions.Demo/host.json) |
| **`local.settings.json` ignorado por git** | Equivalente local de App Settings, con secretos fuera del repo | `.gitignore` + `local.settings.json.example` |
| **Plan Consumption + Storage Account** | Hosting serverless + storage para metadatos del runtime | `scripts/01-provision.sh` |
| **Tests por instanciación directa** | Patrón de tests sin `WebApplicationFactory` — rápido y sin runtime de Functions | `AzureFunctions.Demo.Tests/` |

Los seis ladrillos se repiten en S3.2 (HTTP avanzado), S3.3 (Timer), S3.4 (Blob), S3.5 (Cosmos Change Feed) y S3.6 (Bindings). Lo único que cambia es el atributo del trigger; el `Program.cs`, los tests y el `host.json` son prácticamente idénticos.

---

## 3. Por qué esto importa en tu stack

En M02 tenías una herramienta: App Service. En M03 añades otra: Functions. Las dos sirven para alojar código .NET en Azure; las dos cobran por consumo (App Service por horas de plan, Functions por ejecuciones); las dos integran con el resto de Azure. Lo que cambia es **el modelo de ejecución** y, derivado de eso, el patrón de coste y el patrón de código.

La diferencia respecto a M01/M02 que conviene fijar antes de empezar:

| | App Service (M01/M02) | Functions Consumption (M03) |
| --- | --- | --- |
| **Modelo** | "Una app web que recibe peticiones" | "Funciones que se ejecutan ante eventos" |
| **SDK** | `Microsoft.NET.Sdk.Web` | `Microsoft.NET.Sdk` con `<OutputType>Exe</OutputType>` |
| **Bootstrap** | `WebApplication.CreateBuilder` | `FunctionsApplication.CreateBuilder` |
| **Hosting** | Plan (siempre encendido) | Plan Consumption (scale-to-zero) |
| **Coste base** | ~10-70 €/mes según tier | 0 € hasta 1M ejecuciones/mes |
| **Tests** | `WebApplicationFactory<Program>` | Instanciar la función directamente |
| **Cold start** | Evitable con Always On (≥ B1) | 1-3 s en .NET isolated (Slide 8) |

El cold start de Functions Consumption es el punto donde más se nota la diferencia. Si tu app tiene tráfico continuo, App Service le va mejor. Si tiene tráfico esporádico, Functions ahorra coste — al precio de pagar 1-3 segundos en la primera petición tras una pausa.

---

## 4. El modelo mental: el taxi y el coche propio

Imagina dos formas de moverte por la ciudad.

La primera es tener **coche propio**. Lo aparcas en el garaje y está disponible siempre, listo para usar en cualquier momento. Pagas el seguro, la ITV, los impuestos y la plaza de garaje todos los meses, lo uses o no. Si haces un viaje de cinco minutos, pagas el mismo "base mensual" que si haces viajes todo el día. La ventaja: el coche está calentito y disponible; arrancas, conduces, llegas. La desventaja: pagas por la disponibilidad, no por el uso.

La segunda es usar **taxi**. Llamas cuando lo necesitas. Llega en uno o dos minutos. Pagas el viaje y se va. Si no haces ningún viaje en un mes, no pagas nada. Si haces muchos viajes, sumas. La ventaja: pagas por uso real, no por disponibilidad. La desventaja: hay un mínimo de espera (uno o dos minutos) entre llamada y coche listo — el cold start del taxi.

App Service es el coche propio. Functions Consumption es el taxi. Las dos te llevan donde quieras; la diferencia es **el modelo de facturación** y **el modelo de espera**.

```
App Service B1 (el coche en el garaje)
   ├── Plan factura siempre, lo uses o no
   ├── ~10 €/mes base, Always On
   ├── Cold start: cero
   └── Bueno para tráfico continuo

Functions Consumption (el taxi)
   ├── Factura por ejecución (primer millón gratis al mes)
   ├── 0 € si no se ejecuta
   ├── Cold start: 1-3 s tras pausa larga
   └── Bueno para tráfico esporádico o batch
```

Tres frases para fijar el modelo:

- **Functions Consumption se duerme cuando no se usa.** Tras ~20 min sin tráfico, el host se libera. La siguiente petición paga el coste de arrancar — el cold start (1-3 s en .NET isolated).
- **Functions necesita un Storage Account siempre.** No es una decisión de diseño tuya: es un requisito del runtime para guardar metadatos (locks, leases, function key). Un Storage LRS estándar (~0.02 €/mes) cubre todo y se factura aparte.
- **El precio del taxi es por uso, pero hay umbrales.** 1 millón de ejecuciones/mes son gratis (Always Free de Azure). Por encima, ~0.20 € por millón. Para la mayoría de funciones internas, nunca pasarás del primer millón. Para APIs de cara al usuario con tráfico real, calcula antes.

Vuelve a la imagen del taxi cada vez que dudes "¿esto Functions o App Service?". Si es tráfico continuo, coche propio. Si es esporádico, taxi.

---

## 5. El skeleton: cuatro archivos que se repiten todo el módulo

Mira [`Program.cs`](src/AzureFunctions.Demo/Program.cs):

```csharp
var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
```

Cuatro líneas operativas. **`FunctionsApplication.CreateBuilder`** es la API moderna del Worker SDK 2.x — sustituye al `HostBuilder` clásico que aparece en muchos tutoriales viejos. **`ConfigureFunctionsWebApplication()`** activa el integration con ASP.NET Core para que `[HttpTrigger]` reciba `HttpRequest` y devuelva `IActionResult`, en lugar del legacy `HttpRequestData`/`HttpResponseData`. **`AddApplicationInsightsTelemetryWorkerService`** + **`ConfigureFunctionsApplicationInsights`** habilita la telemetría a App Insights (cuando configures la connection string como App Setting).

Y `HelloFunction.cs`:

```csharp
[Function(nameof(Hello))]
public IActionResult Hello(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "hello")] HttpRequest req)
{
    var name = req.Query["name"].ToString();
    if (string.IsNullOrWhiteSpace(name)) name = "Azure";
    return new OkObjectResult(new { /* ... */ });
}
```

Tres atributos componen la anatomía de toda función:

- **`[Function(nameof(Hello))]`** registra la función con ese nombre en el host. Cuando hagas peticiones, el runtime busca esa función para invocarla.
- **`[HttpTrigger(...)]`** define el **evento** que la dispara. Es lo único que cambia entre los submódulos: en S3.3 será `[TimerTrigger]`, en S3.4 `[BlobTrigger]`, en S3.5 `[CosmosDBTrigger]`. El resto del patrón se mantiene.
- **`AuthorizationLevel.Anonymous`** significa "cualquiera puede llamarla, sin function key". En producción real cambias a `Function` (requiere `?code=<function-key>` en la URL) o pones autenticación delante. Lo veremos a fondo en S3.2.

Y la pareja `host.json` + `local.settings.json`:

- **`host.json`** vive **dentro del repo**. Configura el host de Functions (timeout, routePrefix, logging). Aquí pone `functionTimeout: 00:10:00` (el máximo de Consumption) y `routePrefix: api` por consistencia con los endpoints de M02.
- **`local.settings.json`** está **en `.gitignore`** porque puede contener secretos. Lo creas a partir de `local.settings.json.example`. Cuando despliegues a Azure, su contenido lo sustituyen las App Settings del portal.

> 🧠 **`OutputType=Exe` en el `.csproj` es obligatorio.** Las Function Apps en isolated worker son procesos ejecutables (no DLLs cargadas por el host). El runtime de Functions lanza tu `.exe` y se comunica con él por named pipes. Si te dejas el `<OutputType>Exe</OutputType>`, el deploy "funciona" pero la app no arranca con un error confuso. Lo verás en cada `.csproj` de M03.

---

## 6. Tests sin `WebApplicationFactory`: instanciar la función directamente

En M02 los tests usaban `WebApplicationFactory<Program>` para arrancar el host real en memoria. Aquí no funciona — el runtime de Azure Functions no tiene un equivalente directo. El patrón es distinto y, una vez visto, más simple:

```csharp
// Tests/HelloFunctionTests.cs (simplificado)
var function = new HelloFunction();    // instancia directa, sin host
var context = new DefaultHttpContext();
context.Request.QueryString = new QueryString("?name=Pedro");

var result = function.Hello(context.Request);

var ok = Assert.IsType<OkObjectResult>(result);
// ... aserciones sobre ok.Value
```

Cuatro líneas operativas. Crear la función, fabricar un `HttpRequest` con `DefaultHttpContext`, invocar el método, comprobar el `IActionResult`. Sin runtime de Functions, sin emulador, sin contenedor.

> 🎓 **La lección DI silenciosa.** Esta forma de testear es rápida y suficiente para la lógica del endpoint, pero **no ejercita el contenedor de DI**. Si tu `Program.cs` tiene un servicio sin registrar que la función necesita, el test pasa porque instancias con `new HelloFunction()`, pero la Function App real revienta en runtime con "Unable to resolve service". Por eso en M03 hay que **cruzar a mano los constructores de cada función contra los `AddSingleton`/`AddScoped`/`AddTransient` de `Program.cs`** después de escribirla. Es uno de los avisos del HANDOFF del repo: el bug aparece tarde y es confuso. Más adelante, en M04-S4.5, verás cómo cubrir esta laguna explícitamente.

Y un detalle más: los tests instancian `HelloFunction` con `new` directamente porque aquí la función no tiene dependencias. En cuanto añadas un servicio inyectado (un `ILogger`, un cliente Cosmos, un repositorio), tendrás que pasárselo en el `new` del test. Es la limitación del patrón: rápido para casos simples, manual para casos complejos.

---

## 7. Recorrido guiado

Lanza la Function App en local primero (sección 9) y prueba el endpoint. La parte interesante está en Azure pero el ciclo local es donde se ve el patrón.

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `cp local.settings.json.example local.settings.json` | el archivo existe en `src/AzureFunctions.Demo/` | El equivalente local de App Settings. Va en `.gitignore` por si contiene secretos. |
| 2 | `azurite --silent` en otra terminal | Azurite responde en `:10000-10002` | El emulador de Storage. Functions necesita storage incluso en local — `AzureWebJobsStorage = UseDevelopmentStorage=true` lo conecta. |
| 3 | `func start` en `src/AzureFunctions.Demo` | output con `Hello: [GET] http://localhost:7071/api/hello` | La Function App arrancando. El puerto 7071 es estándar de Functions. |
| 4 | `curl http://localhost:7071/api/hello?name=Pedro` | JSON con `name: "Pedro"`, runtime, os, machineName | El primer endpoint en Functions. |
| 5 | `curl http://localhost:7071/api/hello` (sin name) | `name: "Azure"` por defecto | El fallback documentado. |
| 6 | Azure: deploy + `curl https://<func>.azurewebsites.net/api/hello` | la primera tarda **2-3 segundos**, las siguientes < 100 ms | **Cold start** en directo (sección 4). |

Un experimento que aporta más que la teoría: tras desplegar, llama al endpoint, espera 20 minutos sin hacer nada, vuelve a llamar. La segunda llamada vuelve a tardar 2-3 segundos — la Function App se durmió y arrancó otra vez. Si lo llamas continuamente (cada minuto), la app se mantiene caliente y siempre responde rápido. Ese comportamiento es lo que diferencia el plan Consumption del de App Service.

Y el `time curl 'https://<func>.azurewebsites.net/api/hello'` con `time` por delante es la forma más sencilla de medir el cold start cuando lo escenifiques en clase.

---

## 8. Cold start: lo que importa decidir

Tres números que conviene tener en la cabeza para Functions en .NET 10 isolated:

| Métrica | Valor típico |
| --- | --- |
| Cold start (primera invocación tras parada) | 1-3 segundos |
| Tiempo de vida sin tráfico antes del scale-to-zero | ~20 minutos |
| Pre-warmed instances (Premium plan) | configurable, evita el cold start |

Si tu workload **no tolera 1-3 segundos** en la primera petición — una API pública de cara al cliente, por ejemplo —, Consumption no es tu sitio. Las opciones son **Functions Premium plan** (con pre-warmed instances, sin scale-to-zero, pero con coste base ~150 €/mes), **Flex Consumption** (la opción moderna intermedia), o **mover ese trigger HTTP a App Service** (siempre caliente, B1 mínimo).

Si tu workload **sí tolera ese cold start ocasional** — la mayoría de funciones internas, integraciones, procesos batch —, Consumption es muchas veces lo más barato y simple. La cuota gratuita del primer millón al mes cubre apps pequeñas-medianas sin coste real.

> 🧠 **Decide el plan según el patrón de tráfico, no según el tipo de función.** Una función HTTP puede ir en Consumption (si tolera cold start) o en App Service (si no lo tolera). Un timer trigger nocturno casi siempre va en Consumption (las 30 ejecuciones mensuales caben en la cuota gratis). Un blob trigger procesando uploads continuos puede convenir en Premium si necesitas paralelismo alto sin throttling. La decisión es plan, no tipo de trigger.

---

## 9. Puesta en marcha, ejecución y pruebas

### 9.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y testear | Sí |
| Azure Functions Core Tools (`func`) | `func start` local | Recomendado |
| Azurite | emular Storage local (Functions lo necesita) | Sí (en local) |
| Azure CLI (`az`) | scripts de despliegue | Solo si usas scripts |
| Suscripción Azure | desplegar | Solo si vas a desplegar |

### 9.2 Compilar y arrancar en local

```bash
cd examples/M03-Azure-Functions-I/S3.1-principios-computo-sin-servidor

dotnet build AzureFunctions.Demo.slnx            # 0 errores

cp src/AzureFunctions.Demo/local.settings.json.example \
   src/AzureFunctions.Demo/local.settings.json

# En otra terminal:
azurite --silent

# Volver y arrancar:
cd src/AzureFunctions.Demo
func start
# → http://localhost:7071/api/hello
```

`curl http://localhost:7071/api/hello?name=Pedro` para probar.

### 9.3 Pasar los tests

```bash
dotnet test
```

Resultado: **3 pass · 0 fail**. Sin Azure, sin Docker, sin emulador. Los tests instancian la función directamente (sección 6).

### 9.4 Desplegar a Azure (resumen)

El detalle por Portal está en el [`README.md`](README.md). Pasos clave:

1. **RG + Storage Account** (LRS estándar, ~0.02 €/mes).
2. **Function App** con hosting plan *Consumption (Serverless)*, runtime stack *.NET 10 isolated*, Linux, usando el storage anterior.
3. **Deploy** desde VS Code → *Deploy to Function App*, o con `scripts/02-deploy.sh`.
4. **Verificar** con `curl https://<func>.azurewebsites.net/api/hello?name=Pedro`.

Si tu región no tiene .NET 10 disponible aún para Functions, baja a .NET 8 isolated. El código es idéntico.

### 9.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `func start` falla con "no functions found" | falta `AzureWebJobsStorage` en `local.settings.json` o Azurite no arrancó | comprueba el `local.settings.json` y que Azurite responde en :10000 |
| El primer `curl` tarda 5+ segundos | cold start del Consumption | normal; el segundo es rápido |
| `403 Forbidden` tras desplegar | el atributo cambió a `AuthorizationLevel.Function` | añade `?code=<function-key>` o vuelve a `Anonymous` |
| Deploy OK pero `/api/hello` da 404 | `host.json` no se copió al output | revisa que `<None Update="host.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` está en el `.csproj` |
| `01-provision.sh` falla con `--runtime-version 10` | la región no soporta .NET 10 isolated en Functions todavía | cambia a `--runtime-version 8` en el script |
| Build de tests OOM en CI | el analyzer del Worker SDK consume memoria | sube la RAM del runner o usa `--no-restore` tras el primer restore |

### 9.6 Limpieza

`Portal → Resource groups → rg-curso-m03-s31 → Delete`. Borra Storage Account + Function App + plan Consumption (que no genera costes en sí).

---

## 10. Ideas para llevarte

Lo más útil de esta práctica es **interiorizar el cambio mental**: pasar de "tengo una app web encendida" a "tengo funciones que se ejecutan ante eventos". La diferencia operacional es enorme — coste, despliegue, monitorización, escalado. Y la diferencia de código es menor: misma DI, mismos `ILogger`, mismos Options. Lo que cambia es **quién dispara la función** y **cómo se factura**.

Sobre la **decisión Consumption vs App Service**, mi recomendación honesta: para cualquier proceso esporádico (timer nocturno, procesamiento de blobs ocasional, integración por mensaje), Consumption casi siempre. Para una API pública con tráfico continuo y necesidad de latencia baja en la primera petición, App Service. Y si tienes que elegir un plan único para Functions con tráfico mixto, Flex Consumption es la opción moderna que pivota entre los dos modelos.

Sobre el **skeleton**: copiarás este `Program.cs` y este `host.json` a todos los ejemplos de M03. Aprovecha S3.1 para revisarlos despacio. Cuando llegues a S3.5 (Cosmos Change Feed) y veas el mismo `Program.cs`, el foco estará en el trigger nuevo, no en el bootstrap.

Y un consejo pragmático que se enseña en M03 explícitamente más adelante (S3.4): **los tests que instancian la función con `new` son rápidos pero no ejercitan el DI**. Cruza a mano los constructores de cada función contra el `Program.cs` después de añadir cualquier servicio. Es el bug que más frustración causa la primera vez (compila, pasa los tests, revienta en producción al primer deploy).

---

## 11. Comprueba que lo has entendido

1. ¿Para qué sirve un Storage Account asociado a una Function App? ¿Qué pasa si lo borras por error? *(sección 5)*
2. Tu app tarda 3 segundos en la primera petición tras unas horas sin tráfico, después responde en 50 ms. ¿Es un bug? *(sección 8)*
3. ¿Por qué `<OutputType>Exe</OutputType>` es obligatorio en el `.csproj` de Functions isolated worker? *(sección 5)*
4. Tienes un proceso nocturno que se ejecuta 30 veces al mes y dura 90 segundos cada vez. ¿Plan Consumption o App Service? ¿Por qué? *(sección 2, sección 8)*
5. Los tests de S3.1 instancian la función con `new HelloFunction()` directamente. ¿Qué cosa NO se prueba con ese patrón y dónde podría aparecer el bug? *(sección 6)*
6. En `local.settings.json` configuras `AzureWebJobsStorage=UseDevelopmentStorage=true`. ¿Qué herramienta tienes que tener corriendo y por qué? *(sección 9)*

<details>
<summary>Respuestas</summary>

1. El runtime de Functions usa el Storage Account para metadatos internos: locks de Timer triggers (para que en multi-instance solo uno dispare por intervalo), leases del Cosmos DB Change Feed (para distribuir el procesamiento), function key de seguridad, etc. **Si lo borras por error, la Function App se queda inutilizable** — los triggers no funcionan correctamente, los logs internos fallan. La asociación se hace al crear la Function App y se guarda en la App Setting `AzureWebJobsStorage`. Si tienes que cambiar de Storage, recreas la asociación; nunca borrar el actual sin preparar el nuevo.
2. **No es un bug**, es el **cold start del plan Consumption**. Tras ~20 minutos sin tráfico, el host se libera y la siguiente petición paga el coste de arrancar (1-3 segundos en .NET isolated). Es comportamiento esperado y deliberado: es lo que hace que Consumption sea barato. Si tu workload no tolera ese cold start, las opciones son Functions Premium (~150 €/mes base con pre-warmed instances), Flex Consumption (la opción intermedia moderna), o mover ese trigger a App Service.
3. Porque las Function Apps en isolated worker son **procesos ejecutables**, no DLLs cargadas por el host. El runtime de Functions lanza tu `.exe` y se comunica con él por named pipes. Sin `<OutputType>Exe</OutputType>`, el `Main` no se genera (queda como DLL), el deploy "funciona" sintácticamente pero la app no arranca con un error confuso. Es uno de los detalles que más confusión causa la primera vez y por eso aparece en el skeleton canónico de S3.1 — para que lo veas desde el día uno.
4. **Consumption**, sin dudarlo. 30 ejecuciones de 90 segundos al mes son 2700 segundos-función mensuales — caben de sobra en la cuota gratis del primer millón de ejecuciones. **Coste real: cero**. En App Service, ese mismo proceso necesitaría un plan B1 (~10 €/mes) que está encendido 24/7 sirviendo nada el 99.9% del tiempo. La diferencia anual son ~120 €/año por un proceso que en Consumption es gratis. Es el caso canónico donde Consumption gana por goleada.
5. **No se prueba el contenedor de DI**. El `new HelloFunction()` instancia la función directamente, saltándose el registro de servicios del `Program.cs`. Si tu función tiene un parámetro en el constructor (un `ILogger`, un `IRepository`, un cliente Cosmos) y ese servicio no está registrado correctamente en DI, el test pasa porque le pasas los mocks a mano, pero la Function App real revienta en runtime con "Unable to resolve service for type X" en cuanto se intenta crear la función. La forma de cubrir esta laguna es **cruzar manualmente los constructores contra el `Program.cs`** después de añadir cualquier servicio, o tener un test extra de DI que resuelva la función del host real (lo verás en M04-S4.5).
6. **Azurite** corriendo en `localhost:10000-10002`. `UseDevelopmentStorage=true` es el alias estándar que apunta a esos puertos. Functions necesita un Storage Account incluso en local (mismo motivo que en Azure: metadatos, locks, leases). Azurite lo emula sin tener que crear un Storage real. Sin Azurite corriendo, `func start` falla con "no functions found" o errores de conexión al storage. Las dos opciones para arrancar Azurite: `azurite --silent` (npm) o `docker run … mcr.microsoft.com/azure-storage/azurite` (Docker).

</details>

---

## 12. Hasta aquí

Vuelve a la imagen del taxi y el coche propio de la sección 4. Si tu código se ejecuta esporádicamente (eventos, batch, integraciones), Consumption es el taxi que paga por viaje. Si se ejecuta continuamente, App Service es el coche propio que paga la plaza de garaje. La decisión es por patrón de tráfico, no por tipo de código.

Lo siguiente es [`S3.2 — Trigger HTTP`](../S3.2-trigger-http/MANUAL.md), que reutiliza el skeleton que acabas de ver y profundiza en HTTP: rutas con parámetros, validación, function keys, niveles de autorización. Es la base para construir APIs serias sobre Functions. Después, S3.3 (Timer), S3.4 (Blob), S3.5 (Cosmos Change Feed) cambian el trigger sin cambiar el patrón. Cuando hayas hecho los cuatro, tendrás claro que un Function trigger es **un atributo + una clase + el mismo bootstrap** — el resto es ergonomía.
