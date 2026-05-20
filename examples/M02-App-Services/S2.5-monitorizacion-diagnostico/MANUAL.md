# Manual del alumno — S2.5 · Monitorización y diagnóstico

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: pasos por Portal, scripts `az`, mapeo a slides, KQL queries listas para copiar. Este manual va antes: te cuenta por qué este es el cierre natural del módulo M02 y qué cambio mental hay que hacer para "no servir a ciegas".

Tiempo de lectura: ~25 min. Submódulo de teoría: [M02-S2.5](../../../doc/M02-App-Services/v4-actual/M02-S2.5-monitorizacion-diagnostico-v4.md). Última pieza del módulo M02 antes de las prácticas — añade Application Insights con OpenTelemetry, custom metrics de negocio, alertas, availability tests y PII scrubbing.

*Creado: 2026-05-20 09:16 +0200*

---

## 1. La idea en una frase

Hasta ahora has hecho lo correcto en producción: tier razonable, slots, escalado, secretos en Key Vault. Falta lo más fácil de posponer y lo más importante cuando algo va mal: **saber qué está pasando ahí dentro**. App Insights con OpenTelemetry te lo da con cuatro líneas en `Program.cs` y una App Setting. Logs, traces distribuidos, métricas built-in, métricas de negocio que tú defines, alertas que te llaman antes de que el cliente. Y todo con la promesa de que en local sigue funcionando sin necesidad de Azure: si no hay connection string, OpenTelemetry no se inicializa pero el código corre igual.

Este ejemplo te da la base. Te enseña a contar lo que importa (pedidos creados, importe, tiempo de procesamiento) y a no contar lo que no debe salir nunca (emails, tarjetas, JWT). Esa segunda parte —**PII scrubbing antes de loguear**— es la diferencia entre logs útiles y logs que son una bomba de relojería legal.

---

## 2. El problema real que hay detrás

Una API de un cliente empezó a tener una métrica rara: cada minuto, una petición concreta tardaba ocho segundos. Las otras, milisegundos. El cliente no se quejaba porque solo afectaba a una llamada secundaria del frontend que se ejecutaba en background. Pero, mes a mes, el ratio de abandono del onboarding subía silenciosamente. Tres meses después, alguien hizo el cruce y descubrió la correlación. La lentitud de esa llamada hacía que el onboarding pareciera bloqueado, y los usuarios cerraban la pestaña sin quejarse.

La causa técnica era trivial: un `await` a una API externa que llevaba meses degradándose. La autopsia operativa: **no había telemetría**. Nadie miraba esa llamada porque "no era importante". Con App Insights conectado desde el día uno, el Application Map habría dibujado esa dependencia roja casi desde la primera semana.

Eso es lo que este ejemplo enseña. Cinco piezas que te quitan la ceguera:

| Pieza | Para qué | Dónde la verás |
| --- | --- | --- |
| **OpenTelemetry + Azure Monitor exporter** (Slide 20) | Logs + traces + métricas built-in con cuatro líneas | [`Program.cs`](src/AppService.Demo.Api/Program.cs) → `UseAzureMonitor()` |
| **`AppMeter` con custom metrics** (Slide 22) | Métricas de negocio: pedidos, importe, duración | [`AppMeter.cs`](src/AppService.Demo.Api/Telemetry/AppMeter.cs) |
| **Distributed tracing con tags** (Slide 21) | Filtrar spans en App Insights por dimensiones de negocio | [`OrdersEndpoints.cs`](src/AppService.Demo.Api/Endpoints/OrdersEndpoints.cs) → `Activity.Current.SetTag` |
| **PII scrubbing antes de loguear** (Slide 25) | Que un email/JWT no aparezca jamás en los logs | [`PiiScrubber.cs`](src/AppService.Demo.Api/Telemetry/PiiScrubber.cs) |
| **Action Groups + Alertas** (Slides 12, 26, 27) | Que la luz roja te llame antes que el cliente | scripts `04-create-action-group.sh` y `05-create-alerts.sh` |

Y para escenificar el dashboard en clase: `/demo/orders` que incrementa los counters, `/demo/error?type=500\|exception\|slow\|dependency-fail` que reproduce los cuatro tipos de fallo más comunes, y un generador de tráfico (`07-generate-traffic.sh`) que llena la telemetría en cinco minutos.

