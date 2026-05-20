# Manual del alumno — S9.2 · Claude Code: casos de uso avanzados

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de los 15 casos con sus palabras clave y sus slides, comandos, estructura del repo. Este manual va antes: te cuenta por qué un prompt vago se traduce en cinco turnos perdidos y tokens caros, qué cuatro ingredientes definen a un prompt sólido, y por qué para tareas recurrentes el siguiente paso natural es subir el prompt a un skill versionado en `.claude/`.

Tiempo de lectura: ~25 min. Submódulo de referencia: [M09-S9.2](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.2-claude-code-casos-uso-v3.md). Tres piezas de lógica pura (clasificador de los 15 casos por palabras clave, generador de templates canónicos con los 4 ingredientes, evaluador de calidad del prompt del alumno) más un planificador que las une.

*Creado: 2026-05-21 00:26 +0200*

---

## 1. La idea en una frase

Claude Code es una herramienta muy potente, pero su output es una función directa de la calidad del prompt que recibe. Y el prompt no es magia: tiene **cuatro ingredientes** que siempre, siempre, siempre deben aparecer (contexto, constraints, formato de salida, criterio de éxito). Si falta uno, el agente lo inventa, te pide aclaración o se desvía. Cada turno extra cuesta tokens y cuesta tiempo. La economía del prompt es real.

El submódulo entrena dos habilidades concretas que el alumno va a aplicar todos los días: **reconocer el caso de uso** al que se enfrenta (uno de 15 patrones que cubren el 90% del trabajo real con un agente) y **arrancar siempre desde un template canónico** en vez de escribir el prompt en blanco. La heurística del ejemplo te clasifica el caso a partir de la descripción, te entrega el template y, si le pasas tu propio prompt, te dice qué nota saca (0-100) y qué ingrediente le falta.

---

## 2. El problema real que hay detrás

Tres situaciones que verás en cualquier equipo que adopta Claude Code:

**Caso 1: el prompt de cuatro palabras.** Un developer le pide a Claude Code "optimiza este endpoint" y se queda esperando milagros. El agente, sin contexto, sin métricas actuales, sin objetivo concreto, le devuelve diez sugerencias genéricas (añadir caché, paralelizar, usar `Span<T>`, revisar índices...) que no se aplican a la situación real. El developer descarta ocho, prueba dos, ninguna mejora nada porque el cuello de botella estaba en la BD, no en la app. **Tres turnos perdidos** y la sensación de que "Claude Code no sirve para esto". El template del caso (slide 13) ya pide P50/P95/P99 actuales y el objetivo concreto. Con esos cuatro datos en el primer prompt, la primera respuesta hubiera ido a la BD directamente.

**Caso 2: la migración que se quedó a medias.** Otro equipo arranca una migración de .NET Framework 4.8 a .NET 10. El alumno pega un fichero con `WebClient` y `ConfigurationManager` y le pide "migra esto". Claude Code lo migra, pero rompe el contrato público porque el alumno no le dijo que los nombres públicos no podían cambiar. **Dos horas de debugging** para encontrar los breaking changes y un PR rechazado en review. El template del caso (slide 2) incluye "Mantén la funcionalidad y los nombres públicos" como constraint explícito. Eso es el "constraints" del cuadrante: lo que no debe tocarse, dicho en una línea.

**Caso 3: el equipo que tiene el mismo prompt en cinco cabezas.** Tercer equipo, más experimentado. Cada developer tiene su propia versión del prompt para "generar IaC con Bicep", "documentar APIs desde código" o "expand-contract de columna". Cuando entra alguien nuevo, le toca redescubrir cada uno por separado. Cuando alguien mejora su versión, los demás no se enteran. El template del caso, **versionado en `.claude/templates/<caso>.md`** del repo, se convierte en el contrato del equipo. Es el siguiente paso natural: empieza con el template canónico, lo adaptas a tu sistema concreto, lo guardas, lo iteras como código.

Los tres casos los aborda el ejemplo. `CaseClassifier` te dice qué caso es; `PromptTemplateBuilder` te entrega el template con los placeholders; `PromptQualityEvaluator` te puntúa el prompt y te dice qué ingrediente le falta.

