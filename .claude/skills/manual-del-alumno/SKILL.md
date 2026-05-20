---
name: manual-del-alumno
description: >-
  Genera el MANUAL.md (manual del alumno) de un ejemplo de cualquier curso
  técnico: documento pedagógico que complementa al README.md técnico ya
  existente, explicando el "para qué" y el "por qué" detrás del código.
  Úsalo cuando el usuario pida "documenta este ejemplo", "manual del
  alumno", "crear manual didáctico", "explica el ejemplo en formato
  manual" o cuando tenga un README.md técnico y necesite la versión
  pedagógica complementaria. Aplicable a cualquier dominio (cloud,
  backend, frontend, ML, DevOps, seguridad...) siempre que haya código
  fuente y un README técnico de referencia. Cubre cuatro capacidades:
  redactar el manual completo con plantilla de 12 secciones, validar
  contra checklist con grep anti-muletillas, enlazar el manual desde el
  README del ejemplo, y aplicar SIEMPRE el skill `escritura-humana`
  antes de redactar prosa en español.
---

# Manual del alumno — skill genérico

Este skill destila el proceso, la estructura, el tono y las reglas duras
de un patrón validado en 56 manuales pedagógicos de un curso técnico
real (F-003-Azure). El patrón es genérico — funciona para cualquier
ejemplo de cualquier curso técnico con código fuente y README ya
escrito.

## Qué hace este skill

Convierte un ejemplo de curso que tiene un `README.md` técnico (estructura,
comandos, despliegue, mapeo a slides o lección) en un **manual del alumno**
(`MANUAL.md` por convención) que explica:

- Qué pretende enseñar el ejemplo (la idea en una frase).
- El problema real que justifica que ese ejemplo exista.
- Por qué importa en el stack del alumno.
- Una **analogía vertebradora** (modelo mental concreto: hotel,
  fábrica, hospital, coche, teatro, mercado...) que recorre el manual.
- Recorrido por las piezas de código con el porqué de cada decisión.
- Conversaciones críticas con el equipo o seguridad que el código deja
  abiertas.
- Cómo probarlo en local y verificarlo.
- Anti-patterns y trampas que el ejemplo deja documentadas.
- Glosario breve.
- Cierre con gancho al siguiente paso.

El skill **no** sustituye al README técnico. Lo complementa. El README
sigue siendo la ficha de referencia (comandos, parámetros, mapeo
exacto); el MANUAL es el documento que el alumno lee antes de tocar
código.

## Paso 0 — escritura-humana (obligatorio antes de redactar)

Si el proyecto tiene el skill `escritura-humana` instalado (proyecto o
global), **cárgalo antes de escribir una sola línea de prosa**. Si no
lo tiene, aplica al menos las reglas duras de la sección "Reglas duras"
de este skill — son las mismas que están consolidadas en
`escritura-humana`.

Sin la disciplina de escritura-humana, el manual sale con calcos del
inglés, tripletes simétricos, frase-cápsula como entrega por defecto,
jerga de plantilla y cero voz de formador. Resultado: documentación
que rellena espacio en lugar de enseñar.

## Inputs que necesita el skill

1. **Ruta al ejemplo** (un directorio con código fuente).
2. **README.md** del ejemplo (técnico, ya existente).
3. **Código fuente** del ejemplo (clases principales, entry point,
   tests, scripts).
4. **Opcional**: documento de teoría del curso (lección, slides, capítulo
   del libro) si existe — útil para citar slides o referencias en el
   manual.
5. **Opcional**: ejemplo canónico del mismo curso si ya hay manuales
   redactados — calibra tono y profundidad replicando el canónico.

Si el ejemplo no tiene README, **negocia primero qué información hace
falta** antes de redactar; no inventes.

## Proceso paso a paso

1. **Paso 0**: carga `escritura-humana` si está disponible.
2. **Leer el README** del ejemplo entero: te dice qué NO repetir.
3. **Leer el código fuente** principal (entry point, clases puras,
   tests). Datos técnicos del manual deben verificarse contra el repo,
   nunca suponerse: puerto exacto, conteo de tests, versión del runtime,
   nombre de los archivos.
4. **Leer el documento de teoría** (si existe) por tramos. De ahí salen
   las citas a slides o lecciones, los números (coste, límites), las
   decisiones documentadas.
5. **Identificar el "para qué real"**: ¿qué decisión silenciosa entrena
   este ejemplo? ¿Qué problema operativo justifica que el ejemplo exista?
