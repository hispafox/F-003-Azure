# Manual del alumno — S9.P · Práctica: Claude Code + MCP en acción

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, estructura del proyecto, endpoints, tests, flujo del alumno. Este manual va antes: te cuenta qué significa que esta práctica cierre cinco submódulos teóricos a la vez, por qué la analogía del primer vuelo en solitario del aprendiz de piloto encaja con todas las decisiones del ejemplo, y dónde está el "momento aha" pedagógico que la comparativa de prompts del slide 12 te entrega aunque no la veías venir.

Tiempo de lectura: ~25 min. Submódulo de referencia: [M09-S9.P](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.P-practica-cc-mcp-v3.md). Tres piezas de lógica pura (preflight de requisitos antes de arrancar, evaluador autoaplicable de los 8 ejercicios y comparador de prompts en 3 niveles de detalle) más un planificador que las une en el reporte de la práctica.

*Creado: 2026-05-21 21:58 +0200*

---

## 1. La idea en una frase

Esta práctica es el examen integrador del módulo: ocho ejercicios que cubren los cinco submódulos teóricos (instalar Claude Code, identificar casos de uso, generar IaC, conectar MCP con ADO, respetar las defensas del slide 9) y un noveno momento didáctico que casi nadie ve venir hasta que pasa por él, la comparativa de prompts vago / medio / detallado del slide 12. El ejemplo no ejecuta Claude Code por ti; te da las tres piezas para que evalúes tu propio trabajo: un preflight que detecta si tu entorno está listo, un evaluador que clasifica cada ejercicio en Pasa / Pendiente / Falla con acciones concretas, y un puntuador de prompts contra los cuatro ingredientes canónicos.

El alumno entrena dos decisiones operativas: **abortar antes de despegar si el preflight bloquea** (sin Node 18, sin `claude --version`, sin API key o sin repo local no se empieza, da igual cuántas ganas tengas) y **diferenciar los ejercicios que fallan duro de los que están a un paso de pasar** (Falla si no compila y los tests no pasan; Pendiente si compila pero la validación cojea o las convenciones no se respetan).

---

## 2. El problema real que hay detrás

Tres situaciones que aparecen en cualquier práctica integradora de Claude Code que un equipo intenta hacer sin red:

**Caso 1: el alumno que arrancó con Node 16 y se enteró media hora después.** Una alumna instala Claude Code siguiendo el README rápidamente y arranca el ejercicio 1 (generar un servicio completo con tests). El primer comando del CLI revienta con un error críptico de incompatibilidad. Tras media hora de tutoriales de Stack Overflow descubre que tenía Node 16 instalado (el LTS del año anterior que nunca actualizó), no Node 18 mínimo. Si hubiera pasado el preflight del ejemplo antes, lo habría sabido en cinco segundos con el hallazgo Bloqueante "Node.js 18+ instalado: Claude Code requiere Node 18+. Instala con `nvm install 18` o equivalente". El preflight no es ceremonia; es un check-list pre-vuelo que evita despegar con motor frío.

**Caso 2: el ejercicio 2 de Bicep que "está casi terminado pero".** Otro alumno termina el ejercicio 2 (generar Bicep desde requisitos). El Bicep generado compila (`az bicep build` OK), pero `az deployment group validate` falla con un error sobre un parámetro mal tipado. El alumno lo marca como "casi listo" y pasa al siguiente. Tres ejercicios después, cuando intenta encadenar el deploy real, se da cuenta de que el Bicep no es desplegable. El evaluador del ejemplo lo coge en el sitio: `CompilaOLintOk = true` y `TestsOValidatePasa = false` da `Pendiente`, no `Pasa`. La acción sugerida es directa: "`az deployment group validate` falla. Pásale a Claude el output y pide el fix". Sin esa clasificación, "casi listo" se convierte en deuda silenciosa que aparece dos ejercicios después.

