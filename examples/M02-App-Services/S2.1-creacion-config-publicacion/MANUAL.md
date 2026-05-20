# Manual del alumno — S2.1 · Creación, configuración y publicación

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica del ejemplo: estructura, mapeo a slides, comandos exactos, despliegue por Portal. Este manual va antes: te cuenta por qué este es el segundo paso natural tras S1.P, qué decisiones nuevas aparecen y cómo leerlas.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M02-S2.1](../../../doc/M02-App-Services/v4-actual/M02-S2.1-creacion-configuracion-publicacion-v4.md). Es el primer ejemplo del módulo M02 donde se nota que App Service tiene más cosas que "subir una app" — empieza a aparecer el tier de pago, Always On, el health check de verdad, las settings tipadas, los logs hacia el portal.

*Creado: 2026-05-20 09:16 +0200*

---

## 1. La idea en una frase

En S1.P llegaste a la nube con F1 gratis y la satisfacción de ver tu nombre en una URL pública. Esta práctica te enseña qué cambia cuando el F1 deja de ser suficiente — porque va a dejar de serlo en cuanto la primera persona ajena lo use y se queje del cold start. Y aquí descubres que App Service, bien configurado, tiene una serie de toggles silenciosos que separan "esto va para producción" de "esto es una demo": Always On, Health Check, Options con validación al arrancar, logs hacia el portal y CORS controlado por configuración.

Lo que cambia no es el código —el `Program.cs` sigue siendo pequeño y plano—. Lo que cambia es **lo que App Service hace por ti** cuando le pides bien las cosas en *Configuration*. Esta práctica es ese cambio de actitud: dejar de ver el portal como "el sitio para crear la app" y empezar a verlo como una caja de herramientas operativa.

---

## 2. El problema real que hay detrás

Hace tiempo un equipo me enseñó orgulloso su primera demo en Azure. F1 gratis, un Hello World, una URL pública. La primera prueba del jefe de proyecto: abre la URL en una reunión y... cuarenta y cinco segundos en blanco antes de responder. Risas incómodas. Cold start del F1 después de una hora sin tráfico. El siguiente sprint subieron el plan a B1 (~10 €/mes), activaron **Always On**, y la latencia bajó a milisegundos siempre. Tres clics y unos euros, la diferencia entre demo embarazosa y demo profesional.

Esa anécdota cierra una idea importante: **App Service tiene una capa gratuita estupenda para aprender y una capa de pago barata para empezar a hacerlo en serio**. La capa gratuita tiene cold start, sin Always On, sin slots, sin custom domains, sin SLA serio. La capa B1 levanta esas restricciones y suele ser todo lo que necesitas durante los primeros meses de un proyecto pequeño. El salto B1 → S1 → P1V3 ya es otra conversación, ligada a tráfico y disponibilidad.

Esta práctica entrena las decisiones que conviene tomar **una vez** y no volver a tocar hasta que el negocio crezca:

| Decisión | Para qué | Dónde la verás |
| --- | --- | --- |
| **Always On** (Slide 13) | Mantener la instancia caliente, sin cold start | Portal → *Configuration → General settings* |
| **Health Check `/health`** (Slide 13) | App Service reinicia instancias que dejan de responder bien | [`HealthEndpoints.cs`](src/AppService.Demo.Api/Endpoints/HealthEndpoints.cs) + [`ConfigurableHealthCheck.cs`](src/AppService.Demo.Api/Services/ConfigurableHealthCheck.cs) |
| **Options pattern + validación al arrancar** (Slide 12) | Que la app no arranque si falta una setting obligatoria | [`AppOptions.cs`](src/AppService.Demo.Api/Configuration/AppOptions.cs) + `Program.cs` |
| **Logs a App Service** (Slide 26) | Ver tus `ILogger` desde *Log Stream* en el portal | `Logging.AddAzureWebAppDiagnostics()` |
| **CORS controlado por configuración** (Slide 27) | Cambiar la lista de orígenes sin redesplegar | `AppOptions:AllowedOrigins` |
| **HttpClient typed client** (Slide 31) | Evitar SNAT exhaustion al llamar APIs externas | [`ExternalApiClient.cs`](src/AppService.Demo.Api/Services/ExternalApiClient.cs) |
| **HTTPS forzado fuera de Development** (Slide 21) | Que nadie hable HTTP plano contra tu app | `UseHttpsRedirection` + `UseHsts` |
| **`WEBSITE_RUN_FROM_PACKAGE=1`** (Slide 17) | Despliegue inmutable: el ZIP no se descomprime, se monta | App Setting en el portal |

