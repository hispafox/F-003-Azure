# Manual del alumno — S9.P2 · Práctica: primer comando con Claude Code

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica: tabla de slides, endpoints, tests, flujo del alumno. Este manual va antes: te cuenta por qué esta práctica es la puerta de entrada del módulo y no el examen final, por qué la analogía del primer día del aprendiz en el bar de toda la vida encaja con todas las decisiones del ejemplo, y dónde está el detalle que la separa de la práctica avanzada del S9.P (que sí incluía MCP, Bicep y subagents).

Tiempo de lectura: ~20 min. Submódulo de referencia: [M09-S9.P2](../../../doc/M09-IA-Claude-Code/v3-actual/M09-S9.P2-practica-claude-code-primer-comando-v1.md). Tres piezas de lógica pura (preflight ligero con cuatro bloqueantes y dos avisos, evaluador de los 8 pasos secuenciales y detector de patterns del prompt con 3 anti-patterns y 2 buenos) más un planner que las une en el reporte de la práctica con los 8 slash commands canónicos del slide 9.

*Creado: 2026-05-21 22:37 +0200*

---

## 1. La idea en una frase

Esta práctica es la primera vez que el alumno toca Claude Code en su terminal con un objetivo claro: dejarlo instalado, autenticado, con `CLAUDE.md` generado, una sesión real ejecutada y un test xUnit pasando. No es el examen integrador (eso es S9.P, donde sí hay MCP, Bicep, subagents y los 4 ingredientes del prompt); es **el primer día**. Y el primer día tiene su propio paquete de defensas: un preflight más ligero (4 bloqueantes en vez de 4 + 4 avisos), un evaluador de 2 flags por paso (en vez de 3) y un detector de patterns que cubre los 5 más comunes que cualquier alumno hace en su primera semana, sin necesidad de los 10 del slide 13 que ya entrega S9.5.

El alumno entrena dos hábitos que llevará a todas las sesiones de Claude Code que haga después: **fijarse en si el output esperado es visible** (no basta con haber ejecutado el comando; tienes que ver el "Welcome to Claude Code", la línea de versión, el `Test passed (1/1)`) y **detectar en sus propios prompts los 3 anti-patterns del slide 12** ("mejora el código", "arregla los bugs", "haz todo el sistema") antes de enviarlos.

---

## 2. El problema real que hay detrás

Tres situaciones que aparecen en los primeros días de cualquier alumno que arranca con Claude Code:

**Caso 1: el alumno que instaló Claude pero no recordó hacer login.** Un alumno ejecuta `npm install -g @anthropic-ai/claude-code`, ve "added 1 package" y asume que está listo. Arranca `claude` desde un proyecto. El CLI pide login. El alumno cierra la terminal sin completar y vuelve al README pensando que algo salió mal con la instalación. Hora y media después se da cuenta de que la instalación fue perfecta; lo que faltaba era el segundo paso. El preflight del ejemplo lo coge: `Auth == MetodoAuth.Ninguno` da hallazgo Bloqueante con el mensaje exacto "Elige `claude.ai login` (recomendado) o `ANTHROPIC_API_KEY` (necesario en CI/CD). Sin un método válido, `claude` no arranca". El alumno que pasa el preflight antes de empezar no cae en esta trampa de los dos pasos colapsados en uno mental.

**Caso 2: la primera sesión donde no pasó nada visible.** Otra alumna ejecuta `claude` en un proyecto y le escribe "mejora el código". Claude le devuelve dos párrafos genéricos sobre buenas prácticas: "considera añadir más tests", "el naming podría ser más descriptivo", "verifica que respetes SOLID". La alumna lo lee, no encuentra nada accionable y cierra la sesión pensando que Claude Code "no es lo que esperaba". El detector del ejemplo lo coge: el prompt contiene "mejora el código", anti-pattern `AntiMuyGenerico`, causa "Prompt demasiado genérico: Claude no sabe qué priorizar (slide 12)", fix "Sé concreto: `Refactoriza X para extraer Y a un método separado llamado Z`". La diferencia entre los dos párrafos genéricos y un refactor real son veinte caracteres más en el prompt, no una herramienta mejor.

