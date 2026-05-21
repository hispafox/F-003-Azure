# Manual del alumno — S9.5 · Buenas prácticas y limitaciones de IA en desarrollo

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, endpoints, tests, entregable de equipo. Este manual va antes: te cuenta por qué el último submódulo teórico del módulo es el más importante a largo plazo, por qué la analogía del auditor financiero externo encaja con los tres roles del ejemplo, y dónde están las defensas operativas que separan a un equipo que adopta Claude Code bien de uno que termina con código frankenstein en seis meses.

Tiempo de lectura: ~25 min. Submódulo de referencia: [M09-S9.5](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.5-buenas-practicas-limitaciones-v3.md). Tres piezas de lógica pura (detector de 10 anti-patterns del slide 13, validador de 7 secciones del prompt del slide 12, clasificador acelera vs frena del slide 5) más un planificador que las une con las 7 reglas de oro del slide 2 y una checklist defensiva de 10 puntos.

*Creado: 2026-05-21 22:12 +0200*

---

## 1. La idea en una frase

Este submódulo cierra los cinco teóricos del módulo con la decisión que tu equipo va a defender ante el primer incidente serio: no es "usamos Claude Code para todo" ni "Claude Code es peligroso, no lo toquéis", sino **un mapa de qué tareas acelera, qué tareas frena, qué anti-patterns son las puertas de entrada a la espiral del código frankenstein y qué estructura tiene un prompt que merece guardarse en `.claude/prompts/`**. El ejemplo no es un toolkit para usar Claude Code; es el dashboard pre-uso que decide si vas a usarlo, cómo y para qué.

El alumno entrena dos decisiones que su equipo va a aplicar todas las semanas: **clasificar una tarea del backlog como `[ia-acelera]`, `[ia-frena]` o `[ia-neutro]`** antes de asignarla (la matriz del slide 5 con sus 12 categorías y razones operativas) y **pasar cada prompt nuevo por el validador de 7 secciones** antes de pegarlo a Claude (la del slide 12, que amplía el modelo de 4 ingredientes del S9.2 con Input, Examples y Definition of Done).

---

## 2. El problema real que hay detrás

Tres situaciones que aparecen en cualquier equipo que adopta Claude Code sin defensas:

**Caso 1: el sprint que se fue a Claude y volvió con cuatro tickets duplicados.** Un equipo cierra el sprint el viernes; el product owner pide para el lunes "una pantalla de configuración del perfil de usuario". El developer lo coge, abre Claude Code y le dice "haz toda la pantalla de configuración del perfil". El sábado por la mañana hay 800 líneas de código generadas, ningún test, una clase llamada `UserProfileSettings` que se solapa con `AccountPreferences` que ya existía en el repo y dos llamadas a APIs internas que el alumno no sabe si existen. El lunes el código rompe el build. El detector del ejemplo lo coge en seco: la frase "toda la pantalla" dispara el anti-pattern #1 (`EscribemeTodoElSistema`), y el fix sugerido es "Iterar en chunks pequeños (1 endpoint a la vez) y commits frecuentes". No es una recomendación blanda; es la diferencia entre el sprint que se mete en producción y el que se queda en revisión otras dos semanas.

**Caso 2: el prompt que parecía detallado pero le faltaba la mitad.** Otra developer escribe un prompt que ella siente largo: tres párrafos explicando qué quiere, qué stack, cómo debe quedar el output. Lo pega a Claude. Claude devuelve algo que casi funciona pero "casi" significa que sigue fallando un test, el formato del JSON de salida no es el que ella esperaba (Claude eligió otro porque no estaba especificado), y hay una pequeña dependencia inventada porque Claude no sabía qué versión del framework usar. El validador de estructura del ejemplo lo coge: la developer puntúa 54/100 porque tiene Contexto + Objetivo + Constraints, pero le faltan Input (qué archivos lee), Output (cómo es exactamente el formato esperado), Examples (qué patrón existente debe imitar) y Definition of Done (`tests verdes y compila sin warnings`). Las cuatro sugerencias del validador son específicas, no genéricas: "Falta OUTPUT: formato esperado (archivos a generar, JSON, Markdown, etc.)". La diferencia entre 54/100 y 100/100 son tres frases más en el prompt y un 70% menos de iteraciones con Claude.

