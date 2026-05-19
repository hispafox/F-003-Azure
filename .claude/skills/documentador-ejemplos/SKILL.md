---
name: documentador-ejemplos
description: >-
  Genera el MANUAL.md (manual del alumno) de un ejemplo del curso
  F-003-Azure: un documento pedagógico + técnico + de puesta en marcha,
  separado y complementario del README.md técnico que ya existe en cada
  ejemplo. Úsalo cuando el usuario pida documentar un ejemplo, "crear el
  manual de SX.Y / MXX", "documenta este ejemplo", "manual del alumno",
  o generar/validar un MANUAL.md en examples/MXX-*/SY.Z-*/. Cubre cuatro
  capacidades: generar el manual, validarlo contra checklist, enlazarlo
  en los índices y calcar el ejemplo canónico de referencia (S5.1).
---

# Documentador de ejemplos — F-003-Azure

Este skill destila el proceso, la estructura, el tono y las reglas duras
con que se construyó el manual canónico de referencia:
`examples/M05-Almacenamiento-BBDD/S5.1-azure-storage/MANUAL.md`. **Léelo
entero antes de escribir nada**: es la vara de medir de tono y
profundidad. Las reglas de aquí salieron de iterar ese manual con el
usuario; respétalas.

## Qué es y qué NO es un MANUAL.md

Cada ejemplo del curso ya tiene un `README.md` **técnico de referencia**
(estructura de carpetas, mapeo a slides, comandos de test, despliegue por
Portal, "cuándo usar"). El `MANUAL.md` es **otro documento, no un
sustituto ni un resumen**:

| README.md (ya existe — NO se toca) | MANUAL.md (lo que generas) |
| --- | --- |
| Ficha técnica de referencia | Manual del alumno: el **para qué** y el **porqué** |
| Para quien ya lee el código | Para el alumno que quiere entender antes de leer código |
| Enumera *qué hay* | Explica *por qué está y qué hay que entender* |
| Estructura, slides, deploy | Escenario real, decisiones, SDK explicado, puesta en marcha guiada, autoevaluación |

Regla de oro: **el MANUAL no duplica el README**. Cuando necesite lo
operativo de referencia (despliegue Portal, scripts `az`, mapeo completo
de slides), **enlaza al README** en vez de copiarlo. Sí repite lo justo
para que el manual se lea solo (un bloque mínimo de arranque), pero el
detalle vive en el README.

## Inputs

El usuario indica un ejemplo: `MXX-SY.Z` (p. ej. `M06-S6.2`) o una ruta
`examples/MXX-*/SY.Z-*/`. Si es ambiguo, pídelo. Nunca documentes "todos"
de golpe sin que lo pida explícitamente: un manual a la vez, y enseña el
primero para validar tono antes de seguir (así se hizo con S5.1).

## Proceso (síguelo en orden)

1. **Leer la teoría del submódulo.** `doc/MXX-*/v*-actual/MXX-SY.Z-*.md`
   (1000–1800 líneas → léelo por tramos). De ahí salen: el **escenario /
   problema real**, las **decisiones** que el submódulo entrena, los
   números (coste, límites), y los **slides** que citarás como soporte.
2. **Leer el README.md del ejemplo.** Para saber qué NO repetir y a qué
   enlazar.
3. **Leer el código fuente clave** (no inventes nada técnico):
   - `Program.cs` (DI, bifurcaciones de config/seguridad),
   - `Endpoints/` o `[Function]`s (qué expone, status codes),
   - clases puras (`*Policy`/`*Advisor`/`*Path`/`*Inspector`…),
   - `Repositories/`, `Models/`, `api.http`,
   - `Properties/launchSettings.json` (puerto exacto),
   - `global.json` (versión SDK exacta), `*.csproj`/`.slnx` (nombre,
     paquetes), `appsettings.Development.json` (config local),
   - el README para el conteo de tests y el patrón de capas.
   Todo dato técnico del manual (puertos, comandos, conteo de tests,
   versión SDK) debe estar **verificado contra el repo**, no supuesto.
4. **Identificar el "para qué real".** Antes de redactar, escribe en una
   frase: ¿qué problema real resuelve? ¿qué decisión silenciosa entrena
   (elegir servicio, keyless vs secreto, ACID vs NoSQL, FIFO vs barato…)?
   Ese es el eje del manual; lo demás lo sostiene.
