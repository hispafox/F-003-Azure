# Manual del alumno — S8.6 · Application Insights y Azure Monitor

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, scripts, despliegue por Portal. Este manual va antes: te cuenta qué es observabilidad realmente (métricas + logs + traces), por qué KQL es el idioma común que vale la pena aprender, qué alertas tener encendidas el día uno de producción y cuál es el runbook estándar cuando salta una.

Tiempo de lectura: ~25 min. Submódulo de teoría: [M08-S8.6](../../../doc/M08-DevOps-Automatizacion/v3-actual/M08-S8.6-app-insights-monitor-v3.md). Tres piezas de lógica pura (generador de queries KQL canónicas, recomendador de alertas con runbook, parser del shape de `az monitor app-insights query`) más un planificador.

*Creado: 2026-05-21 00:30 +0200*

---

## 1. La idea en una frase

Application Insights es la pieza de Azure que convierte tu sistema en algo **observable**: graba todas las peticiones HTTP, las dependencias (Cosmos, Service Bus, HttpClient externo), las excepciones, las trazas, los eventos custom. Tu código añade **una línea** (`builder.Services.AddApplicationInsightsTelemetry()`) y a partir de ahí cada operación queda registrada con un `operation_Id` que te permite seguir el rastro end-to-end. La conversación de S8.6: qué consultas KQL **canónicas** vas a ejecutar mil veces (P95 por endpoint, tasa de error 5xx, dependencias lentas, traza por operation_Id), qué **alertas mínimas** activar (5xx alta tasa, latencia > 2 s, excepciones no controladas), y cómo seguir el **runbook** estándar cuando la alerta salta (detectar → diagnosticar → mitigar → resolver → post-mortem).

---

## 2. El problema real que hay detrás

Tres situaciones que justifican observabilidad seria:

**Caso 1 — el incidente de "algo va lento".** Un equipo recibe el típico ticket: "los pedidos van lentos esta tarde". Sin Application Insights, esto es **cinco horas de debug**: alguien mira el código, hipótesis sobre la BD, alguien pide los logs (zip de 200 MB), alguien grep con awk. Con App Insights bien configurado, **15 segundos**: abre Application Map, ve que `dependencies` a Cosmos están en 4s de media, abre la query KQL `DependenciasLentas`, descubre que las llamadas a un container concreto se han disparado. La diferencia entre tener observabilidad y no tenerla es de un orden de magnitud.

**Caso 2 — el deploy que rompió el 3% del tráfico.** Un equipo desplegó una nueva versión. Smoke test pasó. CI verde. Tres horas después, **soporte recibe quejas** de algunos usuarios concretos. ¿Qué pasó? Sin observabilidad, "no sé, parece que funciona la mayoría". Con observabilidad: la query `TasaErrorPorHora` muestra que las 5xx subieron del 0.1% al 3% justo en el momento del deploy. Más concretamente, las 5xx vienen de un endpoint específico, llamado por un user-agent específico. Diagnóstico en cinco minutos. Sin observabilidad, te enteras del problema cuando ya es escándalo.

**Caso 3 — la factura sorpresa de 800 €/mes.** Un equipo activó App Insights "por defecto" sin sampling ni daily cap. Su app generaba 4 GB de logs al día, principalmente trazas verbosas de `/health` que pollea el load balancer cada 5 segundos. Al final del mes, la factura: **800 € de ingesta** (4 GB × 30 días × 2.30 €/GB ≈ 276 € en teoría, pero las trazas verbose multiplican). La solución: filtrar `/health`, activar sampling adaptativo, poner daily cap a 1 GB. La factura bajó a 70 €. La query `UsoEingestaPorTipo` del ejemplo te muestra dónde se gasta.

Los tres casos los aborda el ejemplo: KQL canónico que cubre el 90% de las consultas reales, alertas mínimas que detectan los incidentes antes que el cliente, y conciencia del coste con queries de ingesta.

---

## 3. Por qué esto importa en tu stack

Si tu app va a producción, **observabilidad no es opcional**. Tres preguntas que conviene tener resueltas el día del primer deploy:

- **¿Qué tengo que ver para saber si la app está sana?** P95 de latencia por endpoint, tasa de error 5xx por hora, excepciones agrupadas por tipo, dependencias con latencia alta. Las cuatro queries del ejemplo cubren la respuesta.
- **¿Cómo me entero antes que el cliente cuando algo va mal?** Alertas: 5xx > 5 en 5 minutos, latencia > 2 s, excepciones > 10 en 15 min. Action Group con email + Teams + PagerDuty (si hay on-call).
- **¿Qué hago cuando salta una alerta?** Runbook: detectar (Live Metrics), diagnosticar (Transaction Search por operation_Id), mitigar (rollback / feature flag), resolver (RCA), post-mortem.

Si tienes las tres respuestas, tu sistema es operable. Sin ellas, **vas a tener incidentes en los que descubres el problema cuando un cliente te llama**.

---

## 4. La analogía vertebradora: el monitor de constantes vitales

Imagina que llevas un paciente a la UCI. El equipo de enfermería conecta cuatro sondas básicas:

- **Pulso** (latencia P95): cómo va el flujo. Si sube, algo va mal.
- **Saturación de O₂** (tasa de error 5xx): qué porcentaje de funciones falla. Si baja del umbral, alerta.
- **Temperatura** (excepciones): si sube, hay infección. Si se mantiene elevada, hay que actuar.
- **Tensión arterial** (dependencias lentas): cómo responde el sistema circulatorio. Si las dependencias críticas (Cosmos, Service Bus, API externa) responden lento, el sistema entero sufre.

Y luego hay **alarmas** configuradas en el monitor:

- Si la temperatura sube de 38º (5xx > 5/5min), enfermera al lado del paciente.
- Si la saturación cae del 95% (latencia > 2s), llama al médico de guardia.
- Si la tensión se sale (excepciones > 10/15min), revisa qué tiene el paciente.

Cuando salta una alarma, no improvisa la enfermera. **Hay un protocolo escrito**:

1. **Detectar**: cuál es la alarma. Mirar el monitor en directo.
2. **Diagnosticar**: por qué. Revisar la historia clínica reciente (¿qué se le hizo en las últimas horas? ¿qué medicación se le puso?). En el sistema: ¿qué deploy se hizo? ¿qué cambió?
3. **Mitigar**: medidas inmediatas. Revertir el medicamento, oxígeno, etc. En el sistema: rollback del deploy, apagar feature flag, escalar instancias.
4. **Resolver**: encontrar la causa raíz y arreglar. Diagnóstico médico.
5. **Post-mortem**: documentar qué pasó por si vuelve a ocurrir.

Y la **Smart Detection** son las propias máquinas inteligentes que **detectan anomalías sin que las hayas configurado**: "este paciente tiene un patrón inusual de respiración, mírelo". Las tres clásicas en App Insights: Failure Anomalies (5xx fuera del baseline), Response Time degradation, Memory leak detection. **Activarlas es gratis y suelen detectar lo que no anticipaste**.

Mantén la imagen mientras lees el código: cuatro métricas vitales, alarmas con umbrales, runbook de cinco pasos, Smart Detection como sistema de respaldo.

---

## 5. Recorrido por el código

### `KqlQueryBuilder` — las cinco consultas que vas a ejecutar mil veces

KQL (Kusto Query Language) es el lenguaje de Azure Monitor y App Insights. Tiene su curva pero las consultas operativas más útiles caben en cinco plantillas que el ejemplo genera como texto listo para pegar:

**P95 por endpoint** — qué endpoints son los más lentos:

```kql
requests
| where timestamp > ago(24h)
| summarize p50=percentile(duration, 50),
            p95=percentile(duration, 95),
            p99=percentile(duration, 99),
            count_=count() by name
| where count_ > 100
| order by p95 desc
| take 10
```

Filtra endpoints con tráfico significativo (más de 100 requests) y los ordena por P95 descendente. El que aparece arriba es el primero que tienes que mirar.

**Tasa de error por hora** — chart temporal de 5xx vs total:

```kql
requests
| where timestamp > ago(7d)
| summarize total=count(),
            errores=countif(resultCode >= 500) by bin(timestamp, 1h)
| extend tasaError = round(errores * 100.0 / total, 2)
| where tasaError > 0
| render timechart
```

