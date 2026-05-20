# Checklist de validación del MANUAL.md

Aplica este checklist antes de entregar. Marca **OK / FALTA / NO APLICA**
por casilla y corrige cualquier FALTA. No entregues con FALTAs.

## A. Reglas duras de formato

- [ ] **Línea de fecha de creación** presente en la cabecera, formato
      `*Creado: YYYY-MM-DD HH:MM ±ZZZZ*`, capturada con `date` real
      (no inventada). En reescrituras, `· Actualizado: …` añadido sin
      borrar el `Creado:`.
- [ ] **Headings en texto plano** — ningún `#`/`##`/`###` contiene
      cursiva (`_x_`/`*x*`), código (`` `x` ``) ni enlaces. Si lo lleva,
      muévelo al cuerpo.
- [ ] Numeración de secciones consistente (`## 1.`, `## 2.`, …) sin
      saltos ni duplicados.
- [ ] **Idioma español** consistente (terminología del curso).
- [ ] Callouts usados con criterio (🧠/🎓/💡/⚠️), no más de un par por
      sección.
- [ ] Tablas para decisiones, comparativas y troubleshooting (no
      párrafos largos donde una tabla aclara).

## B. Separación README / MANUAL

- [ ] El MANUAL **no duplica** la estructura, mapeo de slides, ni los
      pasos del Portal del README.
- [ ] Donde el README ya lo cubre (despliegue, scripts `az`, mapeo
      completo de slides), el MANUAL **enlaza** a `README.md` en vez de
      copiar.
- [ ] La cabecera del MANUAL deja claro qué es y qué no es respecto al
      README.

## C. Contenido pedagógico (el "para qué")

- [ ] §1 tiene la **tesis del submódulo en una frase** (callout `>`).
- [ ] §2 plantea un **escenario real concreto** con tabla
      necesidad → servicio/opción → porqué.
- [ ] §3 conecta con módulos anteriores / cambios de stack si los hay.
- [ ] §4 da un **modelo mental** (diagrama o analogía) memorable.
- [ ] Cada decisión técnica se justifica con una **regla de elección**
      (no solo "esto se hace así") y se vincula a coste / escala /
      riesgo cuando aplica.

## D. Contenido técnico (el "cómo se ve")

- [ ] §5 (y §6–§8 si aplican) explica el SDK / arquitectura con
      **bloques de código reales del ejemplo**, no inventados.
- [ ] Cada bloque de código cita la ruta del archivo del ejemplo
      (`src/.../X.cs`).
- [ ] Se citan **slides** de la teoría como respaldo
      (`Slide N` o `Slides N-M`).
- [ ] **Honestidad didáctica**: si algo NO existe (endpoint, capa de
      tests, emulación), el manual explica **por qué**, no lo oculta.

## E. Sección §11 (puesta en marcha y pruebas)

- [ ] **11.1 Requisitos** en tabla, con SDK verificado contra
      `global.json`.
- [ ] **11.2 Compilar** con `dotnet build <slnx>` y recordatorio de
      `TreatWarningsAsErrors`.
- [ ] **11.3 Arrancar dependencias** (npm + Docker como alternativas) y
      qué NO emula el emulador.
- [ ] **11.4 Lanzar app** con **puerto exacto** de
      `Properties/launchSettings.json` (verificado) y prueba de vida.
- [ ] **11.5 Ejercitar** con `api.http` + equivalente `curl`.
- [ ] **11.6 Tests** con **tabla "sin Docker / con Docker"** y conteo
      exacto del README; deja claro que un *skip* es diseño.
- [ ] **11.7 Troubleshooting** en tabla síntoma → causa → solución.
- [ ] **11.8 Contra recurso real** opcional, remitiendo al README para
      Portal/`az`.
- [ ] **No** propone `dotnet run` como verificación automática del curso.

## F. Datos técnicos verificados (no inventados)

- [ ] Puerto HTTP del manual = `applicationUrl` de
      `Properties/launchSettings.json`.
- [ ] Versión SDK = `global.json`.
- [ ] Conteo de tests (pass/skip) = el del README (y/o `dotnet test`).
- [ ] Nombres de proyecto, `.slnx`, paquetes, clases y endpoints
      citados existen tal cual en el código.
- [ ] Connection string / claves de configuración (`StorageConnection`,
      `SqlConnection`…) = las que usa `appsettings.Development.json` /
      `Program.cs`.

## G. Recorrido, ideas y autoevaluación

- [ ] §9 es una **tabla guiada** (petición → respuesta → qué demuestra)
      con al menos 1 experimento "descubre algo".
- [ ] §13 lista **4–6 ideas-clave** con una frase fuerte cada una.
- [ ] §14 tiene **5–8 preguntas** con `*(§N)*` apuntando a la sección.
- [ ] Respuestas en `<details><summary>Respuestas</summary>…</details>`,
      **razonadas** (no solo el resultado).

## H. Enlaces e índices

- [ ] Enlaces internos (`README.md`, archivos `src/.../X.cs`, teoría
      `../../../doc/...`) usan **rutas relativas** y resuelven.
- [ ] Enlace al MANUAL añadido en el **README del ejemplo** (cabecera).
- [ ] Enlace al MANUAL añadido en el **README del módulo**
      (`examples/MXX-*/README.md`) si la tabla lo admite, sin alterar el
      resto.