**Caso 3: el momento de la comparativa de prompts que cambia la conversación.** Tercer alumno. Lleva siete ejercicios resueltos con prompts de tres líneas tipo "haz un servicio que devuelva clientes". Llega al ejercicio 7 (comparativa de prompts del slide 12) y el ejercicio le obliga a escribir el mismo prompt en tres niveles. Vago (≤ 40 chars, una frase suelta). Medio (con stack y una constraint). Detallado (con los 4 ingredientes: contexto, constraints, formato de salida, criterio de éxito). Pasa los tres por el puntuador y ve los números: 25/100 en el vago, 50/100 en el medio, 100/100 en el detallado. La lección del puntuador es contundente: "El nivel de detalle reduce iteraciones de 5-6 a 1-2 (slide 12 de S9.5)". Eso es el ROI del 30-50% del slide 7 del módulo S9.5 que el alumno había leído sin terminar de creérselo. Aquí lo ve numérico.

Los tres los ataca el ejemplo. `PracticaPreflight` evita el caso 1; `EjercicioEvaluator` con su clasificación Pasa / Pendiente / Falla evita el caso 2; `PromptComparison` con el cap a 25 si el prompt mide menos de 40 caracteres entrega el caso 3 como momento didáctico.

---

## 3. Por qué esto importa en tu stack

Si tu equipo o tú vais a hacer la práctica de M09 en serio, tres preguntas que conviene tener resueltas antes de empezar:

- **¿Cuáles son los requisitos no negociables del entorno y cuáles puedo dejar para más tarde?** El preflight del ejemplo lo separa por ti: Node 18, `claude --version` autenticado, API key y repo local son **Bloqueante** (sin esos no se empieza). CLAUDE.md, `az`, `gh` y acceso a ADO son **Aviso** (algunos ejercicios son opcionales, pero la práctica funciona sin ellos). Saber distinguir las dos columnas te ahorra falsos arranques.
- **¿Cómo sé si un ejercicio está completo, casi completo o roto?** El evaluador del ejemplo te lo dice con tres booleanos: compila, tests/validate pasa, convenciones aplicadas. Si los tres son `true`, `Pasa`. Si ni compila ni los tests pasan, `Falla` (problema serio). En medio, `Pendiente` con acciones concretas. La diferencia entre Pasa y Pendiente es operacional, no opinable.
- **¿Cómo demuestro que mi prompt mejoró de verdad?** El puntuador del slide 12 te asigna 25 puntos por cada uno de los 4 ingredientes detectados (contexto, constraints, formato de salida, criterio de éxito). Pasas los tres prompts, miras el delta vago→detallado, y si es mayor de 40 puntos, la lección dice literalmente "El nivel de detalle reduce iteraciones de 5-6 a 1-2". Esa es la métrica que justifica que tu equipo invierta tiempo en escribir prompts buenos.

Sin las tres respuestas claras, la práctica se convierte en "ejecuté Claude Code una tarde, salió algo, no sé si está bien". Con las tres respuestas, es una evidencia que puedes pegar al jefe de equipo con números reales.

---

## 4. La analogía vertebradora: el primer vuelo en solitario del aprendiz de piloto

Un aprendiz de piloto ha pasado por la teoría completa: aerodinámica, meteorología, navegación, instrumentación, procedimientos de emergencia. Ha volado decenas de horas con instructor sentado al lado tocando los mandos cuando algo se torcía. El día del primer **solo flight** el instructor ya no se monta. Se queda en la torre con la radio y observa. El aprendiz hace el preflight check (revisión exterior del avión, sistemas internos, panel de instrumentos), pide autorización de despegue, ejecuta ocho maniobras de su programa de instrucción (despegue, vuelo recto y nivelado, virajes coordinados, aproximación a un punto, aterrizaje normal, motor y al aire, aterrizaje corto, retorno a la pista) y aterriza. El instructor evalúa cada maniobra contra el manual oficial: técnica correcta, tolerancias dentro del rango (altura ±100 pies, rumbo ±10 grados), comunicación radio clara.

Esta práctica de S9.P es ese vuelo en solitario. La teoría son los cinco submódulos previos: S9.1 te enseñó a instalar el avión, S9.2 te enseñó a planificar rutas, S9.3 te enseñó a navegar con visibilidad reducida (Bicep), S9.4 te enseñó a operar el panel multi-radio (MCP), y S9.5 te enseñó los procedimientos de emergencia. Aquí ya no se discute la teoría, se ejecuta. Y se evalúa.

