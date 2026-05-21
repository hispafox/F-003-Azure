# Manual del alumno — S10.P2 · Práctica: mini-proyecto Notas

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: mapeo a slides, endpoints, tests, flujo del alumno. Este manual va antes: te cuenta por qué construir un sistema pequeño completo enseña más que empezar el grande y atascarte, por qué la analogía del cortometraje antes del largometraje encaja con el alcance recortado consciente, y dónde está el detalle que separa esta mini-práctica del proyecto integrador del S10.1.

Tiempo de lectura: ~22 min. Submódulo de referencia: [M10-S10.P2](../../../doc/M10-Proyecto-Integrador/v3-actual/M10-S10.P2-practica-mini-proyecto-notas-v1.md). Tres piezas de lógica pura (preflight ligero con tres bloqueantes y cinco avisos, evaluador de los 11 pasos del end-to-end y comparador de alcance Mini vs Completo) más un planificador con el camino de 7 pasos para escalar de esta práctica al proyecto integrador grande.

*Creado: 2026-05-21 23:53 +0200*

---

## 1. La idea en una frase

Esta práctica construye un sistema cloud completo de verdad (Web App F1 + Table Storage + 5 endpoints CRUD + deploy real + smoke test contra URL pública) en 60-75 minutos, deliberadamente recortado: sin auth, sin Functions, sin Service Bus, sin pipeline complejo. No es un juguete ni una simulación; es Azure real desplegado, solo que con tres capas en lugar de diez. El objetivo no es que el alumno aprenda Table Storage; es que **vea el ciclo completo end-to-end funcionando** (modelar, codear, testear, desplegar, validar, limpiar) antes de enfrentarse al proyecto integrador grande del S10.1, donde el mismo ciclo lleva diez componentes y tres horas.

El alumno entrena dos decisiones que llevará al proyecto grande: **elegir el alcance conscientemente** (el comparador del ejemplo decide Mini / Completo / EmpezarPorMini según tu objetivo y tiempo, no te deja arrancar el grande sin pensar si necesitas sus diez componentes) y **cerrar siempre con el cleanup** (`az group delete` es el paso 11, no opcional; un sistema de práctica que se queda corriendo en Azure genera factura sorpresa, exactamente el anti-pattern que el curso entero ha estado evitando).

---

## 2. El problema real que hay detrás

Tres situaciones que aparecen en cualquier alumno que se enfrenta al proyecto integrador del curso:

**Caso 1: el alumno que empezó por el grande y se atascó en la hora 2 sin nada desplegado.** Un alumno motivado abre el proyecto integrador S10.1 directamente. Empieza por el Bicep de los diez componentes, se pelea con la sintaxis de los módulos de Service Bus, intenta configurar Entra ID, y a las dos horas tiene mucho YAML pero nada desplegado y funcionando. Se frustra porque "no ve nada en pantalla". El comparador del ejemplo se lo habría dicho antes: si tu objetivo es ver un end-to-end mínimo y validar que dominas el ciclo, empieza por la mini-práctica (`Recomendacion.Mini`), que en 60 minutos te da una URL pública respondiendo. El proyecto grande tiene sentido cuando ya has visto el ciclo pequeño funcionar; arrancarlo en frío es la receta del atasco.

**Caso 2: la práctica que se quedó corriendo en Azure tres semanas.** Otra alumna hace el mini-proyecto perfecto: Web App desplegada, smoke test verde, todo funcionando. Cierra el portátil satisfecha. Tres semanas después le llega el aviso de consumo de Azure: el plan F1 es gratis, pero la Storage Account y los recursos asociados llevan 21 días generando microcoste, y lo que es peor, ha dejado un recurso de prueba con datos de ejemplo accesible públicamente. El paso 11 del ejemplo es exactamente esto: `Limpiar` con `az group delete --name <rg> --yes --no-wait` y la verificación de que `az group exists` devuelve `false`. El cleanup no es una cortesía; es la diferencia entre una práctica gratis y una factura sorpresa.