6. **Elegir la analogía vertebradora**. Lee `references/analogias-ejemplo.md`
   para inspirarte con catálogo. La analogía debe ser **concreta y
   cotidiana** (hotel, edificio, fábrica, coche, hospital, restaurante,
   teatro, mercado, oficina, mudanza), no abstracta. Y debe **mantenerse
   a lo largo del documento**: aparece en §4 desarrollada, se referencia
   en §5-§8 cuando ayuda a entender una pieza concreta.
7. **Redactar siguiendo** `references/plantilla.md`. Aplica escritura-humana
   sin levantar el pie. Adapta las secciones técnicas al tema concreto;
   el esqueleto de 12 secciones es fijo.
8. **Validar contra** `references/checklist.md`. Si falla el bloque del
   skill escritura-humana, reescribe la sección antes de entregar.
9. **Enlazar el MANUAL** desde el README del ejemplo con un callout 📘.
10. **No hacer commit ni push.** El usuario decide cuándo subir.

## Estructura canónica de un manual

Lee `references/plantilla.md` para el detalle. Resumen:

1. **Cabecera**: título + meta-data + fecha de creación + tiempo de
   lectura aproximado + referencia al README técnico.
2. **§1 La idea en una frase** — sin callout-tesis-cápsula; afirmación
   directa o mini-anclaje.
3. **§2 El problema real que hay detrás** — historia/historia(s) real(es)
   o plausibles que justifican la existencia del ejemplo.
4. **§3 Por qué esto importa en tu stack** — el "para qué" del alumno
   con preguntas concretas.
5. **§4 La analogía vertebradora** — modelo mental concreto desarrollado
   en 3-5 párrafos.
6. **§5-§7 Recorrido por el código** — las piezas principales con el
   porqué de cada decisión.
7. **§8-§9 Conversaciones críticas** — conversaciones con el equipo o
   seguridad, decisiones operativas que el ejemplo deja abiertas.
8. **§10 Cómo probarlo en local** — endpoints, tests, scripts, con
   ejemplos concretos verificables.
9. **§11 Anti-patterns** — trampas y prácticas a evitar (con números si
   los hay).
10. **§12 Glosario breve** — términos del dominio que aparecen en el
    manual con definición de una línea.
11. **§13 Cierre** — gancho al siguiente ejemplo o submódulo, sin recap.

Las secciones §5-§9 son las que más se adaptan al tema concreto. El
resto del esqueleto es fijo.

## Reglas duras (no negociables)

### Estructurales del MANUAL

- **Fecha de creación.** Cabecera del manual lleva
  `*Creado: YYYY-MM-DD HH:MM ±ZZZZ*` con la hora real (zona local).
  Captúrala con `date "+%Y-%m-%d %H:%M %z"` antes de escribir.
- **Sin hard-wrap en prosa.** Cada párrafo va en **una sola línea** del
  fuente. Los saltos visuales los pone el editor o el preview. Excepciones
  que sí van por línea: filas de tabla, ítems de lista, contenido en
  bloques de código, headings.
- **Headings en texto plano.** Cero cursiva, código o enlaces dentro de
  `#`/`##`/`###`. Rompe el anclaje del preview en algunas
  herramientas.
- **README + MANUAL, no MANUAL ≈ README.** Donde el README ya cubra,
  enlaza con ruta relativa; no copies.
- **Citar slides/lecciones** del documento de teoría como respaldo si
  existe (`Slide N`, `lección Y`, `capítulo X.Z`).
- **Honestidad didáctica.** Si algo NO está (un componente sin tests
  de integración, una capa que falta a propósito), explica el porqué.
  Es contenido, no excusa.
- **Decisiones en tabla.** Comparativas (servicio A vs B vs C, coste,
  troubleshooting): tabla. Lo pedagógico va en prosa de formador.
- **Datos técnicos verificados** contra el filesystem: puerto, versión
  de runtime, conteo de tests, comandos exactos. Jamás supuestos.

### De escritura-humana

- **§1 no abre con callout-tesis-cápsula.** Empieza por una afirmación
  con voz de formador o una mini-historia. Frase-cápsula como apertura
  es el antipatrón clásico.
- **Cero "Imagina que…", "En este capítulo veremos…", "Quédate con esta
  imagen…", "Tres cosas que entender, no solo copiar:", "Lo importante
  de esa frase es:", "Si tengo que resumir esto en una idea:", "Aquí
  está el malentendido más común…", "Vamos al lío:"**. Anuncios de
  transición y etiquetas-de-sección disfrazadas. Empieza por la
  afirmación, no por anunciarla.
- **Sin tripletes simétricos.** Si te sale "tres cosas a entender" con
  tres bullets de la misma longitud, rómpelo: dos cosas y un matiz, o
  uno largo y otro corto y un aparte. La asimetría es la firma del
  humano.