Te dice si hay un pico, una tendencia, o un patrón horario (por ejemplo, errores que aparecen siempre a las 3 de la madrugada coincidiendo con el job batch).

**Excepciones por tipo** — qué excepciones se repiten más:

```kql
exceptions
| where timestamp > ago(24h)
| summarize count_=count() by type, outerMessage
| order by count_ desc
```

Si ves `System.Threading.Tasks.TaskCanceledException` con miles de ocurrencias, hay timeouts. Si ves `Microsoft.Azure.Cosmos.CosmosException` con un código 429, te están throttleando.

**Dependencias lentas** — qué llamadas externas/BD son las que tardan:

```kql
dependencies
| where timestamp > ago(24h)
| where duration > 1000
| summarize avgDur=avg(duration), count_=count() by target, type, name
| order by avgDur desc
```

Si tu app llama a Cosmos, Service Bus, una API externa, esta query te dice cuál es la lenta. Caso 1 de la sección 2: la respuesta a "los pedidos van lentos" sale aquí.

**Traza por operation_Id** — el rastro completo de una operación:

```kql
union requests, dependencies, exceptions, traces
| where operation_Id == "abc-123-def-456"
| order by timestamp asc
```

Esta es **la query más útil de todas**. Cuando un cliente te dice "esta petición concreta falló a las 14:32:15", buscas el `operation_Id` en sus logs y ejecutas esta query. Te devuelve **todo lo que pasó en esa operación**: la request inicial, las llamadas a Cosmos, las llamadas a Service Bus, las trazas del logger, la excepción si hubo. **End-to-end en una sola query**. Es la magia del distributed tracing.

El generador inyecta el operation_Id de forma segura (escapando comillas) para evitar inyecciones triviales.

**Uso de ingesta por tipo** — control de costes:

```kql
Usage
| where TimeGenerated > ago(7d)
| summarize gb=sum(Quantity)/1000 by Solution, DataType
| extend eurEstimado = gb * 2.3
| order by eurEstimado desc
```

Te dice qué tipo de dato está consumiendo cuántos GB y cuánto cuesta. Caso 3 de la sección 2: identificar que `/health` está generando el 70% del coste.

### `AlertRecommender.Recomendar` — la batería mínima

La función genera tres alertas siempre + dos condicionales:

```csharp
public static IReadOnlyList<ReglaAlerta> Recomendar(EscenarioAlertas escenario)
{
    var sev5xx = escenario.ProductoConSlaContratado || escenario.TiempoRealCritico
        ? Sev0Critico : Sev1Alto;
    var sevLatencia = escenario.TiempoRealCritico ? Sev1Alto : Sev2Medio;

    var reglas = new List<ReglaAlerta>
    {
        new("5xx-alta-tasa", sev5xx, "count requests/failed > 5", "5m", "1m"),
        new("latencia-alta", sevLatencia, "avg requests/duration > 2000", "10m", "1m"),
        new("excepciones-no-controladas", Sev2Medio, "count exceptions/server > 10", "15m", "5m"),
    };

    if (escenario.ApiPublica)
        reglas.Add(new("pedidos-fallidos-query", Sev1Alto, "KQL scheduled-query", ...));

    if (escenario.ProductoConSlaContratado)
        reglas.Add(new("sla-availability", Sev0Critico, "AvailabilityPct < 99.9", "1d", "1h"));

    return reglas;
}
```

Tres alertas siempre + adaptación por contexto:

1. **5xx alta tasa**: 5 errores en 5 minutos. Severidad 1 (Alto) por defecto; **Sev 0 (Crítico)** si hay SLA contractual o tiempo real crítico (page inmediato).
2. **Latencia alta**: media de 2 segundos en 10 minutos. Severidad 2 (Medio); **Sev 1 (Alto)** si tiempo real crítico.
3. **Excepciones no controladas**: 10 en 15 minutos. Severidad 2 (Medio).

Y las condicionales:

- **`pedidos-fallidos-query`** (API pública): alerta scheduled-query basada en KQL. Permite condiciones más complejas que las métricas estándar (por ejemplo, filtrar por endpoint específico antes de contar 5xx).
- **`sla-availability`** (SLA contratado): alerta diaria si la disponibilidad cae del 99.9%. **Severidad 0 — Crítico**. Si tu producto vende SLA, esta alerta es vinculante contractualmente.