**Caso 3: el race condition que Claude "arregló" con un sleep y rompió mejor.** Tercer alumno. Tiene un bug intermitente en un test que falla una de cada cinco veces. Le pide a Claude que lo solucione. Claude analiza el código, decide que es un problema de timing y mete un `Thread.Sleep(500)` antes de la aserción. El test pasa local. En CI, en un agente lento, falla. En CI en un agente rápido, pasa. Cuatro semanas después en producción aparece el bug real (un acceso concurrente sin lock) y el `Sleep` solo lo enmascaró. El clasificador del ejemplo es categórico: `DebuggingRaceConditions` → `Frena` con la razón "Race conditions y timing-dependent bugs son difíciles incluso para humanos. Combina IA (lee el código, sugiere hipótesis) con repro determinístico". Esta es la categoría donde Claude no debe decidir por ti; debe ayudarte a pensar, no a parchear.

Los tres los previene el ejemplo. `AntiPatternDetector` coge el caso 1 con la frase canónica, `PromptStructureValidator` cuantifica el caso 2 con un número auditable, `AceleraOFrenaClassifier` clasifica el caso 3 antes de empezar para que ni siquiera lo intentes resolverlo con IA sola.

---

## 3. Por qué esto importa en tu stack

Si tu equipo ya está usando Claude Code en proyectos reales, tres preguntas que conviene tener resueltas en los próximos 30 días:

- **¿Cómo decidimos qué tareas mandamos a Claude y cuáles no?** La matriz `Acelera / Frena / Neutro` del ejemplo. Marcas los tickets del backlog según las 12 categorías del slide 5 y aplicas la regla simple: `[ia-acelera]` se asigna con prompt completo y revisión normal; `[ia-frena]` se asigna a un humano sin IA, o con IA como copiloto vigilado; `[ia-neutro]` se evalúa caso por caso con `claude -p` corto para ver si itera menos de 3 veces. Sin esta matriz, el equipo termina mezclando todo.
- **¿Cómo evitamos que el equipo caiga en los anti-patterns típicos sin verlo venir?** El detector del ejemplo se ejecuta no contra el código, sino contra **la descripción de cómo el equipo está usando Claude Code**. Cada semana o cada retro, alguien escribe un párrafo del tipo "esta semana hemos usado Claude para X, Y, Z" y lo pasa por `/limites/antipatterns`. Las frases canónicas que disparan ("sin revisar", "el primer output", "deja que Claude piense") son las puertas de entrada al código frankenstein; cazarlas es trabajo de proceso, no de herramienta.
- **¿Qué hace que un prompt sea bueno?** El validador del ejemplo cuantifica los 7 bloques del slide 12 con pesos (Contexto, Objetivo, Constraints y Definition of Done valen 18 puntos cada uno; Output y Constraints valen 15; Input y Examples 8). El umbral operativo es **≥ 80**: por debajo, el prompt no se guarda en la biblioteca del equipo. Esa biblioteca compartida es lo que diferencia al equipo que mejora con el tiempo del que repite los mismos prompts vagos cada semana.

Las tres preguntas se responden con tres endpoints. Si tu equipo no las tiene contestadas, la siguiente regresión en producción te va a llegar sin que sepas si es culpa de Claude o de cómo lo estábais usando.

---

## 4. La analogía vertebradora: el auditor financiero externo revisando los libros

Un auditor financiero externo entra a una empresa cada año antes del cierre fiscal. No es el contable interno (que lleva los libros día a día), no es el inspector de Hacienda (que viene si hay sospechas). Es la figura intermedia con un encargo concreto: emitir un informe que dice si las cuentas reflejan la imagen fiel del patrimonio. Tiene tres herramientas que aplica en cada auditoría sin negociar.

