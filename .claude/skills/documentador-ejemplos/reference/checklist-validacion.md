# Checklist de validación del MANUAL.md

Aplica este checklist antes de entregar. Marca **OK / FALTA / NO APLICA**
por casilla y corrige cualquier FALTA. No entregues con FALTAs.

## A. Reglas duras de formato

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