Las ocho son configuración, no código nuevo. Por eso este ejemplo es relativamente corto: pone los **toggles correctos** y te enseña que están ahí.

---

## 3. Por qué esto importa en tu stack

En S1.P viste el ciclo "código → URL pública". En S2.1 das el segundo paso: ese mismo ciclo, **bien configurado**. La diferencia entre un App Service que sobrevive a un viernes a las tres de la tarde y uno que tira un 503 a las primeras horas de uso son tres ajustes en *Configuration*. Esta práctica te los presenta antes de que aparezcan como incidente.

Hay un patrón mental que merece subrayado: **la configuración profesional de App Service vive en el portal, no en el código**. Algunas cosas son `Program.cs` (registrar el health check, el typed client, el CORS) y otras son App Settings (`AppOptions__Greeting`, `WEBSITE_RUN_FROM_PACKAGE`). Las del portal cambian sin redesplegar. Las del código sí requieren un nuevo build. Esa separación es deliberada y es lo que más se aprovecha en producción.

Cambio respecto a S1.P: ahora el tier recomendado es **B1, no F1**. Sale ~10 €/mes y desbloquea Always On + custom domain + slots (que verás en S2.2). El curso recomienda B1 desde aquí en adelante; si tu suscripción es de prácticas y quieres no pagar nada, F1 funciona pero algunas cosas (Always On, health check) no estarán disponibles y lo notarás.

---

## 4. El modelo mental: la cafetería con la luz siempre encendida

Imagina dos cafeterías en la misma calle.

La primera apaga las luces cuando no hay clientes. Cuando llega alguien, el barista enciende la cafetera, espera tres minutos a que se caliente, calienta el horno del croissant, descarga el lavavajillas para coger una taza. Para cuando el cliente recibe su café han pasado diez minutos. Si nadie más entra en una hora, vuelve a apagarlo todo. Eficiencia energética altísima, latencia del primer cliente desastrosa.

La segunda tiene la luz siempre encendida, la cafetera caliente todo el día, las tazas listas. Cuesta más en electricidad — unos veinte euros al mes — pero cualquier cliente que entra recibe su café en treinta segundos. El barista está despierto, las cosas están a temperatura, todo a punto. Si entras a las tres de la tarde de un miércoles cuando no hay nadie, te tratan igual de bien que si entras en la hora punta.

Eso es App Service con y sin **Always On**. Sin Always On (lo que viene por defecto en F1), tu app se "duerme" tras unos veinte minutos sin tráfico. Cuando llega la siguiente petición, App Service la despierta — cargar el runtime, tu DLL, abrir el puerto. Treinta segundos a un minuto. Con Always On (disponible desde B1 hacia arriba), App Service mantiene la app despierta enviándole pings periódicos. La instancia está siempre caliente, lista para servir.

```
App Service Plan B1 (la cafetería en marcha 24/7)
   │
   ├── Always On = ON         ← luz siempre encendida
   ├── Health check = /health ← el inspector que pasa cada dos minutos
   ├── HTTPS Only = ON        ← solo se entra por la puerta principal
   ├── TLS 1.2 mínimo         ← cerradura moderna
   │
   └── Web App "app-curso-m02-s21-pedro"
          ├── Configuration → Application settings (la pizarra del día)
          │      ├── AppOptions__Greeting = "Hola desde App Service en Azure"
          │      ├── AppOptions__Healthy = true
          │      └── WEBSITE_RUN_FROM_PACKAGE = 1
          └── https://app-curso-m02-s21-pedro.azurewebsites.net
```

Tres frases para fijar el modelo:

- **Always On no es solo "no se duerme".** Es la base sobre la que funciona el health check y el web jobs. Sin Always On, el health check no se ejecuta en idle y los jobs en background mueren.
- **El Health Check es un contrato.** Tú prometes `/health` responde 200 si la instancia está sana y otra cosa si no. App Service consulta cada dos minutos. Si responde mal varias veces seguidas, **reinicia la instancia**. Es lo que diferencia "tengo un error y voy hundiéndome" de "tengo un error y App Service me auto-cura".
- **Las App Settings son una pizarra que App Service inyecta como variables de entorno.** `AppOptions__Greeting` (con doble guion bajo) llega a tu código como `AppOptions:Greeting`. La doble guion bajo es la convención de ASP.NET Core para separar secciones cuando vienes de env vars. Es la única "magia" de configuración que tienes que recordar.