El preflight del ejemplo es literal: las cuatro comprobaciones bloqueantes (Node 18, Claude autenticado, API key, repo local) son el equivalente al check de motor, combustible, sistemas hidráulicos y autorización ATC: si una falla, el aprendiz no despega. Las cuatro de aviso (CLAUDE.md, az CLI, gh CLI, acceso a ADO) son el equivalente a la radio secundaria, el GPS auxiliar, el transponder en modo C: si fallan, el vuelo es posible con limitaciones, pero el aprendiz tiene que volar con esa precaución encima. La diferencia entre Bloqueante y Aviso no es arbitraria; es la diferencia entre "no despega" y "despega con cuidado".

Los ocho ejercicios son las ocho maniobras del programa. Cada uno se evalúa con tres criterios independientes: ¿el output **compila o lint pasa**? (técnica básica), ¿los **tests o el validate pasan**? (tolerancias dentro del rango), ¿el output **respeta las convenciones del proyecto**? (procedimiento estándar del avión, no improvisación). Los tres en verde dan `Pasa`. Los dos primeros en rojo dan `Falla` (la maniobra no se completó). Cualquier mezcla intermedia da `Pendiente` con acción concreta: "pásale a Claude el output del error", "pide el fix con `--no-restore`", "regenera los tests primero".

Y la comparativa de prompts del slide 12 es la **calidad de la comunicación radio del piloto** con la torre. Un aprendiz puede pilotar técnicamente bien pero hablar mal: "estoy bajando" (vago) vs "descending to 2000 feet" (medio) vs "Eagle 12, descending FL060 to 2000 feet, approaching VOR Bravo, ETA 5 minutes" (detallado, con los cuatro ingredientes: identificación, altura, posición, tiempo). La torre necesita el tercer nivel para coordinar tráfico; el segundo nivel hace que repita; el primero genera caos en la frecuencia. El puntuador del ejemplo aplica esa misma vara: prompts < 40 caracteres se capan en 25/100 da igual lo que digan; cada ingrediente vale 25 puntos; el delta vago→detallado se calcula y se anota. La torre, en este caso, es Claude.

Mantén la imagen: aprendiz con instructor en la torre, preflight obligatorio, ocho maniobras evaluadas con tres criterios cada una, comunicación radio con la torre como cuarto eje de evaluación. Toda la mecánica del submódulo encaja ahí.

---

## 5. Recorrido por el código: las tres piezas

### El preflight (`PracticaPreflight.Comprobar`)

La pieza más sencilla y la que más previene incidentes. Recibe un `EscenarioPreflight` con ocho banderas booleanas y devuelve un `ReportePreflight` con los hallazgos clasificados y la bandera maestra `ListoParaArrancar`. La lógica de la bandera maestra es estricta: cualquier hallazgo Bloqueante la pone a `false`.

```csharp
bool listo = !hallazgos.Any(h => h.Nivel == NivelPreflight.Bloqueante);
return new ReportePreflight(listo, hallazgos);
```

Cada comprobación se construye con el mismo helper `Check`, que devuelve el hallazgo OK si la bandera es `true`, o el hallazgo con el nivel de fallo correspondiente si es `false`:

```csharp
private static HallazgoPreflight Check(bool ok, string nombre, string mensaje, NivelPreflight nivelFallo)
    => ok
        ? new HallazgoPreflight(NivelPreflight.Ok, nombre, "OK.")
        : new HallazgoPreflight(nivelFallo, nombre, mensaje);
```

Esto permite que el reporte siempre tenga los ocho hallazgos completos, no solo los que fallaron. El alumno ve el check-list completo con su estado, igual que un piloto repasa los ocho ítems del preflight check exterior aunque siete estén bien. La disciplina es ver el cero, no asumir que está bien.

La separación entre Bloqueante y Aviso encapsula una decisión pedagógica importante: **no todos los requisitos faltantes te impiden empezar**. Sin Node 18 no hay nada que hacer; sin GitHub CLI puedes hacer 6 de los 8 ejercicios. Mezclar las dos en un solo "requisitos faltantes" haría que el alumno crea que todo es opcional o todo es obligatorio. La separación da granularidad operativa.

### El evaluador de ejercicios (`EjercicioEvaluator.Evaluar`)

La pieza con más matiz pedagógico del submódulo. Recibe una `EvidenciaEjercicio` con el enum del ejercicio (uno de los 8) y tres booleanos (`CompilaOLintOk`, `TestsOValidatePasa`, `OutputAplicaConvenciones`), y devuelve un `InformeEjercicio` con resultado y acciones sugeridas.