**Caso 3: el alumno que quería "hacerlo bien" y metió auth en la mini-práctica.** Tercer alumno. Hace la mini-práctica pero decide "ya que estoy, le añado autenticación con Entra ID para que sea más completa". Tarda 90 minutos solo en la auth, la mini-práctica de 60 minutos se convierte en tres horas, y al final tiene un sistema a medio camino entre el mini y el grande sin la coherencia de ninguno. El alcance recortado del ejemplo es **consciente y documentado**: las 5 features incluidas (Web App, Persistencia, CRUD, Tests, Deploy) y las 9 explícitamente fuera (Auth, Key Vault, Service Bus, Functions, Cosmos, Pipeline, App Insights, Slots, Managed Identity). Si quieres auth, ese es el proyecto grande, no este. El camino para añadirla está en `CaminoHaciaS101`, paso a paso, no improvisado en mitad de la mini-práctica.

Los tres casos los previene el ejemplo. `AlcanceComparator` evita el caso 1 recomendando empezar por lo pequeño; el paso `Limpiar` del `PasoChecker` cierra el caso 2; el alcance recortado consciente más el `CaminoHaciaS101` ordenado evitan el caso 3.

---

## 3. Por qué esto importa en tu stack

Si vas a hacer el proyecto integrador del curso (o guías a alguien que lo hace), tres preguntas que conviene resolver antes de decidir por dónde empezar:

- **¿Empiezo por el mini-proyecto o por el grande?** El comparador del ejemplo lo decide por tu objetivo real. Si quieres validar que dominas el ciclo end-to-end en menos de una hora, Mini. Si necesitas un sistema con auth, Functions y pipeline para algo de producción, Completo (vete a S10.1). Si tienes tiempo de ambas, EmpezarPorMini (haz el pequeño, luego añade capas). La decisión no es de gusto; es de objetivo y tiempo disponible.
- **¿Qué herramientas necesito sí o sí?** El preflight separa por gravedad: tres son **Bloqueante** (.NET 8 SDK, Azure CLI autenticada, `curl` para los smoke tests) y cinco son **Aviso** (`jq` para parsear JSON, `git`, y el conocimiento previo de M01, M02 y M05). La diferencia es operativa: sin lo Bloqueante no arrancas; los avisos son cosas que te facilitan la vida o que puedes repasar si te atascas.
- **¿Cómo escalo del mini al proyecto grande sin rehacer todo?** El `CaminoHaciaS101` del ejemplo da los 7 pasos aditivos: Table → Cosmos, añadir Entra, secretos a Key Vault con Managed Identity, Service Bus + Functions, pipeline con OIDC y slots, Application Insights con alertas, y finalmente autoevaluar con el `EntregaEvaluator` del S10.1. Cada paso añade una capa sobre el sistema que ya funciona; ninguno te obliga a empezar de cero.

Con las tres respuestas claras, el alumno arranca por donde le conviene y escala de forma ordenada. Sin ellas, o se atasca en el grande o improvisa un híbrido incoherente.

---

## 4. La analogía vertebradora: el cortometraje antes del largometraje

Un director novel quiere hacer su primera película larga. Antes de pedir financiación para un largometraje de 90 minutos con equipo de cincuenta personas, rueda un cortometraje de 12 minutos. El corto no es una maqueta ni un ensayo a media máquina: es cine de verdad, con guion cerrado, actores reales, rodaje en localización, montaje, sonido, etalonaje de color y proyección en un festival. Solo que a escala reducida. El director que ha sacado un corto adelante ha pasado por **todas las fases de hacer cine**, y eso es lo que demuestra a los productores: no que sabe rodar planos bonitos, sino que sabe llevar un proyecto audiovisual de la idea a la pantalla, cerrándolo.

El mini-proyecto Notas es ese cortometraje. Tiene las tres capas reales de cualquier sistema cloud (Web App + persistencia + deploy), se despliega en Azure de verdad, responde en una URL pública de verdad, y se valida con smoke tests de verdad. No es un sistema de juguete; es un sistema real pequeño. El que lo ha sacado adelante ha pasado por el ciclo completo: modelar el dominio, montar la solución, implementar, testear, desplegar, validar end-to-end, limpiar. Eso es lo que entrena para el proyecto grande del S10.1, que es el largometraje: las mismas fases, más equipo, más presupuesto, más localizaciones.