---

## 3. Por qué esto importa en tu stack

Hay una asimetría conocida en cualquier proyecto: añadir telemetría es trivial el primer día y costoso un año después. El primer día son cinco líneas en `Program.cs` y una App Setting. Un año después es "no sabemos qué métricas queremos porque no tenemos referencia histórica de cómo se comporta normal nuestra app". El consejo de fondo: **el ejemplo te enseña a conectar App Insights desde el día uno**. Aunque tu equipo no tenga aún dashboards, el dato está, y cuando aparezca la primera duda ("¿esto siempre fue lento?") tendrás datos para responder.

Cambio respecto a S2.4: misma arquitectura (RG, plan S1, web app), añades dos recursos más en el mismo RG — un **Log Analytics workspace** y un **Application Insights workspace-based** apuntando a ese workspace. Eso son cinco minutos en el portal. Configurar App Settings con la connection string de App Insights son otros treinta segundos. A partir de ese momento, todos los `ILogger`, los traces de ASP.NET Core, las llamadas HttpClient salientes y las métricas de runtime aparecen en App Insights automáticamente.

> 🧠 **Workspace-based, no classic.** Cuando crees Application Insights, asegúrate de elegir *Workspace-based*. La modalidad classic está en deprecación. La diferencia: el modo workspace-based guarda los datos en un Log Analytics workspace que tú gestionas (mismo plan de retención, mismas KQL, mismo coste predecible). Es la opción correcta desde 2023 en adelante y la única disponible para suscripciones nuevas.

---

## 4. El modelo mental: el panel del coche moderno

Imagina dos coches en la misma carretera. Uno es de los noventa: salpicadero con velocímetro, temperatura del agua, indicador de gasolina y un par de luces que se encienden a veces. Si algo va mal, te enteras cuando el coche se cala — o, con suerte, cuando una luz se enciende sin contexto y te toca llevarlo al taller para que lean lo que sea que el ordenador del coche sepa.

El otro coche es moderno. Tienes un panel digital que muestra en tiempo real consumo, RPM, presión de los neumáticos, temperatura de cada componente, distancia hasta el próximo mantenimiento, mensajes específicos cuando algo está cerca del umbral ("filtro de aire al 85%, cámbielo en los próximos 1000 km"). Y por debajo, una caja negra graba todo: si un día tienes un problema, el mecánico enchufa el ordenador y sale exactamente qué pasó, qué sensor leyó qué, cuándo se encendió cada luz.

App Insights con OpenTelemetry te convierte tu App Service del primer coche al segundo. Las cuatro pantallas son:

```
┌─────────────────────────────────────────────────────────────────────┐
│  Live Metrics            (el panel digital — tiempo real)            │
│  • Requests/sec, failures/sec, dependencies/sec                      │
│  • CPU, memoria, GC, threads                                         │
│  • Mientras ocurre, lo ves                                           │
├─────────────────────────────────────────────────────────────────────┤
│  Application Map         (el dibujo del motor)                       │
│  • Tu app + cada servicio externo al que llama                       │
│  • Latencia y % de error de cada dependency                          │
│  • La pieza roja salta a la vista                                    │
├─────────────────────────────────────────────────────────────────────┤
│  Metrics                 (los contadores)                            │
│  • Built-in: requests, response time, exceptions                     │
│  • Custom: demo.orders.created, demo.orders.amount, duration         │
│  • Filtrables por dimensiones (priority, sku, region)                │
├─────────────────────────────────────────────────────────────────────┤
│  Logs (KQL)              (la caja negra)                             │
│  • Cada request, cada exception, cada dependency call                │
│  • Cada custom log que tú escribiste                                 │
│  • Lenguaje de consulta KQL — el SQL de los logs                     │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                Action Groups + Alertas (las luces de check)
                  • Http5xx > 5/5 min → email
                  • Latencia avg > 3 s/10 min → email
                  • Availability < 99% → email
```

Tres frases para fijar el modelo:

- **Live Metrics es para "está pasando ahora".** Lo abres cuando el cliente avisa, ves la curva en tiempo real, identificas el patrón. Es la herramienta de la mesa de operaciones.
- **Application Map es para "¿quién está enfermo en mi arquitectura?".** Un vistazo te dice cuál de tus dependencies está roja. Es lo primero que miras al diagnosticar.
- **Logs con KQL es para "qué pasó exactamente".** Cuando ya sabes dónde mirar (gracias a Live Metrics o Map), KQL te deja reconstruir el incidente con precisión: qué petición, qué excepción, qué stack trace, qué usuario.

Vuelve a esta imagen cuando dudes por qué cada pieza existe. Cada una resuelve un momento operativo distinto.

---

## 5. OpenTelemetry: cuatro líneas para verlo todo

`Program.cs` activa la telemetría con un condicional explícito:

```csharp
var aiConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
                       ?? builder.Configuration["ApplicationInsights:ConnectionString"];

if (!string.IsNullOrEmpty(aiConnectionString))
{
    builder.Services
        .AddOpenTelemetry()
        .UseAzureMonitor(o => o.ConnectionString = aiConnectionString)
        .WithTracing(tracing => tracing.AddSource(AppMeter.MeterName))
        .WithMetrics(metrics => metrics
            .AddMeter(AppMeter.MeterName)
            .AddRuntimeInstrumentation());
}
```

El condicional es deliberado y didáctico: **sin la connection string, OpenTelemetry no se inicializa pero el código funciona igual**. En local sin Azure puedes correr la app, llamar a `/demo/orders`, ver los logs por consola. En el momento en que añades `APPLICATIONINSIGHTS_CONNECTION_STRING`, toda la telemetría empieza a fluir hacia App Insights. La transición es **una sola App Setting**.

¿Qué hace `UseAzureMonitor()` automáticamente?

- **Logs**: todos los `ILogger` de la app se exportan a App Insights con sus structured fields.
- **Traces de ASP.NET Core**: cada request es un span con su duración, status code, route. Las llamadas HttpClient son child spans (dependencies en App Insights).
- **Métricas built-in**: RPS, response time, status codes, GC, threads, working set.

Y los dos `WithTracing`/`WithMetrics` añaden:

- **Tu meter de aplicación** (`AppService.Demo.Api`) para que los counters/histogramas custom lleguen.
- **Runtime instrumentation**: métricas detalladas de .NET (allocations por GC generation, heap size, exceptions/s).

> 🧠 **OpenTelemetry vs el SDK clásico de App Insights.** OpenTelemetry es el estándar abierto (CNCF) que se ha establecido como la forma moderna de hacer telemetría — y Microsoft ha publicado `Azure.Monitor.OpenTelemetry.AspNetCore` como exporter. Si tienes proyectos legacy con `Microsoft.ApplicationInsights.AspNetCore`, siguen funcionando, pero los nuevos van con OTel. La razón es portabilidad: si mañana cambias de proveedor (Datadog, New Relic, Honeycomb), solo cambias el exporter. El código que instrumenta tu app es el mismo.

---

## 6. Custom metrics: contar lo que importa al negocio

[`AppMeter.cs`](src/AppService.Demo.Api/Telemetry/AppMeter.cs) define tres métricas con la API moderna de .NET (`IMeterFactory` + `Meter`):

```csharp
OrdersCreated = meter.CreateCounter<long>("demo.orders.created", unit: "{order}");
OrderAmountTotal = meter.CreateCounter<double>("demo.orders.amount.total", unit: "EUR");
OrderProcessingDuration = meter.CreateHistogram<double>("demo.orders.duration", unit: "ms");
```

Las tres aparecen en App Insights bajo *Metrics → Custom*. Y como `OrdersEndpoints.cs` les pasa una **dimensión** (`priority`) al incrementarlas, en App Insights aparecen por separado para cada valor de prioridad: `demo.orders.created{priority=low}`, `demo.orders.created{priority=high}`, etc.

¿Por qué importa esto en la práctica? Porque permite **alertas de negocio**, no solo técnicas. Una alerta clásica es "Http5xx > 5 en 5 minutos" — te avisa cuando hay errores HTTP. Una alerta de negocio es "demo.orders.created{priority=high} < 1 en 5 minutos" — te avisa cuando dejan de llegar pedidos de alta prioridad, lo que puede pasar **sin que haya un solo error HTTP**. La app está sirviendo perfectamente; los pedidos prioritarios no entran. Eso es un incidente que solo se ve si mides la cosa correcta.