**Caso 3: el `/init` que el alumno saltó porque "no tenía tiempo".** Tercer alumno. Hace los pasos 1 a 6 sin problema (instalar, login, primera petición, ejecutar comandos, permission modes, slash commands). Llega al paso 7 (`/init` para generar `CLAUDE.md`) y decide saltárselo porque "tampoco entiende para qué sirve y se le acaba la hora de la práctica". Una semana después arranca Claude Code en el mismo proyecto y cada conversación le hace dar el contexto del repo desde cero. Pierde la mitad de cada sesión repitiendo "es un proyecto .NET, usamos el patrón X, las convenciones son Y". El evaluador del ejemplo no permite saltarse el paso: si `ComandoEjecutado == false` y `OutputEsperadoVisible == false`, el resultado es `Falla` con la sugerencia "Ejecuta `/init` para generar `CLAUDE.md` automáticamente (slide 10)". Cerrar el paso entonces tiene un coste de cinco minutos; saltárselo tiene un coste de horas semanales.

Los tres los previene el ejemplo. `PrimerComandoPreflight` coge el caso 1 antes de empezar, `PromptPatronDetector` el caso 2 con un veredicto numérico, `PasoEvaluator` el caso 3 forzando los dos flags por paso para que no se cierre con la mitad hecha.

---

## 3. Por qué esto importa en tu stack

Si vas a arrancar con Claude Code tú solo o vas a guiar a un equipo entero que arranca, tres preguntas que conviene resolver el primer día:

- **¿Qué tengo que tener instalado y configurado antes de escribir el primer prompt?** El preflight del ejemplo lo separa por gravedad: cuatro cosas son **Bloqueante** (Node 18+, cuenta Anthropic, método de autenticación elegido, repo donde practicar) y dos son **Aviso** (terminal moderna, git instalado). La diferencia es operativa: sin lo Bloqueante no se arranca; con un Aviso se arranca con cuidado.
- **¿Cómo sé que cada paso de los 8 se completó realmente?** El evaluador del ejemplo te pide dos flags por paso, no uno: `ComandoEjecutado` (lo escribiste y lo enviaste) y `OutputEsperadoVisible` (viste el resultado que decía el slide). Si solo tienes el primero, es `Pendiente`. La regla es estricta a propósito: el alumno que cree haber hecho un paso solo porque tecleó el comando se equivoca el 30% de las veces.
- **¿Cómo evito enviarle a Claude prompts que me van a devolver respuesta genérica y me van a frustrar?** El detector del ejemplo cubre los cinco patterns canónicos del slide 12: tres antis que restan 25 puntos cada uno ("mejora el código", "arregla los bugs", "monta todo el sistema") y dos positivos que suman 25 ("antes de implementar, dime cómo lo harías" y rubber duck "mi enfoque es X, ¿me explico mal?"). Base 50, cap [0, 100]. Cualquier prompt que baje de 50 lo refinas antes de enviarlo.

Con las tres preguntas resueltas, el primer día con Claude Code termina con sensación de control. Sin las tres preguntas resueltas, termina con frustración, "no es para mí" y una sesión más en el cementerio de herramientas instaladas y olvidadas.

---

## 4. La analogía vertebradora: el primer día del aprendiz en el bar de toda la vida

A las 6:30 de la mañana el bar Pepe abre, ya con los cafés calentando y el lavavajillas vacío. Pepe lleva el bar desde hace treinta años. Hoy llega Carlos, el aprendiz de prácticas, un chaval de 19 años que nunca ha trabajado de cara al público. Pepe no le da un manual; le pone el delantal y le dice "hoy aprendes ocho cosas y al final del día las haces tú solo". No hay barra avanzada de cocteles ni clientes complicados; el menú del primer día es básico: café, café con leche, cortado, caña, refresco, tostada, bocadillo, cobrar.

Antes de abrir, Pepe le hace el repaso de la barra: cafetera caliente (luz verde), leche en la jarra (no caducada, no a temperatura ambiente), vasos limpios secándose, terminal abierto, cambio en la caja. Si falta alguna de esas cuatro, el bar no abre; Pepe se queda media hora más. Si la radio FM se oye con interferencias o si Pepe se olvidó de comprar las pajitas, el bar abre igual pero con incomodidad puntual. Esa es la diferencia entre Bloqueante y Aviso del preflight del ejemplo: Node 18, cuenta Anthropic, método de autenticación, repo donde practicar son los cuatro de la barra. Terminal moderna y git son las pajitas y la radio.