Vuelve a esta imagen cuando dudes por qué los toggles del portal importan: el código está caliente cuando llega el cliente.

---

## 5. La capa de configuración: Options pattern con validación

[`AppOptions.cs`](src/AppService.Demo.Api/Configuration/AppOptions.cs) es un POCO pequeño con tres propiedades:

```csharp
public sealed class AppOptions
{
    public const string SectionName = "AppOptions";

    [Required(AllowEmptyStrings = false)]
    public string Greeting { get; init; } = "Hola desde App Service";

    public bool Healthy { get; init; } = true;

    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();
}
```

Y en `Program.cs` el patrón es estándar de ASP.NET Core, pero con un detalle que merece comentario:

```csharp
builder.Services
    .AddOptions<AppOptions>()
    .Bind(builder.Configuration.GetSection(AppOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

`ValidateOnStart()` es la clave. Sin ella, la validación ocurre la primera vez que alguien inyecta `IOptions<AppOptions>` — normalmente en la primera petición. **Con `ValidateOnStart()`, la app no arranca si la validación falla.** Es decir: si `AppOptions:Greeting` está vacío o no existe, App Service marca la instancia como no-saludable y la reinicia. En lugar de servir errores durante horas mientras alguien lo nota.

Esta es la actitud que conviene adoptar en producción: **falla pronto y ruidosamente** mejor que falla tarde y silenciosamente. App Service tiene mecanismos para recuperarse de fallos de arranque (health check, auto-restart, slots), así que el coste de un fallo al arrancar es bajo. El coste de servir mal durante una hora porque nadie miró los logs es mucho más alto.

> 🧠 **App Settings con dos guiones bajos.** App Service inyecta cada App Setting como una variable de entorno. ASP.NET Core lee variables de entorno con `__` como separador de secciones — el equivalente a `:` en JSON. Por eso `AppOptions__Greeting` en el portal se traduce a `AppOptions:Greeting` en tu código. Y por eso `AppOptions__AllowedOrigins__0` es el primer elemento del array. Es la única convención de naming de App Settings que tienes que memorizar; el resto es como cualquier `IConfiguration`.

---

## 6. El health check que hace de verdad lo que dice

Mira [`ConfigurableHealthCheck.cs`](src/AppService.Demo.Api/Services/ConfigurableHealthCheck.cs):

```csharp
public sealed class ConfigurableHealthCheck(IOptionsMonitor<AppOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(options.CurrentValue.Healthy
            ? HealthCheckResult.Healthy("App is healthy.")
            : HealthCheckResult.Unhealthy("AppOptions:Healthy=false"));
    }
}
```

Es una implementación deliberadamente trivial: lee un booleano de configuración y devuelve sano o no-sano. ¿Por qué es interesante?

Primero, usa **`IOptionsMonitor`**, no `IOptions`. La diferencia importante: `IOptions` cachea el valor al primer uso; `IOptionsMonitor.CurrentValue` lo relee cada vez. Si cambias `AppOptions__Healthy` en el portal a `false`, la app se entera **en la siguiente petición al health check**, sin reinicio. Eso es lo que permite el experimento de "simular un fallo sin redesplegar".

Y segundo, el experimento de la Slide 32 es exactamente eso: cambias `AppOptions__Healthy` a `false` en el portal, esperas dos minutos, ves en *Log stream* que App Service ha detectado "Health check failed" y ha reiniciado la instancia. Pones de vuelta `true` y todo vuelve a la normalidad. **Eso es auto-cura en directo, sin código adicional.** Acabas de ver a App Service hacer lo que promete.

En producción real, un health check sano hace cosas más útiles que mirar un booleano: pinga la base de datos, verifica que la caché responde, comprueba que la cola de mensajes acepta conexiones. El patrón aquí es educativo, pero la API (`IHealthCheck.CheckHealthAsync`) es la misma que vas a usar en producción.

---

## 7. CORS, HttpClient y otros detalles de "no me dispares en el pie"

Tres ajustes más en `Program.cs` que merecen comentario porque son **lecciones del primer día en producción** que casi nadie te cuenta:

### 7.1 CORS controlado por configuración

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var origins = builder.Configuration
            .GetSection(AppOptions.SectionName)
            .Get<AppOptions>()?.AllowedOrigins ?? [];

        if (origins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
        }
        else
        {
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});
```