La primera es un cuaderno con las **diez banderas rojas típicas** que ha aprendido a detectar en doce años de oficio: movimientos sospechosos entre cuentas el último día del año, gastos sin justificante, IVA mal aplicado, conciliaciones bancarias que no cuadran, provisiones infladas, etc. Cuando habla con el equipo financiero, no se queda con "todo está bien"; lee el detalle del proceso y busca las frases canónicas que activan cada bandera. Esa es la pieza `AntiPatternDetector` del ejemplo: diez anti-patterns del slide 13, cada uno con sus frases canónicas que delatan ("todo el sistema", "sin revisar", "el primer output", "sin Managed Identity", "sin contexto de negocio"), cada uno con su causa real y su fix concreto.

La segunda es la **estructura obligatoria del informe** que el auditor entrega al final. Siete capítulos no negociables: alcance de la revisión, opinión sobre los estados, balance, cuenta de resultados, flujos de efectivo, notas a las cuentas, hallazgos. Si un capítulo falta, el informe no es válido y la auditoría no se firma. Cada capítulo tiene un peso: la opinión y los hallazgos valen más que las notas. Esa es la pieza `PromptStructureValidator` del ejemplo: siete secciones (Contexto, Objetivo, Constraints, Input, Output, Examples, Definition of Done), cada una con sus marcadores léxicos, cada una con su peso (18 los críticos: Contexto, Objetivo y DoD; 15 los importantes: Constraints, Output; 8 los nice-to-have: Input, Examples). Un prompt que no llegue a 80/100 es como un informe sin la sección de hallazgos: técnicamente lleva texto, pero no sirve para firmar.

La tercera es un **catálogo de riesgos por tipo de operación**. El auditor sabe por experiencia que las compras menores se auditan rápido y rara vez dan problema; los gastos de representación y las fusiones de filiales son trampas mortales que requieren expertise específica y tiempo. Catalogación, no juicio moral. Esa es la pieza `AceleraOFrenaClassifier` del ejemplo: doce tipos de tarea (boilerplate, transformaciones, IaC, docs, análisis de logs, refactoring mecánico → Acelera; lógica de negocio compleja, decisiones de arquitectura, optimización fina, seguridad crítica, race conditions → Frena; resto → Neutro), cada uno con dos razones operativas del slide 5.

Y por encima de todo, el auditor lleva consigo las **siete reglas de oro** del Plan General Contable que aplica sin pensar: revisar todo (nada queda sin firma), pedir contexto (no aceptar respuestas vagas), iterar (no firmar el primer borrador), tener evidencia (cada hallazgo con su soporte), no confiar ciegamente (recalcular incluso lo que parece obvio), proteger información sensible (datos de clientes nunca salen del despacho), documentar prompts útiles (las preguntas que funcionaron este año se reusan el siguiente). Esas son las siete reglas de oro del slide 2 del módulo, traducidas al oficio del developer.

Mantén la imagen: auditor con sus tres herramientas (cuaderno de banderas rojas, plantilla obligatoria del informe, catálogo de riesgos por operación) y sus siete reglas de oro como marco mental. Cada vez que pases un workflow de Claude Code por el ejemplo, estás haciendo esa misma auditoría sobre cómo tu equipo está usando la IA.

---

## 5. Recorrido por el código: las tres piezas

### El detector de anti-patterns (`AntiPatternDetector.Detectar`)

La pieza más operativa del submódulo. Recibe una **descripción libre** de cómo el equipo está usando Claude Code (típicamente uno o dos párrafos de retro o de daily) y devuelve un `InformeAntiPatterns` con la bandera `Limpio` y los `Hallazgos` clasificados.

La lógica es un mapeo de frases canónicas a anti-patterns. Cada uno de los 10 del slide 13 tiene un set de marcadores léxicos:

```csharp
(["todo el sistema", "todo el código", "todo el proyecto", "scaffold all"],
    AntiPattern.EscribemeTodoElSistema,
    "Pedirle a Claude que genere todo el sistema de una vez.",
    "Iterar en chunks pequeños (1 endpoint a la vez) y commits frecuentes."),

(["funciona, no toco", "no entiendo pero compila", "sin revisar", "sin entender"],
    AntiPattern.AceptarSinEntender,
    "Mergear código sin revisar línea a línea.",
    "Code review como si fuera un junior: lee cada línea y pregunta el porqué."),
```