Las ocho cosas que Carlos aprende el primer día son las ocho que tiene que dominar antes de quedarse solo: hacer un café solo decente, espumar la leche sin quemarla, abrir una botella sin que la espuma se desborde, servir una caña con el dedo de espuma correcta, atender al cliente mientras prepara, cobrar y dar cambio sin equivocarse, limpiar la barra cada quince minutos, cerrar la caja al final del turno cuadrando los números. Pepe no lo evalúa una vez al final; lo evalúa **cada vez que Carlos hace una**. Dos cosas tienen que ocurrir: el aprendiz ejecuta la acción (mueve la palanca de la cafetera) y el resultado esperado se ve (el café sale con crema, no aguado). Si las dos cosas ocurren, paso superado. Si solo se ejecuta la acción pero el resultado no es el esperado, paso pendiente; Carlos repite hasta verlo. Esa es la lógica del evaluador del ejemplo: dos flags, tres veredictos.

Y los gestos que Pepe vigila no son técnicos solo. Vigila también cómo Carlos se dirige a los clientes. El primer día le sale lo que sale: "qué quiere", muy genérico, sin saber si el cliente quiere desayuno o solo café. Pepe lo corrige: "pregunta concreto, dile 'café solo o cortado?' o 'tostada con tomate o con mantequilla?'". También vigila que Carlos no se ponga a improvisar bocadillos del menú avanzado el primer día ("haz todo el sistema" = "improvisa el bocata vegetal con guacamole"); le manda repetir el básico hasta que lo borda. Y vigila los gestos que muestran que el aprendiz piensa, no solo ejecuta: cuando Carlos dice "mi enfoque para servir esta mesa de cinco es traer primero los cafés y luego los bocadillos, ¿lo hago así?", Pepe sonríe; el chaval está aprendiendo a pensar el flujo, no solo a poner platos. Eso es el rubber duck del slide 12, traducido al bar.

El vocabulario del oficio se aprende sobre la marcha: "marchando", "para llevar", "con doble", "limpio", "voy" (señal universal del camarero que pasa con la bandeja). Son las ocho señas del oficio. En Claude Code son los ocho slash commands esenciales: `/help`, `/clear`, `/compact`, `/permissions`, `/exit`, `/cost`, `/model`, `/init`. No los dices todos cada vez; los tienes en el bolsillo y los sacas cuando hacen falta.

Mantén la imagen: bar Pepe a las 6:30, aprendiz Carlos con delantal, ocho oficios básicos del primer día, dos flags por evaluación, vocabulario del oficio como repertorio mental. Toda la mecánica del submódulo encaja ahí.

---

## 5. Recorrido por el código: las tres piezas

### El preflight ligero (`PrimerComandoPreflight.Comprobar`)

La pieza más sencilla del submódulo. Recibe un `EscenarioPreflight` con seis banderas (más el `MetodoAuth` como enum) y devuelve un `ReportePreflight` con los hallazgos y la bandera `ListoParaArrancar`.

La pieza interesante es el enum `MetodoAuth` con sus tres valores:

```csharp
public enum MetodoAuth { ClaudeAi, ApiKey, Ninguno }
```

Y la lógica de check correspondiente:

```csharp
Check(e.Auth != MetodoAuth.Ninguno,
    "Método de autenticación configurado",
    "Elige `claude.ai login` (recomendado) o `ANTHROPIC_API_KEY` " +
    "(necesario en CI/CD). Sin un método válido, `claude` no arranca.",
    NivelPreflight.Bloqueante),
```

El detalle pedagógico es que **`ClaudeAi` y `ApiKey` son igualmente válidos**. Muchos alumnos asumen que tienen que tener API key sí o sí desde el primer día; el preflight les aclara que `claude.ai login` (el método con OAuth contra la web de Anthropic) sirve perfectamente para esta práctica. La API key se queda para CI/CD, no para el desarrollo manual. Esa distinción se cuela en el primer mensaje del preflight y le ahorra al alumno una hora buscando dónde generar API keys.