La pregunta importante no es cómo se configura CORS — eso es una línea de docs —. Es **dónde vive la lista de orígenes permitidos**. Aquí vive en `AppOptions:AllowedOrigins`, que en el portal se llena con `AppOptions__AllowedOrigins__0`, `__1`, etc. Si mañana añades un frontend nuevo en otro dominio, **cambias una App Setting y la app se reinicia en treinta segundos**. Sin redeploy, sin pipeline, sin builds. Es exactamente el patrón "cambiar configuración sin redesplegar" que aprendiste en S1.P, aplicado a algo que en producción cambia varias veces al año.

Y el detalle del `if (origins.Length == 0) → bloquea todo`: en producción es mejor que CORS bloquee todo a que permita todo. Si nadie ha configurado los orígenes, no es "olvidamos rellenar la lista, déjalos pasar": es "no había orígenes, no dejes pasar a nadie". Fallar cerrado en seguridad es la opción correcta.

### 7.2 HttpClient como typed client (y la historia de SNAT)

```csharp
builder.Services.AddHttpClient<ExternalApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com");
    client.DefaultRequestHeaders.Add("User-Agent", "AppService.Demo.Api");
});
```

`AddHttpClient<T>` registra `ExternalApiClient` como typed client y, **lo importante**, hace que el `HttpMessageHandler` (que es lo caro: socket, pool de conexiones) se comparta. Si en lugar de esto crearas `new HttpClient()` en cada llamada, abrirías un socket por petición. App Service tiene un límite de **128 puertos efímeros SNAT por instancia**. Lo agotas en unas decenas de peticiones por segundo y empiezas a tirar `SocketException`.

Esto se llama **SNAT exhaustion** y es uno de los problemas más confusos cuando aparece: tu código parece correcto, en local funciona, en Azure falla a los pocos minutos de uso. El diagnóstico es difícil porque no hay un error claro — solo timeouts intermitentes. La solución preventiva es esta: **una instancia de `HttpClient` (o su `HttpMessageHandler`) por servicio externo, registrada en DI, vida singleton**. Es una de las cosas más comunes que pillan al equipo en su primera app Azure seria.

### 7.3 HTTPS solo fuera de Development

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

En Development queremos poder pegar a `http://localhost:5080` sin que ASP.NET nos redirija. En Production, queremos que cualquier `http://` reciba un 301 a `https://` y que el navegador del cliente se acuerde (HSTS) de no intentar HTTP otra vez. Y el toggle "HTTPS Only" del portal hace lo mismo a nivel de App Service: si alguien llega con `http://`, ni siquiera llega a tu código. **Defensa en profundidad**: el código fuerza, el portal también fuerza. Dos cinturones cada uno con su propia razón de ser.

---

## 8. Recorrido guiado

Lanza la API en local (sección 11) y prueba estos pasos. Después repítelos en la URL pública tras desplegar.

| # | Petición / acción | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `GET /` | JSON con `greeting`, `machineName`, `instanceId`, `timestamp` | En local `instanceId: "local"`; en Azure es un hash tipo `xxxxx-instance-X`. Sirve para ver a qué instancia llega cada petición (Slide 4). |
| 2 | `GET /info` | JSON con runtime, OS, `WEBSITE_*` y `appOptions` actuales | Mezcla diagnóstico técnico + configuración efectiva. Útil al desplegar para confirmar que las settings llegaron bien. |
| 3 | `GET /health` | `200` con `Healthy` | El que App Service pinga cada dos minutos cuando configuras el health check. |
| 4 | Cambia `AppOptions:Healthy` a `false` y llama `/health` | `503 Service Unavailable` | Reproducción del fallo (Slide 32). En Azure: cambias `AppOptions__Healthy=false` en el portal, esperas dos minutos y App Service reinicia la instancia. |
| 5 | Vuelve a `true`, llama `/health` | `200 Healthy` otra vez | Auto-cura. El reinicio resolvió el problema. |
| 6 | `OPTIONS /` con `Origin: http://localhost:5173` | `200` con `Access-Control-Allow-Origin: http://localhost:5173` | CORS permitido (en `appsettings.Development.json` ya está esa lista). |
| 7 | `OPTIONS /` con `Origin: https://evil.example.com` | `200` sin el header de CORS | Origen no autorizado. El navegador rechazará la petición real. |
| 8 | En Azure: añade `AppOptions__AllowedOrigins__1=https://otro.com` y repite el preflight desde `otro.com` | Ahora sí pasa | Cambio de configuración sin redesplegar. Treinta segundos. |

