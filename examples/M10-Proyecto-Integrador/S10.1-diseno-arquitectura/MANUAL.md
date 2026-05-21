# Manual del alumno — S10.1 · Proyecto Integrador: diseño y arquitectura

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: mapeo a slides, endpoints, tests, flujo del alumno. Este manual va antes: te cuenta por qué este submódulo NO es una práctica más sino el examen final del curso, por qué la analogía de las pruebas de mar de un barco recién construido encaja con todas las decisiones del ejemplo, y dónde están las dos reglas que no se negocian (orden de bloques, cero connection strings con password).

Tiempo de lectura: ~25 min. Submódulo de referencia: [M10-S10.1](../../../doc/M10-Proyecto-Integrador/v3-actual/M10-S10.1-diseno-arquitectura-v3.md). Tres piezas de lógica pura (checklist de los 10 componentes con su estado, recomendador del bloque siguiente A → B → C → D y evaluador de la entrega con 8 criterios pesados que suman 100%) más un planificador con los 5 retos opcionales del slide 12.

*Creado: 2026-05-21 23:18 +0200*

---

## 1. La idea en una frase

Este no es un submódulo más; es el examen final del curso, donde los nueve módulos previos (App Services, Functions I, Functions II, almacenamiento, seguridad, MSIX, DevOps, IaC, IA) se reúnen en un sistema cloud completo que el alumno construye, despliega y monitoriza en 3 horas reales sobre Azure. Aquí no se modela el sistema en lógica pura porque el sistema real vive en Azure; lo que se modela son **las decisiones que el alumno tiene que tomar antes y durante la construcción**: qué componente toca ahora, en qué orden, cómo se evalúa la entrega, qué retos opcionales suben nota.

El alumno entrena dos disciplinas que no encajan en sí mismas pero juntas determinan la nota: **respetar el orden A → B → C → D de los bloques** (no se pueden hacer en paralelo ni saltar uno; sin infraestructura no hay API, sin API no hay Functions, sin Functions no hay pipeline que probar) y **eliminar todas las connection strings con password del sistema** (la regla de Managed Identity del slide 11 es binaria: si hay una sola password en el código o la config, el criterio `ManagedIdentityCero` falla y se pierden 10 puntos enteros).

---

## 2. El problema real que hay detrás

Tres situaciones que aparecen en cualquier proyecto integrador real de adopción cloud:

**Caso 1: el equipo que arrancó la API antes de tener Bicep.** Un equipo de tres developers se reparte el proyecto en paralelo: uno hace el Bicep, otra escribe los endpoints en local con SQLite "para ir avanzando" y el tercero monta el pipeline de GitHub Actions. El viernes intentan integrar y descubren tres incompatibilidades: el nombre del App Service en el Bicep no es el que el pipeline espera; la API local usa SQLite y migrarla a Cosmos pide cambios estructurales; el pipeline tiene smoke tests contra una URL que no se va a llamar así. Cuatro horas de integración perdidas. El recomendador del ejemplo no permite ese paralelismo: si `Bicep != Desplegado`, el bloque recomendado es **siempre A** con la justificación literal "Sin infra desplegada no hay donde meter la API ni las Functions. Bloque A primero (slide 5)". El orden no es un consejo; es una regla.

**Caso 2: la entrega con la connection string de Cosmos en `appsettings.json`.** Otra alumna llega al final del proyecto con todo funcionando: API responde 2xx, Functions procesan el Change Feed, el pipeline despliega a staging, las alertas están configuradas. Su evaluación parece un 95%. Pero al revisar `appsettings.json` aparece `"Cosmos": { "ConnectionString": "AccountEndpoint=...;AccountKey=ABC..." }` porque era más rápido durante el desarrollo. El criterio `ManagedIdentityCero` falla; pierde 10 puntos enteros. La diferencia entre 95% y 85% son veinte segundos de borrado del `appsettings.json` y diez minutos de configurar `DefaultAzureCredential()`. El evaluador del ejemplo lo coge antes de la entrega: `SinConnectionStringConPassword = false` da hallazgo crítico con el detalle "Cero connection strings con password en el código y la config (slide 11)".