Antes de rodar, el director hace su lista de material mínimo: cámara, sonido, iluminación básica, una localización, los actores. Si falta la cámara o el sonido, no se rueda; son bloqueantes. Si falta el segundo foco de relleno o el director de fotografía tiene poca experiencia, se rueda igual con limitaciones. Esa es la diferencia entre Bloqueante y Aviso del preflight: .NET 8 SDK, Azure CLI y `curl` son la cámara y el sonido; `jq`, `git` y el repaso de M01/M02/M05 son el segundo foco y la experiencia previa. La película mínima necesita los tres primeros; los otros la mejoran.

Las once fases del corto no se hacen en cualquier orden. Primero el guion (diseñar el modelo `Note`), luego el plan de producción (crear la solución), después la preparación de los elementos (implementar modelo y repositorio), el rodaje (los endpoints), las pruebas de cámara (tests unitarios), el visionado de los brutos (smoke tests locales), la preparación del set real (infra Azure), el rodaje en localización (deploy), el pase privado (validación end-to-end), y el desmontaje del set (cleanup). Ese último paso es el que los novatos olvidan: cuando terminas de rodar, devuelves el equipo alquilado y liberas la localización, porque cada día de más que tengas la cámara cuesta dinero. En Azure es idéntico: `az group delete` libera los recursos para que no sigan facturando. El director que se deja el set montado paga de más; el alumno que se deja el Resource Group corriendo, también.

Y hay una decisión previa a todo: ¿corto o largo? El director con una historia íntima y poco presupuesto rueda un corto. El que tiene una saga épica y financiación va directo al largo. El que está empezando y tiene tiempo, rueda primero el corto para aprender y luego escala a largo con lo aprendido. Esa es la decisión del `AlcanceComparator`: Mini para el end-to-end rápido, Completo para el proyecto de producción, EmpezarPorMini para quien quiere validar antes de escalar.

Mantén la imagen: director rodando su cortometraje antes del largo, material mínimo bloqueante vs deseable, once fases en orden, desmontaje del set como paso final no negociable. Toda la mecánica del submódulo encaja ahí.

---

## 5. Recorrido por el código: las tres piezas

### El preflight ligero (`MiniNotasPreflight.Comprobar`)

La pieza más sencilla. Recibe un `EscenarioPreflight` con ocho banderas y devuelve los hallazgos clasificados con la bandera `ListoParaArrancar`.

Lo interesante es la decisión de qué es Bloqueante y qué es Aviso. Las herramientas son bloqueantes:

```csharp
Check(e.TieneDotNet8SDK,
    ".NET 8 SDK instalado",
    "Necesario para `dotnet new webapi` y `dotnet test` (slide 3). " +
    "Instala desde https://dotnet.microsoft.com.",
    NivelPreflight.Bloqueante),
```

Pero el conocimiento previo de módulos anteriores es solo aviso:

```csharp
Check(e.HizoM05,
    "Conocimiento previo M05 (persistencia: Cosmos o Table)",
    "Esta práctica usa Table Storage. Si no recuerdas el modelo " +
    "PartitionKey/RowKey, repasa M05-S5.P2.",
    NivelPreflight.Aviso),
```

La distinción es deliberada y pedagógica: sin .NET SDK no puedes ni empezar (Bloqueante), pero si no recuerdas bien el modelo PartitionKey/RowKey de Table Storage, puedes ir consultando el slide de M05 mientras avanzas (Aviso). El preflight no te castiga por no recordar la teoría; te castiga por no tener las herramientas. Esa diferencia respeta cómo se aprende de verdad: la teoría se repasa sobre la marcha, las herramientas tienen que estar antes.