Un experimento que aporta más que la teoría: lleva el proceso del paso 4 en directo durante una sesión de aula. La gente ve en *Log stream* del portal el aviso de "Health check failing", luego el reinicio, luego el "Health check passing" otra vez. Es la primera vez que la mayoría ve a Azure auto-curándose, y se queda grabado.

---

## 9. Por qué los tests están así

Cuatro archivos en `tests/AppService.Demo.Api.Tests/`, todos con `WebApplicationFactory<Program>` (el patrón que ya viste en S1.P).

- **`HealthEndpointTests`** — cubre los dos caminos: por defecto `200 Healthy`, con `AppOptions:Healthy=false` `503 Service Unavailable`. La capa que demuestra el experimento de Slide 32 sin tener que ir al portal.
- **`HelloEndpointTests`** — verifica que el JSON de `/` lleva los tres campos no vacíos. No prueba el valor exacto del `machineName` porque cambia entre máquinas; sí prueba que existe.
- **`InfoEndpointTests`** — comprueba que `/info` expone los campos esperados y que `appOptions.{greeting, healthy, allowedOrigins}` está poblado correctamente.
- **`CorsConfigurationTests`** — los dos casos del paso 6 y 7 del recorrido. Preflight permitido vs no permitido. Esto es muy valioso porque CORS es uno de los tests más útiles y menos escritos: cubre que la configuración de orígenes funciona, no solo que existe.

No hay capas (unit / component / integration) como en M05: el ejemplo es pequeño y todo cabe en `WebApplicationFactory`. La separación en capas aparecerá en módulos posteriores cuando haya estado persistente.

---

## 10. App Settings y la diferencia con S1.P

En S1.P viste App Settings por primera vez: cambias un valor en el portal, la app se reinicia, el nuevo valor está disponible. S2.1 le añade una vuelta más: **la validación de las settings ocurre al arrancar**. Si fijaste `AppOptions:Greeting` como requerido (`[Required]`) y se queda vacío por error, la app no arranca. App Service marca la instancia como no-saludable, reinicia, vuelve a no arrancar — un bucle visible en *Log stream*.

Esto suena agresivo, y lo es a propósito. La alternativa es que la app arranque con un `Greeting` nulo o vacío y empiece a devolver respuestas raras durante horas hasta que alguien se dé cuenta. El bucle de reinicios es ruidoso, **se ve**. Es el tipo de fallo que llama tu atención y te obliga a arreglarlo. Las cosas que fallan en silencio son las que de verdad te muerden.

Este patrón —Options con validación al arranque— es estándar en producción ASP.NET Core. Cuando llegues a M06 (Seguridad) y M08 (DevOps), lo verás aplicado a connection strings, OAuth secrets, configuración crítica. Aquí lo aprendes en su versión más simple con un `Greeting` de texto.

---

## 11. Puesta en marcha, ejecución y pruebas

### 11.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x (`dotnet --list-sdks`) | compilar y ejecutar | Sí |
| Suscripción Azure activa | desplegar | Sí (si vas a desplegar) |
| VS Code con extensión **Azure App Service** | despliegue por UI | Recomendado |
| (Opcional) dev cert .NET confiado | HTTPS local | Solo si vas a usar `https` launch-profile |

Si quieres HTTPS local: `dotnet dev-certs https --trust` la primera vez.

### 11.2 Compilar y arrancar en local

```bash
cd examples/M02-App-Services/S2.1-creacion-config-publicacion
dotnet build AppService.Demo.slnx                    # 0 errores

dotnet run --project src/AppService.Demo.Api --launch-profile http
# → http://localhost:5080

# o con HTTPS:
dotnet run --project src/AppService.Demo.Api --launch-profile https
# → https://localhost:5081
```