Detalle de diseño importante: **el detector no duplica el mismo anti-pattern** aunque aparezcan varias palabras-clave del mismo grupo. Un `HashSet<AntiPattern>` registra qué patterns ya se reportaron:

```csharp
foreach (var (patrones, pattern, causa, fix) in Reglas)
{
    if (vistos.Contains(pattern)) continue;
    foreach (var p in patrones)
    {
        if (lower.Contains(p, StringComparison.Ordinal))
        {
            hallazgos.Add(new AntiPatternDetectado(pattern, causa, fix));
            vistos.Add(pattern);
            break;
        }
    }
}
```

Esto es importante operativamente: si la descripción dice "sin revisar y sin entender" no quieres dos hallazgos del mismo anti-pattern; quieres uno solo. La regla pedagógica es "el anti-pattern existe en el equipo", no "cuántas veces aparece la frase".

Y los 10 anti-patterns mismos son una lista curada: cada uno con su causa explicada en una línea operativa y un fix ejecutable. No son etiquetas; son banderas rojas con su solución al lado.

### El validador de estructura del prompt (`PromptStructureValidator.Validar`)

La pieza con más matiz pedagógico. Recibe un prompt como string y devuelve una `ValidacionEstructura` con la puntuación (0-100), las secciones detectadas, las que faltan y sugerencias para cada faltante.

La tabla de marcadores por sección es deliberadamente generosa:

```csharp
[SeccionPrompt.Contexto] =
    ["contexto", "stack", "proyecto", "framework", ".net", "arquitectura"],
[SeccionPrompt.Objetivo] =
    ["objetivo", "quiero lograr", "necesito", "crea", "genera", "refactoriza"],
[SeccionPrompt.Constraints] =
    ["constraints", "no añadir", "no romper", "no modificar", "mantener",
     "respetar", "sin cambios en"],
```

El validador no exige un literal "CONTEXTO:" como en formularios rígidos. Cualquier mención clara cuenta. La razón es operacional: pedirle al alumno que estructure el prompt con cabeceras formales es ceremonia que se salta; pedirle que **incluya el concepto** es realista y se mantiene.

Lo más interesante es el sistema de pesos:

```csharp
private static readonly Dictionary<SeccionPrompt, int> Pesos = new()
{
    [SeccionPrompt.Contexto] = 18,
    [SeccionPrompt.Objetivo] = 18,
    [SeccionPrompt.Constraints] = 15,
    [SeccionPrompt.Input] = 8,
    [SeccionPrompt.Output] = 15,
    [SeccionPrompt.Examples] = 8,
    [SeccionPrompt.DefinitionOfDone] = 18,
};
```

Tres tiers. Tier crítico (18 puntos): Contexto, Objetivo, Definition of Done. Sin estos tres el prompt no funciona: Claude no sabe dónde está, qué quieres ni cuándo parar. Tier importante (15 puntos): Constraints, Output. Sin estos el prompt funciona pero el output diverge del esperado. Tier nice-to-have (8 puntos): Input, Examples. Útiles pero no críticos.

Suma: 18×3 + 15×2 + 8×2 = 100. La aritmética está cuadrada para que el 100/100 sea alcanzable con los 7 ingredientes y el ≥ 80 sea alcanzable con los 5 críticos cubiertos (los tres de 18 + los dos de 15 dan 84).

Y las sugerencias son específicas por sección faltante:

```csharp
SeccionPrompt.Constraints =>
    "Faltan CONSTRAINTS: qué NO puede hacer (no añadir deps, no romper API pública).",
SeccionPrompt.DefinitionOfDone =>
    "Falta DoD: cómo sabremos que el resultado es correcto (tests verdes, métrica, etc.).",
```

El alumno no recibe "tu prompt está incompleto"; recibe la frase exacta que tiene que añadir.

### El clasificador acelera vs frena (`AceleraOFrenaClassifier.Clasificar`)

La pieza más estratégica del submódulo. Recibe un `TipoTareaIa` (12 valores enumerados) y devuelve una `ClasificacionTarea` con el impacto (Acelera, Frena o Neutro), el slide de referencia y dos razones operativas.