- [ ] **No se ha tocado** `examples/README.md`, `doc/**`,
      `.gitattributes` ni nada fuera del ejemplo/módulo objetivo.

## I. Higiene de proceso

- [ ] **Sin `git commit` ni `git push`** (regla del repo: el usuario
      dice "sube").
- [ ] No se han ejecutado apps (`dotnet run`/`func start`/`npm run`).
- [ ] El manual nuevo (y los enlaces añadidos) no tienen *junk*
      (BOMs, CRLF si el repo es LF — `.gitattributes` fija `eol=lf`).

## J. escritura-humana (OBLIGATORIO — bloquea entrega si falla)

> Si falla cualquier casilla de este bloque, **no entregues**: reescribe
> la sección afectada y vuelve a pasar. Es la diferencia entre
> "documentación que enseña" y "documentación que rellena espacio".

### J.1 Aperturas y cierres

- [ ] §1 **no abre** con callout-tesis-cápsula. Abre con afirmación de
      voz de formador o mini-historia.
- [ ] §2 abre con una **historia real o plausible** (desastre, alumno,
      descubrimiento, antes/después) que ancla el submódulo.
- [ ] Cero aperturas con "Imagina que…", "En este capítulo veremos…",
      "Quédate con esta imagen…", "Aquí está la idea clave…",
      "Si tengo que resumir esto en una idea:", "Vamos al lío:".
- [ ] §15 cierra con **gancho** al siguiente submódulo / pregunta /
      provocación. **Sin** "Si solo te llevas una frase…", "Hemos visto…",
      "En conclusión…".

### J.2 Estructura y ritmo

- [ ] Cero **tripletes simétricos** ("Tres cosas a entender" con tres
      bullets de longitud similar). Asimetría deliberada: dos cosas y
      un matiz, uno largo y otro corto y un aparte.
- [ ] Cero **frase-cápsula como entrega por defecto** ("No es A: es B.",
      "Esto es Y. Punto.", "La respuesta corta es X."). Una cápsula en
      su clímax sí; una por sección no.
- [ ] Cero **aforismos en bucle**: más de dos "no es X, es Y" por
      página → desármalos.
- [ ] **Guion largo `—`** controlado: máximo ~12-15 en todo el documento.
      Sustituye la mitad por `:`, `.` o coma.
- [ ] **Párrafos asimétricos**: nunca 3 párrafos seguidos de la misma
      longitud. Mezcla largo denso + corto + frase suelta.
- [ ] **Negritas con moderación** y sin abuso decorativo en cada bullet.

### J.3 Voz y storytelling

- [ ] **Tutear** en todo el manual.
- [ ] **3-6 historias** distribuidas (al menos una "del desastre" o
      "del alumno" en §1-§2 que ancle el tema).
- [ ] **1 analogía vertebradora desarrollada** (≥3 párrafos con detalle
      sensorial) en §4 + retorno a esa analogía en otra sección.
- [ ] **3-5 analogías** totales (la vertebradora + puntuales).
- [ ] **6-10 toques de humor** distribuidos *asimétricamente* (ironía
      seca, autocrítica, understatement, hipérbole calculada). No uno
      por sección como un metrónomo.
- [ ] **Opinión con matiz** en cada decisión importante ("Yo iría con
      X. Aunque si tu caso es Y, Z puede ser mejor.").

### J.4 Calcos, jerga y muletillas — grep obligatorio

Pasa `grep -niE` contra el archivo con estos patrones. Si alguno
aparece, reescribe:

```
con esteroides|estado del arte|modelos frontera
abrir la caja|abrimos la caja|throughline
ideas metidas|limpiar (las )?ideas|aguanta toda la frase
caso de uso|casos de uso|en cristiano
gotcha|gotchas
Tres cosas (a |que )entender
Imagina que|Quédate con|Vamos al lío
Como hemos visto|En conclusión|En resumen|En definitiva
Sin lugar a dudas|Indudablemente|Resulta evidente
revolucionario|innovador|disruptivo|holístico|integral
impulsar|fomentar|potenciar|orquestar|catalizar
es importante destacar|cabe mencionar|en este sentido
radicalmente|absolutamente|completamente (distinto|nuevo|clave)
La respuesta corta es|Versión corta|Spoiler:
deck|paper|deadline|target|insight
```

(`slides` se permite cuando se refiere a la presentación del submódulo del propio curso, p. ej. *"Slide 5"*; en cambio "diapositiva" en prosa general también vale.)

- [ ] grep de lista-negra **vacío** (sin matches en prosa; los términos
      técnicos consolidados en inglés —deploy, commit, endpoint…— se
      mantienen, igual los nombres de archivo o herramientas).

### J.5 Test del café (relectura final)

- [ ] Cada apertura y cada cierre de sección: ¿se lo dirías a un alumno
      tomando un café después de clase? Si no, reescribe.
- [ ] Las secciones finales (§11-§15) **no son** más robóticas que las
      primeras (en textos largos la IA se degrada hacia el final;
      revísalas con especial cuidado).
- [ ] Si tuvieras que distinguir esto de algo escrito por un formador
      humano experimentado, ¿podrías?