Un detalle de honestidad técnica: el preflight comprueba **.NET 8 SDK**, no .NET 10. El proyecto demo de este ejemplo (el API de heurísticas que estás leyendo) corre en `net10.0`, pero la mini-app que el alumno construye en Azure usa .NET 8 porque es lo que cubre el slide lectivo y lo que el runtime de App Service `DOTNETCORE:8.0` espera en la práctica. Son dos cosas distintas: el andamiaje de la práctica (net10) y el sistema que el alumno despliega (.NET 8).

### El evaluador de los 11 pasos (`PasoChecker.Evaluar`)

La pieza con el ritmo de la práctica. Misma estructura que los evaluadores de S9.P y S9.P2: dos flags por paso (`ComandoEjecutado`, `OutputEsperadoVisible`), tres veredictos (Pasa, Falla, Pendiente).

```csharp
ResultadoPaso resultado;
if (acciones.Count == 0)
{
    resultado = ResultadoPaso.Pasa;
    acciones.Add($"Paso {e.Paso} completado (slide {slide}).");
}
else if (!e.ComandoEjecutado && !e.OutputEsperadoVisible)
{
    resultado = ResultadoPaso.Falla;
}
else
{
    resultado = ResultadoPaso.Pendiente;
}
```

Lo más útil son las sugerencias específicas, que aquí son **comandos `az` y `dotnet` literales**, no consejos. Para crear la solución:

```csharp
Paso.CrearSolucion =>
    "`dotnet new sln` + `dotnet new webapi -o src/MiniNotas` + " +
    "`dotnet new xunit -o tests/MiniNotas.Tests` + `dotnet sln add` (slide 5).",
```

Para crear la infraestructura:

```csharp
Paso.CrearInfra =>
    "`az group create` + `az storage account create` + `az appservice plan create --sku F1` + " +
    "`az webapp create --runtime DOTNETCORE:8.0` (slide 11).",
```

Y el detalle pedagógico más fino está en las sugerencias del output esperado, que enseñan a **reconocer el éxito**, no solo a ejecutar. Para el deploy:

```csharp
Paso.DesplegarApp =>
    "Tras `az webapp deploy`, `az webapp log tail` muestra el banner de Minimal API arrancando. " +
    "Si tarda, esperar 30-60 s al cold start del F1.",
```

Ese "esperar 30-60 s al cold start del F1" es oro para un alumno nuevo: el plan F1 gratis tiene cold start lento, y sin ese aviso el alumno cree que el deploy falló cuando solo está arrancando. La sugerencia le ahorra el falso fallo.

El paso final, `Limpiar`, es el que cierra el ciclo:

```csharp
Paso.Limpiar =>
    "`az group delete --name <rg> --yes --no-wait` (slide 14). Verifica con " +
    "`az group exists` que devuelve `false`.",
```

No basta con lanzar el delete; hay que verificar que el Resource Group ya no existe. Esa verificación es la diferencia entre creer que limpiaste y haber limpiado de verdad.

### El comparador de alcance (`AlcanceComparator.Comparar`)

La pieza estratégica del submódulo. Recibe un `EscenarioObjetivo` con siete banderas sobre lo que el alumno quiere y devuelve una `AlcanceMiniNotas` con las features incluidas, las no incluidas, la recomendación y las razones.

Las dos listas de features son la columna vertebral de la decisión. Incluidas en el mini:

```csharp
public static IReadOnlyList<Feature> IncluidasEnMini { get; } =
[
    Feature.WebApp,
    Feature.Persistencia,
    Feature.EndpointsCrud,
    Feature.TestsUnitarios,
    Feature.Deploy,
];
```

Y explícitamente fuera (las cubre S10.1):

```csharp
public static IReadOnlyList<Feature> NoIncluidasEnMini { get; } =
[
    Feature.Auth,
    Feature.KeyVault,
    Feature.ServiceBus,
    Feature.Functions,
    Feature.CosmosDb,           // mini usa Table; integrador usa Cosmos
    Feature.PipelineCiCd,
    Feature.AppInsights,
    Feature.SlotsSwap,
    Feature.ManagedIdentity,
];
```