La clasificación tripartita es el corazón de la pieza:

```csharp
ResultadoEjercicio resultado;
if (acciones.Count == 0)
    resultado = ResultadoEjercicio.Pasa;
else if (!e.CompilaOLintOk && !e.TestsOValidatePasa)
    resultado = ResultadoEjercicio.Falla;
else
    resultado = ResultadoEjercicio.Pendiente;
```

Tres niveles, no dos. `Pasa` es cuando todo está en verde. `Falla` es la regresión seria: ni compila ni los tests pasan, el output no sirve. `Pendiente` es cualquier estado intermedio: compila pero los tests cojean, o todo verde menos las convenciones del proyecto. El nombre `Pendiente` (no `Casi`) refuerza que tiene que cerrarse antes de pasar al siguiente; no es un estado estable.

Cada ejercicio tiene sus propias sugerencias específicas si falla el criterio de compilación o el de tests. Por ejemplo, para el ejercicio de generar Bicep:

```csharp
Ejercicio.GenerarBicep =>
    "`az bicep build` falla. Pega el error a Claude y pídele el fix con " +
    "`--no-restore` si aplica (slide 4).",
```

Y para el ejercicio de MCP server custom:

```csharp
Ejercicio.McpServerCustom =>
    "El MCP server no arranca o los tools no validan. Usa `mcp-inspector` para " +
    "ver el schema y arreglar el error (slide 13).",
```

Las sugerencias son operacionalmente directas: "ejecuta `mcp-inspector`", "pásale el output a Claude", "pide el fix con `--no-restore`". El alumno no tiene que pensar qué hacer cuando falla; tiene el comando concreto. La diferencia con un evaluador genérico de "algo está mal, mira la documentación" es la diferencia entre un piloto recibiendo "ajusta la potencia" vs "reduce a 1900 RPM".

### El comparador de prompts (`PromptComparison.Comparar`)

La pieza con más valor didáctico de la práctica. Recibe tres strings (vago, medio, detallado) y devuelve un `ComparativaPrompts` con la puntuación de cada uno, el delta, y las lecciones que se extraen.

El núcleo es la tabla de los cuatro ingredientes con sus marcadores léxicos:

```csharp
private static readonly (string Ingrediente, string[] Marcadores)[] Ingredientes =
[
    ("Contexto", ["proyecto", "stack", ".net", "cosmos", "framework", "azure"]),
    ("Constraints", ["no añadir", "no romper", "mantén", "preserva", "respeta",
        "no inventes"]),
    ("Formato salida", ["output:", "salida:", "devuelve", "formato:", "json",
        "markdown", "guarda en", "archivos"]),
    ("Criterio éxito", ["criterio éxito", "criterio de éxito", "tests verdes",
        "compila", "sin warnings", "criterio:"]),
];
```

Cada ingrediente detectado vale 25 puntos: 4 × 25 = 100/100 si están los cuatro. Y hay un cap explícito que merece la pena entender:

```csharp
// Prompts < 40 chars son siempre vagos (cap a 25).
if (prompt.Trim().Length < 40) puntos = Math.Min(puntos, 25);
```

Aunque tu prompt vago tenga la palabra "stack" (contexto detectado) o "json" (formato detectado), si mide menos de 40 caracteres no llega a 50/100. La razón es operativa: en menos de 40 caracteres no se pueden expresar los cuatro ingredientes con suficiente precisión para que Claude los entienda. El cap previene falsos positivos del puntuador, no del prompt.

Y las lecciones se generan en función del delta, con un umbral simbólico de 40 puntos:

```csharp
if (pD.Puntuacion > pV.Puntuacion + 40)
    lecciones.Add("El nivel de detalle reduce iteraciones de 5-6 a 1-2 " +
        "(slide 12 de S9.5).");
else
    lecciones.Add("La diferencia entre vago y detallado es menor de lo esperado: " +
        "revisa que el prompt detallado incluya los 4 ingredientes.");
```

Si el delta es mayor de 40 puntos, la lección confirma la regla del slide 12. Si es menor, la lección invita a revisar (probablemente el prompt detallado se olvidó de algún ingrediente, no que la regla esté mal). Y si el prompt detallado se olvidó específicamente del Criterio éxito, hay una lección extra que lo anota: "Incluso el prompt detallado se olvidó del criterio éxito. Añade `tests verdes` o un umbral medible".