Y la separación bloqueante vs aviso vuelve a ser la misma idea que ya viste en S9.P: cuatro bloqueantes son los que impiden arrancar (no hay Claude Code instalable sin Node 18, no hay sesión sin cuenta, no hay autenticación sin método, no hay práctica sin proyecto). Dos avisos son los que limitan: terminal moderna (sin Windows Terminal con PowerShell o WSL2 hay incomodidades con caracteres especiales) y git (técnicamente no obligatorio para arrancar, pero muy recomendable para versionar lo que Claude edite).

### El evaluador de los 8 pasos (`PasoEvaluator.Evaluar`)

La pieza con el ritmo pedagógico del submódulo. Recibe una `EvidenciaPaso` con el enum del paso, dos flags (`ComandoEjecutado`, `OutputEsperadoVisible`) y un comentario opcional. Devuelve un `InformePaso` con resultado y acciones sugeridas.

La diferencia con el evaluador de S9.P es que aquí son **dos flags por paso, no tres**. La práctica simplificada no exige el flag de "respeta las convenciones del proyecto" porque el alumno todavía no tiene `CLAUDE.md` hasta el paso 7. Mantenerlo en dos flags hace que la práctica sea defendible para alumnos que arrancan: dos cosas tienen que ocurrir, no tres.

La clasificación es la misma estructura que en S9.P:

```csharp
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

Lo más operativamente útil son las sugerencias específicas por paso. Para el paso 1 (instalar el CLI):

```csharp
Paso.InstalarCli =>
    "Ejecuta `npm install -g @anthropic-ai/claude-code` y luego `claude --version` " +
    "(slide 4).",
```

Y la sugerencia del output esperado:

```csharp
Paso.InstalarCli =>
    "`claude --version` debe devolver `1.x.x`. Si falla con permisos, usa npm con `--prefix` o " +
    "PowerShell como admin (slide 4).",
```

Cada uno de los 8 pasos tiene su par de sugerencias (comando + output). El alumno no recibe genéricos como "verifica el paso"; recibe **el comando exacto** y **el output exacto que tiene que ver**. Para el paso 4 (ejecutar comandos):

```csharp
Paso.EjecutarComandos =>
    "Claude debe pedir tu confirmación con `[y/N]` antes de ejecutar el comando shell (slide 7).",
```

El alumno que no ve el `[y/N]` no ha entendido cómo Claude Code maneja la seguridad de los shell commands. El que lo ve interioriza el modelo: Claude **propone**, tú **autorizas**.

### El detector de patterns de prompt (`PromptPatronDetector.Analizar`)

La pieza más didáctica del submódulo. Recibe un string con el prompt del alumno y devuelve un `AnalisisPrompt` con la bandera `TieneAntiPatterns`, los hallazgos detectados y una puntuación 0-100.

La lógica del scoring es sencilla pero deliberada:

```csharp
// Base 50: prompt neutro. Cada anti-pattern resta 25 (cap 0).
// Cada pattern positivo suma 25 (cap 100).
int p = 50;
foreach (var h in hallazgos)
{
    if (EsAntiPattern(h.Patron)) p -= 25;
    else p += 25;
}
return Math.Clamp(p, 0, 100);
```

Tres detalles importantes del diseño.

El primero es que **un prompt neutro vale 50/100, no 100**. El alumno que escribe "explícame Program.cs" no entra ni en anti ni en positivo: es neutro, vale 50. El sistema no premia la ausencia de anti-patterns; premia la presencia activa de patterns positivos (confirmación previa o rubber duck). Esto empuja al alumno a aprender a pensar la conversación con Claude, no solo a evitar las frases prohibidas.

El segundo es que **anti y positivo se compensan a 50**. Si el prompt dice "mejora el código, mi enfoque es extraer el método CalcularTotal a una clase separada", el detector encuentra un anti (`AntiMuyGenerico`) y un positivo (`BuenoRubberDuck`); 50 - 25 + 25 = 50. La lección operativa es que un pattern positivo no compensa un anti-pattern; los dos hay que tratarlos. El alumno que sabe esto, no lo intenta "salvar" diciendo cosas buenas después de cosas malas; reescribe el principio.

El tercero es la **lista de frases canónicas por pattern**. Para `AntiPedirleAdivinar`:

```csharp
(["arregla los bugs", "arregla todo", "fix the bugs", "arregla esto",
    "arréglalo"],
    PatronPrompt.AntiPedirleAdivinar,
    "Pides a Claude que adivine qué bug arreglar (slide 12).",
    "Da el síntoma: `Cuando ejecuto X sale el error Y en la línea Z. Arréglalo`."),