> 🧠 **Counter vs Histogram, cuál cuándo.** Counter para cosas que solo suman (pedidos creados, importes totales, errores). Histogram para cosas con distribución (duración de cada pedido, tamaño de cada response). En App Insights, el histogram te da percentiles automáticamente: P50, P95, P99 de la duración. El P95 es lo que normalmente importa: "el 95% de los pedidos se procesa en menos de 200 ms". Con un counter solo tienes "todos los pedidos sumados" — útil para volumen, inútil para latencia.

---

## 7. Distributed tracing: enriquecer spans con tags

`OrdersEndpoints.cs` usa el patrón estándar para enriquecer el span del request actual:

```csharp
var activity = Activity.Current;
activity?.SetTag("order.id", orderId);
activity?.SetTag("order.sku", request.Sku);
activity?.SetTag("order.priority", request.Priority);
```

`Activity.Current` es el span activo de OpenTelemetry para esta petición. Las tags que le pongas viajan con el span hasta App Insights y aparecen en cada traza como `customDimensions`. Eso te permite consultas KQL como:

```
requests
| where customDimensions.["order.priority"] == "high"
| where success == false
| project timestamp, name, duration, customDimensions
```

"Dame todos los requests fallidos de pedidos de alta prioridad de la última hora". Sin las tags, esa misma query es imposible — sabes qué requests fallaron, pero no de qué pedidos eran. Con las tags, el contexto de negocio viaja con la telemetría.

Esto es lo que distingue **distributed tracing serio** de logs sueltos. Cada request es una entidad correlacionable (`operation_Id`), las llamadas a APIs externas son child spans con su propio `operation_ParentId`, y las custom dimensions las atan al contexto de negocio. En un incidente real, esa correlación es lo que te lleva del error visible al request original que lo causó.

---

## 8. PII scrubbing: la diferencia entre logs útiles y logs ilegales

`/demo/log` y [`PiiScrubber.cs`](src/AppService.Demo.Api/Telemetry/PiiScrubber.cs) cubren un tema que se infravalora hasta que aparece la auditoría de RGPD: **los logs son persistentes y los lee mucha gente**. Si tu app loguea `"User pedro@empresa.com tried to login"` con el email en claro, ese email queda en App Insights durante meses, lo ven todos los developers con acceso al workspace, y RGPD considera esos logs "datos personales bajo procesamiento". Si la retención es de un año, tienes datos personales durante un año más allá de lo necesario.

`PiiScrubber` aplica tres regex compilados (con `[GeneratedRegex]` de .NET 7+, lo que evita penalización de runtime):

| Patrón | Reemplazo |
| --- | --- |
| Email (`a.b@c.d`) | `[REDACTED:EMAIL]` |
| Tarjeta de crédito (16 dígitos con o sin separadores) | `[REDACTED:CC]` |
| JWT (3 segmentos base64) | `[REDACTED:TOKEN]` |

Orden de aplicación: JWT → tarjeta → email (los patrones más específicos primero, para que el JWT no caiga accidentalmente en una regla más amplia).

El endpoint `/demo/log` aplica el scrubber al mensaje antes de pasarlo a `ILogger`. La respuesta devuelve `{ originalLength, scrubbed, redactionsApplied }` — útil para verificar en tiempo de desarrollo que el scrubbing funciona sin necesidad de mirar los logs de App Insights.

> 🧠 **El scrubbing tiene que estar en el límite de salida.** Aquí está en el handler del endpoint que va a llamar a `logger`. La regla mental: **antes de que el mensaje cruce a un sistema persistente** (logs, métricas, traces) — pásalo por el scrubber. Si el scrubber está en tu código de negocio, te olvidarás de aplicarlo. Si está en un middleware o en un wrapper del logger, va automático. En este ejemplo es simple y explícito porque es educativo; en producción real conviene un wrapper o un `ILoggerProvider` custom.

---

## 9. Alertas: que la luz roja te llame antes que el cliente

Tres alertas representativas (los scripts `04-` y `05-` las crean):