Y dos cosas que vienen de regalo:

```csharp
public static IReadOnlyList<string> SmartDetectionRecomendada { get; } =
[
    "Failure Anomalies (5xx fuera del baseline)",
    "Response Time degradation",
    "Memory leak detection",
    "Dependency failure (API externa o DB que empieza a fallar)",
    "Security: anomalías de tráfico, intentos SQLi",
];
```

**Smart Detection es gratis** en App Insights. Lo activas en Portal → Smart Detection. Detecta anomalías que no anticipaste con tus alertas manuales. Tres clásicas: failure anomalies, response time degradation, memory leak. Más dos útiles: dependency failure y security.

Y el runbook:

```csharp
public static IReadOnlyList<string> Runbook { get; } =
[
    "DETECTAR (0-2 min): Live Metrics + Failures → ¿qué endpoint?",
    "DIAGNOSTICAR (2-10 min): Transaction Search por operation_Id; ¿deploy reciente?",
    "MITIGAR (10-20 min): rollback (swap) / escalar / feature flag OFF",
    "RESOLVER: RCA + fix + tests; actualiza runbook si es escenario nuevo",
    "POST-MORTEM: documentar qué pasó y acción preventiva",
];
```

Cinco pasos con tiempos esperados. Si en 2 minutos no has detectado, hay un problema con tu observabilidad (no estás viendo lo que pasa). Si en 10 no has diagnosticado, hay un problema con tu instrumentación (los logs no te dicen lo suficiente). Si en 20 no has mitigado, tu sistema no tiene rollback automatizado (que es lo que viste en S8.3).

### `MonitorResponseParser.Parsear` — el shape del `az monitor app-insights query`

Cuando ejecutas una query KQL desde `az monitor app-insights query`, el resultado viene en un JSON con estructura `tables[].rows[]`. El parser convierte ese shape al modelo que esperas:

```json
{
  "tables": [
    {
      "name": "PrimaryResult",
      "columns": [
        { "name": "name", "type": "string" },
        { "name": "p95", "type": "real" },
        { "name": "count_", "type": "long" }
      ],
      "rows": [
        ["GET /api/pedidos", 1500.5, 1234],
        ["POST /api/cobros", 800.2, 567]
      ]
    }
  ]
}
```

El parser:

- Localiza la tabla `PrimaryResult` (con búsqueda case-insensitive).
- Mapea cada `column.type` al tipo de C# adecuado (`real` → `double`, `long` → `Int64`, `string` → string, etc.).
- Devuelve filas tipadas con los valores correctos.

Útil cuando construyes un dashboard custom o un script que reacciona a queries KQL: en lugar de parsear JSON a mano, le pasas el resultado al parser y trabajas con objetos.

### `AppInsightsPlanner` — el plan + checklist

El servicio inyectable. Compone: queries canónicas listas para pegar, alertas recomendadas con severidades adaptadas, Smart Detection activable, runbook, checklist del entregable (workspace-based, sampling, daily cap, Application Map pinned, Workbook con dashboard de producción).

---

## 6. La regla operativa: workspace-based, no Classic (slide 13/23)

Una decisión histórica que conviene tener clara: hasta 2020 App Insights era un recurso aparte (Classic). Desde 2024, **Workspace-based es obligatorio** para crear App Insights nuevos. Classic ya no se ofrece en el wizard.

Por qué importa:

- **Workspace-based** integra App Insights con **Log Analytics Workspace**. Es decir, los datos van al mismo workspace donde pueden estar otros recursos (VMs, AKS, otros recursos de Azure). Esto permite hacer cross-queries: una query KQL que une telemetría de tu app con métricas de la VM. Útil cuando el problema es de infraestructura, no de código.
- **Classic** mantenía los datos en un silo separado. Era más simple pero menos potente. Microsoft lo va a retirar progresivamente.

Si te encuentras un App Insights Classic en un proyecto existente, **migra** (Portal lo permite con un botón). No es obligatorio inmediato, pero la dirección está clara.

---

## 7. El control del coste (slide 12, 16, 20)