5. **Redactar** siguiendo `reference/plantilla-manual.md`. Adapta las
   secciones técnicas (§5–§8 y §12) al tema del submódulo; el resto del
   esqueleto es fijo.
6. **Validar** contra `reference/checklist-validacion.md`. No entregues
   un manual que falle una casilla.
7. **Enlazar índices** (ver más abajo).
8. **No hacer commit ni push.** Igual que el resto del repo: el usuario
   dice "sube". Entrega el manual y el resumen de qué se generó.

## Reglas duras (no negociables — salieron de iterar S5.1)

- **Headings en texto plano.** Nada de cursiva, `código` ni enlaces
  dentro de `#`/`##`/`###`. El preview rompe el anclaje y escupe
  `{#… data-source-line}`. Si el concepto lleva código, ponlo en el
  cuerpo, no en el título.
- **Pedagógico + técnico + operativo, los tres.** El manual explica el
  *para qué* (escenario, decisión), enseña *cómo se ve de verdad en el
  SDK* (con código real del ejemplo citado por ruta) y dice *cómo
  arrancarlo y probarlo* (sección técnica completa, §11).
- **No duplicar el README; enlazar.** Rutas relativas desde el MANUAL
  (convención del repo): `README.md`, `src/.../X.cs`,
  `../../../doc/MXX-*/v*-actual/MXX-SY.Z-*.md`.
- **Citar slides** de la teoría como respaldo de cada decisión
  ("Slide 5", "Slides 7-8").
- **Honestidad didáctica.** Si algo NO está (un servicio sin endpoint,
  sin capa de integración), **explica el porqué** (p. ej. "Azurite no
  emula Files", "Entra ID no es emulable") en vez de fingir cobertura.
  Esto es contenido, no una disculpa.
- **Decisiones en tabla.** Servicio vs su "primo caro", coste,
  redundancia, troubleshooting → tablas comparativas.
- **Callouts con criterio:** 🧠 idea-clave / decisión que vale dinero ·
  🎓 por qué del diseño (del código o de los tests) · 💡 experimento o
  tip práctico · ⚠️ trampa. No abuses; uno cada pocas secciones.
- **Autoevaluación con respuestas plegadas** en `<details><summary>`,
  cada pregunta remite a su sección (`*(§N)*`).
- **Español** (regla del curso). Tono de instructor explicando, no doc
  seca ni marketing.
- **TFM net10.0** y reglas del repo: no proponer lanzar la app como
  verificación automática (eso lo hace el alumno); la verificación
  automatizada es build + test.

## Capacidad: enlazar índices (navegación de tres niveles)

Tras generar el `MANUAL.md`, sin romper nada existente:

1. **README del ejemplo** (`examples/MXX-*/SY.Z-*/README.md`): añade,
   cerca de la cabecera, una línea que enlace al manual, p. ej.:
   `> 📘 **¿Primera vez con este ejemplo?** Lee el [MANUAL.md](MANUAL.md) —
   manual del alumno: el para qué, el porqué y cómo ponerlo en marcha.`
2. **README del módulo** (`examples/MXX-*/README.md`): si la tabla de
   submódulos lo permite, añade un enlace `[manual]` junto al del
   ejemplo, sin alterar el resto de filas.
3. No toques `examples/README.md` ni `doc/**` ni `.gitattributes` (los
   gestiona el otro chat del usuario). Cambios mínimos y quirúrgicos.

## Capacidad: validación

Aplica `reference/checklist-validacion.md` al manual generado (o a uno
existente si el usuario pide "revisa/valida el manual de …"). Reporta
casilla por casilla qué cumple y qué no, y corrige antes de entregar.

## Capacidad: plantilla de referencia

`reference/plantilla-manual.md` es el esqueleto y la guía sección por
sección. El **ejemplo canónico** vivo es
`examples/M05-Almacenamiento-BBDD/S5.1-azure-storage/MANUAL.md`: ante
cualquier duda de tono o profundidad, calca ese, no inventes un estilo
nuevo.

## Cómo se mide "done"

Manual generado · pasa el checklist entero · datos técnicos verificados
contra el repo · enlazado en los índices del ejemplo y del módulo · sin
commit/push (espera "sube") · tono y profundidad ≈ S5.1.