---

## 3. Por qué esto importa en tu stack

Si usas Claude Code más allá de una vez al día, los tres problemas de la sección anterior te van a aparecer. Tres preguntas que conviene tener resueltas:

- **¿Cómo reconozco rápido qué tipo de tarea estoy haciendo?** El clasificador del ejemplo cubre 15 casos que aparecen una y otra vez. Migración legacy, documentación desde código, code review, datos de prueba, logs, pipeline, Bicep, pair programming, OpenAPI, schema BD, tests E2E, optimización, docs técnicas, coste Azure, expand-contract. Si tu tarea encaja en uno, **empieza por el template**, no de cero.
- **¿Cómo evito el prompt vago?** Los cuatro ingredientes. Contexto, constraints, formato de salida, criterio de éxito. Aparecen en TODOS los templates del ejemplo. Aplícalos a mano si tu caso no encaja en los 15: el evaluador puntúa según los cuatro.
- **¿Qué hago con los prompts que se repiten cada semana?** Versionarlos en el repo: `.claude/templates/<caso>.md`. Conviértelos en skills (`.claude/skills/<nombre>/SKILL.md`) si están lo bastante maduros y los invoca todo el equipo. Lo aprendiste en S9.1; aquí se aplica.

Las tres respuestas reducen tu uso de tokens, suben la calidad de la primera respuesta del agente y convierten experiencia individual en activo del equipo.

---

## 4. La analogía vertebradora: el recetario del chef profesional

Imagina la cocina de un restaurante con cinco estrellas Michelin. El jefe de cocina lleva un recetario impecable con 15 platos clásicos del menú: salmón en costra, risotto de boletus, paté de hígado, sopa de cebolla, pollo al curry, lo que sea. Cada receta del libro está estructurada igual:

- **Lista de ingredientes** con cantidades exactas. Sin lista de ingredientes, el cocinero improvisa y cada cena sale distinta.
- **Restricciones del plato**: "sin lactosa para la mesa 3", "vegetariano para la 7", "alergia al cacahuete para la 12". El cocinero las respeta o el comensal acaba en urgencias.
- **Presentación final**: cómo se emplata, qué guarnición acompaña, qué temperatura sirve. La cocina puede hacer el guiso perfecto, pero si lo sirven frío en plato hondo cuando tocaba caliente en plato llano, el cliente lo devuelve.
- **Criterio de "está listo"**: "salsa reducida hasta nappage", "carne a 58 grados al corazón", "el risotto suelta una cucharada al inclinar el plato". Sin criterio, el cocinero adivina y a veces falla.

Cuatro componentes en cada receta. Si falta cualquiera, el plato sale dudoso. Y eso es exactamente lo que pasa con un prompt a Claude Code: faltan ingredientes, el agente improvisa, el resultado decepciona.

El chef tiene también dos hábitos extra que el alumno de Claude Code debería copiar. Primero: **no improvisa platos clásicos**. Si entra un comensal y pide salmón en costra, el chef no inventa la receta desde cero, va al libro y la sigue. Los 15 casos de uso del submódulo son las 15 recetas del libro: cuando reconoces el caso, vas al template. Segundo: **cuando un plato se cocina mil veces, se convierte en mise en place permanente** de la cocina. Las salsas madre vienen ya hechas, las verduras preparadas, los caldos a punto. En Claude Code, eso son los skills (`.claude/skills/<nombre>/`): cuando un prompt se usa todos los días, no lo escribes cada vez, lo invocas con `/<nombre>`. Lo viste en S9.1.

Y al final del servicio, el **maître** pasa por las mesas y pregunta qué tal. Si tres comensales devuelven el mismo plato, el chef sabe que la receta tiene un problema. Esa función la hace el `PromptQualityEvaluator`: revisa tu prompt, lo puntúa de 0 a 100, y te dice qué le falta. Antes de mandarlo a la cocina (Claude Code), pasa por el maître y refina.