---

## 6. Los 8 ejercicios y por qué este orden

Los ocho ejercicios no están elegidos al azar: cubren los cinco submódulos teóricos en una progresión de menor a mayor complejidad operativa. Vale la pena verlo en tabla:

| Ej. | Slide | Tema | De qué submódulo viene |
| --- | --- | --- | --- |
| 1 | 3 | Generar servicio completo + tests | S9.2 (casos de uso) |
| 2 | 4 | Generar Bicep + `az bicep build` | S9.3 (CC + infraestructura) |
| 3 | 5 | MCP con Azure DevOps (opcional) | S9.4 (MCP) |
| 4 | 6 | Análisis de error de producción | S9.2 (escenario clásico) |
| 5 | 7 | Refactoring con IA | S9.2 + S9.5 (límites) |
| 6 | 11 | Generar documentación | S9.2 (escenario) |
| 7 | 12 | Comparativa de 3 prompts | S9.5 (calidad del prompt) |
| 8 | 13 | Crear MCP server custom | S9.4 (avanzado) |

Tres lecturas operativas de esta progresión:

La primera es que **los ejercicios 1, 4 y 6 cubren los tres casos de uso más comunes** del día a día (generar código nuevo, diagnosticar un error existente, escribir documentación). Si solo tienes tiempo para tres, son estos tres. Cubren el 80% del trabajo real con Claude Code.

La segunda es que **el ejercicio 7 es el "momento aha" pedagógico** del módulo entero, no solo de la práctica. La comparativa de prompts vago / medio / detallado entrega numéricamente lo que el alumno había leído cualitativamente en el slide 7 de S9.5. Si solo haces un ejercicio para convencer al equipo de que vale la pena escribir prompts buenos, es este.

La tercera es que **los ejercicios 3 y 8 son el techo de la práctica**, los más avanzados, marcados como opcionales en el preflight (acceso a ADO es Aviso, no Bloqueante). MCP custom es la transición de "uso Claude Code" a "construyo herramientas para mi equipo": ya no eres consumidor del ecosistema, eres productor.

---

## 7. La conversación con el equipo: ¿cómo se demuestra que la práctica se hizo?

Si tu equipo evalúa la práctica formalmente (formación interna, certificación de proyecto, evidencia de adopción de Claude Code), el patrón del ejemplo te entrega evidencia auditable sin esfuerzo extra:

1. Pasas el preflight con tu setup real; guardas el `ReportePreflight` como evidencia inicial.
2. Por cada ejercicio, marcas los tres flags (compila, tests/validate, convenciones) en `/practica/ejercicio`; el `InformeEjercicio` con el veredicto Pasa / Pendiente / Falla queda registrado.
3. Para el ejercicio 7, escribes los tres prompts, pasas por `/practica/comparativa`, y guardas el delta numérico como evidencia del slide 12.
4. El endpoint `/practica/plan` te compone todo: preflight + informes de los 8 ejercicios + comparativa + checklist de 10 puntos. Eso es el dossier que pegas al jefe de equipo o al expediente de formación.

La diferencia con una práctica autoevaluada en una hoja Excel es que el evaluador del ejemplo es determinístico: dos alumnos con la misma evidencia llegan al mismo veredicto. Eso reduce la fricción de "yo creo que lo aprobé pero el revisor dice que no", porque la regla es la misma para los dos. Y, sobre todo, el ejercicio 7 entrega una métrica numérica (delta de puntuación entre vago y detallado) que el equipo puede comparar entre alumnos para detectar quién dominó la escritura de prompts y quién todavía está en superficie.

---

## 8. La conversación con el formador: qué tolerancias aplica el evaluador

Hay una decisión sutil en el evaluador que conviene entender. La clasificación `Pendiente` cubre dos escenarios muy distintos en términos operativos:

- El alumno cuyo Bicep compila pero `validate` falla: está cerca, le falta una iteración con Claude para arreglar el error de tipado. Operacionalmente, una hora de trabajo.
- El alumno cuyo servicio compila y los tests pasan, pero el output no respeta las convenciones del proyecto: tiene un naming distinto al del CLAUDE.md, o ha generado clases en una carpeta que no es. Operacionalmente, también iterar con Claude pero apuntando al fichero de convenciones.