Prueba de vida: `curl http://localhost:5080/` debería devolver un JSON con tu `greeting` y `instanceId: "local"`. Cambia `appsettings.Development.json` y reinicia para ver cómo `Options` recoge la nueva configuración.

### 11.3 Pasar los tests

```bash
dotnet test
```

Cubren los cuatro archivos de tests con `WebApplicationFactory`. Sin Azure, sin Docker, en memoria. Si todos están verdes y el build no tiene warnings (`TreatWarningsAsErrors=true`), el ejemplo está en condiciones.

### 11.4 Desplegar a Azure (resumen)

El detalle paso a paso por Portal está en el [`README.md`](README.md): RG → App Service Plan B1 Linux → Web App .NET 10. Lo que conviene **no saltarse** después de crearla:

1. *Configuration → General settings*: **Always On = On**, **HTTPS Only = On**, **TLS = 1.2**.
2. *Configuration → Application settings*: añade `AppOptions__Greeting`, `AppOptions__Healthy=true`, `AppOptions__AllowedOrigins__0` (un dominio), `WEBSITE_RUN_FROM_PACKAGE=1`.
3. *Monitoring → Health check*: **Enable**, path `/health`, load balancing `2 minutes`.
4. Despliega el código (desde VS Code → *Deploy to Web App* es lo más cómodo).
5. Verifica con `curl https://<tu-app>.azurewebsites.net/{,/info,/health}`.

### 11.5 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| La app no arranca y *Log stream* dice "Failed to bind to AppOptions" | `AppOptions__Greeting` vacío o ausente; la validación al arranque falló | Configúralo en *Application settings* y guarda |
| Health check siempre `Unhealthy` | `AppOptions__Healthy=false` quedó por error | Ponlo a `true` y guarda |
| CORS bloquea peticiones legítimas del frontend | el origen no está en `AppOptions__AllowedOrigins__N` | añádelo como App Setting (con índice nuevo) |
| `dotnet dev-certs https --trust` no funciona y el launch HTTPS falla | el dev cert no está confiado | ejecuta el comando con permisos adecuados, o usa el profile `http` |
| La primera petición tarda mucho en local | nada, es el JIT inicial | normal; la segunda ya es rápida |
| Errores intermitentes al llamar a APIs externas en Azure | SNAT exhaustion | revisa que cualquier llamada externa usa `IHttpClientFactory` o un typed client, no `new HttpClient()` |
| No veo logs en *Log stream* | falta `AddAzureWebAppDiagnostics()` o `App Service Logs` no está activado | comprueba `Program.cs` y *Monitoring → App Service logs* |

### 11.6 Limpieza

`Portal → Resource groups → rg-curso-m02-s21 → Delete resource group`. Confirma escribiendo el nombre. Esto borra el plan + web app + settings.

---

## 12. Ideas para llevarte

Lo más útil que sale de esta práctica no es ningún concepto suelto: es **la disciplina de "configurar la app antes de desplegarla"**. Cuando crees una Web App nueva en Azure, el flujo correcto en este orden es:

1. Plan B1 (mínimo razonable, ~10 €/mes).
2. *Configuration → General*: Always On, HTTPS Only, TLS 1.2.
3. *Configuration → Application settings*: tu configuración mínima viable.
4. *Monitoring → Health check*: enable, path `/health`.
5. Y entonces desplegar.

Si saltas algún paso "para luego", lo "luego" es justo lo que no se hace. Especialmente Always On y Health Check, que son lo que separa una app de demo de una app de producción.

Sobre los **Options con validación al arranque**: es el patrón más sencillo de "falla pronto y ruidosamente". Adoptarlo desde el primer proyecto te ahorra el clásico "no entiendo por qué falla en producción cuando en local va bien". En producción falta una App Setting. En local la había en `appsettings.Development.json`. El bucle de reinicios te lo enseña en treinta segundos en lugar de en tres días.

Y un consejo pragmático: **siempre `IHttpClientFactory` o typed clients** (`AddHttpClient<T>`). Nunca `new HttpClient()` en código que vaya a producción Azure. El SNAT exhaustion es uno de los problemas más sutiles y costosos que aparecen, y la prevención cabe en dos líneas de DI. Es de esas cosas que se aprenden la primera vez y no se vuelven a olvidar.

---

## 13. Comprueba que lo has entendido