Mantén la imagen mientras lees el código. Libro de 15 recetas, cuatro ingredientes en cada una, maître que evalúa antes de servir, recetas recurrentes que pasan a mise en place permanente. Toda la mecánica del submódulo cabe en esa cocina.

---

## 5. Recorrido por el código: las tres piezas

### El clasificador de caso (`CaseClassifier.Clasificar`)

La pieza más pragmática del ejemplo. Recibe una descripción libre de la tarea (lo que el alumno teclea cuando le pides "¿qué quieres hacer?") y devuelve el caso canónico más probable, su número de slide y todas las palabras clave que detectó. Funciona por matching de patrones en orden de especificidad:

```csharp
private static readonly (string Patron, CasoUso Caso, string Slide)[] Reglas =
[
    ("expand-contract",         CasoUso.ExpandContractRefactor, "16"),
    ("expand contract",         CasoUso.ExpandContractRefactor, "16"),
    ("rename column",           CasoUso.ExpandContractRefactor, "16"),
    // ...
    (".net framework",          CasoUso.MigracionLegacyANet, "2"),
    ("webclient",               CasoUso.MigracionLegacyANet, "2"),
    // ...
    ("coste mensual",           CasoUso.AnalisisCosteAzure, "15"),
    ("bicep",                   CasoUso.BicepDesdeInfra, "8"),
    // ...
];
```

Dos decisiones de diseño que conviene entender:

**El orden importa**. Cuando la descripción tiene "estima el coste mensual de la infraestructura", aparecen dos patrones: "coste mensual" (caso `AnalisisCosteAzure`, slide 15) y "infraestructura" (caso `BicepDesdeInfra`, slide 8). El primer match gana. Y el primero, intencionadamente, es "coste mensual", porque es más específico que "infraestructura". Esta decisión convierte la heurística en una jerarquía: los patrones más raros y discriminativos van arriba, los genéricos van abajo. Cuando añadas un caso nuevo, ponlo arriba si su palabra clave puede aparecer en frases que también mencionan algo genérico.

**Si nada hace match, devuelve `CasoUso.Otro`**. El template de `Otro` no es un placeholder de error: es el formulario genérico con los cuatro ingredientes vacíos para que el alumno rellene a mano. Es la red de seguridad cuando el caso del alumno no encaja en los 15 catalogados. Y suele ser la señal de que **toca añadir un caso nuevo** al clasificador para la siguiente iteración.

### El generador de templates (`PromptTemplateBuilder.ParaCaso`)

Para cada uno de los 15 casos hay un template con los cuatro ingredientes. Mira el del caso de migración:

```csharp
CasoUso.MigracionLegacyANet => new(caso, "2",
    "Analiza {{archivo}} que usa .NET Framework {{versionLegacy}}. " +
    "Migralo a .NET 10:\n" +
    "- Reemplaza {{patronLegacy}} por {{patronModerno}}\n" +
    "- Usa async/await donde haya I/O sincrono\n" +
    "- Mantén la funcionalidad y los nombres públicos.\n" +
    "Criterio éxito: el código compila sin warnings y los tests siguen verdes.",
    ["archivo", "versionLegacy", "patronLegacy", "patronModerno"]),
```