| Alerta | Condición | Por qué |
| --- | --- | --- |
| **`http5xx`** | `Http5xx > 5` en 5 min, evaluación cada 1 min, severity Critical | Errores 500 visibles para el usuario. Lo más urgente. |
| **`latencia-alta`** | `AverageResponseTime > 3000 ms` en 10 min, severity Warning | Lentitud agregada. Antes de que el cliente se queje, lo ves. |
| **`cpu-alta`** | sobre el **plan**: `CpuPercentage > 80%` en 15 min, severity Warning | Capacidad. Indicio de que toca revisar las reglas de autoscale. |

Las tres mandan al mismo **Action Group**: un email a tu cuenta. En producción real un Action Group tiene varias acciones: email a una lista de distribución, webhook a un sistema de incidentes (PagerDuty, OpsGenie), SMS al de guardia. La práctica usa email por simplicidad — el concepto se traslada igual.

Y hay una alerta más, distinta: el **Availability Test**. Es un ping multi-región a `/health` cada 5 minutos desde tres ubicaciones (Europa + Américas + Asia). Si un porcentaje de las pruebas falla, se dispara la alerta. La diferencia con la alerta de Http5xx: el availability test se ejecuta **desde fuera de Azure** (datacenters de App Insights), así que detecta problemas de red, DNS, certificados — cosas que tu app reportaría como sanas porque ni siquiera están llegando peticiones. Es la red de seguridad externa.

---

## 10. Recorrido guiado: del primer pedido a la primera alerta

| # | Acción | Verificación | Qué demuestra |
| --- | --- | --- | --- |
| 1 | Local: `POST /demo/orders` con `{ sku, quantity, unitPrice }` | `201 Created` con `orderId`, `processingMs` | El endpoint que dispara las custom metrics y tracing. |
| 2 | Local: `GET /demo/error?type=exception` | `500` con stack | Reproduce excepciones para luego ver cómo aparecen en App Insights. |
| 3 | Local: `POST /demo/log` con `{ "message": "User john@test.com tried login with card 4111-1111-1111-1111" }` | `200` con `redactionsApplied: 2` (email + tarjeta) y el `scrubbed` ya redactado | PII scrubber en acción (sección 8). |
| 4 | Azure: configura `APPLICATIONINSIGHTS_CONNECTION_STRING` en App Settings | la app reinicia, OTel se activa | El interruptor único entre "telemetría off" y "telemetría on". |
| 5 | Azure: lanza el generador de tráfico (`07-generate-traffic.sh 5`) | 5 min de requests variados | Pone datos en App Insights para tener qué mirar. |
| 6 | App Insights → Live Metrics (durante el tráfico) | requests/s, dependencies/s, failures/s en directo | El panel del coche moderno. |
| 7 | Application Map | tu app + cada dependency con su latencia | El dibujo del motor (sección 4). |
| 8 | Metrics → Custom → `demo.orders.created` | gráfica por priority | Las métricas de negocio (sección 6). |
| 9 | Logs (KQL): `requests \| where success == false \| project timestamp, name, resultCode` | tabla de los failures | La caja negra (sección 4). |
| 10 | Configura las tres alertas + availability test | en *Monitor → Alerts* aparecen las reglas | La luz de check (sección 9). |
| 11 | Bombardea `/demo/error?type=500` durante 5 minutos | espera ~7-10 min, llega el email | La alerta funcionando de verdad. |

Un experimento que ayuda más que la teoría: tras los pasos 5-9, ejecuta esta query KQL en *Application Insights → Logs*:

```
customMetrics
| where name == "demo.orders.created"
| extend priority = tostring(customDimensions.priority)
| summarize sum(value) by priority, bin(timestamp, 1m)
| render timechart
```

Te dibuja la gráfica de pedidos por minuto desglosada por prioridad. La primera vez que ves tus propios datos de negocio en una gráfica que actualizaste con `curl` cinco minutos antes, el concepto de "metric + dimension + KQL" se entiende mejor que cualquier explicación.

---

## 11. Tests y por qué importan los del PiiScrubber

Cincuenta y ocho tests, incluyendo los heredados de S2.4. Los nuevos cubren las tres piezas didácticas:

- **`PiiScrubberTests` (8)** — emails (varios formatos), tarjetas (con y sin separadores), JWT, mezcla de los tres en un mismo mensaje, texto seguro (no redacta nada), valores null/empty. Es **el test que más vale**: ningún PII se filtra en logs porque hay una suite que lo verifica con cada build. Si alguien añade una regla mal, los tests fallan.
- **`OrdersEndpointTests` (2)** — orderId con prefijo `ORD-`, importe calculado correctamente, validación rechaza cantidad 0.
- **`DemoErrorEndpointTests` (4)** — los cuatro tipos: 500, exception, slow, dependency-fail. Cada uno con el status code esperado. Los tests son baratos pero le dan al endpoint una garantía de contrato útil para el script `07-generate-traffic.sh`.
- **`LogDemoEndpointTests` (2)** — scrub verificado en la respuesta de un mensaje con email, sin redacciones cuando el mensaje es seguro.

Sigue siendo todo `WebApplicationFactory<Program>` en memoria. La validación de App Insights real solo se hace a mano contra Azure.

---

## 12. Puesta en marcha, ejecución y pruebas

### 12.1 Requisitos

| Requisito | Para qué | ¿Obligatorio? |
| --- | --- | --- |
| .NET SDK 10.x | compilar y ejecutar | Sí |
| Suscripción Azure con plan Standard S1 | desplegar | Sí (si vas a desplegar) |
| Permisos para crear Log Analytics + Application Insights + alertas | la práctica completa | Sí |
| `az` CLI, `jq`, `curl`, `zip` | scripts | Solo si usas scripts |
| Cuenta de email accesible | recibir las alertas para verificar | Recomendado |

### 12.2 Compilar y arrancar en local

```bash
cd examples/M02-App-Services/S2.5-monitorizacion-diagnostico
dotnet build AppService.Demo.Monitor.slnx       # 0 errores

dotnet run --project src/AppService.Demo.Api --launch-profile http
# → http://localhost:5080
```

Sin AI connection string, OTel no se inicializa. Los `ILogger` salen por consola. Útil para desarrollar sin gastar nada.

Si quieres telemetría local apuntando a tu propia AI:

```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=...;IngestionEndpoint=https://..."
dotnet run --project src/AppService.Demo.Api
# Live Metrics en el portal mostrará el tráfico desde tu local
```

### 12.3 Pasar los tests

```bash
dotnet test
```

Resultado: **58 pass · 0 fail**.

### 12.4 Desplegar a Azure con App Insights (resumen)

El detalle por Portal está en el [`README.md`](README.md). Pasos clave:

1. **RG + plan S1 + Web App** (mismo patrón que submódulos anteriores).
2. **Log Analytics workspace** en el mismo RG.
3. **Application Insights workspace-based** apuntando a ese workspace; copiar su Connection String.
4. **App Setting**: `APPLICATIONINSIGHTS_CONNECTION_STRING = <connection string>`.
5. Deploy + generador de tráfico para llenar la telemetría.
6. **Action Group** con email, **tres alertas** (Http5xx, latencia, CPU del plan).
7. **Availability test** multi-región contra `/health`.

### 12.5 Scripts `az` (recomendado para escenificar)

```bash
cd scripts
cp .env.demo.example .env.demo
bash demo.sh       # menú interactivo con los 9 pasos
```

El script `07-generate-traffic.sh 5` hace 5 minutos de tráfico variado (peticiones OK + 500s + slow). Combinado con `08-show-kql-queries.sh` (imprime KQL útil para pegar en el portal), tienes el dashboard lleno en cinco minutos.

### 12.6 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| App Insights no muestra nada | falta `APPLICATIONINSIGHTS_CONNECTION_STRING` o tiene typo | confirma la App Setting y reinicia la app |
| Las custom metrics no aparecen | falta `AddMeter(AppMeter.MeterName)` en `Program.cs` | revisa que está dentro del `WithMetrics(...)` |
| Live Metrics dice "Not available" | la app sí está enviando datos pero Live Metrics requiere SDK reciente | confirma versión de `Azure.Monitor.OpenTelemetry.AspNetCore` |
| Las alertas no llegan | el Action Group apunta a un email que no validaste | revisa el inbox del email para confirmar la suscripción al Action Group |
| Availability test siempre rojo | bloqueo en el firewall o IP restrictions | abre el endpoint `/health` sin restricciones, o usa el feature del test para añadir su IP a la allow-list |
| Tarda 5-10 min en aparecer el primer dato | latencia de ingestión normal de App Insights | espera; en Live Metrics es casi inmediato, en Logs hay buffer |
| `NU1902` durante el build | advisory transitiva en OpenTelemetry.Api | está suprimida con `<NoWarn>NU1902</NoWarn>` en los csproj (ver nota del README) |