1. ¿Para qué sirve **Always On** y por qué no está disponible en F1? *(sección 4)*
2. Configuras `AppOptions:Greeting=""` por error y guardas en el portal. ¿Qué pasa cuando la app intenta arrancar y por qué es deseable que pase eso? *(secciones 5, 10)*
3. ¿Qué diferencia hay entre `IOptions<T>` y `IOptionsMonitor<T>`? ¿Por qué el health check usa el segundo? *(sección 6)*
4. ¿Qué es SNAT exhaustion y cómo lo evita el ejemplo? *(sección 7.2)*
5. Un App Setting en el portal se llama `AppOptions__AllowedOrigins__2`. ¿A qué propiedad de C# corresponde y por qué los dobles guiones bajos? *(sección 5)*
6. ¿Qué hace App Service cuando `/health` empieza a devolver `503` durante varios minutos? ¿Por qué eso es bueno? *(sección 6)*

<details>
<summary>Respuestas</summary>

1. Always On mantiene la instancia "caliente" enviando pings periódicos. Sin Always On, la app se duerme tras unos veinte minutos sin tráfico y la siguiente petición paga el cold start (treinta segundos a un minuto). No está disponible en F1 porque F1 es un tier gratuito compartido con límites estrictos; está disponible desde B1 hacia arriba. También permite que jobs en background sobrevivan y que el health check se ejecute correctamente.
2. La app **no arranca**. Tienes `[Required]` en `Greeting` y `ValidateOnStart()` en el binding, así que cualquier valor inválido detiene el arranque. App Service marca la instancia como no-saludable y reinicia — el bucle es visible en *Log stream*. Es deseable porque la alternativa es arrancar con un greeting vacío y servir respuestas raras durante horas sin que nadie se dé cuenta. "Falla pronto y ruidosamente" es la disciplina correcta en producción.
3. `IOptions<T>` cachea el valor al primer uso y no lo refresca. `IOptionsMonitor<T>.CurrentValue` relee el valor cada vez. El health check usa `IOptionsMonitor` porque queremos que un cambio en `AppOptions__Healthy` desde el portal sea visible en la siguiente petición al `/health`, sin reiniciar la app. Eso permite el experimento de "simular fallo sin redesplegar".
4. SNAT (Source Network Address Translation) exhaustion es agotar los puertos efímeros que App Service tiene para conexiones salientes (límite ~128 por instancia). Si haces `new HttpClient()` en cada llamada, abres un socket nuevo cada vez y los agotas con relativa facilidad. El síntoma es errores intermitentes en llamadas a APIs externas. El ejemplo lo evita con `AddHttpClient<ExternalApiClient>(...)` — el `HttpMessageHandler` (que es lo caro) se comparte entre todas las llamadas.
5. Corresponde a `AppOptions.AllowedOrigins[2]` (el tercer elemento del array, índice 2). Los dobles guiones bajos son la convención de ASP.NET Core para separar secciones cuando la configuración viene de variables de entorno; el equivalente a `:` en JSON. App Service inyecta cada App Setting como variable de entorno, así que necesitas usar `__` en lugar de `:`. Es la única convención de naming de App Settings que conviene memorizar.
6. Si configuraste *Monitoring → Health check* con path `/health`, App Service consulta ese endpoint cada dos minutos. Si responde `503` varias veces seguidas, **reinicia la instancia**. Es bueno porque significa que cualquier estado transitorio malo (memoria corrupta, conexión zombie, deadlock) se resuelve solo con un reinicio, sin esperar a que alguien lo vea. Es el patrón básico de "auto-cura" de App Service. Combinado con varias instancias (en S1+), la app sigue sirviendo desde las sanas mientras la mala se recupera.

</details>

---

## 14. Hasta aquí

Esta práctica es la transición de "tengo algo en Azure" a "tengo algo en Azure bien configurado". Always On, Health Check, Options validadas, CORS desde configuración, HttpClient typed. Son cinco o seis ajustes, ninguno complicado, que separan la demo del aula del primer pilar de algo que se puede defender en producción.

Lo siguiente es [`S2.2 — Slots de despliegue`](../S2.2-slots-staging-produccion/MANUAL.md). Ahí aparece el concepto de **deployment slot**: una copia paralela de tu app contra la que despliegas y verificas antes de promocionar a producción con un *swap* sin downtime. Es el paso siguiente natural: tu app está bien configurada, pero el despliegue todavía es "destruir y reconstruir". Los slots cambian eso.