Cuatro placeholders (`{{archivo}}`, `{{versionLegacy}}`, `{{patronLegacy}}`, `{{patronModerno}}`) y los cuatro ingredientes dispersos por el texto: contexto (lo que estás analizando), constraints ("mantén la funcionalidad y los nombres públicos"), formato implícito (código C# .NET 10), criterio de éxito ("compila sin warnings, tests verdes"). Si el alumno rellena los placeholders y manda eso a Claude Code, el primer turno suele ser suficiente.

Tres detalles de los templates que merece la pena nombrar:

**El de pair programming arranca distinto**:

```csharp
CasoUso.PairProgramming => new(caso, "9",
    "[modo interactive — iteramos paso a paso]\n" +
    "Vamos a implementar {{feature}} en {{proyecto}}.\n" +
    "Empezamos por: 1) modelo de datos → 2) repository con paginación → " +
    "3) validación → 4) tests → 5) `dotnet test` y arreglar lo que falle.\n" +
    "Tras cada paso, espera mi confirmación.",
    ["feature", "proyecto"]),
```

Empieza con la **etiqueta del modo de ejecución** que viste en S9.1 (`[modo interactive]`). Es una pista para el alumno de que esta tarea no se hace en one-shot ni en headless: se itera en conversación. Y la pista final ("Tras cada paso, espera mi confirmación") evita que Claude Code se entusiasme y haga las cinco fases del tirón.

**El de optimización exige métricas concretas**:

```csharp
CasoUso.OptimizacionRendimiento => new(caso, "13",
    "Analiza {{endpoint}} y sugiere optimizaciones:\n" +
    "Métricas actuales: P50={{p50}}ms, P95={{p95}}ms, P99={{p99}}ms, " +
    "{{ruPorQuery}} RU/query, {{qpm}} queries/minuto.\n" +
    "Objetivo: reducir P99 a < {{objetivoP99}}ms.\n" +
    "Output: cambios concretos con estimación de impacto.",
    ["endpoint", "p50", "p95", "p99", "ruPorQuery", "qpm", "objetivoP99"]),
```

Siete placeholders. Parece exigente, pero es exactamente lo que falta en el caso 1 de la sección 2. Sin esas siete cifras, Claude Code no tiene de dónde tirar y la respuesta es genérica. Con las siete, va directo al cuello de botella estadístico.

**El de expand-contract orquesta cuatro fases**:

```csharp
CasoUso.ExpandContractRefactor => new(caso, "16",
    "Necesito expand-contract sobre {{recurso}}:\n" +
    "- Cambio: {{cambio}}\n" +
    "- {{nServicios}} servicios consumen este recurso\n" +
    "- Producción tiene {{volumen}}\n" +
    "- Sin downtime\n" +
    "- Tengo {{sprints}} sprints.\n" +
    "Plan en 4 fases (Expand → Dual write → Switch reads → Contract) con " +
    "checklist y subagents paralelos para escanear los servicios.",
    ["recurso", "cambio", "nServicios", "volumen", "sprints"]),
```

Le dice a Claude Code que use **subagents paralelos** (concepto de S9.1) para escanear los servicios consumidores en paralelo en lugar de uno a uno. Es un ejemplo de cómo el prompt puede orquestar varias capacidades de Claude Code a la vez. Lo importante no es que sepas de memoria los template; es que **al ver el de tu caso, copies el patrón en otros que escribas**.

### El evaluador de calidad (`PromptQualityEvaluator.Evaluar`)

La pieza con voz de maître. Recibe un prompt cualquiera y devuelve `EvaluacionPrompt` con cuatro flags (`TieneContexto`, `TieneConstraints`, `TieneFormatoSalida`, `TieneCriterioExito`), puntuación 0-100, nivel cualitativo (Pobre/Aceptable/Bueno/Excelente) y la lista de sugerencias concretas para llegar al siguiente nivel.

La heurística es simple pero efectiva: cada ingrediente vale 25 puntos. Cuatro ingredientes presentes = 100 puntos = Excelente. Y si el prompt es muy corto (< 40 caracteres) se capa al techo de 25 — un prompt corto es siempre vago, por mucho que mencione alguna palabra clave suelta.

La detección de cada ingrediente se hace con listas de **marcadores** (cadenas que típicamente aparecen cuando el ingrediente está):

```csharp
private static readonly string[] MarcadoresContexto =
[
    "este proyecto", "el sistema", "la app", "uso ", "usamos",
    "framework", ".net", "azure", "cosmos", "service bus",
    "infraestructura", "stack",
];

private static readonly string[] MarcadoresConstraints =
[
    "no debe", "mantén", "mantener", "preserva", "preservar",
    "sin romper", "sin cambios en", "respetando", "respeta",
    "no inventes", "no rompas", "compatible con",
];

private static readonly string[] MarcadoresFormato =
[
    "output:", "salida:", "formato:", "devuelve", "responde",
    "json", "markdown", "tabla", "yaml", "csv", "guarda en",
    "guarda como", "genera el archivo",
];

private static readonly string[] MarcadoresCriterio =
[
    "criterio éxito", "criterio de éxito", "objetivo:",
    "tests verdes", "compila", "build limpio", "sin warnings",
    "p99 <", "p95 <", "latencia <", "ru <",
];
```

No es perfecta. Un prompt podría tener constraints implícitos sin usar las palabras del marcador y el evaluador lo penalizaría. Pero como heurística operacional para el alumno funciona muy bien: te obliga a escribir explícitamente las cuatro cosas, lo que **mejora también la lectura humana** del prompt cuando otro miembro del equipo lo revisa en `.claude/templates/`.

Cuando el evaluador encuentra ingredientes ausentes, devuelve sugerencias concretas, no genéricas:

> "Faltan constraints: explica qué NO debe romper (funcionalidad existente, naming público, contratos)."

Cinco palabras de ayuda específica son más útiles que un "tu prompt podría mejorar".

---

## 6. Los cuatro ingredientes, en detalle

Vale la pena dedicar una sección entera al cuadrante porque es el invariante más importante del submódulo. Cualquier prompt a un agente, sea Claude Code o cualquier otro, debería tener los cuatro:

**Contexto: qué eres, qué estás haciendo, en qué tecnología.** "Este es un servicio C# .NET 10 que usa Cosmos DB y Service Bus." Sin contexto, Claude Code asume defaults (probablemente Python o Node) y la primera respuesta llega en el lenguaje equivocado. Una frase de contexto explícita evita ese turno.

**Constraints: qué NO debe pasar.** "No cambies los nombres públicos." "No rompas la API v1, que aún la usan tres clientes." "Sin cambios en el esquema de la BD." Los constraints son lo que distingue una sugerencia teórica buena de una solución aplicable a tu sistema. Sin constraints, Claude Code propone lo más limpio en abstracto, que casi siempre rompe algo concreto.

**Formato de salida: cómo quieres la respuesta.** "Devuélvelo en JSON con campos `{severidad, archivo, linea, descripcion}`." "Genera dos archivos, `Service.cs` y `ServiceTests.cs`, completos." "Markdown con tablas para cada tipo de cambio." Sin formato, Claude Code te da el resultado en un párrafo de prosa que tienes que parsear a mano. Con formato, lo puedes pipeline a otro proceso.

**Criterio de éxito: cuándo sabes que está terminado.** "Cuando `dotnet test` esté verde." "Cuando el P99 baje de 200 ms." "Cuando el build no tenga warnings." Sin criterio, Claude Code para cuando "le parece" que ha terminado, que no siempre coincide con tu definición. Con criterio, sabe cuándo iterar sin que se lo pidas.

El alumno experimentado, después de unos meses con la herramienta, escribe los cuatro casi automáticamente. El alumno nuevo se salta dos o tres y se frustra. El evaluador del ejemplo está pensado para acortar esa curva: en lugar de aprender el patrón a base de turnos perdidos, lo aprendes leyendo las sugerencias.

---

## 7. Cómo probarlo en local

```bash
dotnet run --project src/ClaudeCode.CasosUso.Demo.Api
# http://localhost:5114
```

Endpoints:

```http
### Clasificar una tarea por descripción libre
POST http://localhost:5114/casos/clasificar
Content-Type: application/json

"Quiero migrar este servicio .NET Framework 4.8 que usa WebClient a .NET 10"
# → { caso: "MigracionLegacyANet", slide: "2",
#     palabrasClaveDetectadas: [".net framework", "webclient"] }

### Obtener el template canónico de un caso
GET http://localhost:5114/casos/template/MigracionLegacyANet
# → template con 4 placeholders y los 4 ingredientes

### Evaluar el prompt que escribió el alumno
POST http://localhost:5114/casos/evaluar
Content-Type: application/json

"Optimiza este endpoint"
# → { puntuacion: 0, nivel: "Pobre",
#     sugerencias: ["Falta contexto...", "Faltan constraints...",
#                   "Falta formato de salida...", "Falta criterio de éxito..."] }

### Plan completo (clasificación + template + evaluación + checklist)
POST http://localhost:5114/casos/plan
Content-Type: application/json

{
  "descripcionTarea": "Optimiza el endpoint de checkout",
  "promptDelAlumno": "Optimiza el endpoint de checkout que tiene P99=800ms en producción. Stack: .NET 10 + Cosmos DB. No cambies el contrato público. Output: cambios concretos con estimación de impacto. Criterio éxito: P99 < 300ms."
}
# → caso OptimizacionRendimiento + template + nivel Excelente + checklist
```

Los 38 tests cubren el clasificador con `[Theory]` para los 15 casos, el generador para cada template (incluido el genérico de `Otro` y el de expand-contract con sus 4 fases), y el evaluador con los cuatro niveles de calidad y casos límite (prompt corto, vago, completo, etcétera).

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 8. La conversación con el equipo: del prompt personal al template del repo

Hay un momento de madurez en cualquier equipo que adopta Claude Code: cuando alguien dice "tengo el prompt para esto, te lo paso por Slack". Es el síntoma de que el prompt vale más que tu memoria individual y que debería vivir en otro sitio. Tres niveles de evolución:

**Nivel 1: prompts en mi cabeza.** Funciona el primer mes. Cada developer tiene los suyos. Cuando llega el siguiente, se redescubren. Es donde está el equipo del caso 3.

**Nivel 2: templates en `.claude/templates/<caso>.md`.** El equipo identifica los casos recurrentes, aplica el patrón del submódulo, y versiona los templates en el repo. Cualquiera del equipo abre el archivo, copia, rellena placeholders concretos y manda. Beneficios: el conocimiento se comparte, las mejoras se propagan a través de PRs, los templates evolucionan con el sistema. Coste: ligero. Es el siguiente paso natural después de hacer la práctica del submódulo.

**Nivel 3: skills en `.claude/skills/<caso>/`.** Cuando un template se usa varias veces a la semana, ascender a skill. La diferencia: el skill se invoca con `/<nombre>` y se ejecuta automáticamente con su lógica embebida (no copy-pasteas, lo lanzas). Lo viste en S9.1. La transición de template a skill es trivial cuando el template ya estaba bien estructurado.

Un equipo que adopta los tres niveles tiene **una biblioteca de conocimiento operativo** sobre Claude Code dentro del repo, junto al código. Cuando alguien deja la empresa, su conocimiento no se va con él. Cuando entra alguien nuevo, en lugar de redescubrir los prompts útiles, se los encuentra catalogados.

---

## 9. La conversación con producto: la economía del prompt

Cuando un departamento financiero o un product manager se interesa por la adopción de Claude Code, la pregunta inevitable es "¿cuánto cuesta?" y la respuesta honesta es "depende de cómo se use". Tres factores afectan al coste por tarea, y todos están bajo control del alumno:

**Calidad del prompt inicial.** Un prompt de los cuatro ingredientes suele resolver la tarea en uno o dos turnos. Un prompt vago, en cinco o seis. Cada turno multiplica el contexto que el modelo procesa, y por tanto el coste en tokens. Subir la calidad del prompt es la palanca más directa para reducir factura.

**Elección del modelo.** Para tareas pequeñas (changelog desde commits, refactor de un fichero, análisis de un log), Haiku es suficiente y cuesta una fracción de Sonnet. Para arquitectura, debugging cross-cutting o expand-contract, Sonnet vale el sobrecoste. Opus es para los pocos casos donde necesitas la mejor capacidad de razonamiento. El `settings.json` del proyecto puede tener un modelo por defecto razonable; las tareas concretas lo sobreescriben.

**Uso de subagents para tareas grandes.** Un subagent (slide 18 de S9.1) procesa el contexto pesado en aislado y devuelve el resumen útil. El main thread se mantiene ligero, los siguientes turnos cuestan menos. Para code review de un PR de 800 líneas, esto puede dividir entre tres el coste del turno.

Si producto pregunta "cuánto cuesta", la respuesta no es un número absoluto, es "depende de si seguimos las prácticas del submódulo". Y si las seguimos, el coste por tarea queda predecible y razonable.

---

## 10. Anti-patterns

Cinco prácticas que evitar:

**Anti-pattern 1: escribir el prompt desde cero cada vez.** El equipo del caso 3 reinventa la rueda en cada tarea. Cuesta tiempo, produce prompts inconsistentes y pierde el aprendizaje. Empieza siempre por el template del caso; adáptalo, no lo reescribas.

**Anti-pattern 2: ignorar la puntuación del evaluador.** Si tu prompt saca 50 y el evaluador te dice "falta formato de salida y criterio de éxito", añadirlos cuesta 30 segundos. Mandar el prompt de 50 sin esos dos te va a costar dos turnos extra de "y cuál es el formato que quieres" y "¿esto te vale o lo refinamos?".

**Anti-pattern 3: usar `Otro` como caso permanente.** Si la mayoría de tus tareas caen en `Otro` y nunca aparece nada catalogado, una de dos: tu trabajo no encaja con los 15 casos del submódulo (puede ser legítimo, pero raro), o has elegido descripciones tan vagas que el clasificador no engancha. En el segundo caso, refina la descripción primero.

**Anti-pattern 4: prompts largos que repiten contexto del turno anterior.** Si llevas una conversación de tres turnos con Claude Code, no necesitas volver a explicarle el stack en cada mensaje. El contexto persiste. Repetirlo infla los tokens sin aportar. Manda solo lo nuevo.

**Anti-pattern 5: no versionar los templates en el repo.** Si los prompts buenos viven en hilos de Slack del equipo, ese conocimiento se pierde el día que el canal se archive. `.claude/templates/<caso>.md` con commit y revisión por PR convierte el prompt en código del proyecto.

---

## 11. Glosario breve

- **Prompt**: el mensaje que el alumno envía a Claude Code para que ejecute una tarea.
- **Template canónico**: prompt parametrizado con placeholders, con los cuatro ingredientes presentes, válido para todos los casos de su categoría.
- **Caso de uso**: categoría de tarea recurrente con Claude Code. El ejemplo cataloga 15: migración, code review, IaC, debugging, optimización, etcétera.
- **Cuatro ingredientes** (del prompt): contexto, constraints, formato de salida, criterio de éxito.
- **Marcador**: cadena de texto que el evaluador busca para detectar la presencia de un ingrediente.
- **Placeholder**: variable en formato `{{nombre}}` dentro del template que el alumno rellena con datos concretos.
- **Modo interactive / one-shot / pipe / headless**: los cuatro modos de ejecución de Claude Code que viste en S9.1.
- **Subagent**: proceso paralelo de Claude Code con contexto aislado, ideal para tareas pesadas que pueden saturar el contexto principal (slide 18 de S9.1).
- **`.claude/templates/`**: convención de carpeta para versionar los templates de prompts del equipo en el repo.
- **`.claude/skills/`**: convención de carpeta para versionar skills invocables con `/<nombre>` (viste en S9.1).
- **Expand-contract**: patrón de refactor sin downtime en cuatro fases (Expand, Dual write, Switch reads, Contract). Útil para renombrar columnas, mover datos, evolucionar esquemas.

---

## 12. Cierre

Si te quedas con una sola idea de S9.2: **antes de empezar a teclear un prompt, identifica el caso y arranca por el template del submódulo**. Los 15 casos cubren el grueso del trabajo real; los cuatro ingredientes son el invariante de un prompt sólido; el evaluador es la red de seguridad antes de mandar la petición a Claude Code. Con esas tres herramientas, el agente trabaja mejor, los tokens cuestan menos y el conocimiento que generas se queda en el repo.

Lo siguiente es [`S9.3 — Claude Code + infraestructura (Bicep, ARM, AVM)`](../S9.3-cc-infraestructura/MANUAL.md), donde los templates de IaC del submódulo actual se cruzan con la disciplina de `what-if` y AVM que viste en M08-S8.5.