La división se basa en el slide 5 del módulo y no es opinable: hay tipos de tarea donde la IA acelera con números verificables y otros donde la IA frena igualmente con números verificables. El criterio operativo es el mismo: ¿la respuesta correcta tiene patrón conocido (acelera) o requiere expertise contextual del producto (frena)?

Los seis tipos de tarea donde acelera:

```csharp
TipoTareaIa.Boilerplate => new(tipo, ImpactoIa.Acelera, "5",
    ["Boilerplate (controllers, DTOs, tests) ahorra 60-80% de tiempo (slide 5).",
     "IA genera y humano revisa: ratio típico 5-7x velocidad."]),

TipoTareaIa.RefactoringMecanico => new(tipo, ImpactoIa.Acelera, "5",
    ["Renames, formatting, sustituir patrón A por B en N archivos.",
     "Subagent `code-reviewer` posterior valida que no haya regresiones."]),
```

Y los cinco donde frena:

```csharp
TipoTareaIa.OptimizacionFinaRendimiento => new(tipo, ImpactoIa.Frena, "5",
    ["Optimización fina necesita MEDIR antes (slide 5).",
     "Pásale métricas reales (P95/P99/RU) al prompt — sin medir es adivinanza."]),

TipoTareaIa.SeguridadCritica => new(tipo, ImpactoIa.Frena, "5",
    ["IA puede generar código inseguro si no pides seguridad explícitamente.",
     "Pide hardening + threat model en el prompt; ejecuta security review humana."]),
```

El detalle importante es que **Frena no significa "no uses Claude"**, significa "Claude no debe decidir por ti aquí". Para race conditions, Claude lee el código y sugiere hipótesis; tú validas con repro determinístico. Para seguridad crítica, Claude propone hardening; tú haces la security review. La diferencia con `Acelera` es que el ratio de revisión humana sobre output generado es más alto: en boilerplate revisas el 20% del código generado (lo importante); en seguridad crítica revisas el 100% línea por línea.

Y `Otro` da `Neutro` con la recomendación operativa de probar con `claude -p` corto: si itera menos de 3 veces, ese tipo de tarea entra en `Acelera`; si itera más, en `Frena`. El catálogo es vivo, no inmutable.

---

## 6. Las 7 reglas de oro como marco mental

Las reglas del slide 2 que el `LimitesPlanner` expone como propiedad estática son el resumen denso del módulo entero. Vale la pena verlas no como lista sino como **el ADN del equipo que adopta Claude Code bien**:

| # | Regla | A qué pieza del ejemplo se mapea |
| --- | --- | --- |
| 1 | Revisar siempre: IA genera, humano valida. Nunca mergear sin revisar. | `AntiPattern.AceptarSinEntender` |
| 2 | Dar contexto: prompts vagos producen código genérico. Sé específico. | `PromptStructureValidator` (las 7 secciones) |
| 3 | Iterar: el primer resultado rara vez es perfecto. Refina en 2-3 turnos. | `AntiPattern.ConfianzaEnPrimerOutput` |
| 4 | Tests primero: si los tests definen el comportamiento, el código generado es más fiable. | `AntiPattern.SkipTestsPorVelocidad` |
| 5 | No confiar ciegamente: la IA inventa APIs/métodos. Compila y ejecuta siempre. | `AntiPattern.ConfianzaEnPrimerOutput` + slide 4 |
| 6 | Seguridad: nunca pases secretos reales en el prompt. Variables de entorno. | `AntiPattern.SecretosOPiiEnPrompt` |
| 7 | Documentar prompts útiles: si un prompt funciona, guárdalo en `.claude/prompts/`. | `PromptStructureValidator` con umbral ≥ 80 |

Dos lecturas operativas:

La primera es que las reglas **no son mandamientos morales**, son operativas. Cada una se traduce a un comportamiento concreto del equipo (revisar, contextualizar, iterar, testear, ejecutar, sanitizar, documentar) que se puede auditar en una retro. Si un equipo no las cumple, el detector de anti-patterns lo va a coger antes de que el incidente aparezca.

La segunda es que la regla #7 (documentar prompts) es la que distingue al equipo que mejora del que repite. Sin la biblioteca de prompts, cada developer empieza de cero cada semana. Con ella, los prompts ganadores se reutilizan, se versionan, se mejoran. Y el validador del ejemplo es el filtro: solo los que llegan a ≥ 80 entran. Es la disciplina de ingeniería aplicada al output de IA.