### 12.7 Limpieza

`Portal → Resource groups → rg-curso-m02-s25 → Delete`. Borra plan + web app + LAW + AI + Action Group + alertas + availability test.

---

## 13. Ideas para llevarte

Lo más útil que sale de esta práctica es **conectar App Insights desde el día uno de cualquier proyecto Azure nuevo**. Aunque tu equipo no tenga aún dashboards, los datos están. Cuando aparezca la primera duda ("¿esto siempre fue lento?") podrás responder con datos en lugar de impresiones. El coste es mínimo: una App Setting y un workspace de Log Analytics.

Sobre **custom metrics**: empieza por las tres que te resuelven el 80% — un counter para "cosas creadas" (pedidos, registros, mensajes), un counter para "importe acumulado" si tu negocio mueve dinero, un histogram para "tiempo de procesamiento de la operación crítica". Dimensiónalas por las variables que de verdad uses en alertas o segmentación (prioridad, tipo, región). No te excedas: cada dimensión multiplica la cardinalidad y eso encarece el storage.

Sobre **PII scrubbing**: aplícalo desde el primer log que tu app escribe. Es trivial al inicio y muy doloroso después. Si tu app procesa datos personales (emails, identificadores, tokens), el día que la auditoría de seguridad mire los logs no quieres descubrir que llevan dos años con valores en claro. Tres regex bien compiladas cubren la mayoría de casos; añade las que sean específicas de tu dominio (DNI, IBAN, números de teléfono internos).

Y sobre **alertas**: empieza con las tres del ejemplo y un Availability Test. Son la red de seguridad mínima. Añade alertas de negocio (custom metrics con umbrales) según vayas teniendo histórico de cómo se comporta tu app en normal. La regla práctica: si una alerta se dispara a menudo "por algo normal", es ruido y conviene ajustar el umbral; si nunca se dispara, probablemente está mal calibrada hacia arriba y no te avisará el día del incidente.

---

## 14. Comprueba que lo has entendido

1. La app está en producción dos meses, el cliente reporta que "el dashboard tarda mucho a veces". No tienes App Insights conectado. ¿Por qué tardarías días en diagnosticarlo y qué cambiaría con AI conectado desde el día uno? *(sección 2)*
2. ¿Para qué sirve la dimensión `priority` en `OrdersCreated.Add(1, priorityTag)`? Pon un ejemplo de alerta que se pueda construir gracias a esa dimensión y que sin ella no se podría. *(sección 6)*
3. ¿Cuándo conviene un Counter y cuándo un Histogram? Da un ejemplo de cada uno en tu dominio (no del ejemplo). *(sección 6)*
4. ¿Qué diferencia hay entre una alerta de `Http5xx > 5` y un Availability Test? ¿En qué situación detecta cada una un problema que la otra no? *(sección 9)*
5. Tu app loguea `"User payment failed: card 4111-1111-1111-1111 declined"`. ¿Qué ven los developers en App Insights y por qué es un problema? ¿Dónde se aplica el scrubber? *(sección 8)*
6. ¿Por qué el condicional `if (!string.IsNullOrEmpty(aiConnectionString))` rodea toda la configuración de OpenTelemetry? *(sección 5)*

<details>
<summary>Respuestas</summary>