Esa segunda lista es la pieza más importante del ejemplo, y no hace nada en runtime salvo informar. Su valor es que el alcance recortado está **escrito y es consciente**. El alumno no se queda con la duda de "¿debería haber metido auth?"; ve negro sobre blanco que auth es parte del proyecto grande, no de este. El comentario `// mini usa Table; integrador usa Cosmos` es especialmente fino: explica por qué Cosmos está en la lista de no incluidas aunque "persistencia" sí esté en las incluidas. La mini-práctica persiste en Table Storage (más simple); el grande usa Cosmos.

La lógica de recomendación es una cascada de prioridades:

```csharp
if (e.QuieresProyectoDeProduccion
    || e.NecesitasAuthEntra
    || e.NecesitasFunctionsYSb
    || e.NecesitasPipelineCompleto)
{
    // ... → Recomendacion.Completo
}

if (e.QuieresUnEndToEndMinimo
    || e.TienesMenosDeUnaHora)
{
    // ... → Recomendacion.Mini
}

// Caso por defecto → Recomendacion.EmpezarPorMini
```

El orden de la cascada importa: primero comprueba si necesitas algo que el mini no cubre (entonces Completo, no tiene sentido perder tiempo en el mini); luego si quieres rapidez (Mini); y por defecto, EmpezarPorMini. La prioridad refleja la realidad: si necesitas auth para tu objetivo, hacer el mini primero es un desvío, no un calentamiento.

---

## 6. Los 11 pasos y el ciclo completo end-to-end

Los 11 pasos del `PasoChecker` cubren el ciclo entero de un sistema cloud, de la idea a la limpieza. Vale la pena verlos agrupados por fase para entender la estructura:

| Fase | Pasos | Qué demuestra |
| --- | --- | --- |
| Diseño | 1 (modelo), 2 (solución) | Sabes modelar el dominio y estructurar el proyecto |
| Implementación | 3 (Note), 4 (repositorio), 5 (endpoints) | Sabes escribir las tres capas |
| Verificación local | 6 (tests), 7 (smoke local) | Sabes validar antes de desplegar |
| Despliegue | 8 (infra), 9 (deploy) | Sabes llevarlo a Azure |
| Cierre | 10 (validación e2e), 11 (cleanup) | Sabes confirmar y limpiar |

Dos lecturas operativas:

La primera es que **la verificación local (pasos 6-7) va antes del despliegue (8-9)**. El alumno valida que el sistema funciona en su máquina antes de gastar tiempo desplegándolo. Desplegar algo que no has probado local es la receta del "funciona en mi máquina pero falla en Azure y no sé por qué". Los tests unitarios y el smoke local cierran esa puerta.

La segunda es que **el cleanup (paso 11) está al mismo nivel que los demás**, no como apéndice opcional. Es uno de los 11 pasos evaluados con sus dos flags. Esto reproduce la disciplina del curso entero: un sistema en la nube que dejas corriendo cuesta dinero, y el reflejo de limpiar lo que no usas es parte del oficio cloud, no una nota al pie. El alumno que termina la práctica con el Resource Group borrado ha aprendido algo más valioso que CRUD sobre Table Storage.

---

## 7. La conversación con el alumno ansioso: ¿por qué no empezar directamente por el grande?

Hay una resistencia común en alumnos motivados: "si voy a tener que hacer el proyecto integrador completo, ¿por qué pierdo una hora en el mini?". La respuesta operativa es triple.

Primero, el mini te da **feedback completo en 60 minutos**. Ves una URL pública respondiendo, un smoke test verde, un sistema desplegado de principio a fin. Ese feedback temprano es lo que sostiene la motivación para el proyecto grande de tres horas. Arrancar el grande en frío significa dos horas sin ver nada funcionando, que es exactamente donde la mayoría abandona.

Segundo, el mini te enseña **el ciclo, no los componentes**. El valor no está en Table Storage (que es lo más simple del curso); está en interiorizar la secuencia modelar → implementar → testear → desplegar → validar → limpiar. Esa secuencia es idéntica en el proyecto grande, solo que con diez componentes. El que ha hecho el ciclo una vez con tres capas lo reconoce con diez; el que no, se pierde.