---

## 7. La conversación con el equipo: ¿cómo introducimos esto sin parecer paternalistas?

Hay una resistencia psicológica natural a las defensas de IA en equipos que ya están usando Claude Code con entusiasmo. La conversación suele empezar con "no necesitamos un detector de anti-patterns, no somos críos". El argumento operativo para introducirlo sin fricciones es triple:

Primero, el detector **no se ejecuta sobre código** (que sería invasivo), se ejecuta sobre una descripción de uso que escribe el propio equipo en la retro. Es autoevaluación voluntaria, no vigilancia. La descripción puede ser "esta semana Claude generó tres controllers en X minutos" y el detector no genera ningún hallazgo; o puede ser "le pedimos a Claude que arreglara todo el módulo de auth" y dispara el #1. La diferencia es qué frases el equipo se atreve a escribir.

Segundo, el validador del prompt **se aplica antes de mergear el prompt a la biblioteca compartida** (`.claude/prompts/`), no a cada prompt individual del día a día. Eso significa que los developers escriben prompts como siempre durante la semana; pero cuando uno funciona muy bien y se quiere reutilizar, pasa por el validador y si llega a 80 entra. Es una regla de bibliotecario, no de policía.

Tercero, la matriz Acelera/Frena se aplica **en la planificación del sprint**, no en el momento de coger el ticket. Cuando el equipo discute el backlog, marca cada ticket con su tag `[ia-acelera]`, `[ia-frena]` o `[ia-neutro]`. Si un developer asignado a un ticket `[ia-frena]` decide usar Claude por su cuenta, no pasa nada operativo; pero el tag le recuerda que va a tener que revisar más. Es una etiqueta informativa, no un veto.

Tres mecanismos voluntarios. Si el equipo los rechaza después de probarlos seis semanas, la conversación pasa de "no necesitamos defensas" a "probamos y no encajaron". Eso ya es un debate sustantivo, no resistencia inicial.

---

## 8. La conversación con seguridad: alucinaciones y PII en el prompt

Si tu organización tiene un proceso de revisión de seguridad antes de adoptar herramientas de IA (DPIA, ISO 27001, GDPR), dos hallazgos del ejemplo van a ser los que más preguntas generen:

El primero es el anti-pattern #9 (`SecretosOPiiEnPrompt`): "Compartir secretos o PII reales en el prompt; salen de tu red". La causa operativa es que muchos developers, cuando depuran un caso real, copian y pegan el JSON completo del cliente con su email, su teléfono y a veces su número de tarjeta. El fix del ejemplo es directo: "Sanitiza antes de compartir: usa placeholders y MCP con tokens scope-limited (Enterprise = zero retention)". La frase "zero retention" es la palabra mágica que abre la conversación con compliance: Anthropic Enterprise se compromete a no retener prompts ni outputs para entrenamiento. Sin esa garantía contractual, los datos sensibles no pueden salir de la red interna.

El segundo es el slide 4 del módulo, que el ejemplo modela como el paso 5 de la checklist: "¿Has ejecutado / compilado el output antes de mergear?". Las alucinaciones de IA (APIs inventadas, métodos que no existen, configs imaginarias) son un riesgo de seguridad indirecto: el código compila y pasa los tests si Claude inventa un método con el nombre correcto pero comportamiento diferente. La defensa es "compilar y ejecutar siempre", que parece obvio pero el caso 3 de la sección 2 muestra cómo se cuela en producción cuando el test exhibe el bug "una de cada cinco veces".

Seguridad va a pedir documentación de las dos defensas. La buena noticia es que el ejemplo entrega los textos auditables: la lista de los 10 anti-patterns con su causa y fix, la checklist de 10 puntos del planner, las 7 reglas de oro. Pegas eso a la política interna y tienes el 80% del documento ya escrito.

---

## 9. Cómo probarlo en local

Es un ejemplo offline al 100%. No invoca Claude Code; modela las decisiones que tomas antes de invocarlo.