1. Sin AI conectado, los datos no existen históricamente. Tienes que añadir la conexión, esperar a que los datos se acumulen unos días, e intentar reconstruir el patrón hacia atrás — que es imposible para datos anteriores a la conexión. Con AI desde el día uno, el patrón "el dashboard tarda más los lunes a las 9:00" salta a la vista en cuanto miras la métrica `requests/duration` segmentada por endpoint y por hora del día. La asimetría es brutal: conectar AI son cinco minutos al principio del proyecto; reconstruir el comportamiento histórico cuando no lo tenías es imposible. La diferencia se llama "datos retroactivos vs prospectivos": solo tienes los que has estado registrando.
2. La dimensión `priority` hace que la métrica `demo.orders.created` aparezca en App Insights como series separadas: `demo.orders.created{priority=low}`, `demo.orders.created{priority=high}`, etc. Permite alertas como "**`demo.orders.created{priority=high} < 1` en 5 min**", que detecta que dejan de entrar pedidos prioritarios sin que haya un error HTTP visible — la app está sirviendo bien pero los pedidos importantes no llegan. Sin la dimensión, la métrica es agregada y esa señal se diluye con los pedidos de prioridad normal.
3. **Counter**: cosas que solo suman a lo largo del tiempo. Ejemplos: emails enviados, registros nuevos, errores ocurridos, descargas iniciadas. **Histogram**: cosas con distribución, donde te interesan percentiles o cuartiles. Ejemplos: tiempo de respuesta de una API externa, tamaño de los uploads, número de items por pedido, latencia de la base de datos. Los percentiles (P50, P95, P99) del histogram te dan la cola larga: el P95 dice "el 95% de las peticiones es más rápido que X ms", que es lo que normalmente prometes en un SLA. Con counter solo no sabes la distribución.
4. **`Http5xx > 5`** mide errores que tu app reporta — peticiones que llegaron a tu código y devolvieron 500. **Availability Test** hace pings periódicos desde **fuera** de Azure (datacenters de AI multi-región). Detectan cosas distintas: un Http5xx detecta errores de aplicación (excepciones, bugs, dependencies caídas); un Availability Test detecta problemas que harían que ni siquiera llegues a tu app — DNS roto, certificado expirado, App Service caído, network blocking, política de firewall mal configurada. El Availability Test te avisa cuando tu app reporta sano pero nadie puede llegar a ella.
5. Los developers con acceso a App Insights ven el número de tarjeta completo en los logs durante todo el periodo de retención (típicamente 30-90 días, configurable). Es un problema porque (a) PCI DSS prohíbe almacenar números de tarjeta completos en logs operativos, (b) RGPD considera esos logs "datos personales bajo procesamiento", (c) cualquier developer con permiso de lectura puede ver tarjetas reales. El scrubber se aplica **antes** de pasar el mensaje al `ILogger` — en este ejemplo, en el handler del endpoint `/demo/log`. La regla práctica: scrubbing en el límite de salida, antes de que el dato cruce a un sistema persistente. En producción real conviene un wrapper del logger o un `ILoggerProvider` custom para que sea automático.
6. Porque permite que la misma base de código corra **en local sin Azure** sin tener que comentar/descomentar líneas. Si la connection string no está, OpenTelemetry no se inicializa y `ILogger` cae al provider de consola por defecto. Si está, OTel se activa y todo fluye a Azure Monitor. La transición es **una App Setting**, no un cambio de código. Es el patrón que permite que un desarrollador clone el repo, ejecute `dotnet run`, y empiece a trabajar sin necesidad de configurar ningún recurso de Azure. La telemetría se activa cuando llegas a un entorno donde sí tienes los recursos.

</details>

---

## 15. Hasta aquí

Con S2.5 cierras el módulo M02. Cinco submódulos: aprendiste a publicar (S2.1), a desplegar sin downtime (S2.2), a escalar bajo demanda (S2.3), a guardar secretos de forma segura (S2.4) y a ver lo que pasa cuando ya está en producción (S2.5). Cada uno añadió una pieza concreta a la app de S2.1 sin reescribirla.

A partir de aquí vienen **las prácticas del módulo**: [`S2.P`](../S2.P-practica-slots-swap/MANUAL.md) integra slots + swap multi-fase + canary deployment en un escenario completo; [`S2.P2`](../S2.P2-practica-deploy-basico/MANUAL.md) es la versión "concentrada" de los primeros pasos de despliegue (más útil cuando ya dominas los conceptos). Después de M02 cambia el paradigma — el módulo M03 entra en **Azure Functions** y el código empieza a ser distinto, pero los principios de configuración, monitorización y despliegue que has aprendido aquí siguen valiendo igual.
</thinking>

S2.5 listo. Sigo con las dos prácticas finales del módulo M02: **S2.P (slots+swap)** y **S2.P2 (deploy básico)**:

<function_calls>
<invoke name="Read">
<parameter name="file_path">c:\w\repos\F-003-Azure\examples\M02-App-Services\S2.P-practica-slots-swap\README.md