Los dos casos comparten el veredicto `Pendiente`, pero el formador puede distinguirlos leyendo las acciones sugeridas: la primera te dice "pásale el output a Claude y pide el fix"; la segunda te dice "el output no respeta las convenciones. Revisa `.claude/CLAUDE.md`". El formador que revisa la evidencia puede dar feedback distinto por cada caso sin recodificar el evaluador.

La decisión de no granular más el resultado (no inventar `CasiPasa` o `PendienteMenor`) es deliberada. Tres niveles son fáciles de defender en una conversación con el alumno; cinco se vuelven opinables. El evaluador es una herramienta operativa, no un examen calificado al detalle.

---

## 9. Cómo probarlo en local

Es un ejemplo offline al 100%. Tú haces los 8 ejercicios con Claude Code real en tu terminal y vas registrando evidencia en este API.

```bash
dotnet run --project src/Practica.CcMcp.Demo.Api
# http://localhost:5118
```

Cuatro endpoints útiles, todos POST con JSON:

```http
### Preflight con tu setup real
POST http://localhost:5118/practica/preflight
Content-Type: application/json

{
  "tieneNode18OSuperior": true,
  "claudeInstaladoYAutenticado": true,
  "tieneApiKey": true,
  "tieneRepoLocal": true,
  "claudeMdConfigurado": false,
  "tieneAzCli": true,
  "tieneGhCli": false,
  "tieneAccesoAdo": false
}
# → listoParaArrancar=true; 4 OK + 4 Aviso

### Evaluar un ejercicio concreto
POST http://localhost:5118/practica/ejercicio
Content-Type: application/json

{
  "ejercicio": "GenerarBicep",
  "compilaOLintOk": true,
  "testsOValidatePasa": false,
  "outputAplicaConvenciones": true
}
# → Pendiente, slide 4, acción: "`az deployment group validate` falla..."

### Comparativa de 3 prompts (slide 12)
POST http://localhost:5118/practica/comparativa
Content-Type: application/json

{
  "vago": "haz un servicio que devuelva clientes",
  "medio": "servicio C# .NET 10 que devuelva clientes con paginación",
  "detallado": "Servicio C# .NET 10 stack Azure que devuelva clientes con paginación. Constraint: no añadir dependencias nuevas, respeta el patrón Repository del proyecto. Formato salida: archivos en src/Customers/. Criterio éxito: tests verdes y compila sin warnings."
}
# → vago 25/100, medio 50/100, detallado 100/100; delta 75; lección "5-6 → 1-2"

### Plan completo
POST http://localhost:5118/practica/plan
Content-Type: application/json
{ "preflight": { ... }, "evidencias": [ ... ], "promptVago": "...", "promptMedio": "...", "promptDetallado": "..." }
# → preflight + 8 informes + comparativa + checklist de 10 puntos
```

Los 34 tests cubren:

- Capa 1 (unit): preflight con cada bandera individual (OK, Bloqueante, Aviso), evaluador con cada ejercicio y cada combinación de los tres flags (Pasa, Pendiente, Falla), puntuador con prompts de cada nivel y los casos límite (cap a 25 si < 40 chars, lección extra si Criterio éxito faltante).
- Capa 0 (DI): `IPracticaCcMcpPlanner` como singleton del contenedor real.
- Capa E2E: los cuatro endpoints via `WebApplicationFactory`.

No hay capa de integración real con Claude Code porque ejecutarlo consume tokens y requiere API key. La práctica de verdad la haces tú en tu terminal; este API valida que sabes medir si la hiciste bien.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 10. Anti-patterns

Cinco prácticas que evitar:

**Anti-pattern 1: saltarse el preflight porque "ya tengo todo instalado".** Es el caso 1 de la sección 2. El alumno asume que su setup está bien porque hace dos meses lo configuró. Nueve veces de diez funciona; la décima descubre a media práctica que la versión de Node es la antigua, que la API key caducó, que el repo local no tiene el `.git` o que CLAUDE.md no existe. Cinco segundos de preflight te ahorran horas de debugging encubierto.

**Anti-pattern 2: marcar como Pasa un ejercicio Pendiente "porque está casi".** Es el caso 2 de la sección 2. El evaluador da Pendiente cuando algo no está cerrado del todo; tratarlo como Pasa rompe la trazabilidad de la práctica y, más importante, deja deuda silenciosa que aparece en el siguiente ejercicio. La regla es estricta: si el evaluador dice Pendiente, cierras la acción sugerida antes de pasar al siguiente.