```bash
dotnet run --project src/ClaudeCode.Limites.Demo.Api
# http://localhost:5117
```

Cinco endpoints útiles:

```http
### Las 7 reglas de oro (slide 2)
GET http://localhost:5117/limites/reglas
# → lista de 7 strings con las reglas operativas

### Detectar anti-patterns en cómo el equipo usa Claude Code
POST http://localhost:5117/limites/antipatterns
Content-Type: application/json

{
  "descripcion": "Esta semana le pedimos a Claude que generara todo el sistema de auth de una vez. Aceptamos el primer output sin revisar porque funcionaba. Skip tests porque tenemos prisa."
}
# → 3 hallazgos: #1 EscribemeTodoElSistema, #2 AceptarSinEntender,
#   #4 SkipTestsPorVelocidad; cada uno con causa y fix

### Validar la estructura de un prompt (slide 12)
POST http://localhost:5117/limites/estructura
Content-Type: application/json

{
  "prompt": "Crea un servicio C# .NET 10 (stack Azure) que devuelva clientes paginados. Constraints: no añadir dependencias nuevas, respeta el patrón Repository. Output: archivos en src/Customers/. Criterio éxito: tests verdes y compila sin warnings."
}
# → puntuación ~84/100, falta Input y Examples, sugerencias específicas

### Clasificar acelera vs frena (slide 5)
GET http://localhost:5117/limites/acelera-o-frena/SeguridadCritica
# → impacto Frena, slide 5, dos razones operativas

GET http://localhost:5117/limites/acelera-o-frena/Boilerplate
# → impacto Acelera, slide 5, ratio 5-7x velocidad

### Plan completo
POST http://localhost:5117/limites/plan
Content-Type: application/json

{
  "descripcionUso": "Esta semana usamos Claude para generar Bicep y refactorizar nombres.",
  "promptDelAlumno": "Genera el Bicep modular para App Service + Cosmos con tags obligatorios. Stack Azure. Constraints: usa AVM modules. Output: main.bicep + modules/. Tests verdes con bicep build.",
  "tipoTarea": "InfrastructureAsCode"
}
# → antiPatterns: limpio; estructura: ~84/100; clasificación: Acelera;
#   reglas + checklist de 10 puntos
```

Los 44 tests cubren:

- Capa 1 (unit): detector con cada uno de los 10 anti-patterns y sus combinaciones (no duplica el mismo); validador con prompts de 7/7, parcial, vago, y verificando los pesos; clasificador con cada uno de los 12 tipos y sus razones.
- Capa 0 (DI): `ILimitesPlanner` resoluble del contenedor real.
- Capa E2E: los cinco endpoints via `WebApplicationFactory`.

No hay capa de integración real porque el ejemplo modela decisiones, no ejecuciones. Las decisiones son lógica pura, los tests las cubren al 100%.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 10. Anti-patterns

Cinco prácticas que evitar (meta: estos son los anti-patterns del uso del propio ejemplo, no los del slide 13):

**Anti-pattern 1: ejecutar el detector una vez y olvidarlo.** El detector no es un certificado anual; es una rutina de retrospectiva. La política operativa: cada quincena, alguien del equipo escribe un párrafo de cómo se está usando Claude y lo pasa por `/limites/antipatterns`. Si está limpio dos quincenas seguidas, el ritmo del equipo es sano; si aparece un hallazgo, se discute en la retro. El detector se vuelve ruido si solo se usa para checking inicial.

**Anti-pattern 2: usar el validador como excusa para no escribir prompts.** Algunos developers, al ver el umbral de 80/100, deciden que escribir prompts buenos es trabajo de senior y van a copiar los del repo. El validador no es un obstáculo; es la guía de qué falta. Un prompt nuevo con 54/100 sale del primer intento; el validador te dice qué tres cosas añadir para llegar a 84. El esfuerzo es de cinco minutos, no de una hora.

**Anti-pattern 3: catalogar todas las tareas como `[ia-neutro]` para no comprometerse.** La matriz Acelera/Frena requiere decisión. Si el equipo se refugia en `Neutro` para no discutir, pierde el valor pedagógico de la clasificación. El antídoto operativo: si un tipo de tarea aparece más de tres veces en sprints sucesivos como `Neutro`, se fuerza la decisión en la siguiente retro. Neutro es válido para casos puntuales, no para evitar la conversación.