**Caso 3: el sistema sin alertas que se considera "casi terminado".** Tercer equipo. Pipeline funcionando, smoke tests verdes, Application Insights conectado, dashboard del portal abierto con métricas. La conversación con el formador: "ya está casi todo, solo me faltan las alertas, las hago la semana que viene". La nota oficial: 90%. Una semana después, en un drift del staging, hay un pico de 5xx durante 20 minutos y nadie se entera porque nadie miraba el dashboard. El criterio `AppInsightsAlertas` del ejemplo dice literalmente "telemetría + al menos 2 alertas activas". Sin las dos alertas mínimas (5xx > 5 en 5 min, latencia avg > 2s en 10 min), el criterio falla. La regla del slide 10 es que un sistema sin alertas no es de producción; es un experimento.

Los tres casos los previene el ejemplo. `BloqueRecommender` evita el paralelismo del caso 1 forzando el orden; `EntregaEvaluator` con los pesos del slide 11 cuantifica el coste del caso 2; el criterio `AppInsightsAlertas` cierra el caso 3 como condición binaria, no opinable.

---

## 3. Por qué esto importa en tu stack

Si vas a hacer el proyecto integrador en serio (o vas a guiar a un alumno o equipo que lo hace), tres preguntas que conviene tener resueltas antes de abrir el Portal de Azure:

- **¿Por qué tengo que respetar el orden A → B → C → D si me siento productivo trabajando en paralelo?** Porque cada bloque produce un artefacto que el siguiente necesita como input. Bicep desplegado en A da los recursos a los que B se conecta (App Service URL, Cosmos endpoint, Key Vault URI). API funcionando en B da el endpoint que C consume vía Change Feed. Sistema completo en C es lo que D tiene que probar con su smoke test post-deploy. Saltarte el orden te pone a integrar piezas que no se conocen; respetarlo te da feedback de cada paso antes del siguiente.
- **¿Por qué Managed Identity tiene peso bajo (10%) si el formador insiste en que es innegociable?** Porque el peso refleja **proporción del trabajo**, no **importancia de la regla**. Configurar MI bien es 10 minutos del proyecto; configurar mal MI cuesta 10 puntos. Es una regla de eficiencia operativa: pequeño esfuerzo, alto coste si se incumple. Y aparece como criterio independiente precisamente porque es donde más alumnos pierden puntos sin enterarse.
- **¿Cómo sé cuándo el proyecto está realmente terminado?** Cuando los 8 criterios suman ≥ 70% Y los componentes que más pesan (`BicepDesplegado`, `ApiCrud`, `FunctionsChangeFeed`, `PipelineAutomatizado`) están cumplidos. Los 4 criterios de 15% suman 60%; con dos de 10% adicionales llegas al 80%. La aritmética del slide 11 está diseñada para que un sistema funcional básico apruebe (70%), uno sólido saque notable (80-89%) y uno con monitoring + MI completos saque sobresaliente (90%+).

Las tres preguntas se responden con tres endpoints. Sin las respuestas, el alumno entrega un sistema que cree completo y se sorprende con la nota.

---

## 4. La analogía vertebradora: las pruebas de mar de un barco recién construido