**Anti-pattern 3: hacer la comparativa de prompts (ejercicio 7) al final, deprisa, para terminar.** Es la pérdida del "momento aha" pedagógico. La comparativa va idealmente en medio de la práctica, no al final, para que las lecciones que entrega (4 ingredientes, delta operativo, reducción de iteraciones) se apliquen a los ejercicios que aún quedan. Si haces el 7 al final, te llevas la lección pero no la aplicas en la propia práctica.

**Anti-pattern 4: prompts vagos sistemáticos para "no perder tiempo escribiendo".** Es la trampa del slide 12 vista desde el otro lado. El alumno cree que escribir un prompt detallado le quita tiempo, así que lanza prompts vagos y luego itera con Claude. En el papel suena rápido; en la práctica son 5-6 iteraciones por prompt vago vs 1-2 por prompt detallado. El delta del puntuador lo confirma numéricamente; ignorarlo es cobardía cognitiva.

**Anti-pattern 5: tratar el `Aviso` del preflight como "no hace falta".** No es lo mismo Aviso que no aplicable. Sin `az` CLI puedes hacer 6 de los 8 ejercicios; los ejercicios 2 y 3 quedan limitados o se quedan sin completar. Si tu objetivo es cerrar la práctica entera, instala lo que esté en Aviso; si tu objetivo es entrenar los 6 ejercicios principales, el Aviso es aceptable. La decisión es tuya, pero consciente.

---

## 11. Glosario breve

- **Preflight check** (en aviación): revisión obligatoria del avión y los sistemas antes de cada vuelo. Aquí: revisión del entorno antes de empezar la práctica.
- **Bloqueante** (en preflight): hallazgo que impide arrancar la práctica. Sin Node 18, sin Claude autenticado, sin API key o sin repo local: no se empieza.
- **Aviso** (en preflight): hallazgo que limita pero no impide. Sin CLAUDE.md, `az`, `gh` o acceso ADO: algunos ejercicios se ven afectados.
- **Pasa / Pendiente / Falla**: niveles de veredicto del evaluador de ejercicios. Pasa = todo verde; Falla = ni compila ni tests; Pendiente = cualquier intermedio.
- **4 ingredientes del prompt** (slide 12): contexto, constraints, formato de salida, criterio de éxito. Cada uno vale 25 puntos en el puntuador.
- **Cap de 40 caracteres**: prompts más cortos que 40 caracteres se capan en 25/100 sin importar qué palabras contengan.
- **Delta vago→detallado**: diferencia de puntuación entre el prompt vago y el detallado. > 40 puntos confirma la regla del slide 12.
- **CLAUDE.md** (`.claude/CLAUDE.md`): archivo de convenciones del proyecto que Claude Code lee al arrancar. Sin él, cada sesión arranca de cero (anti-pattern de S9.5).
- **mcp-inspector**: herramienta CLI oficial de Anthropic para inspeccionar el schema de un MCP server custom. Útil cuando un server propio no arranca.
- **TDD-style** (en ejercicio 1): generar los tests primero, luego el servicio que los hace pasar. Anti-pattern correctivo del slide 9.

---

## 12. Cierre

Cuando termines la práctica completa, el dossier que llevas (preflight verde, 8 informes en Pasa, comparativa con delta > 40) no es solo un certificado de que ejecutaste los ejercicios. Es la evidencia de que tu equipo puede adoptar Claude Code de forma trazable: el preflight es el primer artefacto que entras al onboarding del próximo developer; la matriz de Pasa/Pendiente/Falla es el patrón que vais a aplicar a vuestros propios PRs cuando Claude genere código en producción; la comparativa numérica del slide 12 es la regla operativa que justifica que escribir prompts buenos sea parte del trabajo de equipo, no un gusto personal.

Lo siguiente es [`S9.P2 — Práctica: primer comando con Claude Code`](../S9.P2-practica-claude-code-primer-comando/MANUAL.md), una versión ligera para alumnos que arrancan con un solo comando end-to-end y un preflight reducido. Si esta práctica fue tu vuelo en solitario completo, la siguiente es la primera vuelta al campo con instructor todavía al lado.
