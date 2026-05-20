---
name: documentador-ejemplos
description: >-
  Genera el MANUAL.md (manual del alumno) de un ejemplo del curso
  F-003-Azure: documento pedagógico + técnico + de puesta en marcha,
  separado y complementario del README.md técnico que ya existe en cada
  ejemplo. Úsalo cuando el usuario pida documentar un ejemplo, "crear el
  manual de SX.Y / MXX", "documenta este ejemplo", "manual del alumno",
  o generar/validar un MANUAL.md en examples/MXX-*/SY.Z-*/. Cubre cinco
  capacidades: generar el manual, validarlo contra checklist, enlazarlo
  en los índices, calcar el ejemplo canónico de referencia (S5.1) y
  aplicar SIEMPRE el skill `escritura-humana` antes de redactar prosa.
---

# Documentador de ejemplos — F-003-Azure

Este skill destila el proceso, la estructura, el tono y las reglas duras
con que se construye el manual del alumno de cada ejemplo. **Léete entero
el manual canónico de referencia** (`examples/M05-Almacenamiento-BBDD/S5.1-azure-storage/MANUAL.md`)
antes de escribir nada — es la vara de tono y profundidad.

## Paso 0 — escritura-humana (obligatorio antes de redactar)

> **Regla dura del usuario** (memoria `feedback-siempre-escritura-humana`):
> jamás redactar prosa en español sin aplicar el skill `escritura-humana`.
> Sin él, el texto sale con calcos del inglés, tripletes simétricos,
> frase-cápsula como entrega por defecto, jerga de plantilla y cero voz
> de formador. Pasa de "documentación que enseña" a "documentación que
> rellena espacio". Inaceptable.

Antes de escribir una sola sección del MANUAL:

1. Carga en contexto `.claude/skills/escritura-humana/SKILL.md` y sus
   cuatro referencias:
   - `references/lista-negra.md` — palabras, conectores y patrones prohibidos.
   - `references/lista-blanca.md` — arranques y conectores que sí funcionan.
   - `references/antipatrones-y-ejemplos.md` — los 15 antipatrones con
     ejemplos antes/después.
   - `references/distribucion-humor.md` — frecuencia y tipos por longitud.
2. Calibra: un MANUAL completo ronda 5000-7000 palabras → **texto muy
   largo**. Por documento:
   - **3-5 analogías** (1 vertebradora desarrollada en §4 + otras
     puntuales donde más ayuden a la comprensión).
   - **6-10 toques de humor** distribuidos *asimétricamente* (ironía
     seca, autocrítica, understatement, hipérbole calculada).
   - **3-6 historias** (al menos una "del desastre" o "del alumno" en
     §1 o §2 que ancle el tema).