Tercero, el mini te da un **sistema base sobre el que escalar**. El `CaminoHaciaS101` no te dice "ahora empieza el grande de cero"; te dice "sustituye Table por Cosmos, luego añade auth, luego Functions". Cada paso añade una capa sobre lo que ya funciona. Si has hecho el mini, el proyecto grande es una evolución; si no, es una montaña en blanco.

La conversación no es "el mini es obligatorio para todos". Es "si nunca has desplegado un sistema cloud end-to-end completo, el mini te ahorra el atasco del grande. Si ya lo has hecho varias veces, ve directo al grande". El `AlcanceComparator` codifica exactamente esa decisión.

---

## 8. Cómo probarlo en local

Es un ejemplo offline al 100%. La mini-app real la construyes y despliegas en Azure; este API te guía las decisiones.

```bash
dotnet run --project src/Practica.MiniNotas.Demo.Api
# http://localhost:5121
```

Cinco endpoints útiles:

```http
### Decidir el alcance según tu objetivo
POST http://localhost:5121/mininotas/alcance
Content-Type: application/json

{
  "quieresUnEndToEndMinimo": true,
  "tienesMenosDeUnaHora": true,
  "yaConocesM01M02M05": true
}
# → Recomendacion: Mini; incluidas (5 features), no incluidas (9 features)

### Preflight con tu setup real
POST http://localhost:5121/mininotas/preflight
Content-Type: application/json

{
  "tieneDotNet8SDK": true,
  "tieneAzCli": true,
  "tieneCurl": true,
  "tieneJq": false,
  "tieneGit": true,
  "hizoM01": true,
  "hizoM02": true,
  "hizoM05": true
}
# → listoParaArrancar=true; 3 bloqueantes OK, jq en Aviso

### Evaluar un paso concreto
POST http://localhost:5121/mininotas/paso
Content-Type: application/json

{
  "paso": "DesplegarApp",
  "comandoEjecutado": true,
  "outputEsperadoVisible": false
}
# → Pendiente, slide 12, acción sobre el cold start del F1

### Camino del mini al proyecto integrador completo
GET http://localhost:5121/mininotas/camino-s101
# → 7 pasos aditivos: Table→Cosmos, Entra, KV+MI, SB+Functions, pipeline, AppInsights, autoevaluar

### Plan completo
POST http://localhost:5121/mininotas/plan
Content-Type: application/json
{ "preflight": { ... }, "evidencias": [ ... ], "objetivo": { ... } }
# → preflight + 11 pasos + alcance + camino-s101 + checklist
```

Los 42 tests cubren:

- Capa 1 (unit): preflight con cada bandera (herramientas bloqueantes, conocimiento previo como aviso); evaluador con los 11 pasos y las tres combinaciones de flags; comparador con cada objetivo verificando las recomendaciones Mini / Completo / EmpezarPorMini y las listas de features incluidas y no incluidas.
- Capa 0 (DI): `IPracticaMiniNotasPlanner` como singleton del contenedor.
- Capa E2E: los cinco endpoints via `WebApplicationFactory`.

No hay capa de integración porque la mini-app real se despliega en Azure con `az webapp deploy`. Probarlo de verdad es hacer la práctica: 60-75 minutos con tu suscripción.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 9. Anti-patterns

Cinco prácticas que evitar en la mini-práctica:

**Anti-pattern 1: empezar por el proyecto grande sin haber hecho un end-to-end nunca.** Es el caso 1 de la sección 2. Si nunca has desplegado un sistema cloud completo de principio a fin, arrancar el integrador de diez componentes es garantía de atasco. El mini te da el ciclo completo en una hora; úsalo como rampa, no como obstáculo a saltarte.

**Anti-pattern 2: saltarse el cleanup porque "es solo un plan F1 gratis".** Es el caso 2 de la sección 2. El plan F1 es gratis, pero la Storage Account no, y dejar recursos de prueba corriendo es exactamente el descuido que el curso ha estado combatiendo desde M01. El paso 11 no es opcional: `az group delete` y verificar con `az group exists` que devuelve `false`.