**Anti-pattern 4: aplicar las 7 reglas de oro al pie de la letra sin contexto.** Las reglas son operativas pero no son leyes naturales. La regla #1 ("revisar siempre") aplica con intensidad distinta a un controller boilerplate (revisión del 20%) vs un módulo de seguridad (revisión del 100%). Aplicar el mismo nivel a todo es tan malo como no aplicar ninguno: bloquea las tareas donde la IA acelera y deja pasar las donde requiere atención máxima. La matriz Acelera/Frena del slide 5 es la que modula la intensidad.

**Anti-pattern 5: tratar los 10 anti-patterns como una lista negra de palabras.** El detector dispara con frases canónicas porque es lógica pura, no IA semántica. Pero la regla operativa del equipo no es "no digas esas palabras"; es "esas palabras son indicadores de un comportamiento subyacente". Si un developer dice "no entiendo pero compila" en una retro, el problema no es la frase, es la actitud. El detector la coge; arreglarla requiere conversación.

---

## 11. Glosario breve

- **Anti-pattern de IA** (slide 13): patrón de uso de Claude Code que produce código frankenstein o regresiones. El ejemplo cubre los 10 canónicos del módulo.
- **Las 7 reglas de oro** (slide 2): revisar, dar contexto, iterar, tests primero, no confiar ciegamente, seguridad, documentar prompts.
- **Las 7 secciones del prompt** (slide 12): Contexto, Objetivo, Constraints, Input, Output, Examples, Definition of Done. Amplía los 4 ingredientes del S9.2.
- **Definition of Done** (DoD): el criterio explícito que dice cuándo el output de Claude se considera completo. Tests verdes, compila sin warnings, métrica concreta.
- **Acelera / Frena / Neutro**: clasificación de tipos de tarea según el ROI de Claude Code. Acelera = patrón conocido, 5-7x velocidad. Frena = requiere expertise contextual, IA como copiloto vigilado. Neutro = evaluar caso por caso.
- **Código frankenstein**: código generado a trozos por Claude sin convenciones consistentes ni revisión completa. Anti-pattern terminal del módulo.
- **Alucinación de IA** (slide 4): API, método o config que la IA inventa sin que exista. La defensa es compilar y ejecutar siempre.
- **Biblioteca de prompts** (`.claude/prompts/`): repositorio versionado de los prompts del equipo que pasaron el validador con ≥ 80. Activo de equipo, no de individuo.
- **Zero retention** (Anthropic Enterprise): garantía contractual de que los prompts y outputs no se retienen para entrenamiento. Clave para compliance con PII.
- **Pair programming con IA**: modo de uso donde tú decides la dirección y Claude implementa. Antídoto al anti-pattern #7 (`ClaudeLoArreglaTodo`).
- **Race condition**: bug dependiente del orden de ejecución concurrente. Categoría `Frena` del clasificador porque incluso a humanos les cuesta y la IA tiende a parchear con sleeps.
- **MCP con tokens scope-limited** (slide 9): patrón de S9.4 aplicado a la sanitización del prompt. Las credenciales nunca entran en el prompt, entran como variables resueltas por el server MCP.

---

## 12. Cierre

Cuando termines de leer este manual, el cambio que se nota no está en cómo escribes prompts; está en cómo decides **qué tareas mandas a Claude antes de escribir el primer prompt**. El equipo que adopta Claude Code bien tiene esa conversación en cada planificación de sprint y cada retro. El que no la tiene termina en seis meses con código frankenstein que nadie quiere mantener y una espiral donde Claude es la excusa fácil para no entender lo que está pasando.

Lo siguiente del módulo es la práctica que cierra todo: [`S9.P — Práctica: Claude Code + MCP en acción`](../S9.P-practica-cc-mcp/MANUAL.md), donde aplicas los cinco submódulos teóricos sobre los 8 ejercicios y descubres en el ejercicio 7 que la diferencia entre un prompt vago y uno detallado se mide numéricamente y duele cuando ves el delta.