- **Sin frase-cápsula como entrega por defecto.** "No es A: es B.",
  "Esto es Y. Punto.", "La respuesta corta es X." sentenciados a cada
  párrafo. Una cápsula en su clímax está bien; una por sección es
  antipatrón.
- **Aforismos en bucle: matar en seco.** Más de dos "no es X, es Y" en
  una página → desármalos. Tripletas encadenadas → afloja una. Guión
  largo `—` máximo ~12-15 en todo el documento.
- **Cierres sin recap.** §13 cierra con un gancho al siguiente paso,
  una pregunta, una provocación o el matiz que se ganó la sección.
  **Nunca** "Si solo te llevas una frase…" ni "Hemos visto…".
- **Calcos del inglés cazados: cero.** Lista negra en
  `references/lista-negra.md`.
- **Sin intensificadores de relleno.** "radicalmente / absolutamente /
  completamente + adjetivo" → quítalo o ensúcialo según registro.
- **Tutear siempre.** "Puedes hacer X", no "Se puede realizar X".
- **Ritmo asimétrico.** Nunca tres párrafos seguidos de la misma
  longitud. Mezcla largos densos con cortos de tres frases y alguna
  suelta de una.
- **Test del café antes de entregar.** Frase a frase: ¿se lo dirías a
  un alumno tomándote un café? Si no, reescribe.

## Calibración por sección

| Sección | Tono | Humor / historia / analogía |
| --- | --- | --- |
| §1 La idea | afirmación con voz de formador | apertura directa o mini-historia; cero callout-tesis |
| §2 Problema real | historia + tabla | **aquí va la historia del desastre o del alumno** que ancla el tema |
| §3 En tu stack | continuidad con el alumno | tono medio; conecta sin "Como vimos…" |
| §4 Analogía vertebradora | conceptual | **analogía desarrollada en 3-5 párrafos** |
| §5-§7 Recorrido | técnico paralelo | tablas + mini-historias técnicas; ironía seca puntual |
| §8-§9 Conversaciones críticas | opinión | prosa de formador con matices |
| §10 Puesta en marcha | técnico puro | tablas, comandos exactos; humor mínimo |
| §11 Anti-patterns | numerado | cada uno con su porqué operativo |
| §12 Glosario | conciso | una línea por término |
| §13 Cierre | gancho | sin recap; pregunta o link al siguiente paso |

## Capacidad: enlazar el manual desde el README del ejemplo

Tras generar el manual:

1. Añade cerca de la cabecera del README del ejemplo una línea como:
   `> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) — manual del alumno: <una frase con la analogía y los 2-3 conceptos clave>.`
2. Si hay un README de módulo/sección/capítulo agregado, añade el manual
   a la lista de manuales del nivel superior (formato: lista de enlaces
   o tabla, según convención del repo).
3. **No modifiques** archivos fuera del ejemplo y su agregador inmediato.

## Capacidad: validación con checklist

Aplica `references/checklist.md` al manual antes de entregar. Bloque
por bloque, reporta qué cumple y qué no. **Si falla el bloque de
escritura-humana, no entregues** — reescribe y vuelve a pasar.

## Capacidad: plantilla y catálogo de analogías

- `references/plantilla.md` — el esqueleto de 13 secciones con guía de
  qué va en cada una.
- `references/analogias-ejemplo.md` — catálogo de analogías
  vertebradoras que han funcionado en 56 manuales reales (hotel,
  edificio, fábrica, hospital, coche, teatro, mercado, oficina,
  mudanza, monitor de UCI, almacén y camiones, plano del arquitecto,
  director de orquesta, restaurante, etcétera). Útil para no quedarte
  bloqueado eligiendo la imagen mental.

## Cómo se mide "done"

Manual generado · cumple el checklist completo (incluido el bloque de
escritura-humana) · datos técnicos verificados contra el repo · analogía
vertebradora desarrollada y consistente · enlazado en el README del
ejemplo · grep de lista-negra limpio · test del café superado · sin
commit/push (el usuario decide).

## Cuándo NO usar este skill

- El usuario quiere un README técnico (ficha de referencia), no un
  manual pedagógico. Para eso usa la convención normal del proyecto.
- El ejemplo es trivial (un Hello World, una utility pequeña sin
  decisiones de diseño). Un manual de 12 secciones sobre 10 líneas de
  código es ruido. En esos casos, basta con un README claro.
- El usuario pide documentación de API (Swagger, OpenAPI, JSDoc).
  Eso es otro género; este skill es para manuales pedagógicos.
- No hay código que documentar. El skill necesita un README + código
  fuente real como entrada.