App Insights se cobra **por GB ingestados**. A 2.30 €/GB, una app con 10 GB/mes son 23 €/mes, asumible. Una app con 100 GB/mes son 230 €. Una app sin sampling, con `/health` cada 5 segundos y trazas verbose, puede llegar a 800 €/mes fácilmente (caso 3 de la sección 2).

Tres técnicas para controlarlo:

1. **Sampling adaptativo** (slide 12): habilítalo en `appsettings.json`. La librería de App Insights envía solo un porcentaje de las trazas según la carga. En tráfico bajo, envía todo; en tráfico alto, muestrea. Te mantiene precisión estadística reduciendo el coste.
2. **Filtrar telemetría no útil**: `/health` se llama cada pocos segundos por el load balancer; no necesitas guardarlo. Implementa un `ITelemetryProcessor` que descarte requests a `/health` (y a otros endpoints de monitoring). Reduce trafico significativo.
3. **Daily cap** (slide 20): Portal → App Insights → Usage and estimated costs → Daily cap. Pon un límite (p.ej. 5 GB/día). Cuando se alcanza, App Insights **deja de ingestar el resto del día** (los datos se siguen generando pero no se guardan). Es la red de seguridad ante un deploy con telemetría descontrolada.

Las tres técnicas combinadas mantienen el coste por debajo de 50 €/mes para apps de producción razonables.

---

## 8. Cómo probarlo en local

```bash
dotnet run --project src/Monitor.AppInsights.Demo.Api
# http://localhost:5110
```

Endpoints:

```http
### Generar query P95 por endpoint
GET http://localhost:5110/monitor/kql/p95?ventana=24h&minimoTrafico=100

### Generar query traza por operation_Id
GET http://localhost:5110/monitor/kql/traza?operationId=abc-123-def

### Recomendar alertas
POST http://localhost:5110/monitor/alertas/recomendar
Content-Type: application/json

{
  "apiPublica": true,
  "tiempoRealCritico": false,
  "productoConSlaContratado": true,
  "emailEquipo": "ops@empresa.com",
  "webhookTeams": "https://outlook.office.com/webhook/..."
}
# → 5 alertas (3 base + scheduled-query + sla-availability)
#   con severidades Sev0 en 5xx por el SLA contratado

### Listar Smart Detection recomendado
GET http://localhost:5110/monitor/alertas/smart-detection

### Listar runbook
GET http://localhost:5110/monitor/alertas/runbook

### Parsear respuesta de az monitor app-insights query
POST http://localhost:5110/monitor/respuesta/parsear
Content-Type: application/json

{ "tables": [ ... ] }

### Plan completo
POST http://localhost:5110/monitor/plan
```

Los 35 tests cubren cada generador de KQL (con ventanas distintas, umbrales, escapado de comillas), todas las combinaciones del recomendador de alertas, el parser con casos límite (tipo `long`/`real`, case-insensitive, sin `tables`).

Para ejecutar KQL contra un App Insights real:

```bash
./scripts/demo.sh
# 1) 01-query-kql.sh    → az monitor app-insights query (P95, errores, deps)
# 2) 02-alertas-listar.sh → metric alerts + scheduled-query + action groups
```

Solo lectura. Requiere `az` con la extensión `application-insights` (la instala automáticamente la primera vez).

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 9. La línea que se mete en `Program.cs`

Toda la observabilidad de App Insights en .NET se cabea con una línea (slide 3):

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

Y en la App Setting de App Service:

```
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=...;IngestionEndpoint=https://...
```

A partir de ahí, automáticamente se trackean:

- **Requests** (cada petición HTTP que tu app recibe).
- **Dependencies** (cada llamada a HttpClient, EF Core, Cosmos SDK, Service Bus SDK).
- **Exceptions** (cualquier excepción no controlada que llega al middleware de errores).
- **Traces** (los logs que pasas por `ILogger<T>`).

Para telemetría custom (slide 4), añades:

```csharp
private readonly TelemetryClient _telemetry;

public CheckoutService(TelemetryClient telemetry) { _telemetry = telemetry; }

public void Procesar(Pedido pedido)
{
    _telemetry.TrackEvent("PedidoProcesado", new Dictionary<string, string>
    {
        ["clienteId"] = pedido.ClienteId,
        ["total"] = pedido.Total.ToString(),
    });

    _telemetry.GetMetric("PedidosProcesados").TrackValue(1);

    using var op = _telemetry.StartOperation<RequestTelemetry>("ProcesarPago");
    // ... lógica
    // op se completa al disponer y se sube como dependency
}
```

Tres clases de telemetría custom:

- **`TrackEvent`**: eventos de negocio. "Pedido procesado", "Usuario registrado", "Pago confirmado".
- **`GetMetric`**: métricas numéricas con dimensiones. "Pedidos procesados por día y por país".
- **`StartOperation`**: traza una operación lógica con su propio `operation_Id`. Útil para trabajo en background sin request HTTP.

---

## 10. Los anti-patterns del módulo

Cuatro prácticas que evitar:

**Anti-pattern 1 — App Insights Classic en proyectos nuevos**. Microsoft lo va a retirar; siempre Workspace-based desde 2024.

**Anti-pattern 2 — Sin sampling ni daily cap**. El caso 3: factura sorpresa. Sampling adaptativo + daily cap + filtrar `/health` reduce el coste 5-10×.

**Anti-pattern 3 — Solo email en Action Groups**. Si tu sistema tiene tráfico real, el email se pierde entre la avalancha. Action Group con Teams (webhook) o PagerDuty para on-call serio.

**Anti-pattern 4 — Sin runbook escrito**. Cuando la alerta salta a las 3 de la madrugada, el dev de guardia no debe inventarse el procedimiento. Los 5 pasos del slide 21 son el mínimo. Documenta los específicos de tu sistema (qué servicio se reinicia con qué comando, qué feature flag tiene qué efecto).

---

## 11. Glosario breve

- **Application Insights**: producto de Azure para observabilidad de apps. Captura requests, dependencies, exceptions, traces.
- **Azure Monitor**: producto más amplio que cubre métricas de infraestructura. App Insights es parte de él.
- **Log Analytics Workspace**: el almacén común de logs/métricas de Azure. App Insights Workspace-based va aquí.
- **KQL** (Kusto Query Language): el SQL-like de Azure Monitor. Sintaxis tipo pipe (`| where`, `| summarize`).
- **`operation_Id`**: ID de correlación que une todos los eventos de una operación (request + dependencies + traces + exceptions).
- **Smart Detection**: detección automática de anomalías por ML que viene gratis con App Insights.
- **Action Group**: configuración de "qué hacer cuando salta una alerta" (email, SMS, Teams, PagerDuty, webhook).
- **Metric alert**: alerta basada en una métrica predefinida (count, avg, percentile).
- **Scheduled-query alert**: alerta basada en una query KQL custom. Más flexible.
- **Application Map**: visualización del Portal que muestra todos los componentes de tu sistema y las dependencias entre ellos.
- **Live Metrics**: vista en tiempo real (segundos) de lo que está pasando ahora. Útil durante incidentes.
- **Transaction Search**: búsqueda libre de telemetría por filtros (operation_Id, user, fecha...).
- **Workbook**: dashboard de Azure compuesto de queries KQL + visualizaciones. Más rico que un dashboard normal.
- **Sampling adaptativo**: técnica para muestrear telemetría sin perder precisión estadística, reduce coste.
- **Daily cap**: límite máximo de ingesta diaria. Red de seguridad anti-factura sorpresa.

---

## 12. Cierre

S8.6 te da los tres bloques operativos de la observabilidad en Azure: las cinco queries KQL que vas a ejecutar mil veces (P95, tasa error, excepciones, dependencias, traza por operation_Id), la batería de alertas mínima (5xx, latencia, excepciones, + SLA si aplica), y el runbook de cinco pasos para responder cuando salta. Plus la conciencia del coste con sampling adaptativo + daily cap + filtrado de `/health`.

Lo siguiente es [`S8.P — Práctica Pipeline CI/CD`](../S8.P-practica-pipeline-cicd/MANUAL.md), donde se integra todo lo del módulo (Repos+Boards, Pipelines YAML, despliegue automatizado, IaC con Bicep, observabilidad) en un pipeline end-to-end real.