3. Al terminar el MANUAL:
   - `grep -nE` de los términos prohibidos más comunes contra el
     archivo (ver bloque J del checklist).
   - Relectura aplicando el **test del café** ("¿le dirías esto a un
     alumno tomándote un café después de clase?").

**Aplica este paso 0 SIEMPRE**. No hay "esta sección es corta, paso del
skill". El skill se aplica desde la primera frase hasta la última.

## Qué es y qué NO es un MANUAL.md

Cada ejemplo ya tiene un `README.md` técnico (estructura, mapeo a slides,
comandos de test, despliegue por Portal). El `MANUAL.md` es **otro
documento, no un sustituto**:

| README.md (no se toca) | MANUAL.md (lo que generas) |
| --- | --- |
| Ficha técnica de referencia | Manual del alumno: el para qué y el porqué |
| Para quien ya lee el código | Para el alumno que quiere entender antes de leer código |
| Enumera qué hay | Cuenta por qué está y qué hay que entender |
| Estructura, slides, deploy | Escenario real (con historia), decisiones, SDK explicado, puesta en marcha guiada, autoevaluación |

Regla de oro: el MANUAL no duplica el README. Cuando necesite lo
operativo de referencia (Portal, scripts `az`, mapeo completo de
slides), **enlaza al README**; no lo copies.

## Inputs

Submódulo `MXX-SY.Z` o ruta `examples/MXX-*/SY.Z-*/`. Si es ambiguo,
pídelo. Un manual a la vez, y enseña el primero para validar tono antes
de seguir.

## Proceso

1. **Paso 0 cargado** (ver arriba).
2. **Leer la teoría del submódulo** (`doc/MXX-*/v*-actual/MXX-SY.Z-*.md`,
   1000-1800 líneas → por tramos). De ahí: el escenario real, las
   decisiones, los números (coste, límites) y los **slides** que citarás.
3. **Leer el README.md del ejemplo** — para saber qué NO repetir.
4. **Leer el código fuente clave** (no inventes nada técnico):
   `Program.cs` (DI, bifurcaciones de config), `Endpoints/`/`[Function]`s,
   clases puras (`*Policy`/`*Advisor`/...), `Repositories/`, `Models/`,
   `api.http`, `Properties/launchSettings.json` (puerto exacto),
   `global.json` (SDK), `*.csproj`/`.slnx`, `appsettings.Development.json`,
   tests (conteo y capas), README (test count). Todo dato técnico —
   puerto, comando, conteo de tests, versión SDK— **verificado contra
   el repo**, jamás supuesto.
5. **Identificar el "para qué real"** y la **historia ancla** antes de
   redactar. Una frase: ¿qué decisión silenciosa entrena este submódulo?
   ¿Qué historia real o plausible la ilustra (desastre, alumno,
   descubrimiento, antes/después)? Esa historia abre §1 o §2.
6. **Redactar** siguiendo `reference/plantilla-manual.md`. Aplicar
   escritura-humana sin levantar el pie. Adapta las secciones técnicas
   (§5-§8 y §12) al tema; el resto del esqueleto es fijo.
7. **Validar** contra `reference/checklist-validacion.md`. Bloque J
   (escritura-humana) primero — si falla ahí, no entregues.
8. **Enlazar índices** (README del ejemplo + README del módulo).
9. **No commit ni push.** El usuario dice "sube".

## Reglas duras (no negociables)

Salieron de iterar S5.1/S5.2 con el usuario. Hay dos grupos: las
estructurales del MANUAL y las heredadas de `escritura-humana`.

### Estructurales del MANUAL

- **Fecha y hora de creación.** Cada MANUAL.md lleva en la cabecera, en
  cursiva tras el título y el bloque meta-doc, una línea
  `*Creado: YYYY-MM-DD HH:MM ±ZZZZ*` con la fecha y hora reales (zona
  horaria local). Captúrala con `date "+%Y-%m-%d %H:%M %z"` antes de
  escribir el manual; no la inventes. Cuando edites un manual existente
  añade además `· Actualizado: YYYY-MM-DD HH:MM` detrás (no sustituye
  la fecha de creación).
- **Sin hard-wrap en prosa.** Cada párrafo (también dentro de un
  blockquote `>`) va en **una sola línea** del fuente, sin saltos
  intermedios. Los saltos visuales los pone el editor o el preview.
  Se separan párrafos solo con una línea en blanco. Excepciones que
  *sí* llevan salto por línea propia: filas de tabla, ítems de lista,
  contenido dentro de ``` ``` ``` ```, headings.
- **Headings en texto plano.** Cero cursiva, código o enlaces dentro de
  `#`/`##`/`###`. El preview rompe el anclaje y escupe
  `{#… data-source-line}`. Si lleva código, va en el cuerpo.
- **README + MANUAL, no MANUAL ≈ README.** Donde el README ya lo cubre,
  enlaza con ruta relativa; no copies.
- **Cita slides** del submódulo de teoría como respaldo
  (`Slide N` o `Slides N-M`).
- **Honestidad didáctica.** Si algo NO está (un servicio sin endpoint,
  sin capa de integración), explica el porqué. Es contenido, no excusa.
- **Decisiones en tabla.** Servicio vs primo caro, coste, troubleshooting:
  tabla. Lo pedagógico va en prosa de formador (escritura-humana).
- **Autoevaluación con respuestas plegadas** en `<details><summary>`.
- **Datos técnicos verificados** (puerto, SDK, conteo de tests…)
  cazados del filesystem, no de tu cabeza.
- **TFM `net10.0`** y reglas del repo: no propongas `dotnet run` como
  verificación automática (el alumno lanza la app; lo automatizado es
  build + test).

### De escritura-humana (estas las cazas tras escribir)

- **§1 no abre con callout-tesis-cápsula.** Empieza por una afirmación
  con voz de formador o una mini-historia. Lo de "callout `>` con la
  tesis del submódulo en una frase" del antiguo template es exactamente
  el antipatrón 14 (frase-cápsula como entrega por defecto). Prohibido.
- **Cero "Imagina que…", "En este capítulo veremos…", "Quédate con
  esta imagen…", "Tres cosas que entender, no solo copiar:", "Lo
  importante de esa frase es:", "Si tengo que resumir esto en una
  idea:", "Aquí está el malentendido más común…", "Vamos al lío:"**.
  Anuncios de transición y etiquetas-de-sección disfrazadas: empieza
  por la afirmación, no por anunciarla.
- **Sin tripletes simétricos.** Si te sale "tres cosas a entender" con
  tres bullets de la misma longitud, rómpelo: dos cosas y un matiz, o
  uno largo y otro corto y un aparte. La asimetría es la firma del
  humano.
- **Sin frase-cápsula como entrega por defecto.** "No es A: es B.",
  "Esto es Y. Punto.", "La respuesta corta es X." sentenciados a cada
  párrafo. Una cápsula en su clímax está bien; una por sección es
  antipatrón 14. Desarrolla las ideas con su matiz, su porqué, su
  subordinada.
- **Aforismos en bucle: matar en seco.** Más de dos "no es X, es Y" en
  una página → desármalos. Tripletas encadenadas → afloja una. Guion
  largo `—` máximo ~12-15 en todo el documento (en inglés es endémico,
  al traducir mentalmente se cuela).
- **Cierres sin recap.** §15 cierra con un gancho al siguiente
  submódulo, una pregunta, una provocación o el matiz que se ganó la
  sección. **Nunca** "Si solo te llevas una frase…" ni "Hemos visto…".
- **Calcos del inglés cazados: cero.** Sin "con esteroides", "estado
  del arte", "modelos frontera", "abrir la caja (negra)", "throughline",
  "caso de uso", "casos de uso" (di "tarea", "sitio donde encaja"),
  "ideas metidas", "limpiar ideas", "X que aguanta toda la frase".
  Lista negra completa en `references/lista-negra.md` §8.
- **Sin intensificadores de relleno.** "radicalmente / absolutamente /
  completamente + adjetivo" → quítalo o ensúcialo según registro.
- **Sin notas de producción en la prosa.** "Esto es una slide propia",
  "criterio de éxito del bloque": va en las notas internas, jamás en lo
  que lee el alumno.
- **Tutear siempre.** "Puedes hacer X", no "Se puede realizar X".
- **Ritmo asimétrico.** Nunca tres párrafos seguidos de la misma
  longitud. Mezcla largos densos con cortos de tres frases y alguna
  suelta de una. La IA escribe como ladrillos; tú no.
- **Variación tonal a lo largo del texto.** Las secciones finales
  (§11-§15) tienden a robotizarse en textos largos. Reléelas con
  especial cuidado.
- **Test del café antes de entregar.** Frase a frase: ¿se lo dirías a
  un alumno tomándote un café? Si no, reescribe.

## Calibración por sección (no toda lleva el mismo tono)

| Sección | Contexto | Humor / historia / analogía |
| --- | --- | --- |
| §1 La idea | conceptual / opinión | apertura con afirmación o mini-historia; cero callout-tesis-cápsula |
| §2 El problema real | historia + tabla | **aquí va la historia del desastre o del alumno** que ancla el submódulo |
| §3 En tu stack | continuidad con módulos previos | tono medio; conecta sin "Como vimos…" |
| §4 Modelo mental | conceptual | **analogía vertebradora desarrollada en 3-5 párrafos** + diagrama |
| §5-§8 técnico | conceptual + opinión | mini-historias + ironía seca puntual; tabla de decisión por servicio |
| §9 Recorrido | técnico paralelo | tabla — humor bajo, claridad alta |
| §10 Por qué el código | decisión de diseño | mini-historia técnica que justifique alguna decisión no obvia |
| §11 Puesta en marcha | técnico puro | humor mínimo; tablas para requisitos/troubleshooting |
| §12 Checklist | técnico de producción | tono medio, cada casilla explicada con su porqué |
| §13 Ideas para llevarte | opinión | **prosa con opinión y matiz**, no cinco cápsulas numeradas |
| §14 Autoevaluación | preguntas reales | preguntas que activan, respuestas razonadas (no enciclopédicas) |
| §15 Cierre | gancho | sin recap; pregunta, gancho a SX.Y siguiente, o provocación |

## Capacidad: enlazar índices

Tras generar el MANUAL.md:

1. **README del ejemplo** (`examples/MXX-*/SY.Z-*/README.md`): añade
   cerca de la cabecera una línea tipo:
   `> 📘 Si es tu primera vez con este ejemplo, antes del README lee el
   [MANUAL.md](MANUAL.md) — manual del alumno: el para qué, el porqué
   y cómo ponerlo en marcha.`
2. **README del módulo** (`examples/MXX-*/README.md`): añade el enlace
   al MANUAL del submódulo sin alterar el resto.
3. No toques `examples/README.md`, `doc/**`, `.gitattributes` ni nada
   fuera del ejemplo y el módulo objetivo (los modifica el otro chat).

## Capacidad: validación

Aplica `reference/checklist-validacion.md` al manual. Reporta bloque por
bloque qué cumple y qué no. **Si falla el bloque J (escritura-humana),
no entregues** — reescribe la sección y vuelve a pasar.

## Capacidad: plantilla de referencia

`reference/plantilla-manual.md` es el esqueleto. El **ejemplo canónico
vivo** es `examples/M05-Almacenamiento-BBDD/S5.1-azure-storage/MANUAL.md`:
ante cualquier duda de tono o profundidad, calca ese.

## Cómo se mide "done"

Manual generado · cumple el checklist completo (incluido bloque J) ·
datos técnicos verificados contra el repo · enlazado en los índices ·
grep de lista-negra limpio · test del café superado · sin commit/push ·
tono y profundidad ≈ S5.1 reescrito.