```

La causa y el fix son operacionalmente directos. El alumno que recibe "Pides a Claude que adivine qué bug arreglar" entiende inmediatamente el problema; el fix le da el formato exacto para arreglarlo. No es una recomendación abstracta sobre "ser específico"; es "añade el síntoma concreto, dónde sale, en qué línea".

---

## 6. Los 8 slash commands esenciales como repertorio mental

La propiedad estática `SlashCommandsEsencialesSlide9` del planner expone los 8 slash commands que el alumno tiene que tener en la cabeza después de esta práctica. Vale la pena verlos no como lista sino como **vocabulario operativo**:

| Slash | Propósito | Cuándo lo usas |
| --- | --- | --- |
| `/help` | Ver ayuda dentro de la sesión | Cuando no recuerdas un comando |
| `/clear` | Limpiar pantalla | Antes de pegar un fragmento largo y querer verlo limpio |
| `/compact` | Compactar el contexto cuando se acerca al límite | Después de 20-30 turnos largos, antes de que la sesión se ralentice |
| `/permissions` | Cambiar entre default / acceptEdits / plan | Cuando el flujo iterativo cuesta porque Claude pregunta cada edit |
| `/exit` | Salir de Claude Code limpiamente | Al final de la sesión, no con `Ctrl+C` |
| `/cost` | Ver tokens usados en la sesión | Cuando quieres saber cuánto te cuesta una conversación |
| `/model` | Cambiar entre Opus / Sonnet / Haiku | Para tareas rápidas (Haiku) vs análisis profundo (Opus) |
| `/init` | Generar `CLAUDE.md` inicial del proyecto | Una sola vez, al abrir Claude en un repo nuevo |

Dos lecturas operativas de esta tabla:

La primera es que **no son todos igualmente urgentes**. `/help` y `/exit` son inmediatos (los necesitas el primer día). `/cost` y `/model` son optimización (los descubres cuando tienes una rutina). `/init` y `/permissions` son los que cambian la experiencia: sin `CLAUDE.md` cada sesión arranca de cero; sin saber alternar permission modes Claude pregunta confirmación por cada edit y la práctica se vuelve lenta. La práctica de S9.P2 te empuja a tocar los ocho al menos una vez para que sepan que existen.

La segunda es que **`/init` es el único que cambia el estado del proyecto en disco**. Los otros son comandos de sesión: cambian el comportamiento del CLI o muestran información. `/init` crea el `CLAUDE.md` que va a quedarse en el repo y se va a versionar con git. Por eso el paso 7 de la práctica le dedica un comando entero: no es un slash más; es el que convierte el proyecto en un proyecto donde Claude trabaja con contexto.

---

## 7. La conversación con el alumno mayor o más senior: ¿por qué este nivel de simplificación?

Si tu equipo tiene un mix de developers junior y senior, y los senior miran el preflight de 4 bloqueantes con cara de "yo ya sé esto", la respuesta operativa es triple.

Primero: el preflight no es ceremonia para senior, es **scaffold de adopción**. Un senior que arranca Claude Code sin haber pasado por la práctica simplificada va a saltarse `/init`, no va a documentar las convenciones del proyecto, y dos meses después va a estar repitiendo el mismo contexto cada sesión. La práctica le obliga a hacer el paso 7 al menos una vez, y eso se queda como rutina.

Segundo: los 5 patterns del detector son los que un senior **comete a pesar de saber que no debe**. "Mejora el código" no es un anti-pattern de junior; es un anti-pattern de viernes a las cinco de la tarde cuando tienes prisa. El detector dispara igual contra el senior cansado que contra el junior aprendiendo. La validación numérica desactiva el "yo no caería en eso" sin ofender a nadie.

Tercero: la práctica avanzada de S9.P (con MCP, Bicep, comparativa de prompts vago/medio/detallado) es donde el senior demuestra técnica. La de S9.P2 es donde **consolida higiene de sesión**. Las dos son necesarias y no se solapan; saltarse esta porque parece básica es saltarse las defensas que evitan el código frankenstein del slide 13.

La conversación con el equipo no es "todos los senior tienen que hacer la práctica simplificada"; es "todos los senior tienen que pasar por los pasos 5 (`/permissions`), 7 (`/init`) y 8 (test xUnit pidiendo a Claude). El resto se asume".

---

## 8. Cómo probarlo en local

Es un ejemplo offline al 100%. Tú haces los 8 pasos con Claude Code real en tu terminal y vas registrando evidencia en este API.

```bash
dotnet run --project src/Practica.PrimerComando.Demo.Api
# http://localhost:5119
```

Cinco endpoints útiles:

```http
### Preflight con tu setup real
POST http://localhost:5119/primercomando/preflight
Content-Type: application/json