**Anti-pattern 3: añadir features del proyecto grande a la mini-práctica.** Es el caso 3 de la sección 2. Meter auth, Functions o pipeline en el mini lo convierte en un híbrido incoherente que no es ni el corto ni el largo. El alcance recortado es consciente; si quieres esas features, ese es el proyecto S10.1, y el `CaminoHaciaS101` te dice cómo añadirlas en orden.

**Anti-pattern 4: desplegar sin haber pasado el smoke test local.** Los pasos 6 y 7 (tests unitarios y smoke local) van antes del despliegue por una razón. Desplegar algo que no has validado en tu máquina te lleva al "funciona en local pero falla en Azure" sin saber si el problema es tu código o la infra. Valida local, despliega después.

**Anti-pattern 5: confundir el cold start del F1 con un fallo de deploy.** El plan F1 gratis tiene cold start de 30-60 segundos. El alumno que hace `curl` justo después del deploy y recibe un timeout cree que el deploy falló y empieza a depurar algo que funciona. La sugerencia del paso 9 lo avisa: espera al cold start antes de declarar el fallo.

---

## 10. Glosario breve

- **Mini-proyecto**: versión recortada del proyecto integrador. Web App F1 + Table Storage + 5 endpoints CRUD, sin auth/Functions/pipeline.
- **End-to-end mínimo**: sistema completo de principio a fin (modelar → desplegar → validar) con el menor número de componentes posible.
- **Table Storage**: almacén NoSQL clave-valor de Azure, más simple que Cosmos. Modelo PartitionKey + RowKey. La mini-práctica lo usa; el integrador usa Cosmos.
- **`ITableEntity`**: interface que un modelo implementa para persistir en Table Storage. Exige PartitionKey, RowKey, Timestamp, ETag.
- **Plan F1**: tier gratuito de App Service. Sin coste pero con cold start lento (30-60 s) y límites de CPU.
- **Cold start**: latencia de arranque de un servicio que estaba inactivo. En F1 puede ser 30-60 s tras el deploy o tras inactividad.
- **Smoke test**: prueba mínima que verifica que el sistema responde (crear, leer, actualizar, borrar una nota con `curl`). No es exhaustivo; confirma que arrancó.
- **Cleanup**: borrado de los recursos de Azure al terminar (`az group delete`). Evita la factura sorpresa. Paso 11 no opcional.
- **Alcance** (Mini / Completo / EmpezarPorMini): decisión sobre qué construir según objetivo y tiempo. El comparador la recomienda.
- **CaminoHaciaS101**: los 7 pasos aditivos para evolucionar la mini-práctica al proyecto integrador completo sin rehacerla.
- **Feature incluida / no incluida**: las 5 capacidades del mini (WebApp, Persistencia, CRUD, Tests, Deploy) vs las 9 que cubre el grande (Auth, KV, SB, Functions, Cosmos, Pipeline, AppInsights, Slots, MI).

---

## 11. Cierre

Esta mini-práctica es el cortometraje que demuestra que sabes cerrar un sistema cloud de principio a fin: modelar, implementar, testear, desplegar, validar y limpiar. Cuando la termines con el smoke test verde contra una URL pública y el Resource Group borrado, tienes la confianza para el largometraje del S10.1, donde el mismo ciclo lleva diez componentes en lugar de tres. La diferencia entre los dos no es de dificultad conceptual; es de escala. El que ha rodado el corto sabe rodar el largo.

Con S10.P2 se cierra M10 (2/2) y, con M01 a M09 ya cubiertos, **se cierra el curso F-003-Azure**. Si quieres seguir, está [`M11 — Bonus: Claude Code en Azure`](../../M11-Bonus-Claude-Code-Azure/README.md), opcional, para llevar la asistencia de IA que aprendiste en M09 a un proyecto real desplegado en Azure. El curso base termina aquí; el bonus es para quien quiera más.