Un astillero entrega cada año una docena de buques nuevos. Cada uno tiene la misma estructura de cierre antes de que el armador lo acepte y se lo lleve al puerto base: las **pruebas de mar**. Son cuatro o cinco días de trabajo intenso con el barco ya construido, los motores ya instalados y los sistemas ya conectados; lo que se hace ahora es verificar que cada componente funciona y que el conjunto se mueve bajo carga real. Si el inspector de la sociedad de clasificación (Lloyd's Register, Bureau Veritas, DNV) firma el acta favorable, el barco zarpa. Si no firma, vuelve al muelle hasta corregir.

El barco tiene diez sistemas críticos que se inventarían cada vez: el casco con su estanqueidad, la propulsión con su motor principal, la dirección con su timón y bomba hidráulica, la navegación con radar y GPS, las comunicaciones con la VHF y el satélite, la seguridad SOLAS con chalecos y balsas, el sistema eléctrico con generadores y baterías, el sistema sanitario con tanques y bombas, el sistema contraincendios con detectores y CO2, y la cocina-galera con sus equipos refrigeradores. Cada uno con un estado: pendiente, en ensayos, validado. El alumno que mira esa lista entiende inmediatamente qué falta. Esa es la pieza `ArquitecturaChecklist` del ejemplo: diez componentes Azure (App Service, Functions, Cosmos, Service Bus, Entra ID, Key Vault, Managed Identity, App Insights, Bicep, Pipeline), cada uno con su estado y una frase que recuerda qué hace.

Las pruebas en el mar no se hacen en paralelo. Tienen un orden no negociable de cuatro fases. La primera, **estática en muelle**, verifica con el barco aún amarrado que los motores arrancan, las bombas circulan, la electricidad llega, las comunicaciones reciben. Sin estos sistemas funcionando en muelle, el barco no sale a navegar. La segunda, **salida y maniobras**, prueba el sistema de propulsión y dirección en aguas tranquilas a baja velocidad. La tercera, **pruebas a velocidad**, lleva el barco a su régimen máximo y verifica vibraciones, consumo, autonomía. La cuarta, **vuelta y aceptación final**, prueba las maniobras de atraque y se cierran las observaciones del inspector. Si una fase falla, se vuelve al muelle a corregir; no se salta a la siguiente. Esa es la pieza `BloqueRecommender` del ejemplo: cuatro bloques A → B → C → D con duración acumulada de 3 horas, cada uno con sus tareas concretas y la justificación literal de por qué va en ese orden.

Y el informe final del inspector de Lloyd's tiene **ocho criterios pesados** que se evalúan en la cubierta el último día. Tres son de 15% cada uno (estructura del casco, propulsión, sistemas críticos como pipeline en nuestra analogía), cuatro son de 10% (auxiliares, navegación, seguridad pasiva, sanitario), uno es de 15% (sistemas de servicio: contraincendios, eléctrico). Suman 100%. Por debajo de 70% el acta es desfavorable y el barco no zarpa; entre 70% y 80% se entrega con observaciones a corregir antes del primer viaje real; por encima de 80% se entrega limpio. Esa es la pieza `EntregaEvaluator` del ejemplo: ocho criterios canónicos del slide 11 con pesos 15/15/10/10/15/10/15/10, umbral 70%, evidencias binarias.

Mantén la imagen: astillero entregando un barco al armador después de cuatro fases de pruebas, inspector de clasificación firmando el acta con los ocho criterios pesados, diez sistemas críticos verificados. Toda la mecánica del submódulo encaja ahí.

---

## 5. Recorrido por el código: las tres piezas

### El checklist de los 10 componentes (`ArquitecturaChecklist.Inventariar`)

La pieza más mecánica del submódulo. Recibe un `EstadoSistema` con diez banderas `EstadoComponente` (Pendiente, EnProgreso, Desplegado) y devuelve la lista de los diez componentes con su estado actual y una descripción de su rol.

```csharp
new(Componente.AppServiceApi, sistema.AppService,
    "API REST con CRUD productos + crear pedidos + auth JWT (slide 4)"),
new(Componente.AzureFunctions, sistema.Functions,
    "Procesamiento async: Change Feed Cosmos → SB → notificaciones (slide 4/8)"),
new(Componente.CosmosDb, sistema.Cosmos,
    "Cosmos DB serverless con containers: pedidos, productos, analytics (slide 4)"),
```

El detalle pedagógico no está en el código sino en las descripciones: cada componente lleva **una frase que explica qué hace en este sistema concreto**, no qué hace en abstracto. El alumno que lee "Cosmos DB serverless con containers: pedidos, productos, analytics" entiende inmediatamente cómo se modela el dominio del proyecto integrador (tres containers, no uno; particionado por tipo de entidad). El que lee "Cosmos DB - base de datos NoSQL" se queda en la teoría.

Y el cálculo del porcentaje desplegado es deliberadamente estricto:

```csharp
public static int PorcentajeDesplegado(EstadoSistema sistema)
{
    var inv = Inventariar(sistema);
    int total = inv.Count;
    int desplegados = inv.Count(c => c.Estado == EstadoComponente.Desplegado);
    return total == 0 ? 0 : desplegados * 100 / total;
}
```

Solo cuenta `Desplegado`. `EnProgreso` cuenta como cero. Esto es importante operativamente: el alumno que tiene "todo en progreso" cree que va al 50%, pero el porcentaje le dice 0%. La regla operativa que entrena es **terminar lo empezado antes de abrir un componente nuevo**. Sin esa regla, los proyectos integradores se llenan de componentes a medio hacer y nada en producción.

### El recomendador del bloque siguiente (`BloqueRecommender.Recomendar`)

La pieza estratégica del submódulo. Recibe el `EstadoSistema` y devuelve una `RecomendacionBloque` con el bloque que toca, su duración estimada, las tareas concretas y la justificación de por qué va en ese orden.

La lógica es secuencial estricta:

```csharp
// Bloque A — infraestructura base.
if (sistema.Bicep != EstadoComponente.Desplegado)
    return new(
        Bloque: Bloque.A_Infraestructura,
        Duracion: "45 min",
        Tareas: [ /* ... */ ],
        Justificacion: "Sin infra desplegada no hay donde meter la API ni las " +
            "Functions. Bloque A primero (slide 5).");
```

Si Bicep no está desplegado, vuelve A. Punto. Aunque el alumno haya empezado a montar la API por su cuenta. El recomendador no le pregunta qué quiere hacer; le dice qué toca según el estado real del sistema.

El bloque B se considera completo solo cuando **cinco componentes** están desplegados a la vez:

```csharp
bool bloqueBOk =
    sistema.AppService == EstadoComponente.Desplegado
    && sistema.Cosmos == EstadoComponente.Desplegado
    && sistema.Entra == EstadoComponente.Desplegado
    && sistema.KeyVault == EstadoComponente.Desplegado
    && sistema.ManagedIdentity == EstadoComponente.Desplegado;
```

App Service, Cosmos, Entra ID, Key Vault y Managed Identity. La conjunción de los cinco es B; falta uno solo y sigue siendo B. Esto es importante porque el alumno tiende a marcar B como hecho cuando "ya tengo la API y Cosmos funcionando", olvidándose de la auth (Entra), el almacén de secretos (Key Vault) o la identidad (MI). Los cinco son uno mismo; si la API funciona pero usa connection strings con password, B no está hecho.

Y las tareas de cada bloque son **operacionales literales**, no consejos abstractos:

```csharp
Tareas:
[
    "`AddSingleton<CosmosClient>` con `DefaultAzureCredential()` — sin connection strings (slide 7).",
    "`AddMicrosoftIdentityWebApiAuthentication` para JWT de Entra ID (slide 7).",
    "`AddApplicationInsightsTelemetry()` (slide 7).",
    "Endpoints: `GET /api/productos` (filtra activos) y `POST /api/pedidos` " +
        "(extrae `sub` del JWT y persiste en Cosmos partition key `clienteId`) (slide 7).",
    "`GET /health` para los smoke tests del pipeline (slide 7).",
],
```

El alumno no recibe "implementa la auth"; recibe `AddMicrosoftIdentityWebApiAuthentication`. La diferencia es que la primera versión requiere buscar la documentación; la segunda se pega en el `Program.cs`.

### El evaluador de la entrega (`EntregaEvaluator.Evaluar`)

La pieza que cierra el bucle. Recibe una `EvidenciaEntrega` con ocho booleanos (uno por criterio) y devuelve un `InformeEntrega` con el porcentaje obtenido, la bandera `Aprobada` y la lista detallada de criterios.

Los ocho criterios y sus pesos están en una tupla canónica al inicio:

```csharp
private static readonly (Criterio C, int Peso, string Detalle)[] CriteriosPesados =
[
    (Criterio.BicepDesplegado, 15, /* ... */),
    (Criterio.ApiCrud, 15, /* ... */),
    (Criterio.AuthJwt, 10, /* ... */),
    (Criterio.CosmosPersistencia, 10, /* ... */),
    (Criterio.FunctionsChangeFeed, 15, /* ... */),
    (Criterio.ManagedIdentityCero, 10, /* ... */),
    (Criterio.PipelineAutomatizado, 15, /* ... */),
    (Criterio.AppInsightsAlertas, 10, /* ... */),
];
```

Suma: 15 + 15 + 10 + 10 + 15 + 10 + 15 + 10 = 100%. La aritmética está cuadrada al detalle.

La regla del aprobado es estricta y simple:

```csharp
return new InformeEntrega(
    PorcentajeTotal: total,
    Aprobada: total >= 70,        // umbral típico de proyecto integrador
    Criterios: resultados,
    PuntosPendientes: pendientes);
```

70% absoluto. No 7/10 criterios cumplidos. Si fallan los dos criterios de 15% que pesan más (`BicepDesplegado` + `ApiCrud`), aunque cumplas los seis restantes, sumas 60% y suspendes. La aritmética del slide 11 está diseñada precisamente para que **no se pueda aprobar sin tener la base del sistema funcionando**: infra + API + Functions + pipeline son los cuatro de 15% que suman 60%. Sin esos, no llegas al 70% pase lo que pase con los criterios de 10%.

Y los detalles de cada criterio son evidencias verificables, no opiniones:

```csharp
(Criterio.AuthJwt, 10,
    "Endpoint protegido devuelve 401 sin Bearer; 200 con JWT válido (slide 11)."),
(Criterio.ManagedIdentityCero, 10,
    "Cero connection strings con password en el código y la config (slide 11)."),
```

El alumno que evalúa su entrega no decide subjetivamente si "auth está bien"; comprueba si el endpoint devuelve 401 sin Bearer y 200 con JWT. Evidencia binaria, no debate.

---

## 6. La aritmética de la entrega: cómo se compone el 100%

Los 8 criterios y sus pesos tienen una estructura intencional. Vale la pena verla como tabla con sus implicaciones operativas:

| Criterio | Peso | Qué demuestra | Si lo pierdes |
| --- | --- | --- | --- |
| BicepDesplegado | 15 | Sabes hacer IaC reproducible | Suspendes seguro (60% techo) |
| ApiCrud | 15 | Tienes el dominio funcional | Suspendes seguro |
| FunctionsChangeFeed | 15 | Sabes hacer integración async | Suspendes seguro |
| PipelineAutomatizado | 15 | Tienes despliegue automatizado | Suspendes seguro |
| AuthJwt | 10 | Tienes seguridad básica | Bajas al rango justo (70-79) |
| CosmosPersistencia | 10 | Modelas datos correctamente | Bajas al rango justo |
| ManagedIdentityCero | 10 | Eliminaste passwords del sistema | Bajas al rango justo |
| AppInsightsAlertas | 10 | Sabes operar el sistema | Bajas al rango justo |

Dos lecturas operativas:

La primera es que **los cuatro criterios de 15%** son los pilares estructurales. Suman 60%; sin todos los cuatro no llegas a aprobar pase lo que pase con el resto. La regla operativa que se entrena es "asegura primero los cuatro pilares; los criterios de 10% son los que diferencian rangos de nota, no los que aprueban o suspenden".

La segunda es que **los cuatro criterios de 10%** modelan capacidades operativas más que estructurales. AuthJwt y CosmosPersistencia son configuración (cómo conectas y modelas); ManagedIdentityCero y AppInsightsAlertas son disciplina (qué eliminas y qué añades). Si tu equipo aprueba con los cuatro de 15% pero suspende los cuatro de 10%, el sistema funciona pero no es operable en producción. La nota refleja exactamente esa diferencia: 60% (no aprueba) vs 100% (sobresaliente).

---

## 7. La conversación con el formador: cómo defiendes la entrega

Si tu proyecto integrador se defiende ante un tribunal o un formador con criterio, vale la pena llegar con la evidencia preparada en el orden de los criterios. El patrón que entrega el ejemplo te permite hacer esa preparación en 30 minutos:

Primero, pasas tu sistema por `/diseno/arquitectura` con los estados reales. Capturas la lista de 10 componentes con su estado. Eso te da la foto fija: 10/10 desplegados, 9/10, etc. Si tienes algo en `EnProgreso`, lo cierras antes de la defensa o lo marcas como pendiente declarado.

Segundo, pasas las evidencias por `/diseno/entrega`. Para cada uno de los 8 criterios necesitas una evidencia visual o de log:

- `BicepDesplegadoConWhatIf`: captura del `az deployment group what-if` con sus cambios verdes y sin Delete inesperados.
- `ApiCrudDevuelve2xx`: captura del navegador o de Postman llamando a `GET /api/productos` con respuesta 200 y JSON.
- `JwtValidaConEntra`: dos capturas: 401 sin Bearer, 200 con Bearer obtenido del flow de Entra.
- `DatosPersistenEnCosmos`: captura del Data Explorer del Portal de Azure con el documento creado tras el POST.
- `ChangeFeedTriggerFunctions`: captura del log de la Function `DetectarPedido` con el mensaje del trigger y la publicación en Service Bus.
- `SinConnectionStringConPassword`: captura del `appsettings.json` y del Bicep mostrando que no hay credenciales en plano.
- `PipelineDesplegaAStaging`: captura del run del pipeline con todos los stages verdes y el smoke test post-deploy en verde.
- `AppInsightsTieneTelemetryYAlertas`: captura del Application Insights con la sección de alertas mostrando las dos reglas activas.

Tercero, el endpoint te devuelve la nota con los pendientes. Si está en 70-79% y quieres llegar a 85%, sabes exactamente qué criterio sumar. Si está por debajo de 70%, sabes qué arreglar antes de presentarte.

La diferencia con preparar la defensa "a ojo" es que el endpoint te da una nota auditable. Y el formador, al revisar tu evidencia contra los 8 criterios, llega a la misma nota que tú. Defender una entrega sin esa preparación es donde aparecen sorpresas del estilo "yo creía que cumplía Auth" cuando el formador comprueba que el endpoint no devolvía 401 sin Bearer.

---

## 8. Los 5 retos opcionales y cuándo plantearlos

La propiedad estática `RetosOpcionales` del planner expone los cinco retos del slide 12. No son obligatorios; son nota extra para quien tenga el sistema base ya cumpliendo el 80% como mínimo. Vale la pena entender qué demuestra cada uno:

| Reto | Lo que demuestra | Módulo origen |
| --- | --- | --- |
| 1: endpoint de búsqueda con filtros (fecha, importe, estado) | Sabes hacer queries no triviales en Cosmos | M05-S5.3 |
| 2: auto-update MSIX para una app desktop | Sabes integrar el lado cliente con el sistema | M07-S7.6 |
| 3: timer trigger que genera informe diario en Blob Storage | Sabes orquestar procesos programados | M03-S3.3 + M05-S5.1 |
| 4: usar Claude Code para generar uno de los componentes | Sabes operar con IA dentro del workflow | M09 |
| 5: canary deployment con feature flags | Sabes hacer despliegues progresivos | M08-S8.3 |

Tres lecturas operativas:

La primera es que **cada reto rescata un módulo concreto del curso**. El proyecto base aprueba sin tocar M05-S5.3, M07-S7.6, M09 ni M08-S8.3. Los retos te empujan a sacarlos al examen final. Es nota extra, sí, pero también es la oportunidad de demostrar que entendiste el curso entero, no solo los cuatro módulos que aparecen en el proyecto base.

La segunda es que **los retos no se hacen todos**. Elegir uno o dos bien ejecutados vale más que cinco hechos a medias. La regla operativa: si ya tienes 80% en la entrega base, elige el reto que más conecte con tu interés profesional (frontend → reto 2, datos → reto 3, IA → reto 4, DevOps avanzado → reto 5).

La tercera es que el **reto 4 (Claude Code) es especial** porque te permite usar IA para generar otro de los retos. Si vas a hacer el reto 3 (timer trigger), puedes usar Claude para generar el código del trigger y la documentación, y mencionarlo en la entrega como reto 4 al mismo tiempo. Dos retos del precio de uno, con disciplina del slide 13 del S9.5.

---

## 9. Cómo probarlo en local

Es un ejemplo offline al 100%. El sistema real lo construyes en Azure con CLI y Portal; este API te guía las decisiones.

```bash
dotnet run --project src/ProyectoIntegrador.Diseno.Demo.Api
# http://localhost:5120
```

Seis endpoints útiles:

```http
### Inventario de los 10 componentes con su estado
POST http://localhost:5120/diseno/arquitectura
Content-Type: application/json

{
  "bicep": "Desplegado",
  "appService": "Desplegado",
  "cosmos": "Desplegado",
  "entra": "EnProgreso",
  "keyVault": "Pendiente",
  "managedIdentity": "Pendiente",
  "functions": "Pendiente",
  "serviceBus": "Pendiente",
  "appInsights": "Pendiente",
  "pipeline": "Pendiente"
}
# → lista de 10 componentes; sólo 2 cuentan como Desplegado (Bicep+AppService)

### Porcentaje desplegado
POST http://localhost:5120/diseno/arquitectura/porcentaje
Content-Type: application/json
{ /* el mismo EstadoSistema */ }
# → { "porcentaje": 20 }

### Bloque siguiente según el estado actual
POST http://localhost:5120/diseno/bloque-siguiente
Content-Type: application/json
{ /* el mismo EstadoSistema */ }
# → Bloque B (porque Bicep está pero Cosmos/Entra/KV/MI faltan); 5 tareas;
#   justificación "Sin API funcional no hay nada para las Functions..."

### Evaluar la entrega contra los 8 criterios
POST http://localhost:5120/diseno/entrega
Content-Type: application/json

{
  "bicepDesplegadoConWhatIf": true,
  "apiCrudDevuelve2xx": true,
  "jwtValidaConEntra": true,
  "datosPersistenEnCosmos": true,
  "changeFeedTriggerFunctions": true,
  "sinConnectionStringConPassword": false,
  "pipelineDesplegaAStaging": true,
  "appInsightsTieneTelemetryYAlertas": false
}
# → porcentaje 80, aprobada=true, 2 pendientes (ManagedIdentityCero + AppInsightsAlertas)

### Retos opcionales
GET http://localhost:5120/diseno/retos
# → lista de 5 retos del slide 12

### Plan completo
POST http://localhost:5120/diseno/plan
Content-Type: application/json
{ "sistema": { ... }, "entrega": { ... } }
# → arquitectura + porcentaje + bloque + entrega + retos en una sola respuesta
```

Los 27 tests cubren:

- Capa 1 (unit): checklist con cada combinación (todo pendiente, todo desplegado, casos intermedios, EnProgreso que no cuenta); recomendador con cada estado del sistema y verificando el orden A → B → C → D → Terminado; evaluador con evidencias completas, parciales y verificando que los pesos suman exactamente 100%.
- Capa 0 (DI): `IProyectoIntegradorPlanner` como singleton del contenedor.
- Capa E2E: los seis endpoints via `WebApplicationFactory`.

No hay capa de integración porque el sistema real vive en Azure. Probarlo con Bicep desplegado contra una suscripción real es lo que pasa cuando haces el proyecto integrador de verdad.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 10. Anti-patterns

Cinco prácticas que evitar al abordar el proyecto integrador:

**Anti-pattern 1: arrancar en paralelo "para ir más rápido".** Es el caso 1 de la sección 2. La paralelización requiere contratos estables entre componentes; en un proyecto integrador de tres horas los contratos se descubren mientras construyes. El orden A → B → C → D no es burocracia; es la única forma de tener feedback continuo de cada paso. Cuatro horas de integración perdidas al final cuestan más que el tiempo aparente que ganas trabajando en paralelo.

**Anti-pattern 2: dejar Managed Identity para "después de tener todo funcionando".** Es el caso 2 de la sección 2. Configurar MI desde el principio (en el Bicep del bloque A y en el `Program.cs` del bloque B) cuesta cinco minutos por componente. Migrar a MI un sistema ya construido con connection strings cuesta cuatro veces más porque cada conexión ya tiene un patrón de configuración que hay que reescribir. La regla operativa: cero `Server=...Password=...` en ningún archivo desde el commit 1.

**Anti-pattern 3: tratar las 2 alertas del slide 10 como "decoración".** Es el caso 3 de la sección 2. Las alertas no son nice-to-have; son lo que distingue un experimento de un sistema operable. Y son las primeras 30 minutos de operación real una vez el sistema está en staging: configurar las dos alertas mínimas en el portal mientras el pipeline despliega es trabajo paralelo bien hecho, no ceremonia.

**Anti-pattern 4: marcar componentes como Desplegado cuando están EnProgreso.** El alumno que infla el estado se autoengaña. El recomendador del ejemplo solo cuenta `Desplegado`; el porcentaje del Portal solo cuenta recursos reales con sus métricas. Si tu Bicep "casi compila" o tu API "casi responde", está en EnProgreso, no en Desplegado. La diferencia operativa es horas de trabajo en la siguiente fase porque el siguiente bloque depende de tener este realmente cerrado.

**Anti-pattern 5: empezar los retos del slide 12 antes de cerrar la base al 80%.** Los retos son nota extra solo cuando el sistema base aprueba con margen. Antes del 80%, dedicar tiempo al timer trigger del reto 3 mientras tu pipeline no despliega es robar tiempo al criterio que más pesa para regalarlo a uno que ni siquiera puntúa si la base no está. La regla: 80%+ en la base, después un reto bien hecho.

---

## 11. Glosario breve

- **Proyecto integrador**: ejercicio final del curso F-003-Azure que junta los 9 módulos previos en un sistema cloud completo.
- **Componente** (de los 10): pieza individual del sistema (App Service, Functions, Cosmos, Service Bus, Entra ID, Key Vault, Managed Identity, App Insights, Bicep, Pipeline).
- **EstadoComponente**: tres valores (Pendiente, EnProgreso, Desplegado). Solo Desplegado cuenta en el porcentaje.
- **Bloque** (A/B/C/D): fase ordenada del proyecto, con duración estimada y tareas concretas. Orden no negociable.
- **Bloque A**: Infraestructura Bicep, 45 min. Sin él no hay donde meter nada.
- **Bloque B**: API + Cosmos + Auth + KV + MI, 60 min. Cinco componentes que se cierran juntos.
- **Bloque C**: Functions + Service Bus + Change Feed, 45 min. Procesamiento async sobre la API ya operativa.
- **Bloque D**: Pipeline + Monitoring + alertas, 30 min. Cierra el ciclo con despliegue automatizado y alertas mínimas.
- **Criterio** (de los 8): unidad de evaluación de la entrega con su peso (15 o 10) y su evidencia verificable.
- **Umbral 70%**: nota mínima para aprobar el proyecto integrador. Por debajo, suspenso aunque haya 7 de 8 criterios cumplidos.
- **`DefaultAzureCredential()`**: clase del SDK de Azure que detecta automáticamente la Managed Identity disponible. Reemplaza connection strings con password.
- **Change Feed** (Cosmos): mecanismo que dispara una función cada vez que cambia un documento. Núcleo del bloque C.
- **Smoke test post-deploy**: llamada `GET /health` tras el deploy automático para verificar que el sistema arrancó. Criterio del bloque D.
- **Recepción provisional** (analogía marítima): aceptación del barco con observaciones a corregir; equivalente a aprobado con menos del 80%.

---

## 12. Cierre

Si el sistema que entregues tiene los cuatro pilares cumplidos (Bicep desplegado, API CRUD funcionando, Functions con Change Feed, Pipeline automatizado), aprueba con 60%. Si añades las dos disciplinas (Managed Identity completa, alertas activas) y configuras bien Auth + Cosmos, sales con 100%. La diferencia entre 60% y 100% son aproximadamente cuarenta minutos de trabajo bien dirigido sobre un sistema que ya funciona. La curva del proyecto integrador es así: la base es la mayoría del esfuerzo; las disciplinas son lo que cierra la nota.

Lo siguiente del módulo es [`S10.P2 — Práctica mini-proyecto notas`](../S10.P2-practica-mini-proyecto-notas/MANUAL.md), una versión reducida del proyecto integrador (notas en Cosmos + API mínima + pipeline simple) que sirve para entrenar el flujo A → B → D antes del proyecto completo de tres horas. Si esta práctica del S10.1 es la entrega del barco al armador, la siguiente es el bote auxiliar que se prueba en el puerto antes.
