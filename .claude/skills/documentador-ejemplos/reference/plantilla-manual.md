# Plantilla de MANUAL.md — guía sección por sección

Esqueleto fijo. Las secciones marcadas **[ADAPTAR]** cambian según el tema del submódulo; el resto es estructura estable. Ejemplo vivo de referencia: `examples/M05-Almacenamiento-BBDD/S5.1-azure-storage/MANUAL.md`. Numera las secciones con `## N. Título` (texto plano, sin markup).

**Convención de formato del fuente.** Cada párrafo de prosa va en **una sola línea**, sin saltos intermedios. Los saltos visuales los pone el editor/preview. Solo van en su propia línea las filas de tabla, los ítems de lista, los headings, las líneas de bloque de código y los `>` que abren un blockquote (la prosa dentro del blockquote también va en una línea continua, precedida por `> ` solo al inicio del párrafo).

---

## Cabecera (antes de §1)

- `# Manual del alumno — SY.Z · <Tema>`
- Un párrafo (sin callout-tesis-cápsula) que explique **qué es este
  documento** y qué no: el README sigue siendo la ficha técnica; el
  manual va antes y explica el para qué, el porqué y la puesta en
  marcha.
- Línea de meta en prosa o lista compacta: tiempo de lectura · enlace a
  la teoría (`../../../doc/MXX-*/v*-actual/MXX-SY.Z-*.md`, con nº de
  slides) · cómo leerlo (qué secciones son marco mental, técnicas o
  práctica).
- **Línea de fecha de creación** en cursiva, justo bajo el bloque
  meta-doc:
  `*Creado: YYYY-MM-DD HH:MM ±ZZZZ*`
  capturada con `date "+%Y-%m-%d %H:%M %z"`. Si reescribes el manual,
  añade detrás `· Actualizado: YYYY-MM-DD HH:MM` sin borrar el `Creado`.

## §1. La idea en una frase

Un callout `>` con la tesis del submódulo en una frase. Debajo, 1–2
párrafos: por qué la decisión que entrena importa (coste/escala/riesgo),
no la mecánica del SDK.

## §2. El problema real que hay detrás

Escenario concreto y recurrente del curso (la "app de ventas" en M05, o
el que aplique). **Tabla**: necesidad real → ¿va en BD? → servicio/ópción
correcta → por qué. Cierra diciendo qué filas de la tabla son endpoints
reales del ejemplo.

## §3. Por qué esto importa en tu stack

Conecta con lo ya visto en módulos anteriores y con el cambio de stack si
lo hay (p. ej. "ya no es Function, es Minimal API; los tests vuelven a
WebApplicationFactory"). Cita Slide de contexto.

## §4. El modelo mental

Diagrama ASCII + analogía memorable. Qué decisiones se toman "una vez"
(de plataforma/cuenta) vs "por operación". Deja clara la pregunta que el
ejemplo entrena.

## §5. [ADAPTAR] El contenido técnico en profundidad

El corazón técnico. Una subsección por concepto/servicio. Para **cada**
uno:
- **Para qué sirve** (en el escenario de §2).
- **Cómo se ve de verdad en el SDK**: bloque de código mínimo y real,
  citando el archivo del ejemplo por ruta relativa.
- **Cuándo NO** usarlo.
- **Su "primo caro"** y la regla de decisión (callout 🧠).
Patrones de tema: 4-servicios (Blob/Table/Queue/File), relacional
(EF Core/migraciones/retry), NoSQL (partición/RU/consistencia), seguridad
transversal (no emulable → clases puras + DI), Functions (triggers/
bindings). Calca la profundidad de S5.1 §5.

## §6–§8. [ADAPTAR] Más profundidad por ejes del tema

Secciones técnicas adicionales según el submódulo. Ejes típicos: coste y
ciclo de vida (tiers/lifecycle), durabilidad y desastre (redundancia/soft
delete), seguridad (cómo te conectas: keys/SAS/Managed Identity y el `if`
de Program.cs), resiliencia (retry/transitorios), consistencia,
networking. Tabla siempre que sea una comparación o decisión. Cita slides.

## §9. Recorrido guiado

Tabla con columnas: `#` · petición (del `api.http`) · respuesta esperada
(status + cuerpo) · **qué demuestra**. Numerada en el orden del escenario.
Incluye 1–2 **experimentos** (callout 💡) donde el alumno *descubre* algo
(p. ej. "lanza 3 veces y mira el orden → Queue no es FIFO"). Señala el/los
endpoint(s) de lógica pura que no necesitan infra.

## §10. Por qué el código está organizado así

Explica las capas (lógica pura / repos o servicios / endpoints finos) y
**el porqué de los tests** (unit puro rápido; integración real con
emulador en `SkippableFact` que se salta sin Docker; o por qué NO hay
capa de integración si el tema no es emulable). Callout 🎓 con la decisión
de diseño no obvia (p. ej. por qué un repo no tiene endpoint).

## §11. Puesta en marcha, ejecución y pruebas

**Sección técnica y operativa**, imprescindible. Subsecciones:
- **11.1 Requisitos** — tabla: requisito · versión/cómo (verificado en
  `global.json` para el SDK) · para qué · ¿obligatorio?
- **11.2 Compilar** — `dotnet build <X>.slnx`; recordar 0 warnings por
  `TreatWarningsAsErrors`.
- **11.3 Arrancar dependencias** — emulador (Azurite/Cosmos/MsSql) por
  npm y por Docker; qué connection string lo conecta; qué NO emula.
- **11.4 Lanzar la app** — `dotnet run --project …`; **puerto exacto de
  `launchSettings.json`**; prueba de vida `/health`; recordatorio de que
  el alumno la lanza (el curso no).
- **11.5 Ejercitar** — `api.http` y equivalente `curl`; remite a §9 para
  el "qué mirar".
- **11.6 Tests** — `dotnet test`; **tabla "sin Docker / con Docker"** con
  el conteo exacto del README; explicar que un *skip* es diseño, no fallo.
- **11.7 Problemas frecuentes** — tabla síntoma · causa · solución.
- **11.8 Contra recurso real (opcional)** — Managed Identity / config;
  enlaza al README para Portal y scripts `az`, no los repitas.
Todos los valores (puerto, comandos, conteos, SDK) **verificados contra
el repo**.

## §12. [ADAPTAR/OPCIONAL] Checklist de producción explicado

Si la teoría trae checklist de producción: tabla casilla → **de qué te
protege**. No memorizar; entender. Omitir si el submódulo no lo tiene.

## §13. Las N ideas para llevarte

Lista numerada, 4–6 ideas, una frase fuerte cada una. La primera es la
pregunta-eje del submódulo.

## §14. Comprueba que lo has entendido

5–8 preguntas que obligan a aplicar, cada una con `*(§N)*`. Respuestas en
`<details><summary>Respuestas</summary> … </details>`, razonadas (no solo
la solución: el porqué).

## §15. Dónde encaja esto en el módulo

El arco del módulo en una frase. Enlaces a los submódulos vecinos
(anterior/siguiente) explicando qué decisión continúa cada uno. Cierra
con un callout `>`: "si solo te llevas una frase…".