{
  "tieneNode18OSuperior": true,
  "tieneCuentaAnthropic": true,
  "auth": "ClaudeAi",
  "tieneTerminalModerna": true,
  "tieneGit": true,
  "tieneRepoPracticar": true
}
# → listoParaArrancar=true; 4 OK + 2 OK

### Evaluar un paso concreto
POST http://localhost:5119/primercomando/paso
Content-Type: application/json

{
  "paso": "CrearClaudeMd",
  "comandoEjecutado": true,
  "outputEsperadoVisible": false
}
# → Pendiente, slide 10, acción: "`CLAUDE.md` queda en la raíz..."

### Los 8 slash commands canónicos (slide 9)
GET http://localhost:5119/primercomando/slash-commands
# → lista de 8 strings con cada slash y su propósito

### Analizar un prompt
POST http://localhost:5119/primercomando/prompt
Content-Type: application/json

{
  "prompt": "mejora el código, mi enfoque es extraer CalcularTotal a una clase Servicio"
}
# → 1 anti (AntiMuyGenerico) + 1 positivo (BuenoRubberDuck); puntuación 50

### Plan completo (preflight + 8 pasos + análisis + slashes + checklist)
POST http://localhost:5119/primercomando/plan
Content-Type: application/json
{ "preflight": { ... }, "evidencias": [ ... ], "promptDelAlumno": "..." }
# → reporte completo de la práctica
```

Los 39 tests cubren:

- Capa 1 (unit): preflight con cada bandera individual (incluyendo `MetodoAuth.ApiKey` como equivalente válido a `ClaudeAi`), evaluador con cada uno de los 8 pasos y las 3 combinaciones de flags (00→Falla, 11→Pasa, 01 o 10→Pendiente), detector con los 5 patterns canónicos y los cálculos de puntuación.
- Capa 0 (DI): el planner resoluble del contenedor como singleton.
- Capa E2E: los cinco endpoints via `WebApplicationFactory`.

No hay capa de integración real con Claude Code; la práctica de verdad la haces tú en tu terminal y este API valida que reconoces cuándo cada paso se completó.

> Yo no lanzo apps. Tú haces `dotnet run` y `dotnet test`.

---

## 9. Anti-patterns

Cinco prácticas a evitar el primer día (meta: estos son los anti-patterns de cómo abordas la práctica, no los del slide 12 del módulo):

**Anti-pattern 1: instalar y dar por hecho el login.** Es el caso 1 de la sección 2. La instalación es `npm install`; el login es un comando aparte (`claude` arranca y pide la autenticación). El alumno que cierra terminal después del `npm install` y abre Claude por segunda vez una semana después no sabe en qué punto del flujo está. El preflight con la separación `TieneCuentaAnthropic` + `Auth != Ninguno` lo coge.

**Anti-pattern 2: marcar un paso como hecho con un solo flag.** El evaluador pide dos: comando ejecutado y output esperado visible. Marcar como Pasa con solo el primero es lo que produce el "yo creo que lo hice" que falla en el siguiente paso. La regla operativa es estricta a propósito: si no ves el `Test passed (1/1)` del paso 8, el paso 8 no está hecho.

**Anti-pattern 3: pasar el prompt al detector solo cuando ya falló.** El detector no es post-mortem; es **pre-envío**. El alumno pega su prompt en `/primercomando/prompt`, ve la puntuación, refina si baja de 50, y solo entonces lo envía a Claude. Aplicarlo después de que Claude haya devuelto algo genérico es perder la oportunidad pedagógica.

**Anti-pattern 4: saltarse `/init` porque "ya añadiré CLAUDE.md a mano luego".** Es el caso 3 de la sección 2. La diferencia entre el `CLAUDE.md` generado por `/init` y el que escribes a mano una semana después es de tres horas y rara vez se hace. La fricción es escribirlo en frío; el `/init` lo hace en treinta segundos con lo que Claude ya ha leído del proyecto. Mejor revisar y pulir lo que `/init` genera que escribir desde cero.

**Anti-pattern 5: hacer la práctica con un proyecto vacío "para no romper nada".** Sí es buena idea practicar en un sample, no en el repo de producción del cliente. Pero practicar en un proyecto **vacío** (un `dotnet new console` recién hecho) deja la práctica sin tracción: Claude no tiene qué leer, las explicaciones del paso 3 son triviales, el `/init` genera un `CLAUDE.md` de tres líneas. Clona `dotnet/samples` o reutiliza un ejemplo de los módulos anteriores (M02-S2.P2 o M03-S3.P2 son buenos candidatos). El alumno necesita algo no trivial para que la práctica enseñe.

---

## 10. Glosario breve

- **Preflight check**: revisión del entorno antes de empezar la práctica. Más ligero aquí que en S9.P (4 bloqueantes + 2 avisos en vez de 4 + 4).
- **Permission mode** (slide 8): modo de permisos de Claude Code. `default` (pide confirmación cada cambio), `acceptEdits` (acepta edits sin preguntar, pregunta shell), `plan` (solo planifica, no ejecuta).
- **`/init`** (slide 10): slash command que genera `CLAUDE.md` automáticamente leyendo el proyecto. Una sola vez por repo.
- **`CLAUDE.md`**: archivo en la raíz del proyecto con secciones Overview / Tech Stack / Key Files / Common Tasks / Conventions. Lo lee Claude al arrancar la sesión.
- **`MetodoAuth.ClaudeAi`**: autenticación contra claude.ai (OAuth con la cuenta web). Recomendado para desarrollo manual.
- **`MetodoAuth.ApiKey`**: autenticación con `ANTHROPIC_API_KEY` exportada. Necesario en CI/CD.
- **`ComandoEjecutado` / `OutputEsperadoVisible`**: los dos flags del evaluador. Hacer una cosa sin la otra es `Pendiente`, no `Pasa`.
- **Anti-pattern de prompt** (slide 12): `AntiMuyGenerico` ("mejora el código"), `AntiPedirleAdivinar` ("arregla los bugs"), `AntiTodoDeGolpe` ("haz todo el sistema"). Restan 25 puntos cada uno.
- **Pattern positivo de prompt** (slide 12): `BuenoConfirmacionPrevia` ("antes de implementar, dime cómo lo harías") y `BuenoRubberDuck` ("mi enfoque es X, ¿me explico mal?"). Suman 25 puntos cada uno.
- **Slash command esencial** (slide 9): los 8 del repertorio mental: `/help`, `/clear`, `/compact`, `/permissions`, `/exit`, `/cost`, `/model`, `/init`.
- **Rubber duck**: técnica de pensamiento en voz alta donde explicas tu enfoque a alguien (un pato de goma originalmente; aquí Claude) para detectar fallos mentales. Pattern positivo del slide 12.

---

## 11. Cierre

Cuando termines esta práctica el primer día, tu setup va a estar listo para todo lo que viene: Claude Code instalado, autenticado, `CLAUDE.md` generado en tu repo de pruebas, un test xUnit pasando y tres anti-patterns que reconoces en cinco segundos antes de enviarlos. Es poco para presumir en LinkedIn; es mucho para no abandonar Claude Code la semana siguiente.

Con esto cerramos M09 (7/7): los cinco submódulos teóricos (S9.1 a S9.5) y las dos prácticas (S9.P y S9.P2). Lo siguiente del curso es M10, el Proyecto Integrador, donde se aplican los nueve módulos previos sobre un caso completo que cruza arquitectura, App Services, Functions, almacenamiento, seguridad, MSIX, DevOps y la asistencia de IA que acabas de aprender a manejar.
